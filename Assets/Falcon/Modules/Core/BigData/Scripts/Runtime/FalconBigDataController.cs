/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-30
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Điểm vào "báo khoảnh khắc" cho dev game: gọi <c>FalconBigDataController.&lt;Nhóm&gt;
    /// .On&lt;ChuyệnVừaXảyRa&gt;(...)</c>, SDK lo việc phát event gì và quản vòng đời entity
    /// (xem EntityLifecycle-Design.md).
    /// <code>
    /// FalconBigDataController.Level.OnStart(currentLevel: 5, difficulty: "Hard");
    /// FalconBigDataController.Level.OnFail(LevelFailReason.OutOfMoves, levelProgress: 80);
    /// FalconBigDataController.Ad.OnRequested(AdType.Reward, adWhere: "shop");
    /// FalconBigDataController.Iap.OnStarted("pack_starter", where: FIapPlacement.Shop);
    /// FalconBigDataController.Common.Set("gold", () =&gt; wallet.Gold);   // đi theo MỌI log
    /// int maxLevel = FalconBigDataController.Player.General.MaxPassedLevel;  // số SDK đang giữ
    /// </code>
    /// Mỗi nhóm là MỘT entity; trong nhóm, phần <b>đọc/sửa cache</b> đứng trước, phần
    /// <b>báo khoảnh khắc (phát event)</b> đứng sau.
    /// <br/>- Tham số định danh của lượt chơi được cache từ <c>Level.OnStart</c> — các khoảnh khắc
    ///   sau KHÔNG phải nhập lại, chỉ nhập phần mới phát sinh.
    /// <br/>- Muốn ghi đè định danh cho 1 log cụ thể: dùng đường param object
    ///   (new FLevelLog(new LevelPassParamV2 { ... }).Send()) — giá trị tự set luôn thắng cache.
    /// </summary>
    public class FalconBigDataController : MySingleton<FalconBigDataController>
    {
        private readonly LogScheduleService _logScheduleService;
        private readonly FPlayerInfoService _playerInfoService;

        public FalconBigDataController(
            LevelLogService levelLogService, LevelTurnService levelTurnService,
            LevelLogDecorService levelLogDecorService, OfferImpressionService offerImpressionService,
            PurchaseAttemptService purchaseAttemptService, AdRequestService adRequestService,
            ResourceExchangeService resourceExchangeService, FunnelLogService funnelLogService,
            FunnelRegistry funnelRegistry, AppOpenLogService appOpenLogService,
            PermissionLogService permissionLogService, LabelLogService labelLogService, EntityLabelService entityLabelService,
            CommonParamRepository commonParamRepository, FPlayerInfoService playerInfoService,
            LogScheduleService logScheduleService, UiExposureClusterService uiExposureClusterService)
        {
            _logScheduleService = logScheduleService;
            _playerInfoService = playerInfoService;

            LevelApi = new FLevelApi(levelLogService, levelTurnService, levelLogDecorService, labelLogService);
            IapOfferApi = new FIapOfferApi(offerImpressionService);
            AdApi = new FAdApi(adRequestService, labelLogService);
            IapApi = new FIapApi(purchaseAttemptService);
            ResourceApi = new FResourceApi(resourceExchangeService);
            FunnelApi = new FFunnelApi(funnelLogService, funnelRegistry);
            AppApi = new FAppApi(appOpenLogService, permissionLogService);
            LabelApi = new FLabelApi(labelLogService, entityLabelService);
            CommonApi = new FCommonApi(commonParamRepository);
            UiApi = new FUiApi(uiExposureClusterService);
        }

        #region Nhóm API theo entity

        // Property dạng ...Api là chỗ giữ thật; lối vào cho dev game là các static ngắn bên dưới
        // (một cái tên không thể vừa là static vừa là instance, nên tách đôi như vậy).

        public FLevelApi LevelApi { get; }
        public FIapOfferApi IapOfferApi { get; }
        public FAdApi AdApi { get; }
        public FIapApi IapApi { get; }
        public FResourceApi ResourceApi { get; }
        public FFunnelApi FunnelApi { get; }
        public FAppApi AppApi { get; }
        public FLabelApi LabelApi { get; }
        public FCommonApi CommonApi { get; }
        public FUiApi UiApi { get; }
        public FPlayerInfoService PlayerInfo => _playerInfoService;

        /// <summary>Lượt chơi level: <c>Level.OnStart/OnPass/OnFail</c>, cache lượt, streak.</summary>
        public static FLevelApi Level => Instance.LevelApi;

        /// <summary>Offer IAP: <c>Offer.OnShown/OnClicked/OnClosed</c>.</summary>
        public static FIapOfferApi IapOffer => Instance.IapOfferApi;

        /// <summary>Một lần xem ad: <c>Ad.OnRequested/OnShown/OnClosed</c>, adViewId, chỗ chiếu.</summary>
        public static FAdApi Ad => Instance.AdApi;

        /// <summary>Phễu checkout: <c>Purchase.OnStarted/OnFailed</c>.</summary>
        public static FIapApi Iap => Instance.IapApi;

        /// <summary>Giao dịch tài nguyên nhiều vế: <c>Resource.OnExchange(sinks, sources)</c>.</summary>
        public static FResourceApi Resource => Instance.ResourceApi;

        /// <summary>Funnel FTUE &amp; feature: <c>Funnel.OnStep</c>, <c>Funnel.Register</c>.</summary>
        public static FFunnelApi Funnel => Instance.FunnelApi;

        /// <summary>Nguồn mở app &amp; trạng thái quyền: <c>App.ReportOpenSource/ReportPermission</c>.</summary>
        public static FAppApi App => Instance.AppApi;

        /// <summary>Nhãn cho phiên/lượt đang mở: <c>Label.Session/Turn</c>.</summary>
        public static FLabelApi Label => Instance.LabelApi;

        /// <summary>Tham số dùng chung đi theo MỌI log: <c>Common.Set(key, value)</c>.</summary>
        public static FCommonApi Common => Instance.CommonApi;

        /// <summary>Exposure UI (đếm gộp cụm): <c>Ui.OnImpression/OnClicked(surfaceId)</c> — CTR live-op.</summary>
        public static FUiApi Ui => Instance.UiApi;

        /// <summary>
        /// Hồ sơ người chơi mà SDK đang giữ — đây chính là những con số được đóng lên mọi log
        /// (<c>Player.General.MaxPassedLevel</c>, <c>Player.Session.TotalPlayTime</c>,
        /// <c>Player.Iap.InAppCount</c>, <c>Player.Ad.AdLtv</c>…). Đọc ở đây thay vì game đếm bản
        /// thứ hai: đếm hai nơi thì sớm muộn UI và log lệch nhau mà không ai biết bên nào đúng.
        /// <br/>⚠ Mấy repository này có cả setter (Devkit dùng để tự cập nhật). Game GHI vào đó là
        /// tự khai số của mình đè lên số SDK đo được — claim không bằng chứng, đúng thứ §H3 cấm.
        /// Cần đổi thì báo khoảnh khắc tương ứng (<c>Level.OnPass</c>, log IAP…) rồi để SDK tự cộng.
        /// <br/>Đây là passthrough thẳng tới <see cref="FPlayerInfoService"/> chứ không bọc lại:
        /// bọc thì phải chép hơn ba chục property của Devkit và chắc chắn sẽ lệch pha, mà cũng
        /// chẳng chặn được gì — <c>FPlayerInfoService.Instance</c> vốn đã public.
        /// </summary>
        public static FPlayerInfoService Player => Instance.PlayerInfo;

        #endregion

        #region Ống dẫn — cửa trước của pipeline gửi log

        /// <summary>
        /// Cửa trước của pipeline gửi log: mọi log (kể cả đường tiện tay log.Send()) đi qua đây,
        /// được LogDecorService trang trí (decorate-once) tại funnel rồi vào hàng đợi gửi batch.
        /// </summary>
        public void Send(IDataLog log)
        {
            _logScheduleService.Enqueue(log);
        }

        /// <summary>Như <see cref="Send"/> cho cả batch — giữ semantics enqueue-một-lượt (quan trọng lúc app pause).</summary>
        public void SendAll(IEnumerable<IDataLog> logs)
        {
            _logScheduleService.EnqueueAll(logs);
        }

        /// <summary>Gửi ngay trên thread phụ, bỏ qua batch; lỗi thì rơi về hàng đợi bền.</summary>
        public void SendNow(IDataLog log)
        {
            _logScheduleService.SendNow(log);
        }

        #endregion
    }
}
