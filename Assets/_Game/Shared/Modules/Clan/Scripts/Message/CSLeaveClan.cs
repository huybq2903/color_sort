using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_leave_clan")]
    public class CSLeaveClan : CSMessageWaitLoginSuccess
    {
        public string bonusData;
        public CSLeaveClan()
        {

        }
    }
}
