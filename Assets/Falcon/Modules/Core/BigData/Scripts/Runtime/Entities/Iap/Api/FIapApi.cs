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
    /// Entity LƯỢT MUA (phễu checkout) — <c>FalconBigDataController.Iap</c>
    /// (xem EntityLifecycle-Design.md §4e). Không có mốc mở thì server chỉ thấy mua-XONG:
    /// toàn bộ abandonment mù hoàn toàn.
    /// </summary>
    public class FIapApi
    {
        private readonly PurchaseAttemptService _purchaseAttemptService;

        internal FIapApi(PurchaseAttemptService purchaseAttemptService)
        {
            _purchaseAttemptService = purchaseAttemptService;
        }

        // ---- Đọc cache ----

        /// <summary>
        /// Id của lượt mua ĐANG MỞ (null nếu không có) — cùng id sẽ đóng lên log fail hoặc log mua
        /// thành công tương ứng. Game lấy id này để nối với receipt/message gửi game server.
        /// </summary>
        public string CurrentAttemptId => _purchaseAttemptService.OpenAttemptId;

        /// <summary>
        /// Ảnh chụp lượt mua ĐANG mở: id, sản phẩm, vị trí kích hoạt, giá và tiền tệ. Null nếu
        /// không có lượt nào đang mở. Là bản SAO — sửa vào đó không đụng tới state của SDK.
        /// </summary>
        public PurchaseAttemptSnapshot CurrentAttempt => _purchaseAttemptService.CurrentSnapshot();

        // ---- Báo khoảnh khắc ----

        /// <summary>
        /// Bắt đầu một lượt mua — gọi NGAY TRƯỚC khi mở billing flow (launchBillingFlow /
        /// StoreKit payment). SDK sinh <c>purchaseAttemptId</c>, log mốc mở phễu checkout, rồi
        /// tự đóng dấu id đó lên log thất bại hoặc log mua thành công tương ứng.
        /// <br/>CỬA CHUẨN nhận param (<c>param.productId</c> bắt buộc; <c>networkType</c> SDK tự
        /// đo, stamp đè) — bản knob phẳng là shorthand.
        /// </summary>
        public void OnStarted(IapPurchaseAttemptParam param)
        {
            _purchaseAttemptService.StartPurchase(param);
        }

        /// <inheritdoc cref="OnStarted(IapPurchaseAttemptParam)"/>
        /// <param name="productId">Sản phẩm đang mua.</param>
        /// <param name="where">Vị trí/ngữ cảnh kích hoạt mua (cùng nghĩa <c>where</c> của log mua).</param>
        /// <param name="localizedPrice">Giá nội địa hoá — để đo giá trị bị bỏ giỏ mà không phải join catalog.</param>
        /// <param name="isoCurrencyCode">Mã tiền tệ ISO của giá trên.</param>
        /// <param name="extraMeta">Key-value tuỳ ý cho lượt mua — server lưu <c>event_extra_props</c>, log fail cùng lượt tự mang theo; cột hợp đồng thắng khi trùng key.</param>
        public void OnStarted(
            string productId, string where = null,
            decimal? localizedPrice = null, string isoCurrencyCode = null,
            Dictionary<string, object> extraMeta = null)
        {
            var param = new IapPurchaseAttemptParam
            {
                where = where,
                localizedPrice = localizedPrice,
                isoCurrencyCode = isoCurrencyCode,
                extraMeta = extraMeta
            };
            if (productId != null) param.productId = productId;
            _purchaseAttemptService.StartPurchase(param);
        }

        /// <summary>
        /// Mua THÀNH CÔNG — mốc DOANH THU của lượt mua, khép phễu checkout. SDK enqueue
        /// <c>f_sdk_in_app_data</c>; decorator tự đóng <c>purchaseAttemptId</c> (khớp lượt đang
        /// mở), <c>offerImpressionId</c> (khớp offer đang hiển thị), sổ LTV, và nhận diện
        /// PENDING cũ hoàn tất (bẫy #4 §D4 — nhớ điền <c>param.transactionId</c>).
        /// <br/><c>param.localizedPrice</c> KHÔNG được thiếu — field doanh thu, hợp đồng không
        /// cho vắng (CorrectValues chặn null, điền 0 kèm error log).
        /// <br/>⚠ MỘT giao dịch MỘT đường: module InAppValidation hiện gửi qua
        /// <c>BigdataLogger</c> (new FInAppLog(...).Send()) — game dùng module đó thì KHÔNG gọi
        /// đây, chạy cả hai là ĐẾM ĐÔI doanh thu. Cửa này cho tích hợp ngoài module (hoặc khi
        /// module chuyển hẳn về đây).
        /// </summary>
        public void OnPurchased(InAppParam param)
        {
            _purchaseAttemptService.LogPurchase(param);
        }

        /// <summary>
        /// Lượt mua không hoàn tất (billing callback trả lỗi/huỷ/treo) → log fail mang cùng
        /// <c>purchaseAttemptId</c> với lúc mở.
        /// <br/>Cố ý KHÔNG có bản nhận param: payload của log fail do SDK dựng lại từ state lượt
        /// đang mở (kể cả extraMeta đã khai lúc OnStarted) — caller chỉ có đúng hai thứ này để nói.
        /// <br/>Lưu ý <see cref="IapPurchaseFailReason.Pending"/> KHÔNG phải fail thật: giao dịch
        /// còn treo, log mua vẫn sẽ bắn khi hoàn tất sau.
        /// </summary>
        public void OnFailed(IapPurchaseFailReason reason, string pendingTransactionId = null)
        {
            _purchaseAttemptService.FailPurchase(reason, pendingTransactionId);
        }
    }
}
