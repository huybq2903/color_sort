/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
 */

using Falcon.Modules.Core.Network;

namespace Game.Shared.Leaderboard
{
    /// <summary>Bảng xếp hạng của một cặp category/type. Tên sự kiện phải giữ nguyên, server không đổi.</summary>
    [FAMessage("sc_get_lb_data_compress")]
    public class SCGetLBData : SCMessage
    {
        public string leaderboardCategoryStr;
        public string leaderboardTypeStr;
        public string rows;      // List data, kiểu tuỳ leaderboard
        public string bonusData; // Data bổ sung, kiểu tuỳ leaderboard

        // Không cache ở đây: LeaderboardService tự giữ theo request nó gửi đi
        public override void OnData() { }
    }
}
