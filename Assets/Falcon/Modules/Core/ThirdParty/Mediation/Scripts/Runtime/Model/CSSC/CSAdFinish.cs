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
    [FAMessage(("cs_ad_finish"))]
    public class CSAdFinish : CSMessageWaitLoginSuccess
    {
        public string adId;
        public string adType;
        public string adWhere;
        public string adNetwork;
        public string adMediation;
        public string adUnitId;
        public double adRev;
        public int adTime; //second

        public CSAdFinish(string adId, string adType, string adWhere, string adNetwork, string adMediation, string adUnitId, double adRev, int adTime)
        {
            this.adId = adId;
            this.adType = adType;
            this.adWhere = adWhere;
            this.adNetwork = adNetwork;
            this.adMediation = adMediation;
            this.adUnitId = adUnitId;
            this.adRev = adRev;
            this.adTime = adTime;
        }
    }
}