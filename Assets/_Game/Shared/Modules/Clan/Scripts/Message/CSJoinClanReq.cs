using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_join_clan_req")]
    public class CSJoinClanReq : CSMessageWaitLoginSuccess
    {
        public int clan_code;
        public string bonusData;

        public CSJoinClanReq(int clan_code)
        {
            this.clan_code = clan_code;
        }
    }
}
