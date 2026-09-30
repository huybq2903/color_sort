using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_clan_info_v2")]
    public class CSClanInfo : CSMessageWaitLoginSuccess
    {
        public int clan_code;
        public string bonusData;
        public CSClanInfo(int clan_code)
        {
            this.clan_code = clan_code;
        }
    }
}
