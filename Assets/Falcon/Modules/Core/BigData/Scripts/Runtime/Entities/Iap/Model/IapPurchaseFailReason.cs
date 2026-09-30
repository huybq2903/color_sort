/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Lý do một lượt mua không hoàn tất — vocab theo hợp đồng sdk-contract-vnext §D4,
    /// map từ responseCode của billing (giá trị wire snake_case qua EnumMember).
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IapPurchaseFailReason
    {
        /// <summary>Người chơi tự huỷ dialog.</summary>
        [EnumMember(Value = "user_canceled")] UserCanceled,

        /// <summary>Sản phẩm không khả dụng trên store.</summary>
        [EnumMember(Value = "item_unavailable")] ItemUnavailable,

        /// <summary>Dịch vụ billing không sẵn sàng (chưa đăng nhập store, store lỗi...).</summary>
        [EnumMember(Value = "billing_unavailable")] BillingUnavailable,

        /// <summary>Lỗi mạng.</summary>
        [EnumMember(Value = "network")] Network,

        /// <summary>Lỗi cấu hình phía app (sai product id, chưa khai báo...).</summary>
        [EnumMember(Value = "developer_error")] DeveloperError,

        /// <summary>
        /// Google PENDING — KHÔNG phải fail thật: giao dịch còn treo chờ thanh toán,
        /// log mua vẫn sẽ bắn khi hoàn tất sau. Phễu phía server phải đọc hiểu điều này.
        /// </summary>
        [EnumMember(Value = "pending")] Pending,

        // ValidationRejected từng sống ở đây 3 ngày (17→20/08) rồi RÚT theo phương án B với
        // loader: validator reject giờ đi trong PURCHASE stream với validationStatus=rejected
        // (InAppParam.validationStatus) chứ không phải fail stream — fail stream trả về đúng
        // nghĩa billing-fail. Một transaction vẫn đúng một terminal.

        /// <summary>
        /// Chỉ dùng khi KHÔNG map được responseCode sang các giá trị trên — nói thẳng "không biết"
        /// còn hơn gán bừa một lý do sai. Gặp nhiều thì báo loader bổ sung vocab.
        /// </summary>
        [EnumMember(Value = "unknown")] Unknown
    }
}
