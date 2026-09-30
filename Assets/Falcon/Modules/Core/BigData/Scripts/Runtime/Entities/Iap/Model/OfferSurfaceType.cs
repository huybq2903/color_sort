/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-03
 */

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Kiểu bề mặt hiển thị offer. Tồn tại vì hồ sơ lệch chuẩn §3.2: cùng event
    /// <c>iap_offer_impression</c> đang trộn 3 kiểu đo sống chung — card nằm lì trên UI
    /// (49 lần/user/ngày, CTR 0.03%), popup chủ đích (CTR 0.3–5%), và game chỉ log khi user bấm
    /// (CTR = 100%) — nên **mọi chỉ số gộp trên event này vô nghĩa nếu không tách kiểu**.
    /// Không khai thì để null (không biết) chứ đừng đoán.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OfferSurfaceType
    {
        /// <summary>Popup chủ đích chiếm màn hình, người chơi phải xử lý mới đi tiếp được.</summary>
        [EnumMember(Value = "popup")] Popup,

        /// <summary>Card/banner offer nằm trong một màn hình khác (home, result...), không chặn.</summary>
        [EnumMember(Value = "embedded_card")] EmbeddedCard,

        /// <summary>Kệ hàng trong shop — một lượt mở shop là một impression mang mảng sản phẩm.</summary>
        [EnumMember(Value = "shop")] Shop
    }
}
