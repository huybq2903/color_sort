using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Leaderboard
{
    [FAMessage("cs_get_category_data_lb")]
    public class CSGetCategoryDataLB : CSMessageWaitLoginSuccess
    {
        public string leaderboardCategoryStr;

        public CSGetCategoryDataLB(string leaderboardCategory)
        {
            this.leaderboardCategoryStr = leaderboardCategory;
        }
    }
}
