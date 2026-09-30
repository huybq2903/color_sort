using Falcon.Modules.Core.Network;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("sc_clan_of_users")]
    public class SCClanOfUsers : SCMessage
    {
        public List<ClanDataShort> clanInfos = new List<ClanDataShort>();
        public override void OnData()
        {
            ///
        }
    }
}
