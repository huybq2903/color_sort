using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_clan_config")]
    public class CSClanConfig : CSMessageWaitLoginSuccess
    {
        public CSClanConfig()
        {

        }
    }
}
