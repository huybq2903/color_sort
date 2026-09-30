/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-18
using System.Collections.Generic;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.Core.AccountData
{
    [FAMessage("cs_remote_config_rsp")]
    public class CSRemoteConfigRsp : CSMessage
    {
        public Dictionary<string, object> remote_configs;
        public CSRemoteConfigRsp(Dictionary<string, object> remote_configs)
        {
            this.remote_configs = remote_configs;
        }
    }
}
