/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 */
// 2025-03-10

using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.Core.RemoteConfigCms
{
    [FAMessage("sc_remote_configs")]
    public class SCRemoteConfigs : SCMessage
    {
        public Dictionary<string, object> configs;

        public override void OnData()
        {
            MySingletonService.TryGetInstance(out FConfigInitService configInitService);
            if (configInitService != null) configInitService.Init(configs);
        }
    }
}