/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-21
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.MediationMO.Runtime
{
    public class SOAdsMOSetting : ScriptableObject
    {
        [FoldoutGroup("Settings Ads MO", expanded: true), PropertyOrder(1)]
        [InfoBox("📘 Tham khảo hướng dẫn cấu hình tại liên kết bên dưới")]
        [FoldoutGroup("Settings Ads MO")]
        [Button("Mở link tham khảo cấu hình", ButtonSizes.Small)]
        private void OpenDocLink() =>
            Application.OpenURL(
                "https://docs.google.com/spreadsheets/d/1Cq2PCDwv8yH6K3H9uwree1JdRGIVlOTqErJfw5AaKS4/edit?gid=1073199400#gid=1073199400");

        [FoldoutGroup("Settings Ads MO", expanded: true), PropertyOrder(2)]
        [ShowInInspector]
        [InlineProperty]
        [LabelText("Config")]
        public List<ListAdsGroupConfigDto> list;

        [HideInInspector] public string configAds;

        public string ConfigAds
        {
            get
            {
                var dict = list.ToDictionary(t => t.group, t => t);
                var a = JsonConvert.SerializeObject(dict);
                return a;
            }
        }

        [Serializable]
        public class ListAdsGroupConfigDto
        {
            public string group;
            public List<ListAdsConfigDto> configAds;
            public List<AdsReward> adsRewards;
            public int maxTotalAds; //số lần xem tối đa ads trong group
        }

        [Serializable]
        public class ListAdsConfigDto
        {
            public string placementId;
            public AdsConfigV3 adsConfigV3;
        }

        [Serializable]
        public class AdsGroupConfig
        {
            public string group;
            public Dictionary<string, AdsConfigV3> configAds; //key = placement id
            public List<AdsReward> adsRewards;
            public int maxTotalAds; //số lần xem tối đa ads trong group
        }

        [Serializable]
        public class AdsConfigV3
        {
            public int levelUnlock = -1; //level unlock
            public bool isActive = true; //true : bật; false : tắt
            public int sessionLimit = -1; //trong một session thì quảng cáo này hiện bao lần
            public int dayLimit = -1; //trong một ngày thì quảng cáo này được hiện bao lần
            public int levelLimit = -1; //trong một level, một lượt chơi, thì quảng cáo này hiện bao lần

            public float
                intervalTime = -1; //đơn vị giây, khoảng thời gian tối thiểu giữa 2 lần xem quảng cáo tại id này

            public int intervalLevel = -1; //level tối thiểu giữa 2 lần xem quảng cáo tại id này

            public float
                intervalBetweenIv = 60; //đơn vị giây, thời gian min sau khi xem xong rewarded thì xem đc inters
        }

        [Serializable]
        public class AdsReward
        {
            public int fromViewCount;
            public int toViewCount;
            public List<Reward> rewards;
        }

        [Serializable]
        public class AdsManager
        {
            public AdsGroupConfig adsGroupConfig;
            public AdsGroupValue adsGroupValue;
        }

        [Serializable]
        public class AdsGroupValue
        {
            public string group;
            public Dictionary<string, AdsValue> dictionaryAdsValue; //key = placement id
            public int viewCount; //số lần xem quảng cáo của group
            public long remainingTime; //thời gian còn lại đến khi reset số đếm
            public DateTime timeServer; //thời gian server khi nhận bản tin
        }

        [Serializable]
        public class AdsValue
        {
            public int sessionCount; //số lần đã hiện quảng cáo trong một session
            public int dayCount; //số lần đã hiện quảng cáo này trong một ngày
            public DateTime currentDay; //ngày hiện quảng cáo
            public int levelCount; //số lần đã hiện quảng cáo trong một level
            public int level; //level hiện quảng cáo
            public DateTime timeClosed; //khoảng thời gian tối thiểu giữa 2 lần xem quảng cáo tại id này
        }
    }
}