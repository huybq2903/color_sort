/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-17
 */

using System;
using Falcon.Helpers.Devkit;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public interface IAdsService
    {
        void Init(Action actionFromUser = null);

        bool InternalCanShowAppOpen(string placementId);

        void UnregisterEvent();

        void LoadBanner();
        void LoadCollapsibleBanner();
        void ShowBanner(string placementId);
        bool IsBannerReady();
        void ShowCollapsibleBanner();
        void HideBanner(bool fromInit = false);
        void HideCollapsibleBanner();

        void IgnoreNextAoa();

        void LoadInterstitial();
        bool IsInterstitialReady();

        void ShowInterstitial(
            string where, Action onInterstitialClosed = null, Action onFail = null,
            bool needAdBreak = false);

        bool IsRewardedVideoReady();
        float GetBannerHeightInPixels();

        void ShowRewardedVideo(
            string where, Action onRewardedComplete = null, Action onFail = null,
            bool showInterstitialInstead = true);

        public struct AdInfo
        {
            public readonly string placementId;

            public AdInfo(string placementId)
            {
                this.placementId = placementId;
            }
        }
    }
}