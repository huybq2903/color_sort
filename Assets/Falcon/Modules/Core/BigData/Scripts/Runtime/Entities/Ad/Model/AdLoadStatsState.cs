/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-09-23
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Kho gộp telemetry load — v2 (chốt với loader 23/09, thay hai event
    /// <c>f_sdk_ad_load_success</c>/<c>_fail</c> cũ).
    /// <br/>Khác bản cũ ở HÌNH DẠNG ĐẦU RA chứ không ở triết lý: vẫn gộp per (unit × bậc × giá sàn), nhưng
    /// thành-công và thất-bại nằm CHUNG một bản ghi, và cả nhóm unit của một
    /// (adType × mediation) ra MỘT dòng mang mảng <c>units[]</c>. Đo 22/09: bản cũ đẻ ~158
    /// dòng/user/ngày và <c>ad_load_success</c> nén được 1,02 lần/dòng (tức không nén gì) vì mỗi
    /// tổ hợp là một dòng; gộp mảng đưa về ~20 dòng/user/ngày.
    /// <br/><b>network/lastError KHÔNG làm khoá</b> — chúng là "bản cuối của cụm". Đưa vào khoá là
    /// message lạ tách cụm vô hạn, đúng cái bẫy đã tránh ở bản cũ.
    /// <br/><b>tier vắng ≠ tier 0</b> (điều kiện loader đặt): unit không đặt giá sàn thì
    /// <c>tier</c> để null và key biến mất khỏi payload — đó là thứ duy nhất phân biệt được
    /// "không đặt sàn" với "đặt sàn 0", và là căn cứ của chẩn đoán nhóm AdMob-không-đặt-floor.
    /// <br/><b>Khoá gộp chỉ chứa thứ RỜI RẠC</b> (loại ad × mediation × unit × <c>tier</c>).
    /// <c>floor</c> (giá sàn thật, USD) là SỐ ĐO nên nằm trong ruột bản ghi, cộng dồn rồi báo
    /// TRUNG BÌNH — cùng khuôn với <c>okMs</c>/<c>failMs</c>.
    /// <br/>1.4.1 sửa đúng chỗ này (owner bắt 23/09): 1.4.0 để <c>floor</c> trong khoá, mà AdMob
    /// tính <c>floor = revenue × hệ số × 1000</c> với <c>revenue</c> là giá của ad vừa load — số
    /// thực LIÊN TỤC, mỗi attempt một giá trị. Nhánh AdMob vì thế KHÔNG gộp được dòng nào (mất
    /// đúng cái hố volume mà v2 sinh ra để lấp), lại còn làm đầy trần 256 khoá nên cụm của MAX bị
    /// xả sớm theo. SDK không được sập chỉ vì caller truyền vào một số đo thay đổi liên tục — tự
    /// vệ ở đây, đừng trông vào hợp đồng.
    /// <br/>Pure class — không IO/DI/khoá; <see cref="AdLoadStatsService"/> lo thread-safety.
    /// </summary>
    public class AdLoadStatsState
    {
        /// <summary>Một unit đã gộp — thành một phần tử của mảng <c>units[]</c>.</summary>
        public class UnitStat
        {
            public AdType adType;
            public string adMediation;
            public string adUnitId;
            public double? tier;
            public int ok;
            public int fail;
            public long okMs;
            public long failMs;
            public string networkName;
            public string lastErrorMess;

            /// <summary>Tổng giá sàn thật của những lần CÓ giá — chia <see cref="floorSamples"/> ra trung bình.</summary>
            public double floorSum;

            /// <summary>Số lần có giá sàn thật (mediation không biết giá thì không tính vào mẫu).</summary>
            public int floorSamples;

            /// <summary>Giá sàn thật TRUNG BÌNH của bản ghi; null khi không lần nào có giá.</summary>
            public double? FloorAverage => floorSamples > 0 ? floorSum / floorSamples : (double?)null;
        }

        /// <summary>Một dòng log: nhóm unit cùng (adType × mediation), đã chia lô theo trần.</summary>
        public class Batch
        {
            public AdType adType;
            public string adMediation;
            public List<UnitStat> units = new();
        }

        private readonly Dictionary<(AdType type, string mediation, string unit, double? tier), UnitStat>
            _entries = new();

        public int Count => _entries.Count;

        public void Record(AdLoadResultParam param)
        {
            if (param == null) return;

            var key = (param.adType, param.adMediation, param.adUnitId, param.tier);
            if (!_entries.TryGetValue(key, out var entry))
                _entries[key] = entry = new UnitStat
                {
                    adType = param.adType,
                    adMediation = param.adMediation,
                    adUnitId = param.adUnitId,
                    tier = param.tier
                };

            if (param.floor.HasValue)
            {
                entry.floorSum += param.floor.Value;
                entry.floorSamples++;
            }

            if (param.success)
            {
                entry.ok++;
                entry.okMs += param.loadingMs;
                // network chỉ biết khi thắng; giữ bản cuối, không ghi đè bằng null của lần fail
                if (param.networkName != null) entry.networkName = param.networkName;
            }
            else
            {
                entry.fail++;
                entry.failMs += param.loadingMs;
                if (param.errorMess != null) entry.lastErrorMess = param.errorMess;
            }
        }

        /// <summary>
        /// Rút toàn bộ và làm sạch kho, chia thành các lô ≤ <paramref name="maxUnitsPerBatch"/>
        /// phần tử.
        /// <br/>Vượt trần thì CHIA LÔ chứ không cắt bỏ (chốt owner 23/09): mọi số trong bản tin đều
        /// cộng được, nên tách làm nhiều dòng cho tổng y hệt — cắt bỏ là mất dữ liệu thật.
        /// </summary>
        public List<Batch> TakeAll(int maxUnitsPerBatch)
        {
            if (maxUnitsPerBatch <= 0) maxUnitsPerBatch = int.MaxValue;

            var batches = new List<Batch>();
            var openByGroup = new Dictionary<(AdType, string), Batch>();

            foreach (var entry in _entries.Values)
            {
                var groupKey = (entry.adType, entry.adMediation);
                if (!openByGroup.TryGetValue(groupKey, out var batch) || batch.units.Count >= maxUnitsPerBatch)
                {
                    batch = new Batch { adType = entry.adType, adMediation = entry.adMediation };
                    openByGroup[groupKey] = batch;
                    batches.Add(batch);
                }

                batch.units.Add(entry);
            }

            _entries.Clear();
            return batches;
        }
    }
}
