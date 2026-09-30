/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Quyết định mỗi lần app vào foreground có sinh log hay không và dựng tham số cho nó
    /// (xem EntityLifecycle-Design.md). Pure class — không IO/DI/time, caller truyền mốc thời gian vào.
    /// <br/>Không tự lo thread-safety: <see cref="AppOpenLogService"/> khoá giúp — vòng đời app
    /// chạy main thread nhưng ReportOpenSource đến từ callback notification của module push.
    /// </summary>
    public class AppOpenState
    {
        /// <summary>
        /// Báo nguồn mở app cũ hơn ngần này thì không còn thuộc lần mở nào nữa — bỏ đi thay vì
        /// dán nhầm sang lần mở sau (người chơi bấm push lúc 9h, quay lại app lúc 11h là hai chuyện).
        /// </summary>
        public const int SOURCE_REPORT_TTL_MILLIS = 10_000;

        /// <summary>
        /// Báo nguồn TRONG ngần này sau khi log mở app đã bắn = báo muộn: bản tin đã đi rồi,
        /// không đóng dấu được nữa. Dùng để cảnh báo dev, không ảnh hưởng dữ liệu.
        /// </summary>
        public const int LATE_REPORT_GRACE_MILLIS = 10_000;

        private long? _pausedAtMillis;
        private int _openIndex;
        private bool _coldOpened;
        private OpenSource? _pendingSource;
        private string _pendingPushCampaignId;
        private Dictionary<string, object> _pendingExtraMeta;
        private long _pendingAtMillis;
        private long? _lastLoggedOpenAtMillis;

        /// <summary>
        /// App rơi xuống background — ghi mốc để lần quay lại tính được thời lượng nền.
        /// <br/>Mốc wall-clock CÓ CHỦ ĐÍCH (luật 3 của <c>MonotonicClock</c>): thời lượng nền là
        /// khoảng VẮT QUA suspend — đồng hồ đơn điệu đóng băng khi máy ngủ nên đo bằng nó là
        /// backgroundDurationSec đếm thiếu gần hết. ĐỪNG "sửa" sang monotonic.
        /// </summary>
        public void MarkPause(long nowMillis)
        {
            _pausedAtMillis = nowMillis;
        }

        /// <summary>
        /// Module push/deeplink báo lần mở này đến từ đâu — giữ lại để đóng vào bản tin app_open
        /// sắp bắn. Phải báo TRƯỚC lúc bản tin đi (bản tin đã gửi thì không sửa được nữa).
        /// </summary>
        /// <returns>
        /// true nếu vừa có bản tin app_open bắn ngay trước đó — tức là báo MUỘN, lần mở đó
        /// không mang được nguồn. Caller dùng để cảnh báo dev.
        /// </returns>
        public bool ReportSource(
            OpenSource source, string pushCampaignId, long nowMillis,
            Dictionary<string, object> extraMeta = null)
        {
            _pendingSource = source;
            _pendingPushCampaignId = source == OpenSource.Push ? pushCampaignId : null;
            _pendingExtraMeta = extraMeta;
            _pendingAtMillis = nowMillis;

            return _lastLoggedOpenAtMillis.HasValue &&
                   nowMillis - _lastLoggedOpenAtMillis.Value <= LATE_REPORT_GRACE_MILLIS;
        }

        private void ApplyPendingSource(AppOpenParam param, long nowMillis)
        {
            if (_pendingSource.HasValue && nowMillis - _pendingAtMillis <= SOURCE_REPORT_TTL_MILLIS)
            {
                param.openSource = _pendingSource;
                param.pushCampaignId = _pendingPushCampaignId;
                // Extras đi cùng chuyến với nguồn — cùng TTL, cùng số phận
                param.extraMeta = _pendingExtraMeta;
            }

            // Báo quá cũ thì vứt luôn, không để dành dán nhầm sang lần mở sau
            _pendingSource = null;
            _pendingPushCampaignId = null;
            _pendingExtraMeta = null;
            _lastLoggedOpenAtMillis = nowMillis;
        }

        /// <summary>
        /// App vào foreground. Luôn tăng openIndex (kể cả khi bị lọc — khoảng nhảy của openIndex
        /// chính là dấu vết của những lần quay lại ngắn).
        /// </summary>
        /// <param name="minBackgroundSec">
        /// Lần Hot (quay lại) có thời lượng nền dưới ngưỡng này thì KHÔNG log (lọc nhiễu kiểu thoát ra 2 giây
        /// copy OTP / nghe điện thoại). Cold không bao giờ bị lọc.
        /// </param>
        /// <returns>Tham số để log, hoặc null nếu lần này bị lọc.</returns>
        public AppOpenParam TryOpen(long nowMillis, int minBackgroundSec)
        {
            _openIndex++;

            if (!_coldOpened)
            {
                _coldOpened = true;
                var coldParam = new AppOpenParam
                {
                    launchType = LaunchType.Cold,
                    openIndex = _openIndex
                };
                ApplyPendingSource(coldParam, nowMillis);
                return coldParam;
            }

            // Không có mốc pause (lẽ ra không xảy ra vì AppFlowService luôn stop trước khi continue):
            // để null thay vì bịa số 0 — server phân biệt được "không đo được" với "quay lại tức thì".
            int? backgroundSec = _pausedAtMillis.HasValue
                ? (int)Math.Max(0, (nowMillis - _pausedAtMillis.Value) / 1000)
                : null;
            _pausedAtMillis = null;

            if (backgroundSec.HasValue && backgroundSec.Value < minBackgroundSec) return null;

            var param = new AppOpenParam
            {
                // Hot = quay lại foreground cùng process. Unity không sinh Warm (activity/scene
                // dựng lại) vì single-activity gần như không có ca đó — không đoán cái không đo được.
                launchType = LaunchType.Hot,
                backgroundDurationSec = backgroundSec,
                openIndex = _openIndex
            };
            ApplyPendingSource(param, nowMillis);
            return param;
        }
    }
}
