/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using Falcon.Modules.Core.RemoteConfigCms;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class MediationConfig : IFalconConfigCms
    {
        public float timeBreak; //thời gian hiện popup "ad break time" trước khi hiện quảng cáo interstitial.
        //Nếu để 0 thì sẽ ko hiện popup.

        //ko lấy được campaign id max chính xác, chỉ lấy được từ is + gg
        public string appsflyerCampaignIdsForIronSource; //campaign Id lấy từ appsflyer
        public string appsflyerCampaignIdsForGoogle; //campaign Id lấy từ appsflyer
        public int mediationDefault = 0; //0: max, 1: ironsource, 2: gma

        public string bamBooAdsThreshold;
        public string bamBoo3AdsThreshold;
        public string taiChi2AdsThreshold;
        public string taiChi4AdsThreshold;

        public MediationConfig()
        {
            var setting = Resources.Load<SOAdsSetting>("SOAdsSetting");
            if (setting == null)
            {
                Debug.LogError(
                    "SOAdsSetting doesn't exists. Go to 'Falcon/Modules/ThirdParty/Ads Settings' to config.");
                return;
            }

            timeBreak = setting.timeBreak;
        }
    }
}