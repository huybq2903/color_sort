/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    /// <summary>
    /// Module multi call adUnitId for levelPlay sdk
    /// </summary>
    public class MultiCallIronSourceProvider0 : MultiCallIronSourceProviderParent<MultiCallIronSourceProvider0>
    {
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

                MediationManager.Instance.DebugLog("InternalLoadRewardedLoadFailed adUnitId : -" +
                                                   adUnitId);
            }

            var retryDelay = (float)time;
            MediationManager.Instance.DebugLog("InternalLoadRewardedLoadFailed irs provider 0");
            MediationManager.Instance.DebugLog("InternalLoadRewardedLoadFailed delay time : " + retryDelay);
            MultiCall.Instance.DelayCall(retryDelay, adUnitId, LoadRewardedAd);
        }

        public override void LoadRewarded()
        {
            MediationManager.Instance.DebugLog("LoadRewarded irs provider 0");
            foreach (var adTypeMultiCall in MultiCall.Instance.dictionaryRewarded)
            {
                MediationManager.Instance.DebugLog("LoadRewarded key : " + adTypeMultiCall.Key);
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

        public override void LoadInterstitial()
        {
            foreach (var adTypeMultiCall in MultiCall.Instance.dictionaryInterstitial)
            {
                MultiCall.Instance.DelayCall(0, adTypeMultiCall.Key, LoadInterstitialAd);
            }
        }

        #endregion
    }

    public static class MultiCallIronSourceProvider0Bootstrap
    {
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            MultiCallIronSourceProvider0.Instance.Initialize();
        }
    }
}