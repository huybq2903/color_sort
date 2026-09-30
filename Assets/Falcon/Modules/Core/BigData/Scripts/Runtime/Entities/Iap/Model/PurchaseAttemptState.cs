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
    /// Ảnh chụp lượt mua đang mở — bản SAO để game đọc; sửa vào đây không đụng tới state SDK.
    /// </summary>
    [Serializable]
    public class PurchaseAttemptSnapshot
    {
        public string purchaseAttemptId;
        public string productId;
        public string where;
        public decimal? localizedPrice;
        public string isoCurrencyCode;
    }

    /// <summary>
    /// Vòng đời một LƯỢT MUA (xem EntityLifecycle-Design.md §4e): mở lúc gọi billing flow,
    /// đóng khi billing callback trả thành công hoặc thất bại. Id sinh lúc mở và đóng dấu lên
    /// CẢ hai nhánh kết thúc để phễu checkout khớp chính xác + đo được time-to-complete (§D4).
    /// <br/>KHÔNG persist qua process (§A3.4): app bị kill giữa dialog thì lượt mua đó là
    /// "start cụt đuôi" — chính là số đo abandonment, chấp nhận theo bẫy (2) của §D4.
    /// <br/>Pure class — không IO/DI/time. Thread-safety do <see cref="PurchaseAttemptService"/>
    /// lo (bên ghi là UI game, bên đọc là callback billing của Unity IAP).
    /// </summary>
    public class PurchaseAttemptState
    {
        private string _attemptId;
        private string _productId;
        private string _where;
        private decimal? _localizedPrice;
        private string _isoCurrencyCode;
        private Dictionary<string, object> _extraMeta;

        /// <summary>Id của lượt mua ĐANG mở (null nếu không có).</summary>
        public string OpenAttemptId => _attemptId;

        /// <summary>Ảnh chụp lượt mua đang mở (null nếu không có) — bản sao.</summary>
        public PurchaseAttemptSnapshot TakeSnapshot()
        {
            if (_attemptId == null) return null;
            return new PurchaseAttemptSnapshot
            {
                purchaseAttemptId = _attemptId,
                productId = _productId,
                where = _where,
                localizedPrice = _localizedPrice,
                isoCurrencyCode = _isoCurrencyCode
            };
        }

        /// <summary>Mở một lượt mua: sinh id của lượt và giữ state.</summary>
        /// <returns>
        /// (id vừa sinh — caller gắn lên log; replaced = true nếu có lượt cũ chưa kết thúc bị đè,
        /// caller nên cảnh báo lúc dev).
        /// </returns>
        public (string attemptId, bool replaced) Open(IapPurchaseAttemptParam param)
        {
            var replaced = _attemptId != null;

            _attemptId = Guid.NewGuid().ToString();
            _productId = param.productId;
            _where = param.where;
            _localizedPrice = param.localizedPrice;
            _isoCurrencyCode = param.isoCurrencyCode;
            _extraMeta = param.extraMeta;

            return (_attemptId, replaced);
        }

        /// <summary>
        /// Lượt mua thất bại: dựng param cho log fail từ state đang mở rồi đóng lượt, kèm id để
        /// caller gắn lên log.
        /// <br/>Trả (null, null) nếu không có lượt nào đang mở — không bịa ra một lượt mua không tồn tại
        /// (ca thường gặp: app bị kill giữa dialog rồi callback mới về ở phiên sau, state đã chết
        /// cùng process; server đã thấy "start cụt đuôi" nên không mất thông tin).
        /// </summary>
        public (IapPurchaseFailParam param, string attemptId) TakeFail(IapPurchaseFailReason reason)
        {
            if (_attemptId == null) return (null, null);

            var param = new IapPurchaseFailParam
            {
                productId = _productId,
                where = _where,
                localizedPrice = _localizedPrice,
                isoCurrencyCode = _isoCurrencyCode,
                extraMeta = _extraMeta,
                failReason = reason
            };
            var attemptId = _attemptId;
            Clear();
            return (param, attemptId);
        }

        /// <summary>
        /// Giao dịch thành công vừa xảy ra: nếu khớp sản phẩm của lượt đang mở thì trả id để đóng
        /// dấu lên log mua và đóng lượt. Khớp theo productId để lượt mua sản phẩm A không ăn nhầm
        /// giao dịch của sản phẩm B.
        /// </summary>
        /// <returns>purchaseAttemptId nếu khớp, null nếu không.</returns>
        public string TryAttributeSuccess(string productId)
        {
            if (_attemptId == null || _productId != productId) return null;

            var attemptId = _attemptId;
            Clear();
            return attemptId;
        }

        private void Clear()
        {
            _attemptId = null;
            _productId = null;
            _where = null;
            _localizedPrice = null;
            _isoCurrencyCode = null;
            _extraMeta = null;
        }
    }
}
