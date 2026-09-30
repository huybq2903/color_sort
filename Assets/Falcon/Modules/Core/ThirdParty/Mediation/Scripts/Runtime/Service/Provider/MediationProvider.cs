/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class MediationProvider : IMediationProvider
    {
        private static MediationProvider _instance;
        public static MediationProvider Instance => _instance ??= new MediationProvider();

        private bool _initialized;

        public int Priority => 0;
        public string Name => "MediationProvider";

        private MediationProvider()
        {
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            MediationManager.Instance.Register(_instance);
        }


        public bool CanShowAppOpen(string placementId)
        {
            return MediationManager.Instance.GetAdsService().InternalCanShowAppOpen(placementId);
        }

        public void InitFromManager()
        {
        }

        public void ShowBanner(string placement)
        {
            MediationManager.Instance.GetAdsService().ShowBanner(placement);
        }

        public void ShowInterstitial(
            string placementId, Action onInterstitialClosed = null, Action onFail = null, bool needAdBreak = false)
        {
            MediationManager.Instance.GetAdsService()
                .ShowInterstitial(placementId, onInterstitialClosed, onFail, needAdBreak);
        }

        public void AdsClosed(AdType adType, string placementId)
        {
        }

        public void ShowRewardedVideo(
            string placementId, Action onRewardedComplete = null, Action onFail = null,
            bool showInterstitialInstead = true)
        {
            MediationManager.Instance.GetAdsService()
                .ShowRewardedVideo(placementId, onRewardedComplete, onFail, showInterstitialInstead);
        }

        public StatusAds GetStatusAdsFromPlacement(string placementId, bool isRewarded = true)
        {
            return new StatusAds { statusShowAds = StatusShowAds.Active };
        }

        public int GetMaxViewByGroupId(string groupId)
        {
            return -1;
        }

        public int GetViewCountByGroupId(string groupId)
        {
            return -1;
        }

        public RewardedCallback GetRewardedCallbackFromGroupId(string groupId)
        {
            return new RewardedCallback
            {
                listReward = new List<Reward>()
            };
        }
    }

    public static class MediationProviderBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            MediationProvider.Instance.Initialize();
        }
    }
}