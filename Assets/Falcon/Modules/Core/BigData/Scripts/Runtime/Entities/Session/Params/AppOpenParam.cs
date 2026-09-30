/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Tham số của một lần app vào foreground. Toàn bộ do SDK-core tự đo (luật §H3) —
    /// dev game không phải điền gì.
    /// </summary>
    [Serializable]
    public class AppOpenParam : FParam
    {
        /// <summary>Cold = process vừa khởi tạo (mốc mở phiên); Warm = quay lại từ background.</summary>
        public LaunchType launchType;

        /// <summary>
        /// App nằm dưới background bao lâu trước khi quay lại. Null khi Cold (không có nền để đo).
        /// </summary>
        [FKey(RemoveIfNull = true)] public int? backgroundDurationSec;

        /// <summary>
        /// Số thứ tự lần vào foreground trong phiên (bắt đầu từ 1). Đếm CẢ những lần bị lọc vì
        /// nền quá ngắn — nên khoảng nhảy của openIndex cho biết có bao nhiêu lần quay lại ngắn
        /// không được log (giữ được thông tin mà không tốn event).
        /// </summary>
        public int openIndex;

        /// <summary>Loại kết nối lúc mở app — SDK-core tự đo (giải thích config fetch fail đầu phiên).</summary>
        [FKey(RemoveIfNull = true)] public NetworkType? networkType;

        /// <summary>
        ///     Mở app từ đâu (§B4). Unity không đọc được intent/launchOptions nên module
        ///     push/deeplink phải báo qua <c>FalconBigDataController.App.ReportOpenSource</c>; không ai báo thì vắng mặt.
        /// </summary>
        [FKey(RemoveIfNull = true)] public OpenSource? openSource;

        /// <summary>Campaign của push đã kéo người chơi vào — chỉ có khi <see cref="openSource"/> = push.</summary>
        [FKey(RemoveIfNull = true)] public string pushCampaignId;

        /// <summary>
        ///     Thời gian khởi động (ms) — CHỈ có ở lần Cold. Mốc đo: C# sống dậy → lúc dựng bản tin
        ///     này. THIẾU phần native bootstrap nên là số tương đối (xem <see cref="StartupClock"/>).
        /// </summary>
        [FKey(RemoveIfNull = true)] public int? startupDurationMs;

        /// <summary>
        ///     Key-value tuỳ ý đi kèm nguồn mở app (metadata push: message id, deep link...) —
        ///     flatten vào payload, server lưu <c>event_extra_props</c>; cột hợp đồng thắng khi
        ///     trùng key. Vào qua <c>App.ReportOpenSource</c>, cùng TTL với nguồn.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> extraMeta;

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }
    }
}
