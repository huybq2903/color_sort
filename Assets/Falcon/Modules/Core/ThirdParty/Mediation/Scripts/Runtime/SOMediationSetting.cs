/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class SOMediationSetting : ScriptableObject
    {
        [HideInInspector] public string admobAppIdAndroid;
        [HideInInspector] public string admobAppIdIOS;

        [HideInInspector] public string collapsibleBannerIdGmaAndroid;
        [HideInInspector] public string collapsibleBannerIdGmaIOS;

        [HideInInspector] public string nativeIdGmaAndroid;
        [HideInInspector] public string nativeIdGmaIOS;

        [HideInInspector] public string appOpenIdGmaAndroid;
        [HideInInspector] public string appOpenIdGmaIOS;

        #region ironsource

        [LabelText("Use IronSource")] public bool useIronSource;

        [ShowIf(nameof(useIronSource))]
        [FoldoutGroup("Settings IronSource", expanded: true), PropertyOrder(0)]
        [BoxGroup("Settings IronSource/App Key")]
        [LabelText("  Android")]
        public string ironSourceAppKeyAndroid;

        [ShowIf(nameof(useIronSource))] [BoxGroup("Settings IronSource/App Key"), PropertyOrder(0)] [LabelText("  IOS")]
        public string ironSourceAppKeyIOS;

        [ShowIf(nameof(useIronSource))]
        [BoxGroup("Settings IronSource/Admob Id"), PropertyOrder(1)]
        [ShowInInspector]
        [InlineProperty]
        [LabelText("  Android")]
        public string AdmobIdAndroid
        {
            get => admobAppIdAndroid;
            set => admobAppIdAndroid = value;
        }

        [ShowIf(nameof(useIronSource))]
        [BoxGroup("Settings IronSource/Admob Id"), PropertyOrder(1)]
        [ShowInInspector]
        [InlineProperty]
        [LabelText("  IOS")]
        // [ShowIf(nameof(IsLevelPlay))]
        public string AdmobIdIOS
        {
            get => admobAppIdIOS;
            set => admobAppIdIOS = value;
        }

        [ShowIf(nameof(useIronSource))]
        [FoldoutGroup("Settings IronSource/Interstitial Id"), PropertyOrder(2)]
        [LabelText("  Android")]
        // [ShowIf(nameof(IsLevelPlay))]
        public string interstitialIdAndroid;

        [ShowIf(nameof(useIronSource))]
        [FoldoutGroup("Settings IronSource/Interstitial Id"), PropertyOrder(2)]
        [LabelText("  IOS")]
        public string interstitialIdIOS;

        [ShowIf(nameof(useIronSource))]
        [FoldoutGroup("Settings IronSource/Rewarded Id"), PropertyOrder(3)]
        [LabelText("  Android")]
        public string rewardedIdAndroid;

        [ShowIf(nameof(useIronSource))]
        [FoldoutGroup("Settings IronSource/Rewarded Id"), PropertyOrder(3)]
        [LabelText("  IOS")]
        public string rewardedIdIOS;

        [ShowIf(nameof(useIronSource))]
        [FoldoutGroup("Settings IronSource/Banner Id"), PropertyOrder(4)]
        [LabelText("  Android")]
        public string bannerIdAndroid;

        [ShowIf(nameof(useIronSource))]
        [FoldoutGroup("Settings IronSource/Banner Id"), PropertyOrder(4)]
        [LabelText("  iOS")]
        public string bannerIdIOS;

        [FoldoutGroup("Settings IronSource/MREC Id (Optional)"), PropertyOrder(6)]
        [LabelText("  Android")]
        // [ShowIf(nameof(IsLevelPlay))]
        //tạm ẩn
        [HideInInspector]
        public string mrecIdAndroid;

        [FoldoutGroup("Settings IronSource/MREC Id (Optional)"), PropertyOrder(6)]
        [LabelText("  IOS")]
        // [ShowIf(nameof(IsLevelPlay))]
        //tạm ẩn
        [HideInInspector]
        public string mrecIdIOS;

        [FoldoutGroup("Adapter"), PropertyOrder(9)]
        [LabelText("Auto Install Adapter")]
        // [ShowIf(nameof(IsLevelPlay))]
        //tạm ẩn
        [HideInInspector]
        public bool installAdapterIrs;

        public bool UseBannerIrs
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(bannerIdAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(bannerIdIOS);
#endif
                return false;
            }
        }


        public bool UseMrecIrs
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(mrecIdAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(mrecIdIOS);
#endif
                return false;
            }
        }


        public bool UseInterstitialIrs
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(interstitialIdAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(interstitialIdIOS);
#endif
                return false;
            }
        }

        public bool UseRewardedIrs
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(rewardedIdAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(rewardedIdIOS);
#endif
                return false;
            }
        }

        public bool UseAppOpenIrs
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(appOpenIdGmaAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(appOpenIdGmaIOS);
#endif
                return false;
            }
        }

        #endregion

        #region max

        [LabelText("Use Max")] public bool useMax;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max", expanded: true), PropertyOrder(0)]
        [BoxGroup("Settings Max/Max Sdk")]
        [LabelText("Key")]
        [ReadOnly]
        public string appLovinSdkKey =
            "M4GLwqezVT2WDo75OWFGOV873pVg6-3S3Kpz8Rxe_-9CnHI9oXPB2TI5LpnRnqvr8hpH8kw7i4KTMcc891KCad";

        [ShowIf(nameof(useMax))]
        [BoxGroup("Settings Max/Admob Id"), PropertyOrder(1)]
        [ShowInInspector]
        [InlineProperty]
        [LabelText("  Android")]
        public string AdmobIdMaxAndroid
        {
            get => admobAppIdAndroid;
            set => admobAppIdAndroid = value;
        }

        [ShowIf(nameof(useMax))]
        [BoxGroup("Settings Max/Admob Id"), PropertyOrder(1)]
        [ShowInInspector]
        [InlineProperty]
        [LabelText("  IOS")]
        public string AdmobIdMaxIOS
        {
            get => admobAppIdIOS;
            set => admobAppIdIOS = value;
        }

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/Interstitial Id"), PropertyOrder(2)]
        [LabelText("  Android")]
        public string interstitialIdMaxAndroid;

        [ShowIf(nameof(useMax))] [FoldoutGroup("Settings Max/Interstitial Id"), PropertyOrder(2)] [LabelText("  IOS")]
        public string interstitialIdMaxIOS;

        [ShowIf(nameof(useMax))] [FoldoutGroup("Settings Max/Rewarded Id"), PropertyOrder(3)] [LabelText("  Android")]
        public string rewardedIdMaxAndroid;

        [ShowIf(nameof(useMax))] [FoldoutGroup("Settings Max/Rewarded Id"), PropertyOrder(3)] [LabelText("  IOS")]
        public string rewardedIdMaxIOS;

        [ShowIf(nameof(useMax))] [FoldoutGroup("Settings Max/App Open Id"), PropertyOrder(4)] [LabelText("  Android")]
        public string AppOpenIdMaxAndroid;

        [ShowIf(nameof(useMax))] [FoldoutGroup("Settings Max/App Open Id"), PropertyOrder(4)] [LabelText("  IOS")]
        public string AppOpenIdMaxIOS;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/App Open Id"), PropertyOrder(4)]
        [LabelText("  Cooldown time AOA (s)")]
        [InfoBox("Cooldown time between two App Open ad views")]
        public float cooldownTimeMax = 60;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/App Open Id"), PropertyOrder(4)]
        [LabelText("  Minimum level to show AOA")]
        [InfoBox("minimum level can show AOA")]
        public int minLevelShowAoaMax;

        [ShowIf(nameof(useMax))] [FoldoutGroup("Settings Max/Banner Id"), PropertyOrder(5)] [LabelText("  Android")]
        public string bannerIdMaxAndroid;

        [ShowIf(nameof(useMax))] [FoldoutGroup("Settings Max/Banner Id"), PropertyOrder(5)] [LabelText("  IOS")]
        public string bannerIdMaxIOS;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/Banner Id"), PropertyOrder(5)]
        [LabelText("Use Button Close")]
        [InfoBox("Show button close on top right Banner")]
        public bool useButtonCloseBanner;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/App Amazon Id (Optional)"), PropertyOrder(6)]
        [LabelText("  Android")]
        public string appIdAmazonMaxAndroid;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/App Amazon Id (Optional)"), PropertyOrder(6)]
        [LabelText("  IOS")]
        public string appIdAmazonMaxIOS;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/Banner Amazon Id (Optional)"), PropertyOrder(6.5f)]
        [LabelText("  Android")]
        public string bannerIdAmazonMaxAndroid;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/Banner Amazon Id (Optional)"), PropertyOrder(6.5f)]
        [LabelText("  IOS")]
        public string bannerIdAmazonMaxIOS;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/Mrec Ads Id (Optional)"), PropertyOrder(7)]
        [LabelText("  Android")]
        //tạm ẩn
        [HideInInspector]
        public string mrecAdsIdMaxAndroid;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/Mrec Ads Id (Optional)"), PropertyOrder(7)]
        [LabelText("  IOS")]
        //tạm ẩn
        [HideInInspector]
        public string mrecAdsIdMaxIOS;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/Mrec Ads Amazon Id (Optional)"), PropertyOrder(8)]
        [LabelText("  Android")]
        //tạm ẩn
        [HideInInspector]
        public string mrecAdsIdAmazonMaxAndroid;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Settings Max/Mrec Ads Amazon Id (Optional)"), PropertyOrder(8)]
        [LabelText("  IOS")]
        //tạm ẩn
        [HideInInspector]
        public string mrecAdsIdAmazonMaxIOS;

        [ShowIf(nameof(useMax))]
        [FoldoutGroup("Adapter"), PropertyOrder(9)]
        [LabelText("Auto Install Adapter")]
        //tạm ẩn
        [HideInInspector]
        public bool installAdapterMax;

        public bool UseBannerMax
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(bannerIdMaxAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(bannerIdMaxIOS);
#endif
                return false;
            }
        }

        public bool UseMrecMax
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(mrecAdsIdMaxAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(mrecAdsIdMaxIOS);
#endif
                return false;
            }
        }

        public bool UseInterstitialMax
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(interstitialIdMaxAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(interstitialIdMaxIOS);
#endif
                return false;
            }
        }

        public bool UseRewardedMax
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(rewardedIdMaxAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(rewardedIdMaxIOS);
#endif
                return false;
            }
        }

        public bool UseAppOpenMax
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(AppOpenIdMaxAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(AppOpenIdMaxIOS);
#endif
                return false;
            }
        }

        #endregion

        #region GMA

        [HideInEditorMode] [LabelText("Use GMA")]
        public bool useGma;

        [FoldoutGroup("Settings Google Mobile Ads", expanded: true), PropertyOrder(0)]
        [BoxGroup("Settings Google Mobile Ads/Admob Id")]
        [ShowInInspector]
        [InlineProperty]
        [LabelText("  Android")]
        [ShowIf(nameof(useGma))]
        public string AdmobIdGmaAndroid
        {
            get => admobAppIdAndroid;
            set => admobAppIdAndroid = value;
        }

        [BoxGroup("Settings Google Mobile Ads/Admob Id"), PropertyOrder(0)]
        [ShowInInspector]
        [InlineProperty]
        [LabelText("  IOS")]
        [ShowIf(nameof(useGma))]
        public string AdmobIdGmaIOS
        {
            get => admobAppIdIOS;
            set => admobAppIdIOS = value;
        }

        [FoldoutGroup("Settings Google Mobile Ads/Interstitial Id"), PropertyOrder(1)]
        [ShowIf(nameof(useGma))]
        [LabelText("  Android")]
        public string interstitialIdGAAndroid;

        [FoldoutGroup("Settings Google Mobile Ads/Interstitial Id"), PropertyOrder(1)]
        [ShowIf(nameof(useGma))]
        [LabelText("  IOS")]
        public string interstitialIdGAIOS;

        [ShowIf(nameof(useGma))]
        [FoldoutGroup("Settings Google Mobile Ads/Interstitial Id/Multi Call"), PropertyOrder(1.1f)]
        [LabelText("  string remote config Android")]
        [InfoBox(
            "Example : h13_f94b32dda9b193d2_10,15,20,25,30|h8_81a81b26dca1b3b0_10,15,20,25,30|h5_5c8e5e30dcc8b98c_10")]
        public string interstitialIdMultiCallGAAndroid;

        [ShowIf(nameof(useGma))]
        [FoldoutGroup("Settings Google Mobile Ads/Interstitial Id/Multi Call"), PropertyOrder(1.2f)]
        [LabelText("  string remote config IOS")]
        public string interstitialIdMultiCallGAIOS;

        [FoldoutGroup("Settings Google Mobile Ads/Rewarded Id"), PropertyOrder(2)]
        [ShowIf(nameof(useGma))]
        [LabelText("  Android")]
        public string rewardedIdGAAndroid;

        [FoldoutGroup("Settings Google Mobile Ads/Rewarded Id"), PropertyOrder(2)]
        [ShowIf(nameof(useGma))]
        [LabelText("  IOS")]
        public string rewardedIdGAIOS;

        [ShowIf(nameof(useGma))]
        [FoldoutGroup("Settings Google Mobile Ads/Rewarded Id/Multi Call"), PropertyOrder(2.1f)]
        [LabelText("  string remote config Android")]
        [InfoBox(
            "Example : h10_35bb2eb5a779cc71_10,15,20,25,30|h5_d388043caaf710bc_10,15,20,25,30|h2_da48080ebee12ff4_10")]
        public string rewardedIdMultiCallGAAndroid;

        [ShowIf(nameof(useGma))]
        [FoldoutGroup("Settings Google Mobile Ads/Rewarded Id/Multi Call"), PropertyOrder(2.2f)]
        [LabelText("  string remote config IOS")]
        public string rewardedIdMultiCallGAIOS;

        [FoldoutGroup("Settings Google Mobile Ads/AppOpen Id"), PropertyOrder(3)]
        [ShowIf(nameof(useGma))]
        [LabelText("  Android")]
        public string appOpenIdGAAndroid;

        [FoldoutGroup("Settings Google Mobile Ads/AppOpen Id"), PropertyOrder(3)]
        [ShowIf(nameof(useGma))]
        [LabelText("  IOS")]
        public string appOpenIdGAIOS;

        [FoldoutGroup("Settings Google Mobile Ads/AppOpen Id"), PropertyOrder(3.1f)]
        [ShowIf(nameof(useGma))]
        [LabelText("  Cooldown time AOA (s)")]
        [InfoBox("Cooldown time between two App Open ad views")]
        public float cooldownTimeGa = 60;

        [FoldoutGroup("Settings Google Mobile Ads/Banner Id"), PropertyOrder(4)]
        [ShowIf(nameof(useGma))]
        [LabelText("  Android")]
        public string bannerIdGAAndroid;

        [FoldoutGroup("Settings Google Mobile Ads/Banner Id"), PropertyOrder(4)]
        [ShowIf(nameof(useGma))]
        [LabelText("  IOS")]
        public string bannerIdGAIOS;

        [FoldoutGroup("Settings Google Mobile Ads/MREC Id (Optional)"), PropertyOrder(5)]
        [ShowIf(nameof(useGma))]
        //tạm ẩn
        [HideInInspector]
        [LabelText("  Android")]
        public string mrecAdsGAAndroid;

        [FoldoutGroup("Settings Google Mobile Ads/MREC Id (Optional)"), PropertyOrder(5)]
        [ShowIf(nameof(useGma))]
        //tạm ẩn
        [HideInInspector]
        [LabelText("  IOS")]
        public string mrecAdsGAIOS;

        [FoldoutGroup("Settings Google Mobile Ads/Collapsible Banner (GMA - Optional)"), PropertyOrder(6)]
        //tạm ẩn
        // [ShowInInspector]
        [InlineProperty]
        [LabelText("  Android")]
        [ShowIf(nameof(useGma))]
        public string CollapsibleBannerIdAndroid
        {
            get => collapsibleBannerIdGmaAndroid;
            set => collapsibleBannerIdGmaAndroid = value;
        }

        [FoldoutGroup("Settings Google Mobile Ads/Collapsible Banner (GMA - Optional)"), PropertyOrder(6)]
        //tạm ẩn
        // [ShowInInspector]
        [InlineProperty]
        [LabelText("  IOS")]
        [ShowIf(nameof(useGma))]
        public string CollapsibleBannerIdIOS
        {
            get => collapsibleBannerIdGmaIOS;
            set => collapsibleBannerIdGmaIOS = value;
        }

        [FoldoutGroup("Settings Google Mobile Ads/Native Ads (GMA - Optional)"), PropertyOrder(7)]
        //tạm ẩn
        // [ShowInInspector]
        [InlineProperty]
        [LabelText("  Android")]
        [ShowIf(nameof(useGma))]
        public string NativeIdAndroid
        {
            get => nativeIdGmaAndroid;
            set => nativeIdGmaAndroid = value;
        }

        [FoldoutGroup("Settings Google Mobile Ads/Native Ads (GMA - Optional)"), PropertyOrder(7)]
        //tạm ẩn
        // [ShowInInspector]
        [InlineProperty]
        [LabelText("  IOS")]
        [ShowIf(nameof(useGma))]
        public string NativeIdIOS
        {
            get => nativeIdGmaIOS;
            set => nativeIdGmaIOS = value;
        }

        [FoldoutGroup("Adapter"), PropertyOrder(9)]
        [LabelText("Auto Install Adapter")]
        [ShowIf(nameof(useGma))]
        //tạm ẩn
        [HideInInspector]
        public bool installAdapterGma;

        public bool UseBannerGa
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(bannerIdGAAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(bannerIdGAIOS);
#endif
                return false;
            }
        }

        public bool UseCollapsibleBannerGa
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(collapsibleBannerIdGmaAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(collapsibleBannerIdGmaIOS);
#endif
                return false;
            }
        }

        public bool UseNativeAdsGa
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(nativeIdGmaAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(nativeIdGmaIOS);
#endif
                return false;
            }
        }

        public bool UseMrecAdsGa
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(mrecAdsGAAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(mrecAdsGAIOS);
#endif
                return false;
            }
        }

        public bool UseInterstitialGa
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(interstitialIdGAAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(interstitialIdGAIOS);
#endif
                return false;
            }
        }

        public bool UseRewardedGa
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(rewardedIdGAAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(rewardedIdGAIOS);
#endif
                return false;
            }
        }

        public bool UseAppOpenGa
        {
            get
            {
#if UNITY_ANDROID
                return !string.IsNullOrWhiteSpace(appOpenIdGAAndroid);
#elif UNITY_IOS
                return !string.IsNullOrWhiteSpace(appOpenIdGAIOS);
#endif
                return false;
            }
        }

        #endregion

        [InfoBox(
            "For the case where you want to manually initialize the mediation. Disable Automatic Initialization.")]
        [LabelText("Manual Init Mediation")]
        public bool manualInit;
    }
}