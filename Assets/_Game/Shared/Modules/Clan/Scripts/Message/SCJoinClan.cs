using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("sc_join_clan")]
    public class SCJoinClan : SCMessage
    {
        public int clan_code;
        public string bonusData;

        public override void OnData()
        {
            Center.GetOrCreate<ClanService>().OnJoinClanReceived(clan_code);
        }
    }
}
