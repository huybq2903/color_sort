using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_random_list_clan")]
    public class CSRandomListClan : CSMessageWaitLoginSuccess
    {
        public string bonusData;
    }
}
