/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 */
// 2025-04-12


using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FAMessage(("cs_ad_info_v2"))]
    public class CSAdInfo : CSMessageWaitLoginSuccess
    {
        public string adId;
        public string adPlacement;
        public string adType;
        public string adNetwork;
        public string adMediation;
        public int adTime;
        public double adLtv;

        public CSAdInfo(
            string adId, string adPlacement, string adType, string adNetwork, string adMediation, int adTime,
            double adLtv)
        {
            this.adId = adId;
            this.adPlacement = adPlacement;
            this.adType = adType;
            this.adNetwork = adNetwork;
            this.adMediation = adMediation;
            this.adTime = adTime;
            this.adLtv = adLtv;
        }
    }
}