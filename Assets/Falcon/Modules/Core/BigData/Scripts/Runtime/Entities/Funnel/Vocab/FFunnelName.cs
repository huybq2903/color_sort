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
    /// Tên funnel ĐÃ ĐĂNG KÝ với hợp đồng (§D6 + §D9). Mỗi game tự đặt tên tự do thì cross-game
    /// mù — bằng chứng: đội pLTV ngoài phải tự đếm lucky_wheel/daily_quest bằng tracking riêng.
    /// <br/>Dùng hằng số ở đây thay vì gõ chuỗi tay; tên ngoài danh sách vẫn gửi được (additive)
    /// nhưng phải đăng ký với loader trước, không thì server không nhặt.
    /// </summary>
    public static class FFunnelName
    {
        /// <summary>First-time user experience (§D6) — funnel MỘT LẦN, các bước đi đúng thứ tự.</summary>
        public const string Ftue = "ftue";

        public const string BattlePass = "battle_pass";
        public const string DailyQuest = "daily_quest";
        public const string PiggyBank = "piggy_bank";
        public const string LuckyWheel = "lucky_wheel";

        /// <summary>Tiền tố cho game event/season theo mùa — xem <see cref="Event"/>.</summary>
        public const string EventPrefix = "event_";

        /// <summary>
        /// Tên funnel cho một loại game event/season (§D9): <c>Event("halloween")</c> → <c>event_halloween</c>.
        /// Danh tính mùa nào-đang-chạy là việc của server (dim_liveops_event + lens thời gian),
        /// KHÔNG nhét số mùa vào tên funnel — làm thế là đẻ tên mới mỗi mùa, cross-season lại mù.
        /// </summary>
        public static string Event(string eventType) => EventPrefix + eventType;
    }
}
