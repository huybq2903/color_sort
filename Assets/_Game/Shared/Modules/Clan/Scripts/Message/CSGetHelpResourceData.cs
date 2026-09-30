using Falcon;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_help_resource_data_in_clan")]
    public class CSGetHelpResourceData : CSMessageWaitLoginSuccess
    {
        public CSGetHelpResourceData() { }
    }
}