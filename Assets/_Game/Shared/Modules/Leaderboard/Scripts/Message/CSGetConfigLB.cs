using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Leaderboard
{
    [FAMessage("cs_get_config_lb")]
    public class CSGetConfigLB : CSMessageWaitLoginSuccess
    {
        public CSGetConfigLB()
        {

        }
    }
}
