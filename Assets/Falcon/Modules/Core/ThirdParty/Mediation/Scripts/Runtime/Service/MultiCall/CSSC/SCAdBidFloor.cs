/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 */
// 2025-03-10

using System.Collections.Generic;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FAMessage("sc_ad_bid_floor")]
    public class SCAdBidFloor : SCMessage
    {
        public List<AdBidFloor> adBidFloors;

        public override void OnData()
        {
            if (adBidFloors.Count > 0)
                MultiCall.Instance.ListAdBidFloors = adBidFloors;
        }
    }

    public class AdBidFloor
    {
        public string adUnitId; //ad unit id
        public bool rewarded; //true : là rewarded, false : là interstitial
        public float multiplier; //hệ số nhân cho floor
        public string timeConfig;
        public List<int> algorithmIds;
    }
}