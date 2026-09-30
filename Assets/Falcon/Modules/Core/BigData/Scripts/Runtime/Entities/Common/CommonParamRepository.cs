/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-07
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Kho tham số dùng chung khai được LÚC CHẠY: game gọi <c>Set(key, value)</c> là key đó đi
    /// theo MỌI log từ đó trở đi, không phải khai class <see cref="IFCustomInfoRepository"/> riêng.
    /// <br/>Đường class vẫn còn và vẫn đúng cho tham số của cả một module (ab-test, mmp, account…):
    /// nó có ctor để bắt sự kiện, có chỗ giữ state. Kho này là cho ca còn lại — vài key lẻ mà viết
    /// hẳn một file thì không đáng.
    /// <br/>Mặc định KHÔNG sống qua restart vì giá trị của phiên trước chưa chắc còn đúng: người
    /// chơi rời guild lúc app đóng thì <c>guildId</c> cũ thành số liệu SAI, mà sai đắt hơn thiếu.
    /// Key nào biết chắc là bền thì bật <c>persist: true</c> — nó có mặt ngay từ log ĐẦU phiên,
    /// quãng mà game còn chưa chạy tới chỗ set.
    /// <br/>Luật và trạng thái nằm ở <see cref="CommonParamState"/>; class này chỉ lo phần đĩa.
    /// </summary>
    public class CommonParamRepository : IFCustomInfoRepository
    {
        private const string PERSIST_KEY = "Analytic_Common_Params";

        private readonly CommonParamState _state = new();
        private readonly IDataPool _dataPool;
        private readonly object _diskLock = new();
        private volatile bool _restored;

        public CommonParamRepository(IDataPool dataPool)
        {
            _dataPool = dataPool;
        }

        /// <summary>
        /// Khai tham số dùng chung với giá trị CỐ ĐỊNH (tới khi set lại hoặc <see cref="Remove"/>).
        /// Giá trị null = xoá key, để nó vắng mặt khỏi payload chứ không gửi null (§H4).
        /// </summary>
        /// <param name="persist">
        /// Giữ giá trị qua lần khởi động sau. Chỉ bật khi giá trị bền thật (guildId, cohort
        /// onboarding…): giá trị cũ mà đã sai thì sai ngay từ log đầu phiên, không ai sửa hộ được.
        /// </param>
        public void Set(string key, object value, bool persist = false)
        {
            RestoreOnce();
            Report(key, _state.Set(key, value, persist));
            SaveToDisk();
        }

        /// <summary>
        /// Khai tham số dùng chung ĐỌC TƯƠI: hàm được gọi mỗi lần dựng log nên giá trị luôn mới
        /// nhất mà game không phải nhớ set lại (số vàng đang có, tier battle pass hiện tại…).
        /// <br/>Bản này không có <c>persist</c> — provider là code, không lưu xuống đĩa được; mà
        /// cũng không cần, vì game khai lại nó mỗi lần khởi động ở đúng chỗ khai lần đầu.
        /// <br/>Hàm ném exception thì SDK nuốt + cảnh báo: một key hỏng không được phép làm chết
        /// cả đường log.
        /// </summary>
        public void Set(string key, Func<object> provider)
        {
            RestoreOnce();
            Report(key, _state.Set(key, provider));
            SaveToDisk();
        }

        /// <summary>Khai một lúc nhiều tham số cố định.</summary>
        public void SetAll(IEnumerable<KeyValuePair<string, object>> values, bool persist = false)
        {
            if (values == null) return;
            foreach (var (key, value) in values) Set(key, value, persist);
        }

        /// <summary>Thôi gửi key này từ log kế tiếp — xoá cả bản đã lưu qua restart (nếu có).</summary>
        public void Remove(string key)
        {
            RestoreOnce();
            _state.Remove(key);
            SaveToDisk();
        }

        /// <summary>Key này đang được khai không.</summary>
        public bool Has(string key)
        {
            RestoreOnce();
            return _state.Has(key);
        }

        /// <summary>Danh sách key đang khai — để soi lúc debug.</summary>
        public IReadOnlyCollection<string> Keys
        {
            get
            {
                RestoreOnce();
                return _state.Keys;
            }
        }

        public Dictionary<string, object> GetInfo()
        {
            RestoreOnce();
            return _state.BuildInfo((key, e) => AnalyticLogger.Instance.Warning(
                $"Common param '{key}' ném exception lúc đọc — bỏ qua key này cho log hiện tại " +
                $"(các key khác vẫn gửi bình thường): {e.Message}"));
        }

        private void Report(string key, CommonParamSet result)
        {
            switch (result)
            {
                case CommonParamSet.WireKeyRejected:
                    AnalyticLogger.Instance.Warning(
                        $"Common param '{key}' trùng tên một cột hợp đồng trên wire (§H1) nên bị " +
                        "từ chối: cho qua thì nó bị cột thật đè trên event có cột đó và chỉ xuất " +
                        "hiện trên phần còn lại — dữ liệu nửa có nửa không, im lặng. Đặt tên khác, " +
                        "vd feature streak của game thì là 'streak_status' chứ đừng 'win_streak'.");
                    break;

                case CommonParamSet.CentralKeyRejected:
                    AnalyticLogger.Instance.Warning(
                        $"Common param '{key}' trùng key tham số trung tâm của SDK nên sẽ KHÔNG có " +
                        "tác dụng (tham số trung tâm luôn thắng lúc merge). Đặt tên khác đi, đừng " +
                        "để nó nằm im vô hình.");
                    break;

                case CommonParamSet.Added when _state.Count > CommonParamState.WARN_KEY_COUNT:
                    AnalyticLogger.Instance.Warning(
                        $"Đang có {_state.Count} common param — mỗi key ở đây đi theo MỌI log của " +
                        "MỌI user, đây là chỗ đắt nhất để thêm field. Tham số chỉ có nghĩa với một " +
                        "loại khoảnh khắc thì để trên param của log đó; và nhớ đăng ký key với " +
                        "loader, key vô danh server không nhặt.");
                    break;
            }
        }

        /// <summary>
        /// Nạp key đã lưu từ phiên trước — chạy một lần, lúc dùng đầu tiên. Cố tình KHÔNG nạp
        /// trong constructor: pool đọc file ở pha ServiceReady, mà repository này có thể được dựng
        /// trước đó.
        /// </summary>
        private void RestoreOnce()
        {
            if (_restored) return;
            lock (_diskLock)
            {
                if (_restored) return;
                _restored = true;
                _state.Restore(LoadFromDisk());
            }
        }

        private Dictionary<string, object> LoadFromDisk()
        {
            var json = _dataPool.GetOrDefault<string>(PERSIST_KEY, null);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            catch (Exception e)
            {
                // Dữ liệu hỏng thì bỏ, không để nó chặn cả kho tham số
                AnalyticLogger.Instance.Warning($"Không đọc được common param đã lưu, bỏ qua: {e.Message}");
                return null;
            }
        }

        private void SaveToDisk()
        {
            lock (_diskLock)
            {
                var persisted = _state.PersistedValues;
                _dataPool.Compute<string>(PERSIST_KEY,
                    _ => persisted.Count == 0 ? null : JsonConvert.SerializeObject(persisted));
            }
        }
    }
}
