using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_search_clan")]
    public class CSSearchClan : CSMessageWaitLoginSuccess
    {
        public string clan_name;
        public string bonusData;
    }
}
