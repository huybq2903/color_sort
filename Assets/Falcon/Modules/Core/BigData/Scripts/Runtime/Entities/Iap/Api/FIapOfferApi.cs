/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-07
 */

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Entity MỘT LẦN HIỂN THỊ OFFER IAP — <c>FalconBigDataController.IapOffer</c>
    /// (xem EntityLifecycle-Design.md §4d). Log impression bắn lúc ĐÓNG, không log per-frame.
    /// </summary>
    public class FIapOfferApi
    {
        private readonly OfferImpressionService _offerImpressionService;

        internal FIapOfferApi(OfferImpressionService offerImpressionService)
        {
            _offerImpressionService = offerImpressionService;
        }

        // ---- Đọc cache ----

        /// <summary>
        /// Id của lần hiển thị offer ĐANG MỞ (null nếu không có). Cùng id sẽ được đóng lên log
        /// impression lúc offer đóng và lên log mua nếu người chơi mua đúng sản phẩm đó — game
        /// lấy id này để gắn lên message của mình nếu muốn nối dữ liệu DWH với game server.
        /// </summary>
        public string CurrentImpressionId => _offerImpressionService.OpenImpressionId;

        /// <summary>
        /// Ảnh chụp offer ĐANG hiển thị: id, sản phẩm, giá, loại bề mặt, đã bấm/đã mua chưa, và
        /// hiển thị được bao lâu. Null nếu không có offer nào đang mở — offer vừa đóng (còn trong
        /// cửa sổ ân hạn quy giao dịch) cũng trả null, vì với game thì nó đã đóng thật.
        /// <br/>Là bản SAO — sửa vào đó không đụng tới state của SDK.
        /// </summary>
        public OfferSnapshot Current => _offerImpressionService.CurrentSnapshot();

        // ---- Báo khoảnh khắc ----

        /// <summary>
        /// Offer IAP bắt đầu hiển thị. SDK sinh <c>offerImpressionId</c>, bấm giờ thời lượng hiển thị,
        /// và tự đóng dấu id đó lên log mua nếu người chơi mua đúng sản phẩm trong
        /// <see cref="IapOfferParam.offerProductId"/> — đo được conversion offer→purchase thật.
        /// <br/>Log impression chỉ bắn lúc <see cref="OnClosed"/> (một event cho cả lượt hiển thị,
        /// không log per-frame). 4 field <c>offerImpressionId</c> / <c>impressionDuration</c> /
        /// <c>isClicked</c> / <c>isPurchased</c> do SDK quản, game không cần set.
        /// <br/>Nhiều offer trên một kệ shop = MỘT lần gọi mang mảng <c>offerProductId</c>,
        /// KHÔNG gọi mỗi offer một lần (luật volume §G).
        /// </summary>
        public void OnShown(IapOfferParam param)
        {
            _offerImpressionService.OpenOffer(param);
        }

        /// <summary>Người chơi bấm vào offer đang hiển thị.</summary>
        public void OnClicked()
        {
            _offerImpressionService.MarkClicked();
        }

        /// <summary>
        /// Offer đóng lại (người chơi tắt popup / rời màn hình) → bắn log impression kèm thời lượng,
        /// đã bấm hay chưa, và có dẫn tới mua hay không.
        /// </summary>
        /// <param name="extraMeta">
        /// Key-value tuỳ ý biết được LÚC ĐÓNG (vd lý do đóng) — gộp vào extras đã khai lúc
        /// <see cref="OnShown"/>, tin mới thắng khi trùng key. Offer đã bị chốt lúc app pause thì
        /// extras này rơi (payload đã đi).
        /// </param>
        public void OnClosed(Dictionary<string, object> extraMeta = null)
        {
            _offerImpressionService.CloseOffer(extraMeta);
        }
    }
}
