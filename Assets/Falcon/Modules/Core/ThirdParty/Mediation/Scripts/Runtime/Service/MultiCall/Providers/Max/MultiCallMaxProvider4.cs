/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-16
 */

using System.Globalization;
using Falcon.Modules.Core.RemoteConfigCms;
using Falcon.Modules.Core.Utils.Time.Runtime;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    /// <summary>
    /// giống provider 1, khác biệt duy nhất là giá trị ecpm trả về sẽ được tính trung bình sau mỗi lần lấy được ecpm default
    /// số lượng ads load tiếp theo sẽ được đưa vào remote config chứ ko hard code = 3
    /// </summary>
    public class MultiCallMaxProvider4 : MultiCallMaxProviderParent<MultiCallMaxProvider4>
    {
#if MAX_ENABLE
        protected override int IndexProvider => 4;

        private int _numberAdsToLoad = 3;

        private int _cntRewarded;
        private double _qtyRewarded;
        private int _cntInterstitial;
        private double _qtyInterstitial;

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

                var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                MediationManager.Instance.DebugLog("time diff: " + diff);
            }

            var retryDelay = (float)time;
            MediationManager.Instance.DebugLog("InternalLoadRewardedLoadFailed delay time : " + retryDelay);
            MultiCall.Instance.DelayCall(retryDelay, adUnitId, LoadRewardedAd);
        }

        protected override void UpdateMultiplier(string adUnitId)
        {
            MultiCall.Instance.DelayCall(1, adUnitId, LoadRewardedAd);
        }

        public override void LoadRewarded()
        {
            //load 1 id default
            //load xong thì load tiếp 3 id high floor nhân với multiplier
            //delay 5,10s để load id default tiếp theo
            //nếu đã load được id với ecpm cao, thì ko load id thấp
            MediationManager.Instance.DebugLog("LoadRewarded max provider 4");
            _numberAdsToLoad = FConfigControllerCms.Instance.Config<MultiCallConfig>().numberAdsToLoadNext;
            var list = MultiCall.Instance.GetDictionarySortedByMultiplier(MultiCall.Instance.dictionaryRewarded);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Value.multiplier == 0)
                {
                    MultiCall.Instance.DelayCall(i * 5, list[i].Key, LoadRewardedAd);
                }
            }
        }

        protected override void InternalLoadRewarded(string adUnitId)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                if (a.multiplier != 0)
                {
                    MediationManager.Instance.DebugLog("floor : " + MultiCall.Instance.valueDefaultRewarded + " xxxx " +
                                                       a.multiplier + " === " +
                                                       a.multiplier * MultiCall.Instance.valueDefaultRewarded);
                    MaxSdk.SetRewardedAdExtraParameter(adUnitId, MultiCall.KEY_SET_BID_FLOOR, (a.multiplier *
                            MultiCall.Instance.valueDefaultRewarded * MultiCall.MULTIPLIER)
                        .ToString(CultureInfo.InvariantCulture));
                }

                MaxSdk.LoadRewardedAd(adUnitId);
            }
        }

        protected override void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            base.OnRewardedAdLoadedEvent(adUnitId, adInfo);
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                if (a.multiplier == 0)
                {
                    _cntRewarded++;
                    _qtyRewarded += adInfo.Revenue;
                    MultiCall.Instance.valueDefaultRewarded = _qtyRewarded / _cntRewarded;
                }

                var list = MultiCall.Instance.GetDictionarySortedByMultiplier(MultiCall.Instance.dictionaryRewarded);
                //load 3 id high tiep theo
                var mul = a.multiplier;
                int cnt = 0;
                foreach (var pair in list)
                {
                    if (pair.Value.multiplier > mul)
                    {
                        MultiCall.Instance.DelayCall(cnt * 0.1f, pair.Key, LoadRewardedAd);
                        cnt++;
                        if (cnt >= _numberAdsToLoad)
                        {
                            break;
                        }
                    }
                }
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
            var list = MultiCall.Instance.GetDictionarySortedByMultiplier(MultiCall.Instance.dictionaryInterstitial);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Value.multiplier == 0)
                {
                    MultiCall.Instance.DelayCall(i * 5, list[i].Key, LoadInterstitialAd);
                }
            }
        }

        protected override void InternalLoadInterstitial(string adUnitId)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                if (a.multiplier != 0)
                {
                    MaxSdk.SetInterstitialExtraParameter(adUnitId, MultiCall.KEY_SET_BID_FLOOR, (a.multiplier *
                            MultiCall.Instance.valueDefaultInterstitial * MultiCall.MULTIPLIER)
                        .ToString(CultureInfo.InvariantCulture));
                }

                MaxSdk.LoadInterstitial(adUnitId);
            }
        }

        protected override void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            base.OnInterstitialLoadedEvent(adUnitId, adInfo);
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                if (a.multiplier == 0)
                {
                    _cntInterstitial++;
                    _qtyInterstitial += adInfo.Revenue;
                    MultiCall.Instance.valueDefaultInterstitial = _qtyInterstitial / _cntInterstitial;
                }

                var list = MultiCall.Instance.GetDictionarySortedByMultiplier(MultiCall.Instance
                    .dictionaryInterstitial);
                //load 3 id high tiep theo
                var mul = a.multiplier;
                int cnt = 0;
                foreach (var pair in list)
                {
                    if (pair.Value.multiplier > mul)
                    {
                        MultiCall.Instance.DelayCall(cnt * 0.1f, pair.Key, LoadInterstitialAd);
                        cnt++;
                        if (cnt >= _numberAdsToLoad)
                        {
                            break;
                        }
                    }
                }
            }
        }

        #endregion

#endif
    }

    public static class MultiCallMaxProvider4Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            MultiCallMaxProvider4.Instance.Initialize();
        }
    }
}