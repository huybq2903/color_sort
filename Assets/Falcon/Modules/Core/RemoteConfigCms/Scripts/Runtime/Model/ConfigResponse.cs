/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    [Serializable]
    public class ConfigResponse
    {
        public Dictionary<string, object> configs;
        public Dictionary<string, object> Configs => configs ??= new Dictionary<string, object>();

        [Preserve]
        public ConfigResponse()
        {
        }
    }
}