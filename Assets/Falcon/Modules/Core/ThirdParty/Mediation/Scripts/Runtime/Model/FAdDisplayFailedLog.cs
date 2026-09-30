/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.BigData;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [Serializable]
    public class FAdDisplayFailedLog : AFalconLog
    {
        [FKey(Name = nameof(type))] public AdType type;
        [FKey(Name = nameof(adWhere))] public string adWhere;
        [FKey(RemoveIfNull = true)] public string adMediation;
        [FKey(RemoveIfNull = true)] public int? currentLevel;

        [Preserve]
        public FAdDisplayFailedLog()
        {
        }

        public FAdDisplayFailedLog(
            AdType type, string adWhere, string adMediation, int? currentLevel = null)
        {
            LogParams(type, adWhere, adMediation, currentLevel);
            this.type = type;
            this.adWhere = adWhere;
            this.adMediation = adMediation;
            this.currentLevel = currentLevel;
        }

        public override string Event => "f_sdk_ads_display_failed_data";
    }
}