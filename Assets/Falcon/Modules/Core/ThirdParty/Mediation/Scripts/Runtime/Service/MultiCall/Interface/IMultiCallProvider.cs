/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using System;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public interface IMultiCallProvider
    {
        int GetIndex();
        bool IsInterstitialReady();
        void ShowInterstitial(string where, Action onRewardedComplete, Action onFailed, bool showForRewarded);
        bool IsRewardedVideoReady();
        void ShowRewardedVideo(string where, Action onRewardedComplete, Action onFailed = null);
        void RegisterEvent();
        void LoadInterstitial();
        void LoadRewarded();
    }
}