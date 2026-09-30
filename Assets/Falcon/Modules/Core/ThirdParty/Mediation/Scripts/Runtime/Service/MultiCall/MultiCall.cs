/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.RemoteConfigCms;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Modules.Core.Utils.Time.Runtime;
#if IRONSOURCE_ENABLE
using Unity.Services.LevelPlay;
#endif
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    /// <summary>
    /// Module multi call adUnitId for max sdk
    /// </summary>
    public class MultiCall
    {
        private const string _DISABLE_B2_B = "disable_b2b_ad_unit_ids";
        private const string _DISABLE_RETRIES = "disable_auto_retries";
        private const string _TRUE = "true";
        public bool enableLogMultiCall;

        public const string MAX = "MAX";
        public const string IRON_SOURCE = "IRONSOURCE";
        public const string ADS_LOAD_SUCCESS = "ads_load_success";
        public const string ADS_LOAD_FAILED = "ads_load_failed";
        public const string UNKNOWN = "Unknown";
        public const string ADS_REVENUE_PAID = "ads_revenue_paid";
        public const string ADS_DISPLAY_FAILED = "ads_display_failed";
        public const string ADS_DISPLAYED = "ads_displayed";
        public const string KEY_SET_BID_FLOOR = "jC7Fp";
        private List<AdBidFloor> _listAdBidFloors = new();
        public bool getListAdBidFloorFromServer;
        public double valueDefaultRewarded = 0;
        public double valueDefaultInterstitial = 0;

        public const float MULTIPLIER = 1000;

        public List<AdBidFloor> ListAdBidFloors
        {
            get
            {
                if (_listAdBidFloors.Count == 0)
                {
                    _listAdBidFloors = SaveLoadHandler.Load("FALCON_LIST_AD_BID_FLOORS", new List<AdBidFloor>());
                }

                return _listAdBidFloors;
            }
            set
            {
                _listAdBidFloors.Clear();

                var type = MediationManager.Instance.CurrentMediationType();
                var index = 0;
                switch (type)
                {
                    case MediationType.Max:
                        index = FConfigControllerCms.Instance.Config<MultiCallConfig>().moMulProviderIndexMax;
                        // index = 3;
                        break;
                    case MediationType.LevelPlay:
                        index = FConfigControllerCms.Instance.Config<MultiCallConfig>().moMulProviderIndexIronSource;
                        break;
                    case MediationType.Admob:
                        index = FConfigControllerCms.Instance.Config<MultiCallConfig>().moMulProviderIndexAdmob;
                        break;
                }

                foreach (var adBidFloor in value)
                {
                    if (adBidFloor.multiplier == 0 || adBidFloor.algorithmIds == null ||
                        adBidFloor.algorithmIds.Contains(index) || (index == 3 &&
                                                                    (adBidFloor.algorithmIds.Contains(0) ||
                                                                     adBidFloor.algorithmIds.Contains(1) ||
                                                                     adBidFloor.algorithmIds.Contains(2))))
                    {
                        _listAdBidFloors.Add(adBidFloor);
                    }
                }

                _listAdBidFloors.Sort((a, b) => a.multiplier.CompareTo(b.multiplier));
                getListAdBidFloorFromServer = true;
                SaveLoadHandler.Save("FALCON_LIST_AD_BID_FLOORS", _listAdBidFloors);
            }
        }

        public void ProcessAdUnitId(MediationType type)
        {
            if (dictionaryRewarded.Count + dictionaryInterstitial.Count != _listAdBidFloors.Count)
            {
                dictionaryRewarded.Clear();
                dictionaryInterstitial.Clear();
                foreach (var adBidFloor in _listAdBidFloors)
                {
                    var timeConfig = adBidFloor.timeConfig;
                    var timeConfigs = timeConfig.Split(',');
                    var list = new List<int>();
                    foreach (var config in timeConfigs)
                    {
                        bool a = int.TryParse(config, out int b);
                        if (a)
                        {
                            list.Add(b);
                        }
                    }

                    if (adBidFloor.rewarded)
                    {
                        if (!dictionaryRewarded.ContainsKey(adBidFloor.adUnitId))
                        {
                            if (type == MediationType.LevelPlay)
                            {
                                dictionaryRewarded.Add(adBidFloor.adUnitId, new AdTypeMultiCall
                                {
                                    index = dictionaryRewarded.Count, adUnitId = adBidFloor.adUnitId,
                                    listTimeRetry = list, countLoadAttempt = 0, timeLoad = TimeUtils.UTCNow,
                                    eCpm = -100, multiplier = adBidFloor.multiplier
#if IRONSOURCE_ENABLE
                                    , levelPlayRewardedAd = new LevelPlayRewardedAd(adBidFloor.adUnitId)
#endif
                                });
                            }
                            else
                            {
                                dictionaryRewarded.Add(adBidFloor.adUnitId, new AdTypeMultiCall
                                {
                                    index = dictionaryRewarded.Count, adUnitId = adBidFloor.adUnitId,
                                    listTimeRetry = list, countLoadAttempt = 0, timeLoad = TimeUtils.UTCNow,
                                    eCpm = -100, multiplier = adBidFloor.multiplier
                                });
                            }
                        }
                    }
                    else
                    {
                        if (!dictionaryInterstitial.ContainsKey(adBidFloor.adUnitId))
                        {
                            if (type == MediationType.LevelPlay)
                            {
                                dictionaryInterstitial.Add(adBidFloor.adUnitId, new AdTypeMultiCall
                                {
                                    index = dictionaryInterstitial.Count, adUnitId = adBidFloor.adUnitId,
                                    listTimeRetry = list, countLoadAttempt = 0, timeLoad = TimeUtils.UTCNow,
                                    eCpm = -100, multiplier = adBidFloor.multiplier
#if IRONSOURCE_ENABLE
                                    , levelPlayInterstitialAd = new LevelPlayInterstitialAd(adBidFloor.adUnitId)
#endif
                                });
                            }
                            else
                            {
                                dictionaryInterstitial.Add(adBidFloor.adUnitId, new AdTypeMultiCall
                                {
                                    index = dictionaryInterstitial.Count, adUnitId = adBidFloor.adUnitId,
                                    listTimeRetry = list, countLoadAttempt = 0, timeLoad = TimeUtils.UTCNow,
                                    eCpm = -100, multiplier = adBidFloor.multiplier
                                });
                            }
                        }
                    }
                }
            }
        }

        public List<KeyValuePair<string, AdTypeMultiCall>> GetDictionarySortedByMultiplier(
            Dictionary<string, AdTypeMultiCall> dict)
        {
            return dict.OrderBy(x => x.Value.multiplier).ThenBy(x => x.Value.index).ToList();
        }

        public List<KeyValuePair<string, AdTypeMultiCall>> GetDictionarySortedByIndex(
            Dictionary<string, AdTypeMultiCall> dict)
        {
            return dict.OrderBy(x => x.Value.index).ToList();
        }

        public AdTypeMultiCall GetAdTypeMultiCallByIndex(int index, Dictionary<string, AdTypeMultiCall> dict)
        {
            foreach (var multiCall in dict)
            {
                if (multiCall.Value.index == index)
                {
                    return multiCall.Value;
                }
            }

            return null;
        }

        public DateTime dateTimeTimeShow;

        private static MultiCall _instance;

        public static MultiCall Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MultiCall();
                }

                return _instance;
            }
        }

        private IMultiCallProvider _provider;

        private readonly List<IMultiCallProvider> _multiCallProvidersMax = new();
        private readonly List<IMultiCallProvider> _multiCallProvidersIronSource = new();
        private readonly List<IMultiCallProvider> _multiCallProvidersAdmob = new();
        public readonly Dictionary<string, AdTypeMultiCall> dictionaryInterstitial = new();
        public readonly Dictionary<string, AdTypeMultiCall> dictionaryRewarded = new();

        public void Register(IMultiCallProvider provider, MediationType mediationType)
        {
            if (provider == null) return;
            switch (mediationType)
            {
                case MediationType.Max:
                    if (_multiCallProvidersMax.Contains(provider)) return;
                    _multiCallProvidersMax.Add(provider);
                    break;
                case MediationType.LevelPlay:
                    if (_multiCallProvidersIronSource.Contains(provider)) return;
                    _multiCallProvidersIronSource.Add(provider);
                    break;
                case MediationType.Admob:
                    if (_multiCallProvidersAdmob.Contains(provider)) return;
                    _multiCallProvidersAdmob.Add(provider);
                    break;
            }
        }

        private IMultiCallProvider GetMultiCallProvider()
        {
            if (_provider == null)
            {
                MediationManager.Instance.DebugLog("multiplier from remote config : " + MULTIPLIER);
                var type = MediationManager.Instance.CurrentMediationType();
                var index = 0;
                var list = new List<IMultiCallProvider>();
                switch (type)
                {
                    case MediationType.Max:
                        index = FConfigControllerCms.Instance.Config<MultiCallConfig>().moMulProviderIndexMax;
                        // index = 3;
                        list = _multiCallProvidersMax;
                        break;
                    case MediationType.LevelPlay:
                        index = FConfigControllerCms.Instance.Config<MultiCallConfig>().moMulProviderIndexIronSource;
                        list = _multiCallProvidersIronSource;
                        break;
                    case MediationType.Admob:
                        index = FConfigControllerCms.Instance.Config<MultiCallConfig>().moMulProviderIndexAdmob;
                        list = _multiCallProvidersAdmob;
                        break;
                }

                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].GetIndex() == index)
                    {
                        _provider = list[i];
                        break;
                    }
                }

                if (_provider == null && list.Count > 0)
                {
                    _provider = list[0];
                }
            }

            return _provider;
        }

        private MultiCall()
        {
        }

        private bool? _isLowEndDeviceCached;

        public bool IsLowEndDevice()
        {
            if (_isLowEndDeviceCached.HasValue) return _isLowEndDeviceCached.Value;
            var ram = SystemInfo.systemMemorySize / 1024f;
            var lowEnd = ram < FConfigControllerCms.Instance.Config<MultiCallConfig>().minRamMultiCall;
            _isLowEndDeviceCached = lowEnd;
            if (lowEnd)
            {
                MediationManager.Instance.DebugLog("RAM : " + ram);
            }

            return lowEnd;
        }

        public bool IsUseMobileData()
        {
            if (FConfigControllerCms.Instance.Config<MultiCallConfig>().blockMulticallFromMobileData)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable ||
                    Application.internetReachability ==
                    NetworkReachability.ReachableViaCarrierDataNetwork)
                {
                    return true;
                }
            }

            return false;
        }

        public void OnMediationInitCompleted()
        {
            MediationManager.Instance.DebugLog("Init mediation completed");
            GetMultiCallProvider()?.RegisterEvent();
        }

        private readonly HashSet<string> _hashInterstitial = new();
        private readonly HashSet<string> _hashRewarded = new();
        private readonly HashSet<string> _hashBanner = new();
        private readonly HashSet<string> _hashAppOpen = new();

        public void SetIdInterstitialRewarded()
        {
            foreach (var adBidFloor in ListAdBidFloors)
            {
                if (adBidFloor.rewarded)
                {
                    _hashRewarded.Add(adBidFloor.adUnitId);
                }
                else
                {
                    _hashInterstitial.Add(adBidFloor.adUnitId);
                }
            }
        }

        public void CallBeforeInit()
        {
            var result = string.Join(",", _hashInterstitial.Union(_hashRewarded));
#if MAX_ENABLE
            MaxSdk.SetExtraParameter(_DISABLE_B2_B, result);
            MediationManager.Instance.DebugLog("CallBeforeInit : " + _DISABLE_B2_B + " - " + result);
#endif
        }

        public void CallAfterInit()
        {
#if MAX_ENABLE
            foreach (var v in _hashInterstitial)
            {
                MaxSdk.SetInterstitialExtraParameter(v, _DISABLE_RETRIES, _TRUE);
                MediationManager.Instance.DebugLog("CallAfterInit interstitial: " + v + " - " + _DISABLE_RETRIES);
            }

            foreach (var v in _hashRewarded)
            {
                MaxSdk.SetRewardedAdExtraParameter(v, _DISABLE_RETRIES, _TRUE);
                MediationManager.Instance.DebugLog("CallAfterInit rewarded: " + v + " - " + _DISABLE_RETRIES);
            }
#endif
        }

        public void SetIdBanner(string idBanner)
        {
            _hashBanner.Add(idBanner);
        }

        public void SetIdAppOpen(string idAppOpen)
        {
            _hashAppOpen.Add(idAppOpen);
        }

        public string[] GetInitParams()
        {
            var merged = _hashInterstitial.Union(_hashRewarded).Union(_hashBanner).Union(_hashAppOpen);
            return merged.ToArray();
        }

        public bool IsInterstitialReady()
        {
            var provider = GetMultiCallProvider();
            if (provider != null)
            {
                return provider.IsInterstitialReady();
            }

            return false;
        }

        public class AdTypeMultiCall
        {
            public int index;
            public string adUnitId;
            public List<int> listTimeRetry; //list time retry, split by ","
            public int countLoadAttempt; //number load ads
            public DateTime timeLoad; //time call request load ads
            public bool isWaiting;
            public double eCpm;
            public string networkName;
            public float multiplier; //multiplier 
#if IRONSOURCE_ENABLE
            public LevelPlayInterstitialAd levelPlayInterstitialAd;
            public LevelPlayRewardedAd levelPlayRewardedAd;
#endif
        }

        public void ShowInterstitial(
            string placementId, Action onInterstitialClosed, Action onFailed, bool showForRewarded = false)
        {
            var provider = GetMultiCallProvider();
            if (provider != null)
            {
                provider.ShowInterstitial(placementId, onInterstitialClosed, onFailed, showForRewarded);
            }
            else
            {
                MediationManager.Instance.DebugError("show inter multicall provider is null");
                onInterstitialClosed?.Invoke();
            }
        }

        public bool IsRewardedVideoReady()
        {
            var provider = GetMultiCallProvider();
            if (provider != null)
            {
                return GetMultiCallProvider().IsRewardedVideoReady();
            }

            return false;
        }

        public void ShowRewardedVideo(string placementId, Action onRewardedComplete, Action onFailed)
        {
            var provider = GetMultiCallProvider();
            if (provider != null)
            {
                GetMultiCallProvider().ShowRewardedVideo(placementId, onRewardedComplete, onFailed);
            }
            else
            {
                MediationManager.Instance.DebugError("show reward video multicall provider is null");
                onFailed?.Invoke();
            }
        }

        public void LogToServer(
            string eventName, int loadTimes, AdType adType, string adWhere, string networkName,
            string mediation, double revenue, string errorMess, string adUnitId, string floor, double loadingTime)
        {
            if (!enableLogMultiCall) return;
            new MAdLog(eventName, loadTimes, adType, adWhere, networkName, mediation, revenue, errorMess, adUnitId,
                floor, loadingTime).Send();
        }

        public void DelayCall(float time, string adUnitId, Action<string> action)
        {
            MainGameObj.Instance.StartCoroutine(RetryDelay());

            IEnumerator RetryDelay()
            {
                yield return new WaitForSecondsRealtime(time);
                action.Invoke(adUnitId);
            }
        }
    }
}