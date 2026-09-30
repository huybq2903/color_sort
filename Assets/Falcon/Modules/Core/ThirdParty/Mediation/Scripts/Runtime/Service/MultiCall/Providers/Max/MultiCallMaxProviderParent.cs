/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using System;
using System.Globalization;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.Utils.Time.Runtime;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    /// <summary>
    /// Module multi call adUnitId for max sdk
    /// </summary>
    public abstract class MultiCallMaxProviderParent<T> : MultiCallProvider<T>
        where T : MultiCallMaxProviderParent<T>, new()
    {
        protected override int IndexProvider { get; }
        protected override MediationType MediationType => MediationType.Max;

        protected sealed override void RegisterProvider()
        {
            MultiCall.Instance.Register(Instance, MediationType);
        }

        #region rewarded

#if MAX_ENABLE
        protected override bool IsRewardedVideoReady(string adUnitId)
        {
            return MaxSdk.IsRewardedAdReady(adUnitId);
        }

        protected override void InternalShowRewarded(string adUnitId, string where)
        {
            var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
            MediationManager.Instance.DebugLog("Internal Show Rewarded : " + adUnitId + " x " +
                                               a.multiplier);
            closedRewarded = false;
            receivedRewarded = false;
            MaxSdk.ShowRewardedAd(adUnitId, where);
        }

        protected override void RegisterRewardedEvents()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
            //optimize performance
            if (enableLog)
            {
                MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            }

            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        }

        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                if (enableLog)
                {
                    var type = AdType.Reward;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_REVENUE_PAID, 1, type, type.ToString(),
                        adInfo.NetworkName, MultiCall.MAX, adInfo.Revenue, null, adUnitId, "", 0);
                }

                MediationManager.Instance.DebugLog("On Rewarded Ad Revenue Paid Event: " + adUnitId + " x " +
                                                   a.multiplier);
            }

            MediationManager.Instance.DebugLog("On Rewarded Ad Revenue Paid Revenue : " + adInfo.Revenue);
            ResetMultiplier(adUnitId);
            OnAdRevenuePaidEvent(AdType.Reward, adInfo);
        }

        private void OnRewardedAdDisplayFailedEvent(
            string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                if (enableLog)
                {
                    var type = AdType.Reward;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_DISPLAY_FAILED, 1, type, type.ToString(),
                        adInfo.NetworkName, MultiCall.MAX, adInfo.Revenue, errorInfo.Message, adUnitId, "",
                        0);
                }

                MultiCall.Instance.DelayCall(1, adUnitId, LoadRewardedAd);
                MediationManager.Instance.DebugLog("On Rewarded Ad Display Failed Event: " + adUnitId + " x " +
                                                   a.multiplier);
            }
            else
            {
                MultiCall.Instance.DelayCall(1, adUnitId, LoadRewardedAd);
            }

            onRewardedFailed?.Invoke();
            MediationManager.Instance.DebugLog("On Rewarded Ad Display Failed Revenue : " + adInfo.Revenue);
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                if (enableLog)
                {
                    var type = AdType.Reward;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_DISPLAYED, 1, type, type.ToString(),
                        adInfo.NetworkName, MultiCall.MAX, adInfo.Revenue, null, adUnitId, "", 0);
                }

                MediationManager.Instance.DebugLog("On Rewarded Ad Displayed Event: " + adUnitId + " x " +
                                                   a.multiplier);
            }

            MediationManager.Instance.DebugLog("On Rewarded Ad Displayed Revenue : " + adInfo.Revenue);
        }

        private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                if (ConsumeShowSession())
                {
                    var time = (int)(TimeUtils.UTCNow - MultiCall.Instance.dateTimeTimeShow).TotalSeconds;
                    new CSAdFinish(idAd, AdType.Reward.ToString(), Placement(placementId), adInfo.NetworkName,
                        MultiCall.MAX, adUnitId, adInfo.Revenue, time).Send();
                }
                else
                {
                    MediationManager.Instance.DebugError(
                        "MAX multicall > bỏ qua CSAdFinish rewarded lặp của lượt show đã kết thúc : " + adUnitId);
                }

                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                MediationManager.Instance.DebugLog("On Rewarded Ad Hidden Event: " + adUnitId + " x " +
                                                   a.multiplier);
                //update multiplier
                UpdateMultiplier(adUnitId);
            }
            else
            {
                MultiCall.Instance.DelayCall(1, adUnitId, LoadRewardedAd);
            }

            closedRewarded = true;
            onRewardedHidden?.Invoke();
            CheckReceivedRewarded();
            MediationManager.Instance.DebugLog("On Rewarded Ad Hidden Revenue : " + adInfo.Revenue);
        }

        protected virtual void UpdateMultiplier(string adUnitId)
        {
        }

        private void OnRewardedAdReceivedRewardEvent(
            string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            receivedRewarded = true;
            onRewardedComplete?.Invoke();
            CheckReceivedRewarded();
        }

        private void CheckReceivedRewarded()
        {
            if (closedRewarded && receivedRewarded)
            {
                //tiêu thụ 2 cờ (được reset lại ở InternalShowRewarded) để callback lặp
                //không tính thêm 1 lượt xem nữa vào cap của placement
                closedRewarded = false;
                receivedRewarded = false;
                MediationManager.Instance.AdsClosed(AdType.Reward, placementId);
            }
        }

        private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                InternalLoadRewardedLoadFailed(adUnitId);
                a.isWaiting = false;
                if (enableLog)
                {
                    var type = AdType.Reward;
                    var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_LOAD_FAILED, a.countLoadAttempt, type, type.ToString(),
                        MultiCall.UNKNOWN, MultiCall.MAX, 0, errorInfo.Message, adUnitId, "", diff);
                }

                MediationManager.Instance.DebugLog("On Rewarded Ad Load Failed Event: " + adUnitId + " x " +
                                                   a.multiplier);

                var b = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId);
                if (b != null)
                {
                    if (!b.errorDetail.ContainsKey(errorInfo.Code.ToString()))
                    {
                        b.errorDetail.Add(errorInfo.Code.ToString(), 0);
                    }

                    b.errorDetail[errorInfo.Code.ToString()]++;
                }
                else
                {
                    Debug.LogError("don't exists rewarded load failed id : " + adUnitId);
                }
            }
        }
#endif
        protected virtual void InternalLoadRewardedLoadFailed(string adUnitId)
        {
        }
#if MAX_ENABLE

        protected virtual void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                a.countLoadAttempt = 0;
                a.isWaiting = false;
                a.eCpm = adInfo.Revenue;
                a.networkName = adInfo.NetworkName;
                if (enableLog)
                {
                    var countAttempt = a.countLoadAttempt;
                    var type = AdType.Reward;
                    var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_LOAD_SUCCESS, countAttempt, type, type.ToString(),
                        adInfo.NetworkName, MultiCall.MAX, adInfo.Revenue, null, adUnitId, "", diff);
                }

                MediationManager.Instance.DebugLog("On Rewarded Ad Loaded Event: " + adUnitId);
                if (a.multiplier != 0)
                {
                    MediationManager.Instance.DebugLog("floor : " + MultiCall.Instance.valueDefaultRewarded + " xxxx " +
                                                       a.multiplier + " === " +
                                                       a.multiplier * MultiCall.Instance.valueDefaultRewarded);
                }
                else
                {
                    MediationManager.Instance.DebugLog("multiplier : 0");
                }

                var b = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId);
                if (b != null)
                {
                    var c = new AdSuccessInfo
                    {
                        index = b.numRequest,
                        revenueUsd = adInfo.Revenue,
                        time = new DateTimeOffset(TimeUtils.UTCNow).ToUnixTimeMilliseconds(),
                        adMediation = "Max",
                        adNetwork = adInfo.NetworkName
                    };
                    b.adSuccessInfos.Add(c);
                }
                else
                {
                    Debug.LogError("don't exists adRequestLogItem rewarded id : " + adUnitId);
                }
            }

            MediationManager.Instance.DebugLog("On Rewarded Ad Loaded Revenue : " + adInfo.Revenue);
        }
#endif

        #endregion

        #region interstitial

#if MAX_ENABLE
        protected override void InternalShowInterstitial(string adUnitId)
        {
            MaxSdk.ShowInterstitial(adUnitId);
        }

        protected override bool IsInterstitialReady(string adUnitId)
        {
            return MaxSdk.IsInterstitialReady(adUnitId);
        }

        protected override void RegisterInterstitialEvents()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialAdRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
        }

        private void OnInterstitialAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                if (enableLog)
                {
                    var type = AdType.Interstitial;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_REVENUE_PAID, 1, type, type.ToString(),
                        adInfo.NetworkName, MultiCall.MAX, adInfo.Revenue, null, adUnitId, "", 0);
                }
            }

            ResetMultiplier(adUnitId);
            OnAdRevenuePaidEvent(AdType.Interstitial, adInfo);
        }

        private void OnAdRevenuePaidEvent(AdType type, MaxSdkBase.AdInfo adInfo)
        {
            //log to adInfo
            double timeShow = (TimeUtils.UTCNow - MultiCall.Instance.dateTimeTimeShow).TotalSeconds;
            new CSAdInfo(idAd, Placement(placementId), type.ToString(), adInfo.NetworkName, "MAX", (int)timeShow,
                    adInfo.Revenue)
                .Send();
        }

        private void OnInterstitialAdDisplayFailedEvent(
            string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                if (enableLog)
                {
                    var type = AdType.Interstitial;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_DISPLAY_FAILED, 1, type, type.ToString(),
                        adInfo.NetworkName, MultiCall.MAX, adInfo.Revenue, errorInfo.Message, adUnitId, "", 0);
                }

                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                MultiCall.Instance.DelayCall(1, adUnitId, LoadInterstitialAd);
            }
            else
            {
                MultiCall.Instance.DelayCall(1, adUnitId, LoadInterstitialAd);
            }

            onInterstitialFailed?.Invoke();
        }

        private void OnInterstitialAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                if (enableLog)
                {
                    var type = AdType.Interstitial;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_DISPLAYED, 1, type, type.ToString(),
                        adInfo.NetworkName, MultiCall.MAX, adInfo.Revenue, null, adUnitId, "", 0);
                }
            }

            MediationManager.Instance.NumberShowInterstitial++;
        }

        private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                if (ConsumeShowSession())
                {
                    var time = (int)(TimeUtils.UTCNow - MultiCall.Instance.dateTimeTimeShow).TotalSeconds;
                    new CSAdFinish(idAd, AdType.Interstitial.ToString(), Placement(placementId), adInfo.NetworkName,
                        MultiCall.MAX, adUnitId, adInfo.Revenue, time).Send();
                }
                else
                {
                    MediationManager.Instance.DebugError(
                        "MAX multicall > bỏ qua CSAdFinish interstitial lặp của lượt show đã kết thúc : " + adUnitId);
                }

                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                MultiCall.Instance.DelayCall(1, adUnitId, LoadInterstitialAd);
            }
            else
            {
                MultiCall.Instance.DelayCall(1, adUnitId, LoadInterstitialAd);
            }

            onInterstitialClosed?.Invoke();
        }

        private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                InternalLoadInterstitialLoadFailed(adUnitId);
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                a.isWaiting = false;
                if (enableLog)
                {
                    var type = AdType.Interstitial;
                    var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_LOAD_FAILED, a.countLoadAttempt, type, type.ToString(),
                        MultiCall.UNKNOWN, MultiCall.MAX, 0, errorInfo.Message, adUnitId, "", diff);
                }

                var b = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId);
                if (b != null)
                {
                    if (!b.errorDetail.ContainsKey(errorInfo.Code.ToString()))
                    {
                        b.errorDetail.Add(errorInfo.Code.ToString(), 0);
                    }

                    b.errorDetail[errorInfo.Code.ToString()]++;
                }
                else
                {
                    Debug.LogError("don't exists inter load failed id : " + adUnitId);
                }
            }
        }
#endif
        protected virtual void InternalLoadInterstitialLoadFailed(string adUnitId)
        {
        }
#if MAX_ENABLE
        protected virtual void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                a.countLoadAttempt = 0;
                a.isWaiting = false;
                a.eCpm = adInfo.Revenue;
                a.networkName = adInfo.NetworkName;

                if (enableLog)
                {
                    var countAttempt = a.countLoadAttempt;
                    var type = AdType.Interstitial;
                    var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                    MultiCall.Instance.LogToServer(MultiCall.ADS_LOAD_SUCCESS, countAttempt, type, type.ToString(),
                        adInfo.NetworkName, MultiCall.MAX, adInfo.Revenue, null, adUnitId, "", diff);
                }

                var b = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId);
                if (b != null)
                {
                    var c = new AdSuccessInfo
                    {
                        index = b.numRequest,
                        revenueUsd = adInfo.Revenue,
                        time = new DateTimeOffset(TimeUtils.UTCNow).ToUnixTimeMilliseconds(),
                        adMediation = "Max",
                        adNetwork = adInfo.NetworkName
                    };
                    b.adSuccessInfos.Add(c);
                }
                else
                {
                    Debug.LogError("don't exists interstitial load failed id : " + adUnitId);
                }
            }
        }
#endif

        #endregion
    }
}