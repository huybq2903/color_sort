/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-30
 */

using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FAMessage("cs_multi_call_ads_data")]
    public class CSMultiCallAdsData : CSMessageWaitLoginSuccess
    {
        public string platform; //"android", "ios"
        public int mediationType; //0 : max, 1 : ironsource, 2 : admob

        public CSMultiCallAdsData(string platform, int mediationType)
        {
            this.platform = platform;
            this.mediationType = mediationType;
        }
    }
}