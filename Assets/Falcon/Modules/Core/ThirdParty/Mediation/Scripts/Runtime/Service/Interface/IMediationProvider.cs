/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-21
 */

using System;
using Falcon.Helpers.Devkit;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public interface IMediationProvider
    {
        int Priority { get; }
        string Name { get; }
        bool CanShowAppOpen(string placementId);
        void InitFromManager();
        void ShowBanner(string placement);

        void ShowInterstitial(
            string placementId, Action onInterstitialClosed = null, Action onFail = null, bool needAdBreak = false);

        void AdsClosed(AdType adType, string placementId);

        void ShowRewardedVideo(
            string placementId, Action onRewardedComplete = null, Action onFail = null,
            bool showInterstitialInstead = true);

        public StatusAds GetStatusAdsFromPlacement(string placementId, bool isRewarded = true);
        public RewardedCallback GetRewardedCallbackFromGroupId(string groupId);
        public int GetMaxViewByGroupId(string groupId);
        public int GetViewCountByGroupId(string groupId);
    }
}