/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    [Serializable]
    public class ConfigRequest
    {
        public string runningAbTesting;

        public string packageName;

        public Dictionary<string, object> abTestingConfigs;

        public Dictionary<string, bool> campaignMeta;

        public Dictionary<string, object> properties;

    }
}