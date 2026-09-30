using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("sc_clan_set_role")]
    public class SCSetMemberRoleInClan : SCMessage
    {
        public int role;
        public string bonusData;

        public override void OnData()
        {
            Center.GetOrCreate<ClanService>().RaiseSetMemberRole(this);
        }
    }
}
