/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Vocab bước funnel chuẩn (§D6 + §D9). Game thêm bước riêng sau các bước lõi thoải mái,
    /// nhưng bước lõi phải đúng tên này thì mới so được cross-game.
    /// </summary>
    public static class FFunnelAction
    {
        // ---- 3 bước lõi của funnel ftue (§D6) ----
        /// <summary>Vào game scene lần đầu (qua loading/menu).</summary>
        public const string SceneEnter = "scene_enter";

        /// <summary>Bắt đầu tutorial.</summary>
        public const string TutorialStart = "tutorial_start";

        /// <summary>Xong tutorial.</summary>
        public const string TutorialComplete = "tutorial_complete";

        // ---- Vocab feature engagement (§D9): battle pass, daily quest, event/season... ----
        /// <summary>
        /// Người chơi tham gia/mở feature. Với funnel LẶP LẠI, đây cũng là mốc mở một "mùa" mới:
        /// SDK ghi lại ngày này làm <c>funnelDay</c> cho các bước sau (server derive
        /// "vào mùa được mấy ngày" từ đó).
        /// </summary>
        public const string Join = "join";

        /// <summary>Đạt một mốc tiến độ (tier/level của feature) — <c>priority</c> = số mốc.</summary>
        public const string Milestone = "milestone";

        /// <summary>Nhận thưởng của một mốc.</summary>
        public const string Claim = "claim";

        /// <summary>Hoàn thành trọn feature/mùa.</summary>
        public const string Complete = "complete";
    }
}
