using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_clan_remove_member")]
    public class CSRemoveMemberInClan : CSMessageWaitLoginSuccess
    {
        public int member_code;
        public string bonusData;

        public CSRemoveMemberInClan(int member_code)
        {
            this.member_code = member_code;
        }
    }
}
