using Falcon.Modules.Core.Network;
using System.Collections.Generic;
using Falcon.Helpers.EventBus;

namespace Falcon.Modules.Core.ThirdParty.MediationMO.Runtime
{
    [FAMessage("sc_ads_mo_config_v3")]
    public class SCAdsMoConfigV3 : SCMessage
    {
        public const string RECEIVED_ADS_CONFIG = "received_ads_config";
        public Dictionary<string, SOAdsMOSetting.AdsGroupConfig> adsGroupConfigs;

        public override void OnData()
        {
            foreach (var adsGroupConfig in adsGroupConfigs)
            {
                if (adsGroupConfig.Value.configAds == null)
                {
                    return;
                }
            }

            GameEvent<Dictionary<string, SOAdsMOSetting.AdsGroupConfig>>.Emit(RECEIVED_ADS_CONFIG,
                adsGroupConfigs);
        }
    }
}