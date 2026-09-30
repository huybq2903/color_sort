
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
 */

using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Leaderboard
{
    [FAMessage("cs_get_lb_data_compress")]
    public class CSGetLBData : CSMessageWaitLoginSuccess
    {
        public string leaderboardCategoryStr; // Tự được set trong module, không cần set.
        public string leaderboardTypeStr; // Tự được set trong module, không cần set.
        public string bonusData; // Nếu cần thêm gì gửi lên server, custom ở đây

        public CSGetLBData()
        {
            this.bonusData = "";
        }
    }
}
