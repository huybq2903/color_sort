/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
#if ADJUST_ENABLE
using AdjustSdk;
#endif
using UnityEngine.Scripting;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Runtime
{
#if ADJUST_ENABLE
    [Serializable]
    public class AdjustAttributionExtends : AdjustAttribution
    {
        [Preserve]
        public AdjustAttributionExtends()
        {
        }

        public AdjustAttributionExtends(AdjustAttribution data)
        {
            Network = data.Network;
            Adgroup = data.Adgroup;
            Campaign = data.Campaign;
            Creative = data.Creative;
            ClickLabel = data.ClickLabel;
            TrackerName = data.TrackerName;
            TrackerToken = data.TrackerToken;
            CostType = data.CostType;
            CostAmount = data.CostAmount;
            CostCurrency = data.CostCurrency;
            FbInstallReferrer = data.FbInstallReferrer;
        }
    }
#endif
}