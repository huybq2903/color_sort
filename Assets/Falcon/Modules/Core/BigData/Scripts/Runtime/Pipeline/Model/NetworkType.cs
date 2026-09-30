/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-03
 */

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Loại kết nối lúc một mốc xảy ra — SDK-core tự đo (luật §H3).
    /// Gắn ở những chỗ mà mạng giải thích được thất bại: xin ad (fill-fail), mở billing flow
    /// (lỗi mua kiểu network), mở app. CỐ TÌNH không đưa vào central user params: thêm một field
    /// vào MỌI log là đắt trong khi phần lớn log không cần.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NetworkType
    {
        [EnumMember(Value = "none")] None,
        [EnumMember(Value = "wifi")] Wifi,
        [EnumMember(Value = "cellular")] Cellular
    }

    public static class NetworkTypeExtensions
    {
        /// <summary>Loại kết nối hiện tại của thiết bị.</summary>
        public static NetworkType Current()
        {
            return FromReachability(Application.internetReachability);
        }

        /// <summary>Map thuần từ giá trị Unity — tách riêng để test được.</summary>
        public static NetworkType FromReachability(NetworkReachability reachability)
        {
            return reachability switch
            {
                NetworkReachability.ReachableViaLocalAreaNetwork => NetworkType.Wifi,
                NetworkReachability.ReachableViaCarrierDataNetwork => NetworkType.Cellular,
                _ => NetworkType.None
            };
        }
    }
}
