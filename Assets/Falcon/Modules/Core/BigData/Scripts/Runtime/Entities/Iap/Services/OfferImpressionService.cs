/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bắn log cho vòng đời offer IAP — state nằm ở <see cref="OfferImpressionCache"/> (tách riêng
    /// sau án deadlock 12/08, xem doc bên đó); class này chỉ còn vai DỰNG BẢN TIN tại các khoảnh
    /// khắc + cảnh báo lúc dev.
    /// </summary>
    public class OfferImpressionService : MySingleton<OfferImpressionService>, IAppPauseLogGenerator
    {
        private readonly OfferImpressionCache _cache;
        private readonly LogScheduleService _logScheduleService;
        private readonly AnalyticConfigService _configService;

        public OfferImpressionService(
            OfferImpressionCache cache, LogScheduleService logScheduleService,
            AnalyticConfigService configService)
        {
            _cache = cache;
            _logScheduleService = logScheduleService;
            _configService = configService;
        }

        private long GraceMillis => _configService.OfferAttributionGraceSec * 1000L;

        /// <summary>Id của offer đang hiển thị (null nếu không có) — cửa cũ, bản chất ở cache.</summary>
        public string OpenImpressionId => _cache.OpenImpressionId;

        /// <summary>Ảnh chụp offer đang hiển thị (null nếu không có).</summary>
        public OfferSnapshot CurrentSnapshot()
        {
            return _cache.TakeSnapshot();
        }

        /// <summary>
        /// Offer bắt đầu hiển thị. Nếu còn offer trước chưa đóng thì offer đó được chốt và log ngay
        /// (kèm warning lúc dev) — surface cũ đã thực sự kết thúc, bỏ đi là mất một impression.
        /// </summary>
        public void OpenOffer(IapOfferParam param)
        {
            if (param == null) return;

            var (_, replaced) = _cache.Open(param, GraceMillis);
            if (replaced == null) return;

            AnalyticLogger.Instance.Warning(
                $"Offer {param.offerId} shown while offer {replaced.param.offerId} still open — closing the previous one (call OnOfferClosed explicitly)");
            _logScheduleService.Enqueue(LogOf(replaced));
        }

        /// <summary>Người chơi bấm vào offer đang hiển thị.</summary>
        public void MarkClicked()
        {
            var click = _cache.MarkClicked();
            if (click.result != OfferClick.RecordedAfterLogged) return;

            // Bản tin impression đã đi từ lúc app pause với isClicked = false — payload đóng băng,
            // và luật 1-vé-1-dòng cấm bắn dòng impression thứ hai (loader chốt ĐẾM DÒNG 12/08).
            // Chở cú bấm muộn bằng kênh nhãn instance (§D11): bản tin nhãn mang vé nên server vá
            // được đúng impression — COALESCE(isClicked, labels.clicked) — mà số dòng không đổi.
            // Và vì MỌI nhãn "clicked" đều sinh từ đúng ca này, tỉ lệ click-muộn ngoài đời =
            // count(f_sdk_iap_offer_label[clicked]) / count(f_sdk_iap_offer_data) — chính là con số
            // đang thiếu để biết đếm-dòng có đủ tốt không.
            _logScheduleService.Enqueue(
                new FIapOfferLabelLog(new LabelParam { labelKey = "clicked", labelValue = true })
                {
                    offerImpressionId = click.offerImpressionId
                });
        }

        /// <summary>
        /// Offer đóng lại → bắn log impression (span-record). Extras khai lúc đóng được gộp vào
        /// param trước khi chốt (tin mới thắng); offer đã chốt lúc app pause thì extras rơi —
        /// payload đã đi, không sửa được nữa.
        /// </summary>
        public void CloseOffer(Dictionary<string, object> extraMeta = null)
        {
            if (extraMeta != null) _cache.MergeExtraMeta(extraMeta);
            var record = _cache.Close(GraceMillis);
            if (record != null) _logScheduleService.Enqueue(LogOf(record));
        }

        /// <summary>
        /// App xuống nền lúc offer còn mở → chốt và gửi impression NGAY, vì app có thể bị kill mà
        /// không bao giờ quay lại. Đổi lại, bản tin mang thời lượng tính tới lúc rời app và
        /// <c>isPurchased</c> để trống (chưa kết luận được) — xem
        /// <see cref="FIapOfferLog.impressionDuration"/>.
        /// <br/>Mỗi impression chỉ ra ĐÚNG MỘT bản tin: chốt ở đây rồi thì <see cref="CloseOffer"/>
        /// thôi bắn, nên server không phải dedupe. Chuyển đổi vẫn đo được vì log mua mang cùng
        /// <c>offerImpressionId</c> (§C) — đó mới là nguồn chân lý, không phải cờ isPurchased.
        /// </summary>
        public IEnumerable<IDataLog> GetLogsOnAppPause()
        {
            var record = _cache.TakePauseSnapshot();
            if (record != null) yield return LogOf(record);
        }

        /// <summary>Dựng bản tin từ một lần hiển thị đã chốt: param của game + số đo của SDK.</summary>
        private static FIapOfferLog LogOf(OfferImpressionRecord record)
        {
            return new FIapOfferLog(record.param)
            {
                offerImpressionId = record.offerImpressionId,
                impressionDuration = record.impressionDurationSec
            };
        }
    }
}
