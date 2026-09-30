using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("sc_leave_clan")]
    public class SCLeaveClan : SCMessage
    {
        public string bonusData;
        public override void OnData()
        {
            Center.GetOrCreate<ClanService>().OnLeaveClanReceived();
        }
    }
}
