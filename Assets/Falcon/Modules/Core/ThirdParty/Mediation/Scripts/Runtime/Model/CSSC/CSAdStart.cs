/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 */
// 2025-09-18


using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FAMessage(("cs_ad_start"))]
    public class CSAdStart : CSMessageWaitLoginSuccess
    {
        public string adId;
        public string adType;
        public string adWhere;
        public string adNetwork;
        public string adMediation;
        public string adUnitId;

        public CSAdStart(string adId, string adType, string adWhere, string adNetwork, string adMediation, string adUnitId)
        {
            this.adId = adId;
            this.adType = adType;
            this.adWhere = adWhere;
            this.adNetwork = adNetwork;
            this.adMediation = adMediation;
            this.adUnitId = adUnitId;
        }
    }
}