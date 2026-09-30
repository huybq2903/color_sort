/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-24
 */

using Falcon.Modules.Core.AccountData;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Runtime
{
    [FGameDataType("appsflyer_data")]
    public class GameData4Appsflyer : FGameData<GameData4Appsflyer>
    {
        public string appsflyerID;
        public string adGroupID;
        public string origCost;
        public string afCostCurrency;
        public bool isFirstLaunch;
        public string campaignID;
        public string campaignName;
        public string afCid;
        public string mediaSource;
        public string advertisingID;
        public string afStatus;
        public double costCentsUsd;
        public double afCostValue;
        public string afCostModel;
        public string afAD;
        public bool isRetargeting;
        public string adgroup;
    }
}