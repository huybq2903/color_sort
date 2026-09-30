/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Gộp impression banner để giảm số log gửi lên server (số đo thực địa: trung bình 4,2
    /// impression/dòng, p99 = 31 — bỏ gộp là thêm cỡ 125k event/ngày cho MỘT game). Đây đúng là
    /// mẫu span-record mà hợp đồng §G bắt dùng cho khoảnh khắc dày đặc.
    /// <br/>Cụm được chốt và gửi lúc app pause (<see cref="IAppPauseLogGenerator"/>) — mà trên
    /// Android mỗi lần chiếu inter/rewarded cũng là một lần pause, nên nhịp chốt thực tế là vài
    /// phút chứ không phải cả phiên.
    /// <br/>KILL-SAFE (loader audit 12/08): cache được persist sau mỗi impression và nạp-rồi-gửi
    /// lúc khởi động — app bị kill cứng thì rev+impression của cụm dở sống lại ở phiên sau thay vì
    /// mất trắng. Cụm phục hồi mang timestamp của phiên sau (bản tin dựng lúc boot) — chấp nhận,
    /// cùng loại xê dịch với chính cơ chế chốt-lúc-pause.
    /// <br/>⚠ Gộp là đổi CHI TIẾT TỪNG LẦN lấy TỔNG ĐÚNG: chỉ những gì cộng được (doanh thu, số
    /// impression) hoặc nằm trong <see cref="BannerKey"/> mới còn đúng trên dòng gộp. Định danh
    /// từng lần xem (độ trễ fill, <c>playTurnId</c>) bị bỏ trống — xem
    /// <see cref="FAggregatedAdLog"/>. Vì vậy KHÔNG dùng cách này cho inter/rewarded: hai loại đó
    /// người ta phân tích theo từng lần (chuỗi mệt mỏi, eCPM theo vị trí, phễu fill).
    /// </summary>
    public class BannerLogService : MySingleton<BannerLogService>, IAppPauseLogGenerator, IInit
    {
        private const string PERSIST_KEY = "Analytic_BannerClusters";

        /// <summary>
        /// Tiết lưu sync đĩa: dataPool.Compute chỉ ghi RAM, đĩa vốn 5 phút mới sync một lần — với
        /// banner (~impression mỗi 10-30s) thì kill-safe thành kill-safe trên giấy. Sync theo mỗi
        /// impression thì lại thừa IO; 30s là đủ chặt (mất tối đa ~1-2 impression thay vì 5 phút).
        /// </summary>
        private const long SYNC_THROTTLE_MILLIS = 30_000;

        private readonly ConcurrentDictionary<BannerKey, BannerValue> _cache = new();
        private long _lastSyncMillis = long.MinValue;
        private readonly AdViewCache _adViewCache;
        private readonly IDataPool _dataPool;
        private readonly LogScheduleService _logScheduleService;

        public BannerLogService(
            AdViewCache adViewCache, IDataPool dataPool, LogScheduleService logScheduleService)
        {
            _adViewCache = adViewCache;
            _dataPool = dataPool;
            _logScheduleService = logScheduleService;
        }

        /// <summary>
        /// Nạp cụm dở của phiên bị kill cứng và gửi luôn. Chạy trong Init (không phải ctor) để
        /// không kéo cả pipeline dậy giữa lúc DI đang dựng đồ thị.
        /// </summary>
        public Task Init(CancellationToken cancellationToken = default)
        {
            var json = _dataPool.GetOrDefault<string>(PERSIST_KEY, null);
            if (string.IsNullOrEmpty(json)) return Task.CompletedTask;

            // Xoá bản lưu TRƯỚC khi gửi: kill đúng giữa quãng gửi thì mất (như hành vi cũ),
            // còn xoá sau mà kill là phiên tới gửi ĐÔI — doanh thu đếm hai lần tệ hơn thiếu.
            _dataPool.Compute<string>(PERSIST_KEY, _ => null);

            try
            {
                var clusters = JsonConvert.DeserializeObject<List<BannerClusterSnapshot>>(json);
                if (clusters == null) return Task.CompletedTask;

                foreach (var cluster in clusters)
                    _logScheduleService.Enqueue(cluster.ToLog());
                AnalyticLogger.Instance.Info($"Phục hồi {clusters.Count} cụm banner của phiên bị kill.");
            }
            catch (Exception e)
            {
                AnalyticLogger.Instance.Warning($"Không đọc được cụm banner đã lưu, bỏ qua: {e.Message}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Ghi nhận một impression banner vào cụm đang gom. Không có <c>adLtv</c>: LTV do sổ cái
        /// tự cộng (Devkit cộng từ chính <paramref name="adRev"/> này, còn game dùng module
        /// Mediation thì Mediation cộng) — truyền từ ngoài vào chỉ là con số bị vứt.
        /// </summary>
        public void Log(
            string adWhere, string adPrecision, string adCountry, double adRev,
            string adNetwork, string adMediation, int currentLevel = 0)
        {
            var key = new BannerKey(adWhere, adPrecision, adCountry, adNetwork, adMediation, currentLevel);
            // Đọc id ở ĐÂY chứ không bắt Mediation truyền vào: id là của SDK, và cụm phải biết
            // ngay lúc gom thì mới phát hiện được nó có vắt qua hai instance hay không.
            var adViewId = _adViewCache.CurrentViewId(AdType.Banner);
            _cache.Compute(key, val =>
            {
                var value = val ?? new BannerValue();
                value.Update(adRev, adViewId);
                return value;
            });
            PersistSnapshot();
        }

        public IEnumerable<IDataLog> GetLogsOnAppPause()
        {
            // Cùng luật xoá-trước-gửi với Init: bản lưu không được sống lâu hơn khoảnh khắc cụm
            // bắt đầu rời cache, không thì kill giữa chừng là phiên tới gửi đôi phần đã đi.
            _dataPool.Compute<string>(PERSIST_KEY, _ => null);

            var infos = new List<BannerKey>(_cache.Keys);
            foreach (var info in infos)
                if (_cache.TryRemove(info, out var value))
                    yield return BannerClusterSnapshot.From(info, value).ToLog();
        }

        /// <summary>
        /// Chụp cache xuống đĩa — gọi sau mỗi impression. Snapshot dựng lại từ cache nên tự đúng
        /// (idempotent); race với flush thì bản chụp sau cùng thắng, tệ nhất mất đúng impression
        /// cuối thay vì cả cụm như trước.
        /// </summary>
        private void PersistSnapshot()
        {
            var clusters = new List<BannerClusterSnapshot>();
            foreach (var (key, value) in _cache)
                clusters.Add(BannerClusterSnapshot.From(key, value));
            var json = clusters.Count == 0 ? null : JsonConvert.SerializeObject(clusters);
            _dataPool.Compute<string>(PERSIST_KEY, _ => json);

            var now = MonotonicClock.ElapsedMillis;
            if (now - _lastSyncMillis < SYNC_THROTTLE_MILLIS) return;
            _lastSyncMillis = now;
            _dataPool.TrySync();
        }

        /// <summary>Dạng phẳng của một cụm để persist — key + phần cộng được, đủ dựng lại bản tin.</summary>
        [Serializable]
        private class BannerClusterSnapshot
        {
            public string where;
            public string precision;
            public string country;
            public string network;
            public string mediation;
            public int level;
            public double rev;
            public int count;
            public string adViewId;

            public static BannerClusterSnapshot From(BannerKey key, BannerValue value)
            {
                return new BannerClusterSnapshot
                {
                    where = key.AdWhere, precision = key.AdPrecision, country = key.AdCountry,
                    network = key.AdNetwork, mediation = key.AdMediation, level = key.CurrentLevel,
                    rev = value.AdRev, count = value.ImpressionCount, adViewId = value.AdViewId
                };
            }

            public FAggregatedAdLog ToLog()
            {
                return new FAggregatedAdLog(new AdParam
                {
                    type = AdType.Banner,
                    adWhere = where,
                    adPrecision = precision,
                    adCountry = country,
                    adRev = rev,
                    adNetwork = network,
                    adMediation = mediation,
                    currentLevel = level
                })
                {
                    impressionCount = count,
                    adViewId = adViewId
                };
            }
        }
    }
}
