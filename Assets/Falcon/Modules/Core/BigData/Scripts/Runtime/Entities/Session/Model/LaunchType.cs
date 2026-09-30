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
    /// Kiểu mở app (giá trị wire là snake_case qua EnumMember, dev không gõ string tay).
    /// <br/>Trên Unity chỉ có <see cref="Cold"/> và <see cref="Hot"/> — single-activity gần như
    /// không có ca <see cref="Warm"/> thật, và §H4 cấm đoán cái không đo được. Giá trị Warm giữ
    /// trong enum cho SDK native Android/iOS dùng sau.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LaunchType
    {
        /// <summary>
        /// Process vừa khởi tạo — giá trị DUY NHẤT mang tải nghĩa: nó tương đương mốc mở phiên
        /// (session_start) nên quyết định biên phiên phía server. Tuyệt đối không được false-positive:
        /// resume mà báo cold là đẻ ra phiên ma.
        /// </summary>
        [EnumMember(Value = "cold")] Cold,

        /// <summary>Process sống nhưng activity/scene dựng lại — dành cho SDK native, Unity không dùng.</summary>
        [EnumMember(Value = "warm")] Warm,

        /// <summary>Quay lại foreground trong CÙNG process — đây là ca của Unity.</summary>
        [EnumMember(Value = "hot")] Hot
    }
}
