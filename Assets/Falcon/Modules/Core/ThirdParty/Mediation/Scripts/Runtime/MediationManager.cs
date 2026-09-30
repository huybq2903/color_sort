/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.RemoteConfigCms;
using Falcon.Modules.Core.ThirdParty.Ump.Runtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [Serializable]
    public class RewardedCallback
    {
        public List<Reward> listReward;
    }

    [Serializable]
    public class Reward
    {
        public string name;
        public int amount;
        public string data;
    }

    public class AppsflyerCampaignInfo
    {
        public string campId;
        public string country;
        public string campName;
        public float cost;
        public float cpi;
        public int installs;
        public string firstDataTime;
        public string lastUpdatedTime;
        public DateTime createdAt;
        public DateTime lastUpdatedAt;
    }

    public class StatusAds
    {
        public StatusShowAds statusShowAds;
        public double remainingTime;
        public int remainingAttempts;
        public double remainingTimeForGroup;
    }

    public enum StatusShowAds
    {
        Active,
        LevelLimit,
        IntervalTime,
        IntervalLevel,
        IntervalBetweenIv,
        DayLimit,
        SessionLimit,
        LevelUnlock,
        DeActive,
        DayLimitGroup
    }

    public class MediationManager : AutoSingleton<MediationManager>
    {
        private bool _isInitialized;
        private bool _isInitializedManual;
        public Action onBannerShow;
        public Action onBannerHide;
        public Action onBannerLoaded;
        public Action onInitialized;

        private SOMediationSetting _setting;

        public int NumberShowInterstitial { get; set; }

        private MediationType _mediationType;

        public MediationType CurrentMediationType()
        {
            return _mediationType;
        }

        public Action onButtonRemoveAds;

        private readonly List<IMediationProvider> _providers = new();

        public void Register(IMediationProvider provider)
        {
            if (provider == null) return;
            if (_providers.Contains(provider)) return;
            _providers.Add(provider);
            _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }


        private IAdsService _mediationGma;

        private IAdsService MediationGma
        {
            get
            {
                if (_mediationGma == null)
                    _mediationGma = new FalconGmaService();

                return _mediationGma;
            }
        }

        private IAdsService _mediationMax;

        private IAdsService MediationMax
        {
            get
            {
                if (_mediationMax == null)
                    _mediationMax = new FalconMaxService();

                return _mediationMax;
            }
        }

        private IAdsService _mediationIronSource;

        private IAdsService MediationIronSource
        {
            get
            {
                if (_mediationIronSource == null)
                    _mediationIronSource = new FalconIronSourceService();

                return _mediationIronSource;
            }
        }

        public IAdsService GetAdsService()
        {
            var mediationDefault = FConfigControllerCms.Instance.Config<MediationConfig>().mediationDefault;
            switch (mediationDefault)
            {
                case (int)MediationType.Admob:
                    return MediationGma;
                case (int)MediationType.LevelPlay:
                    return MediationIronSource;
            }

            return MediationMax;
        }

        public const string EVENT_START_PURCHASE = "falcon.modules.iap.start_purchase";
        public const string EVENT_START_INIT = "falcon.modules.mediation.start_init";
        public const string EVENT_SHOWING_ADS = "falcon.modules.mediation.showing_ads";

        private bool _register;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            _ = Instance;
        }

        private IMediationProvider GetProvider()
        {
            if (_providers.Count > 0)
            {
                return _providers[0];
            }

            return null;
        }

        private bool _doneConfigUpdateFromNet;
        private bool _doneUmp;
        private bool _doneCampaignIdAppsflyer;
        private bool _doneCampaignNameAppsflyer;

        private string _campaignId;
        private string _campaignName;

        private void OnUpdateCampaignNameFromAppsflyer(string campaignName)
        {
            if (_doneCampaignNameAppsflyer) return;
            _doneCampaignNameAppsflyer = true;
            _campaignName = campaignName;
        }

        private void OnUpdateCampaignIdFromAppsflyer(string campaignId)
        {
            if (_doneCampaignIdAppsflyer) return;
            _doneCampaignIdAppsflyer = true;
            _campaignId = campaignId;
        }

        public void SetUpBidFloor(List<AdBidFloor> adBidFloor)
        {
            foreach (var bidFloor in adBidFloor)
            {
                Debug.Log("adunitid : " + bidFloor.adUnitId);
                Debug.Log("isrewarded : " + bidFloor.rewarded);
                Debug.Log("multiplier : " + bidFloor.multiplier);
            }
        }

        private IEnumerator SendRequest()
        {
            string url =
                    "https://dwh-access.data4game.com/api/v1/client/appsflyer-camp-cpi/" +
                    _campaignId +
                    "?packageName=" +
                    Application.identifier +
                    "&platform="
#if UNITY_ANDROID
                    + "android"
#elif UNITY_IOS
                    + "ios"
#endif
                ;
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Client-Key", "68f88e5d9179f33fa5c416a6");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<AppsflyerCampaignInfo>(request.downloadHandler.text);
                if (DateTime.TryParseExact(data.firstDataTime, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime parsedDate))
                {
                    data.createdAt = parsedDate;
                }

                if (DateTime.TryParseExact(data.lastUpdatedTime, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime lastUpdated))
                {
                    data.lastUpdatedAt = lastUpdated;
                }

                _doneCampaignNameAppsflyer = true;
                _campaignName = data.campName;
            }
            else
            {
                Debug.LogError($"Request failed: {request.result} | {request.error}");
            }
        }

        private IEnumerator InitMediationFromCampaignName(string campaignName)
        {
            if (campaignName.ToLower().Contains("_ios_ap_") || campaignName.ToLower().Contains("_and_ap_"))
            {
                InternalInitMediation(MediationType.Max);
            }
            else if (campaignName.ToLower().Contains("_ios_is_") || campaignName.ToLower().Contains("_and_is_"))
            {
                InternalInitMediation(MediationType.LevelPlay);
            }
            // else if (campaignName.ToLower().Contains("_ios_ga_") || campaignName.ToLower().Contains("_and_ga_"))
            // {
            //     InitMediation(MediationType.Admob);
            // }
            else
            {
                //đợi remote config
                yield return new WaitUntil(() => _doneConfigUpdateFromNet || Time.time - _startTime >= _TIME_OUT);
                InitMediationFromConfig();
            }
        }

        private void InitMediationFromConfig()
        {
            DebugLog("Init Mediation From Config");
            var mediationDefault = FConfigControllerCms.Instance.Config<MediationConfig>().mediationDefault;
            DebugLog("mediationDefault : " + mediationDefault);

            if (mediationDefault == (int)MediationType.LevelPlay)
            {
                InternalInitMediation(MediationType.LevelPlay);
            }
            else
            {
                InternalInitMediation(MediationType.Max);
            }
            // else
            // {
            //     InitMediation(MediationType.Admob);
            // }
        }

        private float _startTime;
        private const float _TIME_OUT = 7f;

        private IEnumerator StartProcess()
        {
            DebugLog("start process");
            _startTime = Time.time;
            // đợi ump
            yield return new WaitUntil(() => _doneUmp);
            DebugLog("ump done");
            if (_setting.useMax && _setting.useIronSource)
            {
                //đợi appsflyer service
                yield return new WaitUntil(() => _doneCampaignIdAppsflyer || Time.time - _startTime >= _TIME_OUT);
                DebugLog(_doneCampaignIdAppsflyer
                    ? "done campaign id from appsflyer"
                    : "timeout wait campaign id from appsflyer");
                _doneCampaignIdAppsflyer = true;
                //có campaign name
                if (!string.IsNullOrEmpty(_campaignName))
                {
                    DebugLog("init mediation from campaign name : " + _campaignName);
                    StartCoroutine(InitMediationFromCampaignName(_campaignName));
                }
                else
                {
                    //không có campaign name
                    //đợi remote config
                    DebugLog("campaign name is null, waiting remote config");
                    yield return new WaitUntil(() => _doneConfigUpdateFromNet || Time.time - _startTime >= _TIME_OUT);
                    DebugLog(_doneConfigUpdateFromNet ? "done wait remote config" : "timeout wait remote config");
                    //nếu campaign id rỗng thì lấy remote config init default
                    if (string.IsNullOrEmpty(_campaignId))
                    {
                        DebugLog("campaign id is null");
                        InitMediationFromConfig();
                    }
                    else
                    {
                        //campaign id khác rỗng
                        //so sánh với chuỗi campaign id của ironsource và admob
                        DebugLog("campaign id is NOT null");
                        var ironSource = FConfigControllerCms.Instance.Config<MediationConfig>()
                            .appsflyerCampaignIdsForIronSource;
                        var google = FConfigControllerCms.Instance.Config<MediationConfig>()
                            .appsflyerCampaignIdsForGoogle;

                        var listCamGma = new List<string>();
                        var listCamIrs = new List<string>();

                        if (!string.IsNullOrEmpty(ironSource))
                        {
                            listCamIrs = ironSource.Split(';').ToList();
                        }

                        if (!string.IsNullOrEmpty(google))
                        {
                            listCamGma = google.Split(';').ToList();
                        }

                        if (listCamIrs.Contains(_campaignId))
                        {
                            //init ironsource
                            InternalInitMediation(MediationType.LevelPlay);
                        }
                        // else if (listCamGma.Contains(_campaignId))
                        // {
                        //     //init gma
                        //     InitMediation(MediationType.Admob);
                        // }
                        else
                        {
                            //lấy campaign name từ api
                            DebugLog("get campaign name from api");
                            yield return StartCoroutine(SendRequest());
                            if (!string.IsNullOrEmpty(_campaignName))
                            {
                                DebugLog("init mediation from campaign name : " + _campaignName);
                                StartCoroutine(InitMediationFromCampaignName(_campaignName));
                            }
                            else
                            {
                                DebugLog("init mediation from remote config");
                                InitMediationFromConfig();
                            }
                        }
                    }
                }
            }
            else if (_setting.useMax)
            {
                yield return new WaitUntil(() =>
                    MultiCall.Instance.getListAdBidFloorFromServer || Time.time - _startTime >= _TIME_OUT);
                InternalInitMediation(MediationType.Max);
            }
            else if (_setting.useIronSource)
            {
                yield return new WaitUntil(() =>
                    MultiCall.Instance.getListAdBidFloorFromServer || Time.time - _startTime >= _TIME_OUT);
                InternalInitMediation(MediationType.LevelPlay);
            }
        }

        private void Start()
        {
            GetProvider()?.InitFromManager();
            GameEvent<string>.Register("falcon.modules.thirdparty.appsflyer.campaign_name",
                OnUpdateCampaignNameFromAppsflyer, null);
            GameEvent<string>.Register("falcon.modules.thirdparty.appsflyer.campaign_id",
                OnUpdateCampaignIdFromAppsflyer, null);
            GameEvent<bool>.Register(FalconUMP.FALCON_UMP_COMPLETE, DoneUmp, null);
            GameEvent<bool>.Register("falcon.modules.thirdparty.mediation.remove_ads", SetRemoveAds, null);

            FConfigControllerCms.Instance.OnUpdateFromNet += () => { _doneConfigUpdateFromNet = true; };
            _setting = Resources.Load<SOMediationSetting>("SOMediationSetting");
            StartCoroutine(StartProcess());
        }

        private void DoneUmp(bool obj)
        {
            _doneUmp = true;
        }

        private void InternalInitMediation(MediationType type)
        {
            if (_isInitialized) return;
            _isInitialized = true;
            _mediationType = type;
            DebugLog("mediation type : " + type);
            if (_setting.manualInit)
            {
                CheckInit();
                return;
            }
            InitMediationByType(type);
        }

        private void CheckInit()
        {
            if (_isInitialized && _isInitializedManual)
            {
                InitMediationByType(_mediationType);
            }
        }

        private void InitMediationByType(MediationType type)
        {
            DebugLog("Init mediation : " + type);
            switch (type)
            {
                case MediationType.Max:
                    MediationMax.Init(() =>
                    {
                        onInitialized?.Invoke();
                        FsnAdManager.Instance.InitFsnAd();
                    });
                    break;
                case MediationType.LevelPlay:
                    MediationIronSource.Init(() =>
                    {
                        onInitialized?.Invoke();
                        FsnAdManager.Instance.InitFsnAd();
                    });
                    break;
                case MediationType.Admob:
                    MediationGma.Init(() =>
                    {
                        onInitialized?.Invoke();
                        FsnAdManager.Instance.InitFsnAd();
                    });
                    break;
            }
        }

        public void InitMediation()
        {
            if (_isInitializedManual) return;
            _isInitializedManual = true;
            CheckInit();
        }

        public bool CanShowAppOpen(string placementId = "AppOpen")
        {
            if (_setting.UseAppOpenMax && _mediationType == MediationType.Max)
            {
                var provider = GetProvider();
                if (provider != null)
                {
                    return provider.CanShowAppOpen(placementId);
                }
            }

            return true;
        }

        public bool IsBannerReady()
        {
            return GetAdsService().IsBannerReady();
        }

        private bool IsRemoveAds()
        {
            return GameData4RemoveAds.Instance.removeAds;
        }

        public void ShowBanner(string placement = "Banner")
        {
            if (IsRemoveAds()) return;
            DebugLog("mediation manager -> show banner");
            GetProvider()?.ShowBanner(placement);
        }

        public void HideBanner()
        {
            GetAdsService().HideBanner();
        }

        public void IgnoreNextAoa()
        {
#if MAX_ENABLE
            MediationMax.IgnoreNextAoa();
#else
            MediationGma.IgnoreNextAoa();
#endif
        }

        public void ShowInterstitial(
            string placementId, Action onInterstitialClosed = null, Action onFail = null, bool needAdBreak = false)
        {
            DebugLog("mediation manager -> show interstitial");
            var provider = GetProvider();
            if (provider != null)
            {
                provider.ShowInterstitial(placementId, onInterstitialClosed, onFail, needAdBreak);
            }
            else
            {
                DebugError("show inter : provider is null");
                onInterstitialClosed?.Invoke();
            }
        }

        public bool IsInterstitialReady()
        {
            return GetAdsService().IsInterstitialReady();
        }

        public void AdsClosed(AdType adType, string placementId)
        {
            GetProvider()?.AdsClosed(adType, placementId);
        }

        public void SetRemoveAds(bool value)
        {
            GameData4RemoveAds.Instance.removeAds = value;
            GameData4RemoveAds.Instance.Save();
            GameData4RemoveAds.Instance.UpdateToServer();
        }

        public void ShowRewardedVideo(
            string placementId, Action onRewardedComplete = null, Action onFail = null,
            bool showInterstitialInstead = true)
        {
            DebugLog("mediation manager -> show rewarded");
            var provider = GetProvider();
            if (provider != null)
            {
                provider.ShowRewardedVideo(placementId, onRewardedComplete, onFail, showInterstitialInstead);
            }
            else
            {
                DebugError(" ShowRewardedVideo provider is null");
                onFail?.Invoke();
            }
        }

        public bool IsRewardedVideoReady()
        {
            return GetAdsService().IsRewardedVideoReady();
        }

        public void DebugLog(object obj)
        {
            Debug.Log("<color=#FFFF80>-----  " + obj + "  -----</color>");
        }

        public void DebugError(object obj)
        {
            Debug.LogError(obj);
        }

        public StatusAds GetStatusAdsFromPlacement(string placementId, bool isRewarded = true)
        {
            var provider = GetProvider();
            if (provider != null)
            {
                return provider.GetStatusAdsFromPlacement(placementId, isRewarded);
            }

            var sa = new StatusAds
            {
                statusShowAds = StatusShowAds.DayLimit
            };
            DebugError("GetStatusAdsFromPlacement provider is null");
            return sa;
        }

        public RewardedCallback GetRewardedCallbackFromGroupId(string placementId)
        {
            var provider = GetProvider();
            if (provider != null)
            {
                return provider.GetRewardedCallbackFromGroupId(placementId);
            }

            DebugError("GetRewardedCallbackFromGroupId provider is null");
            return new RewardedCallback()
            {
                listReward = new List<Reward>()
            };
        }

        public int GetMaxViewByGroupId(string groupId)
        {
            var provider = GetProvider();
            if (provider != null)
            {
                return provider.GetMaxViewByGroupId(groupId);
            }

            DebugError("GetMaxViewByGroupId provider is null");
            return -1;
        }

        public int GetViewCountByGroupId(string groupId)
        {
            var provider = GetProvider();
            if (provider != null)
            {
                return provider.GetViewCountByGroupId(groupId);
            }

            DebugError("GetViewCountByGroupId provider is null");
            return 0;
        }

        public float GetBannerHeightInPixels()
        {
            return GetAdsService().GetBannerHeightInPixels();
        }
    }

    public enum MediationType
    {
        Max,
        LevelPlay,
        Admob
    }
}