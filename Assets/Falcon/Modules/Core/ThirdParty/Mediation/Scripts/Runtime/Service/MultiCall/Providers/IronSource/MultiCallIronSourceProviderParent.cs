/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Modules.ActionLog.Runtime;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Modules.Core.ThirdParty.Firebase.Scripts.Runtime;
using Falcon.Modules.Core.ThirdParty.Mmp.Runtime;
using Falcon.Modules.Level.Core;
using Falcon.Modules.Core.Utils.Time.Runtime;
#if IRONSOURCE_ENABLE
using Unity.Services.LevelPlay;
#endif
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    /// <summary>
    /// Module multi call adUnitId for levelPlay sdk
    /// </summary>
    public abstract class MultiCallIronSourceProviderParent<T> : MultiCallProvider<T>
        where T : MultiCallIronSourceProviderParent<T>, new()
    {
        private const string _BANNER_IRS = "banner";
        private const string _INTERSTITIAL_IRS = "interstitial";
        private const string _REWARDED_IRS = "rewarded_video";
        private const string _IRON_SOURCE = "ironSource";
        protected override int IndexProvider { get; }
        protected override MediationType MediationType => MediationType.LevelPlay;

        protected sealed override void RegisterProvider()
        {
            MultiCall.Instance.Register(Instance, MediationType);
        }

        #region rewarded

        protected override bool IsRewardedVideoReady(string adUnitId)
        {
            if (!MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId)) return false;
#if IRONSOURCE_ENABLE
            var a = MultiCall.Instance.dictionaryRewarded[adUnitId].levelPlayRewardedAd;
            return a != null && a.IsAdReady();
#endif
            return false;
        }

        protected override void InternalShowRewarded(string adUnitId, string where)
        {
#if IRONSOURCE_ENABLE
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                _rewardedHasClosed = false;
                _rewardedHasRewarded = false;
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                MediationManager.Instance.DebugLog("Internal Show Rewarded : " + a.prefix + "-" + adUnitId);
                a.levelPlayRewardedAd?.ShowAd(where);
            }
#endif
        }

        protected override void InternalLoadRewarded(string adUnitId)
        {
#if IRONSOURCE_ENABLE
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                MediationManager.Instance.DebugLog("Internal Load Rewarded : " + a.prefix + "-" + adUnitId);
                a.levelPlayRewardedAd?.LoadAd();
            }
#endif
        }

        protected override void RegisterRewardedEvents()
        {
#if IRONSOURCE_ENABLE
            foreach (var levelPlayRewardedAd in MultiCall.Instance.dictionaryRewarded)
            {
                levelPlayRewardedAd.Value.levelPlayRewardedAd.OnAdLoaded += OnRewardedAdLoaded;
                levelPlayRewardedAd.Value.levelPlayRewardedAd.OnAdLoadFailed += OnRewardedAdLoadFailed;
                levelPlayRewardedAd.Value.levelPlayRewardedAd.OnAdDisplayed += OnRewardedAdDisplayed;
                levelPlayRewardedAd.Value.levelPlayRewardedAd.OnAdDisplayFailed += OnRewardedAdDisplayFailed;
                levelPlayRewardedAd.Value.levelPlayRewardedAd.OnAdClosed += OnRewardedAdClosed;
                levelPlayRewardedAd.Value.levelPlayRewardedAd.OnAdRewarded += OnRewardedAdRewarded;
            }

            LevelPlay.OnImpressionDataReady += OnImpressionDataReadyEvent;
#endif
        }

        private bool _rewardedHasClosed, _rewardedHasRewarded;

#if IRONSOURCE_ENABLE
        private void OnImpressionDataReadyEvent(LevelPlayImpressionData adInfo)
        {
            if (adInfo?.Revenue == null) return;
            AdType type;
            switch (adInfo.AdFormat.ToLower())
            {
                case _INTERSTITIAL_IRS:
                    type = AdType.Interstitial;
                    break;
                case _BANNER_IRS:
                    type = AdType.Banner;
                    break;
                case _REWARDED_IRS:
                    type = AdType.Reward;
                    break;
                default:
                    type = AdType.AppOpen;
                    break;
            }

            //log to adInfo
            AdLogInfo(adInfo, type);
        }

        private void AdLogInfo(LevelPlayImpressionData adInfo, AdType type)
        {
            if (adInfo?.Revenue == null) return;
            double timeShow = (TimeUtils.UTCNow - MultiCall.Instance.dateTimeTimeShow).TotalSeconds;
            new CSAdInfo(idAd, placementId, type.ToString(), adInfo.AdNetwork, "IronSrc", (int)timeShow,
                adInfo.Revenue.Value).Send();
        }

        private void OnRewardedAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward adRewarded)
        {
            _rewardedHasRewarded = true;
            if (!CheckFireRewardedEvent()) return;
            MediationManager.Instance.StartCoroutine(GetReward());
            onRewardedComplete?.Invoke();
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_COMPLETE);
        }

        private bool CheckFireRewardedEvent()
        {
            return _rewardedHasClosed && _rewardedHasRewarded;
        }

        private void OnRewardedAdClosed(LevelPlayAdInfo adInfo)
        {
            _rewardedHasClosed = true;
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adInfo.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adInfo.AdUnitId];
                var time = (int)(TimeUtils.UTCNow - MultiCall.Instance.dateTimeTimeShow).TotalSeconds;
                new CSAdFinish(idAd, AdType.Reward.ToString(), placementId, adInfo.AdNetwork, MultiCall.IRON_SOURCE,
                    adInfo.AdUnitId, a.eCpm, time).Send();

                var dict = new Dictionary<string, string>
                {
                    { "ad_where", placementId },
                    { "ad_network", adInfo.AdNetwork }
                };
                FActionLogManager.Log($"finish_{AdType.Reward.ToString()}", dict);
                if (a.index < QuantityIDDefault)
                {
                    MultiCall.Instance.DelayCall(1, adInfo.AdUnitId, LoadRewardedAd);
                }
                else
                {
                    MainGameObj.Instance.StartCoroutine(LoadRewardedFollowLogic());
                }

                MediationManager.Instance.DebugLog("On Rewarded Ad Hidden Event: " + a.prefix + "-" + adInfo.AdUnitId);
            }
            else
            {
                MultiCall.Instance.DelayCall(1, adInfo.AdUnitId, LoadRewardedAd);
            }

            MediationManager.Instance.DebugLog("On Rewarded Ad Hidden Event : " + adInfo.AdUnitId);
            MediationManager.Instance.DebugLog("On Rewarded Ad Hidden Revenue : " + adInfo.Revenue);
            if (!CheckFireRewardedEvent()) return;
            MediationManager.Instance.StartCoroutine(GetReward());
        }

        IEnumerator GetReward()
        {
            yield return new WaitForEndOfFrame();
            onRewardedComplete?.Invoke();
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_COMPLETE);
        }

        private void OnRewardedAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError adError)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adInfo.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adInfo.AdUnitId];
                if (enableLog)
                {
                    var type = AdType.Reward;
                    var floorRewarded = a.prefix;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_DISPLAY_FAILED, 1, type, type.ToString(),
                        adInfo.AdNetwork, MultiCall.IRON_SOURCE, a.eCpm, adError.ErrorMessage, adInfo.AdUnitId,
                        floorRewarded, 0);
                }

                FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_SHOW_FAIL, adError.ErrorMessage);
                if (a.index < QuantityIDDefault)
                {
                    MultiCall.Instance.DelayCall(1, adInfo.AdUnitId, LoadRewardedAd);
                }
                else
                {
                    MainGameObj.Instance.StartCoroutine(LoadRewardedFollowLogic());
                }

                MediationManager.Instance.DebugLog("On Rewarded Ad Display Failed Event: " + a.prefix + "-" +
                                                   adInfo.AdUnitId);
            }
            else
            {
                MultiCall.Instance.DelayCall(1, adInfo.AdUnitId, LoadRewardedAd);
            }

            onRewardedFailed?.Invoke();
            MediationManager.Instance.DebugLog("On Rewarded Ad Display Failed Event : " + adInfo.AdUnitId);
            MediationManager.Instance.DebugLog("On Rewarded Ad Display Failed message : " + adError.ErrorMessage);
        }

#endif
        protected virtual void InternalLoadRewardedLoadFailed(string adUnitId)
        {
        }
#if IRONSOURCE_ENABLE
        private void OnRewardedAdLoadFailed(LevelPlayAdError adError)
        {
            MediationManager.Instance.DebugError("load rewarded fail message : " + adError.ErrorMessage);
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adError.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adError.AdUnitId];
                InternalLoadRewardedLoadFailed(adError.AdUnitId);
                a.isWaiting = false;
                if (enableLog)
                {
                    var floorRewarded = a.prefix;
                    var type = AdType.Reward;
                    var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_LOAD_FAILED, a.countLoadAttempt, type, type.ToString(),
                        MultiCall.UNKNOWN, MultiCall.IRON_SOURCE, 0, adError.ErrorMessage, adError.AdUnitId,
                        floorRewarded,
                        diff);
                }

                MediationManager.Instance.DebugLog("On Rewarded Ad Load Failed Event: " + a.prefix + "-" +
                                                   adError.AdUnitId);

                var b = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adError.AdUnitId);
                if (!b.errorDetail.ContainsKey(adError.ErrorCode.ToString()))
                {
                    b.errorDetail.Add(adError.ErrorCode.ToString(), 0);
                }

                b.errorDetail[adError.ErrorCode.ToString()]++;
            }
            else
            {
                InternalLoadRewardedLoadFailed(adError.AdUnitId);
            }
        }

        private void OnRewardedAdLoaded(LevelPlayAdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adInfo.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adInfo.AdUnitId];
                a.countLoadAttempt = 0;
                a.isWaiting = false;
                if (adInfo.Revenue != null) a.eCpm = adInfo.Revenue.Value;
                a.networkName = adInfo.AdNetwork;
                if (enableLog)
                {
                    var floorRewarded = a.prefix;
                    var countAttempt = a.countLoadAttempt;
                    var type = AdType.Reward;
                    var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_LOAD_SUCCESS, countAttempt, type, type.ToString(),
                        adInfo.AdNetwork, MultiCall.IRON_SOURCE, a.eCpm, null, adInfo.AdUnitId, floorRewarded, diff);
                }

                MediationManager.Instance.DebugLog("On Rewarded Ad Loaded Event: " + a.prefix + "-" + adInfo.AdUnitId);

                var b = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adInfo.AdUnitId);
                if (b != null)
                {
                    var c = new AdSuccessInfo
                    {
                        index = b.numRequest,
                        revenueUsd = a.eCpm,
                        time = new DateTimeOffset(TimeUtils.UTCNow).ToUnixTimeMilliseconds(),
                        adMediation = "Max",
                        adNetwork = a.networkName
                    };
                    b.adSuccessInfos.Add(c);
                }
                else
                {
                    Debug.LogError("don't exists adRequestLogItem rewarded id : " + adInfo.AdUnitId);
                }
            }

            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_LOAD);
            MediationManager.Instance.DebugLog("On Rewarded Ad Loaded Event : " + adInfo.AdUnitId);
            MediationManager.Instance.DebugLog("On Rewarded Ad Loaded Revenue : " + adInfo.Revenue);
        }

        private void OnRewardedAdDisplayed(LevelPlayAdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adInfo.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adInfo.AdUnitId];
                if (enableLog)
                {
                    var type = AdType.Reward;
                    var floorRewarded = a.prefix;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_DISPLAYED, 1, type, type.ToString(), adInfo.AdNetwork,
                        MultiCall.IRON_SOURCE, a.eCpm, null, adInfo.AdUnitId, floorRewarded, 0);
                }

                FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_SHOW_SUCCESS);
                FalconMmpLog.LogEvent(FalconMmpLog.MMP_REWARDED_DISPLAYED, new Dictionary<string, string>
                {
                    { "af_level", LevelData.Instance.level + "" }
                });
                var dict = new Dictionary<string, string>
                {
                    { "ad_where", placementId },
                    { "ad_network", adInfo.AdNetwork }
                };
                FActionLogManager.Log($"start_{AdType.Reward.ToString()}", dict);
                MediationManager.Instance.DebugLog(
                    "On Rewarded Ad Displayed Event: " + a.prefix + "-" + adInfo.AdUnitId);
            }

            MediationManager.Instance.DebugLog("On Rewarded Ad Displayed Event : " + adInfo.AdUnitId);
            MediationManager.Instance.DebugLog("On Rewarded Ad Displayed Revenue : " + adInfo.Revenue);
        }
#endif

        #endregion

        #region interstitial

        protected override void InternalShowInterstitial(string adUnitId)
        {
#if IRONSOURCE_ENABLE
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                a.levelPlayInterstitialAd?.ShowAd();
            }
#endif
        }

        protected override bool IsInterstitialReady(string adUnitId)
        {
#if IRONSOURCE_ENABLE
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                return a.levelPlayInterstitialAd != null && a.levelPlayInterstitialAd.IsAdReady();
            }
#endif
            return false;
        }

        protected override void InternalLoadInterstitial(string adUnitId)
        {
#if IRONSOURCE_ENABLE
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                a.levelPlayInterstitialAd?.LoadAd();
            }
#endif
        }

        protected override void RegisterInterstitialEvents()
        {
#if IRONSOURCE_ENABLE
            foreach (var levelPlayInterstitial in MultiCall.Instance.dictionaryInterstitial)
            {
                levelPlayInterstitial.Value.levelPlayInterstitialAd.OnAdLoaded += OnInterstitialAdLoaded;
                levelPlayInterstitial.Value.levelPlayInterstitialAd.OnAdLoadFailed += OnInterstitialAdLoadFailed;
                levelPlayInterstitial.Value.levelPlayInterstitialAd.OnAdDisplayed += OnInterstitialAdDisplayed;
                levelPlayInterstitial.Value.levelPlayInterstitialAd.OnAdDisplayFailed += OnInterstitialAdDisplayFailed;
                levelPlayInterstitial.Value.levelPlayInterstitialAd.OnAdClosed += OnInterstitialAdClosed;
                levelPlayInterstitial.Value.levelPlayInterstitialAd.OnAdClicked += OnInterstitialAdClicked;
            }
#endif
        }

#if IRONSOURCE_ENABLE
        private void OnInterstitialAdClicked(LevelPlayAdInfo adInfo)
        {
            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_CLICKED);
        }

        private void OnInterstitialAdClosed(LevelPlayAdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adInfo.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adInfo.AdUnitId];
                if (a.index < QuantityIDDefault)
                {
                    MultiCall.Instance.DelayCall(1, adInfo.AdUnitId, LoadInterstitialAd);
                }
                else
                {
                    MainGameObj.Instance.StartCoroutine(LoadInterstitialFollowLogic());
                }

                var time = (int)(TimeUtils.UTCNow - MultiCall.Instance.dateTimeTimeShow).TotalSeconds;
                new CSAdFinish(idAd, AdType.Interstitial.ToString(), placementId, adInfo.AdNetwork,
                    MultiCall.IRON_SOURCE, adInfo.AdUnitId, a.eCpm, time).Send();

                var dict = new Dictionary<string, string>
                {
                    { "ad_where", placementId },
                    { "ad_network", adInfo.AdNetwork }
                };
                FActionLogManager.Log($"finish_{AdType.Interstitial.ToString()}", dict);
            }
            else
            {
                MultiCall.Instance.DelayCall(1, adInfo.AdUnitId, LoadInterstitialAd);
            }

            MainGameObj.Instance.StartCoroutine(Wait1Frame());

            IEnumerator Wait1Frame()
            {
                yield return new WaitForEndOfFrame();
                onInterstitialClosed?.Invoke();
            }
        }

        private void OnInterstitialAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError adError)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adInfo.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adInfo.AdUnitId];
                if (enableLog)
                {
                    var type = AdType.Interstitial;
                    var floorInterstitial = a.prefix;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_DISPLAY_FAILED, 1, type, type.ToString(),
                        adInfo.AdNetwork, MultiCall.IRON_SOURCE, a.eCpm, adError.ErrorMessage, adInfo.AdUnitId,
                        floorInterstitial, 0);
                }

                if (a.index < QuantityIDDefault)
                {
                    MultiCall.Instance.DelayCall(1, adInfo.AdUnitId, LoadInterstitialAd);
                }
                else
                {
                    MainGameObj.Instance.StartCoroutine(LoadInterstitialFollowLogic());
                }
            }
            else
            {
                MultiCall.Instance.DelayCall(1, adInfo.AdUnitId, LoadInterstitialAd);
            }

            onInterstitialFailed?.Invoke();
        }

        private void OnInterstitialAdDisplayed(LevelPlayAdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adInfo.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adInfo.AdUnitId];
                if (enableLog)
                {
                    var type = AdType.Interstitial;
                    var floorInterstitial = a.prefix;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_DISPLAYED, 1, type, type.ToString(), adInfo.AdNetwork,
                        MultiCall.IRON_SOURCE, a.eCpm, null, adInfo.AdUnitId, floorInterstitial, 0);
                }

                FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_SHOW);
                FalconMmpLog.LogEvent(FalconMmpLog.MMP_INTERS_DISPLAYED, new Dictionary<string, string>
                {
                    { "af_level", LevelData.Instance.level + "" }
                });
                var dict = new Dictionary<string, string>
                {
                    { "ad_where", placementId },
                    { "ad_network", adInfo.AdNetwork }
                };
                FActionLogManager.Log($"start_{AdType.Interstitial.ToString()}", dict);
            }

            MediationManager.Instance.NumberShowInterstitial++;
        }

        private void OnInterstitialAdLoadFailed(LevelPlayAdError adError)
        {
            MediationManager.Instance.DebugError("OnInterstitialAdLoadFailed : " + adError.AdUnitId);
            MediationManager.Instance.DebugError("adError : " + adError.ErrorMessage);
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adError.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adError.AdUnitId];
                InternalLoadInterstitialLoadFailed(adError.AdUnitId);
                a.isWaiting = false;
                if (enableLog)
                {
                    var floorInterstitial = a.prefix;
                    var type = AdType.Interstitial;
                    var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_LOAD_FAILED, a.countLoadAttempt, type, type.ToString(),
                        MultiCall.UNKNOWN, MultiCall.IRON_SOURCE, 0, adError.ErrorMessage, adError.AdUnitId,
                        floorInterstitial, diff);
                }

                FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_LOAD_FAILED, adError.ErrorMessage);

                var b = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adError.AdUnitId);
                if (!b.errorDetail.ContainsKey(adError.ErrorCode.ToString()))
                {
                    b.errorDetail.Add(adError.ErrorCode.ToString(), 0);
                }

                b.errorDetail[adError.ErrorCode.ToString()]++;
            }
        }

#endif
        protected virtual void InternalLoadInterstitialLoadFailed(string adUnitId)
        {
        }
#if IRONSOURCE_ENABLE
        private void OnInterstitialAdLoaded(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("load success inter : " + adInfo.AdUnitId);
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adInfo.AdUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adInfo.AdUnitId];
                a.countLoadAttempt = 0;
                a.isWaiting = false;
                if (adInfo.Revenue != null)
                    a.eCpm = adInfo.Revenue.Value;
                a.networkName = adInfo.AdNetwork;

                if (enableLog)
                {
                    var countAttempt = a.countLoadAttempt;
                    var floorInterstitial = a.prefix;
                    var type = AdType.Interstitial;
                    var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_LOAD_SUCCESS, countAttempt, type, type.ToString(),
                        adInfo.AdNetwork, MultiCall.IRON_SOURCE, a.eCpm, null, adInfo.AdUnitId, floorInterstitial,
                        diff);
                }

                var b = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adInfo.AdUnitId);
                if (b != null)
                {
                    var c = new AdSuccessInfo
                    {
                        index = b.numRequest,
                        revenueUsd = a.eCpm,
                        time = new DateTimeOffset(TimeUtils.UTCNow).ToUnixTimeMilliseconds(),
                        adMediation = "Max",
                        adNetwork = a.networkName
                    };
                    b.adSuccessInfos.Add(c);
                }
                else
                {
                    Debug.LogError("don't exists adRequestLogItem interstitial id : " + adInfo.AdUnitId);
                }
            }

            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_LOAD_SUCCESS);
        }
#endif

        #endregion
    }
}