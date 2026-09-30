/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-28
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Falcon.Modules.Core.RemoteConfigCms;
using Falcon.Modules.Core.Utils.Time.Runtime;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    /// <summary>
    /// Module multi call adUnitId for max sdk
    /// cập nhật giá trị valueDefault khi load xong qc
    /// hệ số bắt đầu từ 1.0
    /// </summary>
    public class MultiCallMaxProvider7 : MultiCallMaxProviderParent<MultiCallMaxProvider7>
    {
#if MAX_ENABLE
        protected override int IndexProvider => 7;

        private readonly List<float> _listMultiplierRewardedDefault = new();
        private readonly List<float> _listMultiplierInterstitialDefault = new();
        private int _defaultCountRewarded;
        private int _defaultCountInterstitial;
        private bool _isBatchRunningRewarded;
        private bool _isBatchRunningInterstitial;
        private int _currentAnchorIndexRewarded = -1;
        private int _currentAnchorIndexInterstitial = -1;
        private int _periodDecreaseFloor;
        private const int _GROUP_COUNT = 3; // 3 multiplier liên tiếp

        #region rewarded

        protected override void UpdateMultiplier(string adUnitId)
        {
            //cập nhật lại multiplier gốc cho adUnitId này
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var list = MultiCall.Instance.GetDictionarySortedByIndex(MultiCall.Instance.dictionaryRewarded);
                for (int i = _defaultCountRewarded; i < list.Count; i++)
                {
                    if (list[i].Key.Equals(adUnitId))
                    {
                        list[i].Value.multiplier = _listMultiplierRewardedDefault[i - _defaultCountRewarded];
                        break;
                    }
                }

                MultiCall.Instance.DelayCall(1, adUnitId, LoadRewardedAd);
            }

            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var listInter =
                    MultiCall.Instance.GetDictionarySortedByIndex(MultiCall.Instance.dictionaryInterstitial);
                for (int i = _defaultCountRewarded; i < listInter.Count; i++)
                {
                    if (listInter[i].Key.Equals(adUnitId))
                    {
                        listInter[i].Value.multiplier = _listMultiplierInterstitialDefault[i - _defaultCountRewarded];
                        break;
                    }
                }

                MultiCall.Instance.DelayCall(1, adUnitId, LoadInterstitialAd);
            }
        }

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

                MediationManager.Instance.DebugLog("InternalLoadRewardedLoadFailed adUnitId : " + adUnitId + " x " +
                                                   a.multiplier);
                var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                MediationManager.Instance.DebugLog("time diff: " + diff + " - attemp : " + countAttempt);

                if (_listMultiplierRewardedDefault.Count > 0 && countAttempt % _periodDecreaseFloor == 0)
                {
                    if (a.index == _defaultCountRewarded)
                    {
                        a.multiplier = Mathf.Max(a.multiplier - 0.1f, 1f);
                    }
                    else if (a.index > _defaultCountRewarded)
                    {
                        var b = MultiCall.Instance.GetAdTypeMultiCallByIndex(a.index - 1,
                            MultiCall.Instance.dictionaryRewarded);
                        if (b != null)
                        {
                            a.multiplier = Mathf.Max(a.multiplier - 0.1f, b.multiplier);
                        }
                    }
                }
            }

            var retryDelay = (float)time;
            MediationManager.Instance.DebugLog("InternalLoadRewardedLoadFailed delay time : " + retryDelay);
            MultiCall.Instance.DelayCall(retryDelay, adUnitId, LoadRewardedAd);
        }

        private void ReloadDefaultCountRewarded()
        {
            if (_defaultCountRewarded == 0)
            {
                foreach (var call in MultiCall.Instance.dictionaryRewarded)
                {
                    if (call.Value.multiplier == 0)
                    {
                        _defaultCountRewarded++;
                    }
                }
            }
        }

        public override void LoadRewarded()
        {
            //xử lý _listMultiplierDefault nếu chưa có phần tử
            if (_listMultiplierRewardedDefault.Count == 0)
            {
                ReloadDefaultCountRewarded();
                var countMultiplierGreaterThan0 = MultiCall.Instance.dictionaryRewarded.Count - _defaultCountRewarded;
                for (int i = 0; i < countMultiplierGreaterThan0; i++)
                {
                    _listMultiplierRewardedDefault.Add(1f + i * 0.5f);
                }

                _periodDecreaseFloor = FConfigControllerCms.Instance.Config<MultiCallConfig>().periodDecreaseFloor;
            }

            //load 1 id default
            //delay 5,10s để load id default tiếp theo
            //nếu đã load được id với ecpm cao, thì ko load id thấp
            MediationManager.Instance.DebugLog("LoadRewarded max provider 7");
            var list = MultiCall.Instance.GetDictionarySortedByIndex(MultiCall.Instance.dictionaryRewarded);
            for (int i = 0; i < list.Count; i++)
            {
                if (i < _defaultCountRewarded)
                {
                    list[i].Value.multiplier = 0;
                    MultiCall.Instance.DelayCall(i * 5, list[i].Key, LoadRewardedAd);
                }
                else
                {
                    list[i].Value.multiplier = _listMultiplierRewardedDefault[i - _defaultCountRewarded];
                }
            }
        }

        private void ResetAnchorIndexRewarded()
        {
            //reset giá trị _currentAnchorIndexRewarded dành cho trường hợp show rewarded có multiplier cao nhất
            //tìm index có qc với multiplier cao nhất
            _currentAnchorIndexRewarded = -1;
            var list = MultiCall.Instance.GetDictionarySortedByIndex(MultiCall.Instance.dictionaryRewarded);
            for (int i = list.Count - 1; i >= _defaultCountRewarded; i--)
            {
                if (list[i].Value.multiplier > 0 && list[i].Value.eCpm >= 0)
                {
                    _currentAnchorIndexRewarded = i - _defaultCountRewarded;
                    MediationManager.Instance.DebugLog("current anchor index : " + _currentAnchorIndexRewarded);
                    break;
                }
            }
        }

        protected override void InternalLoadRewarded(string adUnitId)
        {
            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                // ResetAnchorIndexRewarded();
                MediationManager.Instance.DebugLog("Internal Load Rewarded : " + adUnitId + " x " + a.multiplier);
                MediationManager.Instance.DebugLog("value default : " + MultiCall.Instance.valueDefaultRewarded);
                MediationManager.Instance.DebugLog("floor : " +
                                                   (a.multiplier * MultiCall.Instance.valueDefaultRewarded)
                                                   .ToString(CultureInfo.InvariantCulture));
                MaxSdk.SetRewardedAdExtraParameter(adUnitId, MultiCall.KEY_SET_BID_FLOOR, (a.multiplier *
                        MultiCall.Instance.valueDefaultRewarded * MultiCall.MULTIPLIER)
                    .ToString(CultureInfo.InvariantCulture));
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
                    MultiCall.Instance.valueDefaultRewarded = adInfo.Revenue;

                if (a.multiplier == 0 && _isBatchRunningRewarded)
                {
                    return;
                }

                var list = MultiCall.Instance.GetDictionarySortedByIndex(MultiCall.Instance.dictionaryRewarded);
                ResetAnchorIndexRewarded();
                for (int i = _defaultCountRewarded; i < list.Count; i++)
                {
                    if (list[i].Key.Equals(adUnitId))
                    {
                        if (_currentAnchorIndexRewarded > i - _defaultCountRewarded) return;
                        _currentAnchorIndexRewarded = i - _defaultCountRewarded;
                        MediationManager.Instance.DebugLog("_currentAnchorIndexRewarded : " +
                                                           _currentAnchorIndexRewarded);
                    }
                }

                _isBatchRunningRewarded = true;
                int startIndex = _currentAnchorIndexRewarded + 1;
                if (startIndex >= _listMultiplierRewardedDefault.Count)
                {
                    MediationManager.Instance.DebugLog("Reached final multiplier. Flow completed.");
                    return;
                }

                MediationManager.Instance.DebugLog("startIndex : " + startIndex);
                int endIndexExclusive = Mathf.Min(startIndex + _GROUP_COUNT, _listMultiplierRewardedDefault.Count);
                for (int i = startIndex; i < endIndexExclusive; i++)
                {
                    var multiplier = _listMultiplierRewardedDefault[i];
                    //tìm trong dictionary element đang available, nếu có multiplier nằm trong nhóm đang đợi load thì bỏ qua
                    bool existsMultiplier = false;
                    for (int j = list.Count - 1; j >= _defaultCountRewarded + i; j--)
                    {
                        if (list[j].Value.eCpm >= 0)
                        {
                            MediationManager.Instance.DebugLog("đã tồn tại : x" + list[i].Value.multiplier + " >= x" +
                                                               multiplier);
                            existsMultiplier = true;
                            break;
                        }
                    }

                    //đã tồn tại mức này
                    //bỏ qua
                    if (existsMultiplier) continue;

                    //tìm trong dictionary id tương ứng để load
                    if (!list[i + _defaultCountRewarded].Value.isWaiting &&
                        list[i + _defaultCountRewarded].Value.eCpm == -100)
                    {
                        MultiCall.Instance.DelayCall((i - startIndex) * 0.1f, list[i + _defaultCountRewarded].Key,
                            LoadRewardedAd);
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

                // MediationManager.Instance.DebugLog("InternalLoadInterstitialLoadFailed adUnitId : " + adUnitId + " x " +
                // a.multiplier);
                var diff = (TimeUtils.UTCNow - a.timeLoad).TotalSeconds;
                // MediationManager.Instance.DebugLog("time diff: " + diff + " - attemp : " + countAttempt);

                if (_listMultiplierInterstitialDefault.Count > 0 && countAttempt % _periodDecreaseFloor == 0)
                {
                    if (a.index == _defaultCountInterstitial)
                    {
                        a.multiplier = Mathf.Max(a.multiplier - 0.1f, 1f);
                    }
                    else if (a.index > _defaultCountInterstitial)
                    {
                        var b = MultiCall.Instance.GetAdTypeMultiCallByIndex(a.index - 1,
                            MultiCall.Instance.dictionaryInterstitial);
                        if (b != null)
                        {
                            a.multiplier = Mathf.Max(a.multiplier - 0.1f, b.multiplier);
                        }
                    }
                }
            }

            var retryDelay = (float)time;
            // MediationManager.Instance.DebugLog("InternalLoadInterstitialLoadFailed delay time : " + retryDelay);
            MultiCall.Instance.DelayCall(retryDelay, adUnitId, LoadInterstitialAd);
        }

        private void ReloadDefaultCountInterstitial()
        {
            if (_defaultCountInterstitial == 0)
            {
                foreach (var call in MultiCall.Instance.dictionaryInterstitial)
                {
                    if (call.Value.multiplier == 0)
                    {
                        _defaultCountInterstitial++;
                    }
                }
            }
        }

        public override void LoadInterstitial()
        {
            //xử lý _listMultiplierDefault nếu chưa có phần tử
            if (_listMultiplierInterstitialDefault.Count == 0)
            {
                ReloadDefaultCountInterstitial();
                var countMultiplierGreaterThan0 =
                    MultiCall.Instance.dictionaryInterstitial.Count - _defaultCountInterstitial;
                for (int i = 0; i < countMultiplierGreaterThan0; i++)
                {
                    _listMultiplierInterstitialDefault.Add(1f + i * 0.5f);
                }
            }

            //load 1 id default
            //delay 5,10s để load id default tiếp theo
            //nếu đã load được id với ecpm cao, thì ko load id thấp
            // MediationManager.Instance.DebugLog("LoadInterstitial max provider 7");
            var list = MultiCall.Instance.GetDictionarySortedByIndex(MultiCall.Instance.dictionaryInterstitial);
            for (int i = 0; i < list.Count; i++)
            {
                if (i < _defaultCountInterstitial)
                {
                    list[i].Value.multiplier = 0;
                    MultiCall.Instance.DelayCall(i * 5, list[i].Key, LoadInterstitialAd);
                }
                else
                {
                    list[i].Value.multiplier = _listMultiplierInterstitialDefault[i - _defaultCountInterstitial];
                }
            }
        }

        private void ResetAnchorIndexInterstitial()
        {
            //reset giá trị _currentAnchorIndexInterstitial dành cho trường hợp show Interstitial có multiplier cao nhất
            //tìm index có qc với multiplier cao nhất
            _currentAnchorIndexInterstitial = -1;
            var list = MultiCall.Instance.GetDictionarySortedByIndex(MultiCall.Instance.dictionaryInterstitial);
            for (int i = list.Count - 1; i >= _defaultCountInterstitial; i--)
            {
                if (list[i].Value.multiplier > 0 && list[i].Value.eCpm >= 0)
                {
                    _currentAnchorIndexInterstitial = i - _defaultCountInterstitial;
                    // MediationManager.Instance.DebugLog("current anchor index : " + _currentAnchorIndexInterstitial);
                    break;
                }
            }
        }

        protected override void InternalLoadInterstitial(string adUnitId)
        {
            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                // ResetAnchorIndexInterstitial();
                // MediationManager.Instance.DebugLog("Internal Load Interstitial : " + adUnitId + " x " + a.multiplier);
                // MediationManager.Instance.DebugLog("value default : " + MultiCall.Instance.valueDefaultInterstitial);
                // MediationManager.Instance.DebugLog("floor : " +
                //                                    (a.multiplier * MultiCall.Instance.valueDefaultInterstitial)
                //                                    .ToString(CultureInfo.InvariantCulture));
                MaxSdk.SetInterstitialExtraParameter(adUnitId, MultiCall.KEY_SET_BID_FLOOR, (a.multiplier *
                        MultiCall.Instance.valueDefaultInterstitial * MultiCall.MULTIPLIER)
                    .ToString(CultureInfo.InvariantCulture));
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
                    MultiCall.Instance.valueDefaultInterstitial = adInfo.Revenue;

                if (a.multiplier == 0 && _isBatchRunningInterstitial)
                {
                    return;
                }

                var list = MultiCall.Instance.GetDictionarySortedByIndex(MultiCall.Instance.dictionaryInterstitial);
                ResetAnchorIndexInterstitial();
                for (int i = _defaultCountInterstitial; i < list.Count; i++)
                {
                    if (list[i].Key.Equals(adUnitId))
                    {
                        if (_currentAnchorIndexInterstitial > i - _defaultCountInterstitial) return;
                        _currentAnchorIndexInterstitial = i - _defaultCountInterstitial;
                        // MediationManager.Instance.DebugLog("_currentAnchorIndexInterstitial : " +
                        //                                    _currentAnchorIndexInterstitial);
                    }
                }

                _isBatchRunningInterstitial = true;
                int startIndex = _currentAnchorIndexInterstitial + 1;
                if (startIndex >= _listMultiplierInterstitialDefault.Count)
                {
                    // MediationManager.Instance.DebugLog("Reached final multiplier. Flow completed.");
                    return;
                }

                // MediationManager.Instance.DebugLog("startIndex : " + startIndex);
                int endIndexExclusive = Mathf.Min(startIndex + _GROUP_COUNT, _listMultiplierInterstitialDefault.Count);
                for (int i = startIndex; i < endIndexExclusive; i++)
                {
                    var multiplier = _listMultiplierInterstitialDefault[i];
                    //tìm trong dictionary element đang available, nếu có multiplier nằm trong nhóm đang đợi load thì bỏ qua
                    bool existsMultiplier = false;
                    for (int j = list.Count - 1; j >= _defaultCountInterstitial + i; j--)
                    {
                        if (list[j].Value.eCpm >= 0)
                        {
                            // MediationManager.Instance.DebugLog("đã tồn tại : x" + list[i].Value.multiplier + " >= x" +
                            //                                    multiplier);
                            existsMultiplier = true;
                            break;
                        }
                    }

                    //đã tồn tại mức này
                    //bỏ qua
                    if (existsMultiplier) continue;

                    //tìm trong dictionary id tương ứng để load
                    if (!list[i + _defaultCountInterstitial].Value.isWaiting &&
                        list[i + _defaultCountInterstitial].Value.eCpm == -100)
                    {
                        MultiCall.Instance.DelayCall((i - startIndex) * 0.1f, list[i + _defaultCountInterstitial].Key,
                            LoadInterstitialAd);
                    }
                }
            }
        }

        #endregion

#endif
    }

    public static class MultiCallMaxProvider7Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            MultiCallMaxProvider7.Instance.Initialize();
        }
    }
}