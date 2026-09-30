/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Người chơi mở app TỪ ĐÂU (hợp đồng §B4). Thiếu nó thì push analytics không khâu được
    /// push → open → phiên.
    /// <br/>Unity không đọc được intent (Android) / launchOptions (iOS) — module push/deeplink
    /// biết thì báo lại qua <c>FalconBigDataController.App.ReportOpenSource</c>; không ai báo thì
    /// field vắng mặt chứ SDK KHÔNG mặc định <see cref="Icon"/> (đoán bừa thì con số "mở từ icon"
    /// chỉ đang đếm những game chưa wire).
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OpenSource
    {
        [EnumMember(Value = "icon")] Icon,
        [EnumMember(Value = "push")] Push,
        [EnumMember(Value = "deeplink")] Deeplink,
        [EnumMember(Value = "widget")] Widget,
        [EnumMember(Value = "other")] Other
    }
}
