/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using System.Globalization;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    /// <summary>
    /// Module multi call adUnitId for max sdk
    /// </summary>
    public class MultiCallMaxProvider0 : MultiCallMaxProviderParent<MultiCallMaxProvider0>
    {
#if MAX_ENABLE
        protected override int IndexProvider => 0;

        #region rewarded
        protected override void InternalLoadRewardedLoadFailed(string adUnitId)
        {
            var time = 0;
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                var countAttempt = a.countLoadAttempt;
                try
                {
                    if (a.listTimeRetry.Count > countAttempt - 1)
                    {
                        time = a.listTimeRetry[countAttempt - 1];
                    }
                    else if (a.listTimeRetry.Count > 0)
                    {
                        time = a.listTimeRetry[a.listTimeRetry.Count - 1];
                    }
                }
                catch
                {
                    time = 0;
                }
            }

            var retryDelay = (float)time;
            MediationManager.Instance.DebugLog("InternalLoadRewardedLoadFailed delay time : " + retryDelay);
            MultiCall.Instance.DelayCall(retryDelay, adUnitId, LoadRewardedAd);
        }

        protected override void UpdateMultiplier(string adUnitId)
        {
            MultiCall.Instance.DelayCall(1, adUnitId, LoadRewardedAd);
        }

        protected override void InternalLoadRewarded(string adUnitId)
        {
            MediationManager.Instance.DebugLog("Internal Load Rewarded : " + adUnitId);
            MaxSdk.LoadRewardedAd(adUnitId);
        }

        public override void LoadRewarded()
        {
            foreach (var adTypeMultiCall in MultiCall.Instance.dictionaryRewarded)
            {
                MultiCall.Instance.DelayCall(0, adTypeMultiCall.Key, LoadRewardedAd);
            }
        }

        #endregion

        #region interstitial

        protected override void InternalLoadInterstitialLoadFailed(string adUnitId)
        {
            var time = 0;
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                var countAttempt = a.countLoadAttempt;
                try
                {
                    if (a.listTimeRetry.Count > countAttempt - 1)
                    {
                        time = a.listTimeRetry[countAttempt - 1];
                    }
                    else if (a.listTimeRetry.Count > 0)
                    {
                        time = a.listTimeRetry[a.listTimeRetry.Count - 1];
                    }
                }
                catch
                {
                    time = 0;
                }
            }

            var retryDelay = (float)time;
            MultiCall.Instance.DelayCall(retryDelay, adUnitId, LoadInterstitialAd);
        }

        protected override void InternalLoadInterstitial(string adUnitId)
        {
            MediationManager.Instance.DebugLog("Internal Load Interstitial : " + adUnitId);
            MaxSdk.LoadInterstitial(adUnitId);
        }


        public override void LoadInterstitial()
        {
            foreach (var adTypeMultiCall in MultiCall.Instance.dictionaryInterstitial)
            {
                MultiCall.Instance.DelayCall(0, adTypeMultiCall.Key, LoadInterstitialAd);
            }
        }

        #endregion
#endif
    }

    public static class MultiCallMaxProvider0Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            MultiCallMaxProvider0.Instance.Initialize();
        }
    }
}