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
    public class MAdLog : AFalconLog
    {
        [FKey(Name = nameof(eventName) + "$")] public string eventName;
        public int loadTimes;
        [FKey(Name = nameof(adType) + "$")] public AdType adType;
        [FKey(Name = nameof(adWhere) + "$")] public string adWhere;
        [FKey(RemoveIfNull = true)] public string networkName;
        [FKey(RemoveIfNull = true)] public string mediation;
        [FKey(RemoveIfNull = true)] public double revenue;
        [FKey(RemoveIfNull = true)] public string errorMess;
        [FKey(RemoveIfNull = true)] public string adUnitId;
        [FKey(RemoveIfNull = true)] public string floor;
        [FKey(RemoveIfNull = true)] public double loadingTime;

        [Preserve]
        public MAdLog()
        {
        }

        public MAdLog(string eventName, int loadTimes, AdType adType, string adWhere, string networkName,
            string mediation, double revenue, string errorMess, string adUnitId, string floor, double loadingTime)
        {
            this.eventName = eventName;
            this.loadTimes = loadTimes;
            this.adType = adType;
            this.adWhere = adWhere;
            this.networkName = networkName;
            this.mediation = mediation;
            this.revenue = revenue;
            this.errorMess = errorMess;
            this.adUnitId = adUnitId;
            this.floor = floor;
            this.loadingTime = loadingTime;
        }

        public override string Event => "f_sdk_mo_multiple_floor_adunit";
    }
}