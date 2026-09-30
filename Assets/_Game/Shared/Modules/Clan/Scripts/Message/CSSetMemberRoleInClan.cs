using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_clan_set_role")]
    public class CSSetMemberRoleInClan : CSMessageWaitLoginSuccess
    {
        public int code;
        public int role;
        public string bonusData;

        public CSSetMemberRoleInClan(int code, ClanRole role)
        {
            this.code = code;
            this.role = (int)role;
        }
    }
}
