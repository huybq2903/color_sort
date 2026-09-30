/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-30
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FAMessage("sc_multi_call_ads_data")]
    public class SCMultiCallAdsData : SCMessage
    {
        public string idsInterstitial;
        public string idsRewarded;

        public override void OnData()
        {
        }
    }
}