/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-18
using System.Collections.Generic;
using Falcon.Modules.Core.Network;
using Falcon.Modules.Core.RemoteConfig;
using UnityEngine;

namespace Falcon.Modules.Core.AccountData
{
    [FAMessage("sc_remote_config_req")]
    public class SCRemoteConfigReq : SCMessage
    {
        public string name;
        public override void OnData()
        {
            if (string.IsNullOrEmpty(name) || name.CompareTo("all") == 0)
                new CSRemoteConfigRsp(FConfigController.Instance.Configs).Send();
            else
            {
                if (FConfigController.Instance.Configs.ContainsKey(name))
                {
                    object value = FConfigController.Instance.Configs[name];
                    new CSRemoteConfigRsp(new Dictionary<string, object>(){{name, value}}).Send();
                }
                else
                {
                    new CSRemoteConfigRsp(new Dictionary<string, object>() { { name, null } }).Send();
                }

            }
        }
    }
}
