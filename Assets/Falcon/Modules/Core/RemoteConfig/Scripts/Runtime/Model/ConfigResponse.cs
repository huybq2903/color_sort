/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    [Serializable]
    public class ConfigResponse
    {
        public string runningAbTesting;

        public string[] abTestingFields;

        public Dictionary<string, bool> campaignMeta;

        public Dictionary<string, object> configs;

        public string[] AbTestingField => abTestingFields ??= Array.Empty<string>();
        
        public Dictionary<string, bool> CampaignMeta => campaignMeta ??= new Dictionary<string, bool>();
        public Dictionary<string, object> Configs => configs ??= new Dictionary<string, object>();

        [Preserve]
        public ConfigResponse()
        {
        }

        public Dictionary<string, object> TestingConfigs()
        {
            var result = new Dictionary<string, object>();

            foreach (var abTestingField in AbTestingField)
                result.Add(abTestingField, Configs.GetValueOrDefault(abTestingField));
            return result;
        }

        public Dictionary<string, object> NonTestConfigs()
        {
            var result = new Dictionary<string, object>();

            foreach (var keyValuePair in Configs)
                if (!AbTestingField.Contains(keyValuePair.Key))
                    result.Add(keyValuePair.Key, keyValuePair.Value);
            return result;
        }
    }
}