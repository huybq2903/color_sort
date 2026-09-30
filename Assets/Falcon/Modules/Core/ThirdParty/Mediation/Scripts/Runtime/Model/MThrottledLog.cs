/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.BigData;
using UnityEngine.Scripting;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [Serializable]
    public class MThrottledLog : AFalconLog
    {
        [FKey(Name = nameof(ram) + "$")] public float ram;
        [FKey(Name = nameof(maxAds) + "$")] public int maxAds;
        [FKey(Name = nameof(optionAb) + "$")] public int optionAb;

        [Preserve]
        public MThrottledLog()
        {
        }

        public MThrottledLog(float ram, int maxAds, int optionAb)
        {
            this.ram = ram;
            this.maxAds = maxAds;
            this.optionAb = optionAb;
        }

        public override string Event => "f_sdk_mo_throttled";
    }
}