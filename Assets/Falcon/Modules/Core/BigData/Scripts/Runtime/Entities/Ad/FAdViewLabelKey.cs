/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-17
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Tên nhãn HỢP ĐỒNG trên kênh <c>f_sdk_ad_view_label</c> — cùng vai
    /// <see cref="FLevelLabelKey"/>/<see cref="FUserLabelKey"/>: SDK bắn qua các cửa
    /// <see cref="FAdApi.OnLoadFailed"/>/<see cref="FAdApi.OnShowFailed"/> với đúng tên này để
    /// server so chéo được mọi game; game tự gõ tên khác là mất khả năng đó.
    /// </summary>
    public static class FAdViewLabelKey
    {
        /// <summary>ErrorCode khi mediation load ad thất bại (OnAdLoadFailedEvent) — no-fill, timeout, network...</summary>
        public const string LOAD_ERROR = "load_error";

        /// <summary>ErrorCode khi gọi hiển thị thất bại (OnAdDisplayFailedEvent).</summary>
        public const string SHOW_ERROR = "show_error";
    }
}
