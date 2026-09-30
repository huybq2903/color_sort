using Falcon;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_receive_help_resource_in_clan")]
    public class CSReceiveHelpResource : CSMessageWaitLoginSuccess
    {
        public int player_code;
        public CSReceiveHelpResource(int player_code)
        {
            this.player_code = player_code;
        }
    }
}