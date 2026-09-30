/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.ActionLog.Runtime;
using Falcon.Modules.Core.BigData;
using Falcon.Modules.Core.RemoteConfigCms;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Modules.Core.ThirdParty.Firebase.Scripts.Runtime;
using Falcon.Modules.Core.ThirdParty.Mmp.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Modules.Level.Core;
#if IRONSOURCE_ENABLE
using Falcon.Modules.Core.Utils.Time.Runtime;
using Falcon.Modules.Level.Core;
using Unity.Services.LevelPlay;
#endif
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class FalconIronSourceService : IAdsService
    {
        private string _placementIdBanner;
        private string _placementId;
        private const string _BANNER_IRS = "banner";
        private const string _INTERSTITIAL_IRS = "interstitial";
        private const string _REWARDED_IRS = "rewarded_video";
        private const string _IRON_SOURCE = "ironSource";

        private SOMediationSetting _settings;

        private readonly IFDeviceInfoRepository _deviceInfoRepository =
            MySingletonService.Instance<IFDeviceInfoRepository>();

        private bool _isInitSuccessful, _rewardedHasClosed, _rewardedHasRewarded;
        private readonly Dictionary<string, DateTime> _dictionaryTimeAds = new();

        private Action _actionFromUser;

        private Action _onRewardedComplete;
        private Action _onRewardedFailed;
        private Action _onInterstitialClosed;
        private Action _onInterstitialFailed;
        private DateTime _dateTimeTimeShow;
        private bool _hasClick;
        private float _timeBreak;
        private bool _bannerIsLoaded;
        private bool _bannerIsShow;
        private bool _useMultiCall;

#if IRONSOURCE_ENABLE
        private LevelPlayBannerAd _bannerAd;
        private LevelPlayInterstitialAd _interstitialAd;
        private LevelPlayRewardedAd _rewardedAd;
#endif

        public void Init(Action actionFromUser = null)
        {
            _timeBreak = FConfigControllerCms.Instance.Config<MediationConfig>().timeBreak;
            var settings = Resources.Load<SOMediationSetting>("SOMediationSetting");
            if (_isInitSuccessful) return;
            _actionFromUser = actionFromUser;
            _settings = settings;
#if IRONSOURCE_ENABLE
            LevelPlay.SetMetaData("is_child_directed", "false");
#if UNITY_IOS
            appId = _settings.ironSourceAppKeyIOS;
#endif
            var id = settings.interstitialIdMultiCallIrsAndroid;
#if UNITY_IOS
            id = settings.interstitialIdMultiCallIrsIOS;
#endif
            _useMultiCall = !string.IsNullOrWhiteSpace(id) && !MultiCall.Instance.IsUseMobileData() &&
                            !MultiCall.Instance.IsLowEndDevice();
            if (_useMultiCall)
            {
                MultiCall.Instance.ProcessConfigFromServer(MediationType.LevelPlay);
            }
            else
            {
                MediationManager.Instance.DebugLog("isLowEnd Device : " + MultiCall.Instance.IsLowEndDevice());
            }

            LevelPlay.SetPauseGame(true);
            LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
            LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
            LevelPlay.SetMetaData("is_test_suite", "enable");
#endif
            MediationManager.Instance.DebugLog("Init IronSource");
            if (!_useMultiCall)
            {
                EventStartInit();
            }
            else
            {
                GameEvent.Register(MediationManager.EVENT_START_INIT, EventStartInit, null);
            }
        }

        public bool InternalCanShowAppOpen(string placementId)
        {
            return false;
        }

        private void EventStartInit()
        {
            var userId = _deviceInfoRepository.DeviceId;
            var appId = _settings.ironSourceAppKeyAndroid;
#if IRONSOURCE_ENABLE
            LevelPlay.Init(appId, userId);
#endif
        }

#if IRONSOURCE_ENABLE
        void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
        {
            MediationManager.Instance.DebugLog("unity-script: I got SdkInitializationCompletedEvent with config: " +
                                               config);
            // LevelPlay.LaunchTestSuite();
            // LevelPlay.ValidateIntegration();
            _isInitSuccessful = true;

            if (_settings.UseBannerIrs)
            {
                InitBanner();
                LoadBanner();
                // ShowBanner();
                HideBanner(true);
            }

            if (_settings.UseInterstitialIrs)
            {
                if (!_useMultiCall)
                {
                    InitInterstitial();
                    LoadInterstitial();
                }
                else
                {
                    // MultiCall.Instance.LoadInterstitial();
                }
            }

            if (_settings.UseRewardedIrs)
            {
                if (!_useMultiCall)
                {
                    InitRewardedVideo();
                    LoadRewardedVideo();
                }
                else
                {
                    // MultiCall.Instance.LoadRewarded();
                }
            }

            LevelPlay.OnImpressionDataReady += OnImpressionDataReadyEvent;
            _actionFromUser?.Invoke();
            GameEvent.Emit(MediationManager.EVENT_INIT_MEDIATION_COMPLETED);
        }

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

            //log to client data
            AdLogClient(adInfo, type);
            //log bigdata
            AdLog(adInfo, type);
            if (!_useMultiCall)
            {
                //log to adInfo
                AdLogInfo(adInfo, type);
            }

            LogBambooTaichi.Instance.Log(adInfo.Revenue.Value, type);

            //log firebase
            FalconFirebaseLog.LogImpression(_IRON_SOURCE, adInfo.AdNetwork, adInfo.InstanceName, adInfo.AdFormat,
                adInfo.Revenue.Value);
            //log mmp
            FalconMmpLog.LogRevenue(adInfo.AdNetwork, adInfo.AdFormat, adInfo.Revenue.Value, adInfo.InstanceName,
                adInfo.Placement);
        }

        private void AdLogInfo(LevelPlayImpressionData adInfo, AdType type)
        {
            if (adInfo?.Revenue == null) return;
            double timeShow = 0;
            switch (type)
            {
                case AdType.Interstitial:
                case AdType.Reward:
                    timeShow = (DateTime.Now - _dateTimeTimeShow).TotalSeconds;
                    break;
            }

            new CSAdInfo(_idAd, _placementId, type.ToString(), adInfo.AdNetwork, "IronSrc", (int)timeShow,
                adInfo.Revenue.Value).Send();
        }

        private void AdLogClient(LevelPlayImpressionData adInfo, AdType type)
        {
            if (adInfo?.Revenue == null) return;
            if (GameData4AdInfo.Instance.adTypeToInfo == null)
            {
                GameData4AdInfo.Instance.adTypeToInfo = new Dictionary<AdType, AdInfo>();
            }

            if (GameData4AdInfo.Instance.adTypeToInfo.ContainsKey(type))
            {
                GameData4AdInfo.Instance.adTypeToInfo[type].ltv += adInfo.Revenue.Value;
                GameData4AdInfo.Instance.adTypeToInfo[type].count++;
            }
            else
            {
                GameData4AdInfo.Instance.adTypeToInfo.Add(type, new AdInfo { count = 1, ltv = adInfo.Revenue.Value });
            }

            GameData4AdInfo.Instance.UpdateToServer();
        }

        void SdkInitializationFailedEvent(LevelPlayInitError error)
        {
            MediationManager.Instance.DebugLog("unity-script: I got SdkInitializationFailedEvent with error: " + error);
        }

#endif

        public void UnregisterEvent()
        {
            if (_settings.UseBannerIrs) UnregisterBannerEvents(); //Remove AdInfo Banner Events
            if (_settings.UseInterstitialIrs) UnregisterInterstitialEvents(); //Remove Interstitial Events
            if (_settings.UseRewardedIrs) UnregisterRewardedEvents(); //Remove RewardedVideo Events
#if IRONSOURCE_ENABLE
            LevelPlay.OnImpressionDataReady -= OnImpressionDataReadyEvent;
#endif
        }

#if IRONSOURCE_ENABLE
        private void AdLog(LevelPlayImpressionData impressionData, AdType type)
        {
            if (impressionData?.Revenue == null) return;
            double timeShow = (DateTime.Now - _dateTimeTimeShow).TotalSeconds;
            if (type == AdType.Banner)
            {
                if (string.IsNullOrEmpty(_placementIdBanner))
                {
                    _placementIdBanner = "Banner";
                }

                MySingletonService.Instance<BannerLogService>().Log(_placementIdBanner, impressionData.Precision,
                    impressionData.Country, impressionData.Revenue.Value, impressionData.AdNetwork, "IronSrc", LevelData.Instance.level);
                return;
            }

            new ExtendAdLog(type, _placementId, impressionData.Precision, impressionData.Country,
                impressionData.Revenue.Value, impressionData.AdNetwork, "IronSrc", timeShow, _hasClick,
                LevelData.Instance.level).Send();
            _hasClick = false;
        }

#endif

        //banner
        private int _countAttemptBanner = 0;

        private void RegisterBannerEvents()
        {
#if IRONSOURCE_ENABLE
            _bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
            _bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
            _bannerAd.OnAdClicked += BannerOnAdClickedEvent;
            _bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
            _bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;
            _bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;

#endif
        }

        private void UnregisterBannerEvents()
        {
#if IRONSOURCE_ENABLE
            _bannerAd.OnAdLoaded -= BannerOnAdLoadedEvent;
            _bannerAd.OnAdLoadFailed -= BannerOnAdLoadFailedEvent;
            _bannerAd.OnAdClicked -= BannerOnAdClickedEvent;
            _bannerAd.OnAdCollapsed -= BannerOnAdCollapsedEvent;
            _bannerAd.OnAdExpanded -= BannerOnAdExpandedEvent;
            _bannerAd.OnAdDisplayed -= BannerOnAdDisplayedEvent;
#endif
        }

        public void IgnoreNextAoa()
        {
        }

        private float _height;

        private void InitBanner()
        {
            var id = _settings.bannerIdAndroid;
#if UNITY_IOS
            id = _settings.bannerIdIOS;
#endif
#if IRONSOURCE_ENABLE
            var configBuilder = new LevelPlayBannerAd.Config.Builder();
            configBuilder.SetSize(LevelPlayAdSize.CreateAdaptiveAdSize());
            configBuilder.SetPosition(LevelPlayBannerPosition.BottomCenter);
            _bannerAd = new LevelPlayBannerAd(id, configBuilder.Build());
            RegisterBannerEvents();
#endif
        }

        public void LoadBanner()
        {
            if (!_isInitSuccessful) return;
            MediationManager.Instance.DebugLog("IronSource > LoadBanner");
#if IRONSOURCE_ENABLE
            _bannerAd.LoadAd();
#endif
        }

        public void LoadCollapsibleBanner()
        {
            MediationManager.Instance.DebugError("ironsource not support collapsible Banner --> call from Google");
            // throw new NotImplementedException();
        }

        public void ShowCollapsibleBanner()
        {
            MediationManager.Instance.DebugError("ironsource not support collapsible Banner --> call from Google");
            // throw new NotImplementedException();
        }

        public void HideCollapsibleBanner()
        {
            MediationManager.Instance.DebugError("ironsource not support collapsible Banner --> call from Google");
            // throw new NotImplementedException();
        }

        public void DestroyBanner()
        {
            MediationManager.Instance.DebugLog("IronSource > DestroyBanner");

            if (!_isInitSuccessful || !_settings.UseBannerIrs) return;
#if IRONSOURCE_ENABLE
            _bannerAd.DestroyAd();
#endif
        }

        private bool _callShowBanner;

        public void ShowBanner(string where)
        {
            MediationManager.Instance.DebugLog("Call > ShowBanner");
            _callShowBanner = true;
            _placementIdBanner = where;
            if (_settings == null) return;
            if (!_isInitSuccessful) return;
            if (!_settings.UseBannerIrs) return;
            if (IsRemoveAds()) return;
            MediationManager.Instance.DebugLog("IronSource > ShowBanner *** UseBanner: " + _settings.UseBannerIrs);

#if IRONSOURCE_ENABLE
            _bannerAd.ShowAd();
#endif
            if (_bannerIsLoaded)
            {
                _bannerIsShow = true;
                MediationManager.Instance.onBannerShow?.Invoke();
                if (_settings.useButtonCloseBanner)
                {
                    BannerCloseButtonUI.Show(_height, false);
                }
            }
        }

        public void HideBanner(bool fromInit = false)
        {
            MediationManager.Instance.DebugLog("Call > HideBanner");
            if (!fromInit)
            {
                _callShowBanner = false;
            }

            if (_settings == null) return;
            if (!_isInitSuccessful) return;
            if (!_settings.UseBannerIrs) return;
            MediationManager.Instance.DebugLog("IronSource > HideBanner *** UseBanner: " + _settings.UseBannerIrs);
#if IRONSOURCE_ENABLE
            _bannerAd.HideAd();
#endif
            if (_bannerIsShow)
            {
                _bannerIsShow = false;
                MediationManager.Instance.onBannerHide?.Invoke();
            }
        }

        public bool IsBannerReady()
        {
            return _bannerIsLoaded;
        }

        public float GetBannerHeightInPixels()
        {
            return _height;
        }

#if IRONSOURCE_ENABLE
        //Invoked once the banner has loaded
        private void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            if (adInfo.AdSize != null)
            {
                var heightDp = adInfo.AdSize.Height;
                _height = heightDp * (Screen.dpi / 160f);
            }

            MediationManager.Instance.DebugLog("IronSource > BannerOnAdLoadedEvent");
            MediationManager.Instance.DebugLog("adformat : " + adInfo.AdFormat);
            _countAttemptBanner = 0;
            _bannerIsLoaded = true;
            MediationManager.Instance.onBannerLoaded?.Invoke();
            if (_callShowBanner)
            {
                ShowBanner(_placementIdBanner);
                _callShowBanner = false;
            }
        }

        //Invoked when the banner loading process has failed.
        private void BannerOnAdLoadFailedEvent(LevelPlayAdError ironSourceError)
        {
            _countAttemptBanner++;
            MediationManager.Instance.DebugError(
                "IronSource > BannerOnAdLoadFailedEvent *** IronSourceError: " + ironSourceError);
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptBanner));
            DelayCall(retryDelay, LoadBanner);
            _bannerIsLoaded = false;
        }

        // Invoked when end user clicks on the banner ad
        private void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > BannerOnAdClickedEvent");
        }

        private void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > BannerOnAdCollapsedEvent");
        }

        private void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > BannerOnAdExpandedEvent");
        }

        private void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
        {
        }

#endif
        //end banner
        //Interstitial

        private int _countAttemptInterstitial = 0;
        private string _idAd;

        private void RegisterInterstitialEvents()
        {
#if IRONSOURCE_ENABLE
            _interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
            _interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailed;
            _interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
            _interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
            _interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
            _interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
#endif
        }

        private void UnregisterInterstitialEvents()
        {
#if IRONSOURCE_ENABLE
            _interstitialAd.OnAdLoaded -= InterstitialOnAdLoadedEvent;
            _interstitialAd.OnAdLoadFailed -= InterstitialOnAdLoadFailed;
            _interstitialAd.OnAdDisplayed -= InterstitialOnAdDisplayedEvent;
            _interstitialAd.OnAdClicked -= InterstitialOnAdClickedEvent;
            _interstitialAd.OnAdDisplayFailed -= InterstitialOnAdDisplayFailedEvent;
            _interstitialAd.OnAdClosed -= InterstitialOnAdClosedEvent;
#endif
        }

        private void InitInterstitial()
        {
            var adId = _settings.interstitialIdAndroid;
#if UNITY_IOS
            adId = _settings.interstitialIdIOS;
#endif
#if IRONSOURCE_ENABLE
            _interstitialAd = new LevelPlayInterstitialAd(adId);
            RegisterInterstitialEvents();
#endif
        }

        public void LoadInterstitial()
        {
            MediationManager.Instance.DebugLog("IronSource > LoadInterstitial");
#if IRONSOURCE_ENABLE
            _interstitialAd.LoadAd();
#endif
        }

        public bool IsInterstitialReady()
        {
            if (_settings == null) return false;
            if (!_isInitSuccessful) return false;
            if (_useMultiCall) return MultiCall.Instance.IsInterstitialReady();
#if IRONSOURCE_ENABLE
            return _settings.UseInterstitialIrs && _interstitialAd.IsAdReady();
#endif
            return false;
        }

        private void ShowInterstitial(
            string where, Action onInterstitialClosed = null, Action onFail = null,
            bool needAdBreak = false, bool showForRewarded = false)
        {
            if (_settings == null) return;
            if (IsRemoveAds() && !showForRewarded)
            {
                onInterstitialClosed?.Invoke();
                return;
            }

            if (!IsInterstitialReady())
            {
                onFail?.Invoke();
                return;
            }

            MediationManager.Instance.DebugLog("IronSource > ShowInterstitial");
            _placementId = where;
            GameEvent<object>.Emit(MediationManager.EVENT_SHOWING_ADS);
            _onInterstitialClosed = onInterstitialClosed;
            _onInterstitialFailed = onFail;
            _hasClick = false;

            if ((needAdBreak && _timeBreak > 0 && !showForRewarded) ||
                MediationManager.Instance.GetRewardedCallbackFromGroupId(where).listReward.Count > 0)
            {
                UIWrapper.OpenPopup("UIPopupAdBreak",
                    p => { p.SendMessage("UpdateUI", (_timeBreak, (Action)OnClose)); });
            }
            else
            {
                OnClose();
            }

            return;

            void OnClose()
            {
                _dateTimeTimeShow = DateTime.Now;
#if IRONSOURCE_ENABLE
                if (_useMultiCall)
                    MultiCall.Instance.ShowInterstitial(_placementId, onInterstitialClosed, onFail);
                else
                {
                    _idAd = Guid.NewGuid().ToString();
                    new CSAdStart(_idAd, AdType.Interstitial.ToString(), _placementId, _networkNameInterstitial,
                        _IRON_SOURCE, _interstitialAd.AdUnitId).Send();
                    new FAdCalledLog(AdType.Interstitial, _placementId, "IronSource", LevelData.Instance.level)
                        .Send();
                    _interstitialAd.ShowAd();
                }

#endif
                FalconMmpLog.LogEvent(FalconMmpLog.MMP_INTERS_SHOW, new Dictionary<string, string>
                {
                    { "af_level", LevelData.Instance.level + "" }
                });
            }
        }

        public void ShowInterstitial(
            string where, Action onInterstitialClosed = null, Action onFail = null,
            bool needAdBreak = false)
        {
            ShowInterstitial(where, onInterstitialClosed, onFail, needAdBreak, false);
        }

        private string _networkNameInterstitial;
        private string _networkNameRewarded;

#if IRONSOURCE_ENABLE
        //Invoked when the interstitial ad was loaded successfully.
        private void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > InterstitialOnAdLoadedEvent");
            double ecpm = 0;
            if (adInfo.Revenue != null)
            {
                ecpm = adInfo.Revenue.Value;
            }

            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_LOAD_SUCCESS);
            if (_useMultiCall) return;
            _countAttemptInterstitial = 0;
            _networkNameInterstitial = adInfo.AdNetwork;
        }

        //Invoked when the initialization process has failed.
        private void InterstitialOnAdLoadFailed(LevelPlayAdError ironSourceError)
        {
            MediationManager.Instance.DebugError("IronSource > InterstitialOnAdLoadFailed *** IronSourceError: " +
                                                 ironSourceError);
            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_LOAD_FAILED, ironSourceError.ErrorMessage);
            if (_useMultiCall) return;
            _countAttemptInterstitial++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptInterstitial));
            DelayCall(retryDelay, LoadInterstitial);
        }

        //Invoked when the Interstitial Ad Unit has opened. This is the impression indication.
        private void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > InterstitialOnAdDisplayedEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_SHOW);
            FalconMmpLog.LogEvent(FalconMmpLog.MMP_INTERS_DISPLAYED, new Dictionary<string, string>
            {
                { "af_level", LevelData.Instance.level + "" }
            });
            //log FActionLog
            var dict = new Dictionary<string, string>
            {
                { "ad_where", _placementId },
                { "ad_network", adInfo.AdNetwork }
            };
            FActionLogManager.Log($"start_{AdType.Interstitial.ToString()}", dict);
            MediationManager.Instance.NumberShowInterstitial++;
        }

        //Invoked when end user clicked on the interstitial ad
        private void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > InterstitialOnAdClickedEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_CLICKED);
            _hasClick = true;
        }

        //Invoked when the ad failed to show.
        private void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError ironSourceError)
        {
            MediationManager.Instance.DebugError(
                "IronSource > InterstitialOnAdDisplayFailedEvent *** IronSourceError: " +
                ironSourceError);
            if (_useMultiCall) return;
            _onInterstitialFailed?.Invoke();
            new FAdDisplayFailedLog(AdType.Interstitial, _placementId, "IronSource", LevelData.Instance.level)
                .Send();
            DelayCall(0.5f, LoadInterstitial);
        }

        //Invoked when the interstitial ad closed and the user went back to the application screen.
        private void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > InterstitialOnAdClosedEvent");

            if (!_useMultiCall)
            {
                var time = (int)(DateTime.Now - _dateTimeTimeShow).TotalSeconds;
                new CSAdFinish(_idAd, AdType.Interstitial.ToString(), _placementId, adInfo.AdNetwork, _IRON_SOURCE,
                    adInfo.AdUnitId, adInfo.Revenue ?? 0, time).Send();
            }

            MediationManager.Instance.AdsClosed(AdType.Interstitial, _placementId);
            //log FActionLog
            var dict = new Dictionary<string, string>
            {
                { "ad_where", _placementId },
                { "ad_network", adInfo.AdNetwork }
            };
            FActionLogManager.Log($"finish_{AdType.Interstitial.ToString()}", dict);

            if (_useMultiCall) return;
            _onInterstitialClosed?.Invoke();
            DelayCall(0.5f, LoadInterstitial);
        }

#endif
        //end interstitial
        //rewarded

        private int _countAttemptRewarded = 0;
        private IAdsService _adsServiceImplementation;

        private void RegisterRewardedEvents()
        {
#if IRONSOURCE_ENABLE
            _rewardedAd.OnAdDisplayed += RewardedVideoOnAdDisplayedEvent;
            _rewardedAd.OnAdClosed += RewardedVideoOnAdClosedEvent;
            _rewardedAd.OnAdLoaded += RewardedVideoOnAdLoadedEvent;
            _rewardedAd.OnAdLoadFailed += RewardedVideoOnAdLoadFailed;
            _rewardedAd.OnAdDisplayFailed += RewardedVideoOnAdDisplayFailedEvent;
            _rewardedAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
            _rewardedAd.OnAdClicked += RewardedVideoOnAdClickedEvent;
#endif
        }

        private void UnregisterRewardedEvents()
        {
#if IRONSOURCE_ENABLE
            _rewardedAd.OnAdDisplayed -= RewardedVideoOnAdDisplayedEvent;
            _rewardedAd.OnAdClosed -= RewardedVideoOnAdClosedEvent;
            _rewardedAd.OnAdLoaded -= RewardedVideoOnAdLoadedEvent;
            _rewardedAd.OnAdLoadFailed -= RewardedVideoOnAdLoadFailed;
            _rewardedAd.OnAdDisplayFailed -= RewardedVideoOnAdDisplayFailedEvent;
            _rewardedAd.OnAdRewarded -= RewardedVideoOnAdRewardedEvent;
            _rewardedAd.OnAdClicked -= RewardedVideoOnAdClickedEvent;
#endif
        }

        private void InitRewardedVideo()
        {
            var adId = _settings.rewardedIdAndroid;
#if UNITY_IOS
            adId = _settings.rewardedIdIOS;
#endif
#if IRONSOURCE_ENABLE
            _rewardedAd = new LevelPlayRewardedAd(adId);
            RegisterRewardedEvents();
#endif
        }

        private void LoadRewardedVideo()
        {
            MediationManager.Instance.DebugLog("IronSource > LoadRewardedVideo");
#if IRONSOURCE_ENABLE
            _rewardedAd.LoadAd();
#endif
        }

        public bool IsRewardedVideoReady()
        {
            if (_settings == null) return false;
            if (!_isInitSuccessful) return false;
#if IRONSOURCE_ENABLE
            if (_useMultiCall) return MultiCall.Instance.IsRewardedVideoReady();
            return _settings.UseRewardedIrs && _rewardedAd.IsAdReady();
#endif
            return false;
        }

        public void ShowRewardedVideo(
            string where, Action onRewardedComplete = null, Action onFail = null,
            bool showInterstitialInstead = true)
        {
            if (_settings == null) return;
            if (!IsRewardedVideoReady())
            {
                if (showInterstitialInstead)
                {
                    MediationManager.Instance.DebugLog(" Reward not available, show Interstitial instead");
                    if (IsInterstitialReady())
                    {
                        ShowInterstitial(where, onRewardedComplete, onFail, true);
                    }
                    else
                    {
                        onFail?.Invoke();
                    }
                }
                else
                {
                    onFail?.Invoke();
                }

                return;
            }

            MediationManager.Instance.DebugLog("IronSource > ShowRewardedVideo");

            _onRewardedComplete = onRewardedComplete;
            _onRewardedFailed = onFail;

            _placementId = where;
            GameEvent<object>.Emit(MediationManager.EVENT_SHOWING_ADS);
            _rewardedHasClosed = false;
            _rewardedHasRewarded = false;
            _hasClick = false;
            _dateTimeTimeShow = DateTime.Now;
#if IRONSOURCE_ENABLE
            if (_useMultiCall)
                MultiCall.Instance.ShowRewardedVideo(_placementId, onRewardedComplete, onFail);
            else
            {
                _idAd = Guid.NewGuid().ToString();
                new CSAdStart(_idAd, AdType.Reward.ToString(), _placementId, _networkNameRewarded, _IRON_SOURCE,
                    _rewardedAd.AdUnitId).Send();
                new FAdCalledLog(AdType.Reward, _placementId, "IronSource", LevelData.Instance.level)
                    .Send();
                _rewardedAd.ShowAd();
            }

#endif
            FalconMmpLog.LogEvent(FalconMmpLog.MMP_REWARDED_SHOW, new Dictionary<string, string>
            {
                { "af_level", LevelData.Instance.level + "" }
            });
        }

#if IRONSOURCE_ENABLE
        //Indicates that there’s an available ad.
        //The adInfo object includes information about the ad that was loaded successfully
        //This replaces the RewardedVideoAvailabilityChangedEvent(true) event
        private void RewardedVideoOnAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > RewardedVideoOnAdAvailable");
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_LOAD);
            if (_useMultiCall) return;
            _countAttemptRewarded = 0;
            _networkNameRewarded = adInfo.AdNetwork;
        }

        //Indicates that no ads are available to be displayed
        //This replaces the RewardedVideoAvailabilityChangedEvent(false) event
        private void RewardedVideoOnAdLoadFailed(LevelPlayAdError error)
        {
            MediationManager.Instance.DebugError("IronSource > RewardedVideoOnAdLoadFailed ---- " + error);
            if (_useMultiCall) return;
            _countAttemptRewarded++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptRewarded));
            DelayCall(retryDelay, LoadRewardedVideo);
        }

        //The Rewarded Video ad view has opened. Your activity will loose focus.
        private void RewardedVideoOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > RewardedVideoOnAdDisplayedEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_SHOW_SUCCESS);
            FalconMmpLog.LogEvent(FalconMmpLog.MMP_REWARDED_DISPLAYED, new Dictionary<string, string>
            {
                { "af_level", LevelData.Instance.level + "" }
            });

            //log FActionLog
            var dict = new Dictionary<string, string>
            {
                { "ad_where", _placementId },
                { "ad_network", adInfo.AdNetwork }
            };
            FActionLogManager.Log($"start_{AdType.Reward.ToString()}", dict);
        }

        //The Rewarded Video ad view is about to be closed. Your activity will regain its focus.
        //Note:  The onRewardedVideoAdRewardedEvent and onRewardedVideoAdClosedEvent are asynchronous.
        //Make sure to set up your listener to grant rewards even in cases where onRewardedVideoAdRewarded is fired after the onRewardedVideoAdClosedEvent.
        private void RewardedVideoOnAdClosedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > RewardedVideoOnAdClosedEvent");
            _rewardedHasClosed = true;
            MediationManager.Instance.AdsClosed(AdType.Reward, _placementId);
            if (!_useMultiCall)
            {
                var time = (int)(DateTime.Now - _dateTimeTimeShow).TotalSeconds;
                new CSAdFinish(_idAd, AdType.Reward.ToString(), _placementId, adInfo.AdNetwork, _IRON_SOURCE,
                    adInfo.AdUnitId, adInfo.Revenue ?? 0, time).Send();
            }

            //log FActionLog
            var dict = new Dictionary<string, string>
            {
                { "ad_where", _placementId },
                { "ad_network", adInfo.AdNetwork }
            };
            FActionLogManager.Log($"finish_{AdType.Reward.ToString()}", dict);

            if (_useMultiCall) return;
            if (!CheckFireRewardedEvent()) return;
            MediationManager.Instance.StartCoroutine(GetReward());
        }

        IEnumerator GetReward()
        {
            yield return new WaitForEndOfFrame();

            _onRewardedComplete?.Invoke();
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_COMPLETE);
        }

        //The user completed to watch the video, and should be rewarded.
        //The placement parameter will include the reward data.
        //When using server-to-server callbacks, you may ignore this event and wait for the ironSource server callback.
        private void RewardedVideoOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            MediationManager.Instance.DebugLog("IronSource > RewardedVideoOnAdRewardedEvent");
            _rewardedHasRewarded = true;
            if (!CheckFireRewardedEvent()) return;
            MediationManager.Instance.StartCoroutine(GetReward());
        }

        private bool CheckFireRewardedEvent()
        {
            return _rewardedHasClosed && _rewardedHasRewarded;
        }

        //The rewarded video ad was failed to show.
        private void RewardedVideoOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            MediationManager.Instance.DebugError("IronSource > RewardedVideoOnAdShowFailedEvent *** IronSourceError: " +
                                                 error);
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_SHOW_FAIL, error.ErrorMessage);
            if (_useMultiCall) return;
            _onRewardedFailed?.Invoke();
            new FAdDisplayFailedLog(AdType.Reward, _placementId, "IronSource", LevelData.Instance.level)
                .Send();
            DelayCall(0.5f, LoadRewardedVideo);
        }

        //Invoked when the video ad was clicked.
        //This callback is not supported by all networks, and we recommend using it only if
        //it’s supported by all networks you included in your build.
        private void RewardedVideoOnAdClickedEvent(LevelPlayAdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("IronSource > RewardedVideoOnAdClickedEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_CLICK);
            _hasClick = true;
        }

#endif
        //end rewarded

        private void DelayCall(float time, Action action)
        {
            MediationManager.Instance.StartCoroutine(RetryDelay());

            IEnumerator RetryDelay()
            {
                yield return new WaitForSecondsRealtime(time);
                action.Invoke();
            }
        }

        private bool IsRemoveAds()
        {
            return GameData4RemoveAds.Instance.removeAds;
        }
    }
}