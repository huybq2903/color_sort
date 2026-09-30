/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-04
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.AccountData;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FGameDataType("mediation_ad_info_data")]
    public class GameData4AdInfo : FGameData<GameData4AdInfo>
    {
        public Dictionary<AdType, AdInfo> adTypeToInfo;
    }

    [Serializable]
    public class AdInfo
    {
        public double ltv;
        public int count;
    }
}