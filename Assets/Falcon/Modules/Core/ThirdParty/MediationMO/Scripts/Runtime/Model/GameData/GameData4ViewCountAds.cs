/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-03
 */

using System;
using System.Collections.Generic;
using Falcon.Modules.Core.AccountData;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FGameDataType("mediation_mo_ad_info")]
    public class GameData4MOInfo : FGameData<GameData4MOInfo>
    {
        public Dictionary<string, AdsMoInfo> viewCountInfo; //key = group_id
    }

    [Serializable]
    public class AdsMoInfo
    {
        public string group;
        public string dateTime; //yyyy-MM-dd
        public int viewCount;
    }
}