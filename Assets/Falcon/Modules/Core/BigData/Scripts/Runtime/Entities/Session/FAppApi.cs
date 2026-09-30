/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-07
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// PHIÊN & TRẠNG THÁI QUYỀN — <c>FalconBigDataController.App</c>: những thứ SDK-core không tự
    /// nhìn thấy được nên phải có module khác báo vào (nguồn mở app §B4, quyền/consent §D8).
    /// Mốc mở app, thời lượng phiên, hiệu năng... thì SDK tự lo, game không phải gọi gì.
    /// </summary>
    public class FAppApi
    {
        private readonly AppOpenLogService _appOpenLogService;
        private readonly PermissionLogService _permissionLogService;

        internal FAppApi(AppOpenLogService appOpenLogService, PermissionLogService permissionLogService)
        {
            _appOpenLogService = appOpenLogService;
            _permissionLogService = permissionLogService;
        }

        /// <summary>
        /// Module push/deeplink báo lần mở app này đến từ đâu (§B4) — thiếu nó thì không khâu được
        /// push → open → phiên. Unity không đọc được intent (Android) / launchOptions (iOS) nên
        /// SDK-core không tự biết; không ai báo thì field vắng mặt chứ SDK không mặc định `icon`.
        /// <br/>⚠ Phải gọi TRƯỚC lúc bản tin app_open bắn, vì bản tin đã gửi thì không sửa được:
        /// ca Cold bản tin bắn ngay trong pha init (gọi từ <c>RuntimeInitializeOnLoadMethod</c> /
        /// <c>IPioneer</c>), ca Hot thì gọi ngay trong callback nhận notification. Gọi muộn thì SDK
        /// cảnh báo trong log và bỏ qua — KHÔNG dán sang lần mở kế tiếp (dán là số liệu sai).
        /// </summary>
        /// <param name="pushCampaignId">Chỉ dùng khi <paramref name="source"/> = <see cref="OpenSource.Push"/>.</param>
        /// <param name="extraMeta">
        /// Key-value tuỳ ý đi kèm nguồn mở (metadata push: message id, deep link...) — server lưu
        /// <c>event_extra_props</c>. Cùng TTL với nguồn: báo quá cũ thì vứt cùng nhau, không dán
        /// sang lần mở sau.
        /// </param>
        public void ReportOpenSource(
            OpenSource source, string pushCampaignId = null, Dictionary<string, object> extraMeta = null)
        {
            _appOpenLogService.ReportOpenSource(source, pushCampaignId, extraMeta);
        }

        /// <summary>
        /// Báo TRẠNG THÁI quyền/consent (§D8): ATT, đồng ý quảng cáo, quyền nhận push. Dùng
        /// <see cref="PermissionType"/> + hằng số <see cref="FPermissionValue"/>.
        /// <code>
        /// App.ReportAttStatus(AttStatus.Denied);      // hoặc bản gõ tay bên dưới
        /// App.ReportAdsConsent(granted: true);
        /// App.ReportPushPermission(granted: false);
        /// </code>
        /// Ba hàm trên là đường khuyến nghị — kiểu hoá sẵn nên không gõ sai vocab được. Hàm
        /// <c>ReportPermission</c> nhận string là cửa thoát cho quyền mới chưa kịp có hàm riêng.
        /// Thiếu mấy trạng thái này thì so cohort doanh thu ads có khi chỉ đang đo lệch tỷ lệ
        /// ATT/consent chứ không phải đo cái mình tưởng.
        /// <br/>Gọi thoải mái ở bất cứ đâu (mỗi lần khởi động, mỗi lần user đổi trong setting):
        /// SDK nhớ giá trị đã gửi nên báo trùng KHÔNG đẻ log.
        /// <br/>Mỗi loại quyền là MỘT event riêng (<c>f_sdk_permission_att</c>…) chứ không phải
        /// một event chung mang key — xem <see cref="PermissionType"/> để biết vì sao.
        /// </summary>
        /// <param name="extraMeta">
        /// Key-value tuỳ ý cho LẦN ĐỔI trạng thái này — server lưu <c>event_extra_props</c>.
        /// Giá trị không đổi thì không có log, extras cũng không đi. Ba hàm kiểu-hoá bên dưới
        /// không nhận extras — cần thì gọi cửa này.
        /// </param>
        public void ReportPermission(
            PermissionType type, string value, int? currentLevel = null,
            Dictionary<string, object> extraMeta = null)
        {
            _permissionLogService.ReportPermission(type, value, currentLevel, extraMeta);
        }

        /// <summary>
        /// Trạng thái ATT (iOS) — gọi ngay sau callback của
        /// <c>ATTrackingManager.RequestTrackingAuthorization</c>, và cả lúc khởi động để bắt ca
        /// user đổi trong Settings ngoài app (báo trùng không đẻ log nên gọi thoải mái).
        /// <br/>Đây là trạng thái quyết định eCPM và match-rate attribution: so cohort doanh thu
        /// ads mà thiếu nó thì có khi chỉ đang đo lệch tỷ lệ ATT chứ không phải đo cái mình tưởng.
        /// </summary>
        public void ReportAttStatus(AttStatus status, int? currentLevel = null)
        {
#if UNITY_IOS
            ReportPermission(PermissionType.Att, status.ToPermissionValue(), currentLevel);
#else
            // §D8: Android KHÔNG bắn event này. ATT là khái niệm của iOS — gửi "not_determined"
            // cho Android là bịa một trạng thái không tồn tại, và làm hỏng mẫu số khi tính tỉ lệ.
            AnalyticLogger.Instance.Info($"Bỏ qua ReportAttStatus({status}): ATT chỉ có trên iOS");
#endif
        }

        /// <summary>
        /// Đồng ý quảng cáo (GDPR/UMP) — gọi sau khi form consent đóng, và lúc khởi động.
        /// </summary>
        /// <param name="granted">User đồng ý cho dùng dữ liệu quảng cáo hay không.</param>
        public void ReportAdsConsent(bool granted, int? currentLevel = null)
        {
            ReportPermission(PermissionType.AdsConsent, Grant(granted), currentLevel);
        }

        /// <summary>
        /// Quyền nhận push — mẫu số của mọi phân tích push ("bao nhiêu % user còn nghe được mình").
        /// Gọi sau khi xin quyền, và lúc khởi động để bắt ca user tắt trong Settings.
        /// </summary>
        public void ReportPushPermission(bool granted, int? currentLevel = null)
        {
            ReportPermission(PermissionType.Push, Grant(granted), currentLevel);
        }

        private static string Grant(bool granted)
        {
            return granted ? FPermissionValue.Granted : FPermissionValue.Denied;
        }
    }
}
