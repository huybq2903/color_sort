/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bắn log ba mốc vòng đời ad view (request/show/close) — §B + §C. State nằm ở
    /// <see cref="AdViewCache"/> (tách riêng sau án deadlock 12/08 — xem doc bên đó); class này
    /// chỉ còn vai DỰNG BẢN TIN tại các khoảnh khắc, nên nó là bên duy nhất của entity ad ôm
    /// <see cref="LogScheduleService"/>.
    /// </summary>
    public class AdRequestService : MySingleton<AdRequestService>
    {
        private readonly AdViewCache _cache;
        private readonly LogScheduleService _logScheduleService;
        private readonly BannerLogService _bannerLogService;
        private readonly AdLoadStatsService _loadStatsService;

        public AdRequestService(
            AdViewCache cache, LogScheduleService logScheduleService, BannerLogService bannerLogService,
            AdLoadStatsService loadStatsService)
        {
            _cache = cache;
            _logScheduleService = logScheduleService;
            _bannerLogService = bannerLogService;
            _loadStatsService = loadStatsService;
        }

        /// <summary>
        /// Mediation gọi load một ad → mở vòng đời + log mốc request. Service CHỈ nhận param —
        /// nén knob phẳng thành param là việc của <see cref="FAdApi"/> (một đường xử lý ở đây).
        /// </summary>
        public void RequestAd(AdViewParam param)
        {
            if (param == null) return;
            // networkType là SỐ ĐO của SDK, không phải cái game khai — stamp đè bất kể param
            param.networkType = NetworkTypeExtensions.Current();
            var adViewId = _cache.Open(param);
            _logScheduleService.Enqueue(new FAdRequestLog(param) { adViewId = adViewId });
        }

        /// <summary>Mediation gọi hiển thị ad → log mốc show (kèm thời gian chờ từ lúc request).</summary>
        /// <summary>
        /// Game muốn chiếu ad — bắn TRƯỚC nhánh kiểm tra kho. Id view chỉ đóng khi có ad sẵn:
        /// kho rỗng mà vẫn đóng id của đợt đang load là nói dối "sắp chiếu cái này".
        /// </summary>
        public void ShowAttempt(AdType type, string adWhere, bool adAvailable, string adWhen, string adMediation)
        {
            _logScheduleService.Enqueue(new FAdShowAttemptLog
            {
                type = type,
                adWhere = adWhere,
                adWhen = adWhen,
                adMediation = adMediation,
                adAvailable = adAvailable,
                adViewId = adAvailable ? _cache.CurrentViewId(type) : null
            });
        }

        public void ShowAd(AdShowParam param)
        {
            if (param == null) return;
            var shown = _cache.MarkShown(param);
            if (shown.rotated)
                AnalyticLogger.Instance.Warning(
                    $"OnShown({param.type}) trên view đã có impression — SDK xoay adViewId mới cho " +
                    "lần hiển thị này. Dấu hiệu mediation thiếu OnRequested cho đợt load mới.");
            _logScheduleService.Enqueue(new FAdShowLog(param)
            {
                adViewId = shown.adViewId,
                requestToShowMs = shown.requestToShowMs
            });
        }

        /// <summary>
        /// Impression/paid event → log doanh thu. Banner đi vào cụm gộp (per-impression là đúng
        /// cái hố volume mà cụm sinh ra để lấp); format khác enqueue <see cref="FAdLog"/> —
        /// decorator tự đóng adViewId/fillLatency/context/LTV như đường của module Mediation.
        /// </summary>
        public void LogImpression(AdParam param)
        {
            if (param == null) return;

            if (param.type == AdType.Banner)
            {
                _bannerLogService.Log(param.adWhere, param.adPrecision, param.adCountry, param.adRev,
                    param.adNetwork, param.adMediation, param.currentLevel ?? 0);
                return;
            }

            _logScheduleService.Enqueue(new FAdLog(param));
        }

        /// <summary>
        /// Kết quả một lần gọi load của một unit → vào KHO GỘP CỤM, không enqueue thẳng
        /// (per-attempt là volume không chặn trên — đúng vụ mo_multiple_floor bị chặn server).
        /// Không đụng cache ad view — đây không phải mốc của vòng đời một lần xem.
        /// </summary>
        public void LogLoadResult(AdLoadResultParam param)
        {
            // Bẫy đổi tên 23/09: trường cũ tên `floor` mang HỆ SỐ, giờ `floor` là GIÁ THẬT (USD) còn
            // hệ số dời sang `tier`. Code mediation cũ (`floor = a.multiplier`) VẪN COMPILE nên sai
            // im lặng — cảnh báo tại chỗ là thứ duy nhất bắt được.
            if (param is { floor: > 0, tier: null })
                AnalyticLogger.Instance.Warning(
                    $"OnLoadResult({param.adType}/{param.adUnitId}): có `floor` = {param.floor} (GIÁ SÀN " +
                    "THẬT, USD) nhưng không có `tier`. Nếu đang truyền hệ số bậc trong config thì đổi " +
                    "sang `tier` — `floor` chỉ dành cho mediation BIẾT giá sàn thật (vd AdMob).");

            _loadStatsService.Record(param);
        }

        /// <summary>
        /// Người chơi bấm vào ad → ghi cờ vào view hiện tại, log close của cùng view tự mang
        /// <c>hasClick</c>. KHÔNG bắn bản tin riêng (hiện tại) — xem doc ở <see cref="FAdApi.OnClicked"/>.
        /// </summary>
        public void ClickAd(AdType type)
        {
            if (_cache.MarkClicked(type)) return;

            AnalyticLogger.Instance.Warning(
                $"OnClicked({type}) bỏ qua: format này chưa đi qua OnRequested nên không có lần xem " +
                "ad nào để nhận cú click.");
        }

        /// <summary>Ad đóng lại → log mốc cuối của vòng đời (kèm thời lượng hiển thị).</summary>
        public void CloseAd(AdCloseParam param)
        {
            if (param == null) return;
            var closed = _cache.MarkClosed(param);
            _logScheduleService.Enqueue(new FAdCloseLog(param)
            {
                adViewId = closed.adViewId,
                shownDurationSec = closed.shownDurationSec
            });
        }

        /// <summary>
        /// Ghi vị trí chiếu cho lần xem ad hiện tại của format này (preload xong mới biết chiếu ở
        /// đâu). Các mốc sau — show, impression, close — tự mang theo, khỏi nhập lại từng mốc.
        /// Chưa có request nào của format thì cảnh báo và bỏ qua.
        /// </summary>
        public void SetAdContext(AdType type, string adWhere = null, string adWhen = null, string adMediation = null)
        {
            if (_cache.SetContext(type, adWhere, adWhen, adMediation)) return;

            AnalyticLogger.Instance.Warning(
                $"UpdateAdContext({type}) bỏ qua: format này chưa đi qua OnAdRequested nên chưa có " +
                "lần xem ad nào để ghi tham số.");
        }

        // ---- Đọc — giữ làm cửa cũ cho FAdApi/mediation; bản chất nằm ở AdViewCache ----

        /// <inheritdoc cref="AdViewCache.CurrentViewId"/>
        public string CurrentAdViewId(AdType type)
        {
            return _cache.CurrentViewId(type);
        }

        /// <inheritdoc cref="AdViewCache.TakeSnapshot"/>
        public AdViewSnapshot CurrentAdView(AdType type)
        {
            return _cache.TakeSnapshot(type);
        }

        /// <inheritdoc cref="AdViewCache.FillLatencyMs"/>
        public int? FillLatencyMs(AdType type)
        {
            return _cache.FillLatencyMs(type);
        }

        /// <inheritdoc cref="AdViewCache.ApplyContext"/>
        public void ApplyAdContext(AdViewParam param, AdType type)
        {
            _cache.ApplyContext(param, type);
        }
    }
}
