/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
 */

using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;

namespace Game.Shared.Leaderboard
{
    /// <summary>Config của leaderboard; về một lần sau khi đăng nhập.</summary>
    [FAMessage("sc_get_config_lb")]
    public class SCGetConfigLB : SCMessage
    {
        public int levelUnlock;
        public int refreshInterval;             // giây, chu kỳ tự lấy lại data
        public int refreshByUserInterval = 10;  // giây, cooldown nút reload

        public override void OnData()
        {
            Center.GetOrCreate<LeaderboardService>()
                .SetConfig(levelUnlock, refreshInterval, refreshByUserInterval);
        }
    }
}
