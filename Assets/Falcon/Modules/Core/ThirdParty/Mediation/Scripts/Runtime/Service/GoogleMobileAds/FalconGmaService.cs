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
using Falcon.Modules.Core.ThirdParty.Mmp.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Modules.Core.ThirdParty.Firebase.Scripts.Runtime;
using Falcon.Modules.Level.Core;
#if GOOGLE_MOBILE_ADS_ENABLE
using Falcon.Modules.Level.Core;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class FalconGmaService : IAdsService
    {
        private string _placementIdBanner;
        private string _placementId;
        private bool _isShowingAds;
        private SOMediationSetting _settings;

        private static bool? _isInitialized;

        private Action _onRewardedComplete;
        private Action _onRewardedFailed;
        private Action _onInterstitialClosed;
        private Action _onInterstitialFailed;
        private DateTime _dateTimeTimeShow;
        private bool _hasClick;
        private float _timeBreak;
        private DateTime _showAppOpenTime;
        private bool _useMultiCall;

        private Action _actionFromUser;

        public void Init(Action actionFromUser = null)
        {
            if (_isInitialized.HasValue)
            {
                return;
            }

            GameEvent<object>.Register(MediationManager.EVENT_START_PURCHASE, Action, null);
            GameEvent<object>.Register(MediationManager.EVENT_SHOWING_ADS, Action, null);
            _actionFromUser = actionFromUser;

            void Action(object obj)
            {
                _isShowingAds = true;
            }

            _timeBreak = FConfigControllerCms.Instance.Config<MediationConfig>().timeBreak;

            var settings = Resources.Load<SOMediationSetting>("SOMediationSetting");
            _settings = settings;
            _isInitialized = false;
            // Initialize the Google Mobile Ads Unity plugin.
            MediationManager.Instance.DebugLog("Google Mobile Ads Initializing.");
            var idInterstitial = settings.interstitialIdMultiCallGAAndroid;
#if UNITY_IOS
                idInterstitial = settings.interstitialIdMultiCallGAIOS;
#endif
            var idRewarded = settings.rewardedIdMultiCallGAAndroid;
#if UNITY_IOS
                idRewarded = settings.rewardedIdMultiCallGAIOS;
#endif
            _useMultiCall = MultiCall.Instance.ListAdBidFloors.Count > 0 && !MultiCall.Instance.IsUseMobileData() &&
                            !MultiCall.Instance.IsLowEndDevice();
            if (!_useMultiCall)
            {
                EventStartInit();
            }
            else
            {
                GameEvent.Register(MediationManager.EVENT_START_INIT, EventStartInit, null);
            }
        }

        private void EventStartInit()
        {
#if GOOGLE_MOBILE_ADS_ENABLE
            MobileAds.Initialize((InitializationStatus initstatus) =>
            {
                if (initstatus == null)
                {
                    MediationManager.Instance.DebugError("Google Mobile Ads initialization failed.");
                    _isInitialized = null;
                    DelayCall(0.5f, () => { Init(); });
                    return;
                }

                // If you use mediation, you can check the status of each adapter.
                //var adapterStatusMap = initstatus.getAdapterStatusMap();
                //if (adapterStatusMap != null)
                //{
                //    foreach (var item in adapterStatusMap)
                //    {
                //        MediationManager.Instance.DebugLog(string.Format("Adapter {0} is {1}",
                //            item.Key,
                //            item.Value.InitializationState));
                //    }
                //}
                _isInitialized = true;
                MediationManager.Instance.DebugLog("Google Mobile Ads initialization complete.");
                if (_settings.UseBannerGa)
                {
                    LoadBanner();
                    HideBanner(true);
                }

                if (_settings.UseInterstitialGa)
                {
                    if (!_useMultiCall)
                        LoadInterstitial();
                }

                if (_settings.UseRewardedGa)
                {
                    if (!_useMultiCall)
                        LoadRewardedAd();
                }

                if (_settings.UseAppOpenIrs && !IsRemoveAds())
                {
                    LoadAppOpenAd();
                    GMAEvent.Instance.applicationAwakeEvent += ApplicationAwakeEvent;
                    GMAEvent.Instance.RegisterEvent();
                }

                if (_settings.UseCollapsibleBannerGa)
                {
                    LoadCollapsibleBanner();
                }

                _actionFromUser?.Invoke();
            });
#endif
        }

#if GOOGLE_MOBILE_ADS_ENABLE
        public void OnAdPaid(AdValue adValue, AdType type, AdapterResponseInfo adapterResponseInfo)
        {
            if (adValue == null) return;
            double value = adValue.Value * 0.000001f;
            //log for client data
            AdLogClient(value, type);
            //log to data4game
            AdLog(value, type, adapterResponseInfo);
            //log firebase
            FalconFirebaseLog.LogImpression("admob", adapterResponseInfo.AdSourceName,
                adapterResponseInfo.AdSourceInstanceName, type.ToString(), value);
            //log mmp
            FalconMmpLog.LogRevenue("admob", type.ToString(), value,
                adapterResponseInfo.AdSourceInstanceName);
        }

        private void AdLogClient(double value, AdType type)
        {
            if (GameData4AdInfo.Instance.adTypeToInfo == null)
            {
                GameData4AdInfo.Instance.adTypeToInfo = new Dictionary<AdType, AdInfo>();
            }

            if (GameData4AdInfo.Instance.adTypeToInfo.ContainsKey(type))
            {
                GameData4AdInfo.Instance.adTypeToInfo[type].ltv += value;
                GameData4AdInfo.Instance.adTypeToInfo[type].count++;
            }
            else
            {
                GameData4AdInfo.Instance.adTypeToInfo.Add(type, new AdInfo { count = 1, ltv = value });
            }

            GameData4AdInfo.Instance.UpdateToServer();
        }

        private void AdLog(double value, AdType type, AdapterResponseInfo adapterResponseInfo)
        {
            double timeShow = (DateTime.Now - _dateTimeTimeShow).TotalSeconds;
            if (type == AdType.Banner)
            {
                if (string.IsNullOrEmpty(_placementIdBanner))
                {
                    _placementIdBanner = "Banner";
                }

                MySingletonService.Instance<BannerLogService>().Log(_placementIdBanner, "",
                    "", value, adapterResponseInfo.AdSourceName, "Admob", LevelData.Instance.level);
                return;
            }

            new ExtendAdLog(type, _placementId, "", "", value, adapterResponseInfo.AdSourceName, "Admob",
                timeShow, _hasClick, LevelData.Instance.level).Send();

            _hasClick = false;
        }

#endif

        #region app open

#if GOOGLE_MOBILE_ADS_ENABLE
        private AppOpenAd _appOpenAd;
#endif
        private int _countAttemptAppOpen = 0;

        private void LoadAppOpenAd()
        {
            if (IsRemoveAds()) return;
#if GOOGLE_MOBILE_ADS_ENABLE
            // Clean up the old ad before loading a new one.
            if (_appOpenAd != null)
            {
                UnregisterAppOpenEventHandlers(_appOpenAd);
                _appOpenAd.Destroy();
                _appOpenAd = null;
            }

            MediationManager.Instance.DebugLog("GMA > Loading the app open ad.");
            // Create our request used to load the ad.
            var adRequest = new AdRequest();
            // send the request to load the ad.
            var id = _settings.appOpenIdGAAndroid;
#if UNITY_IOS
            id = _settings.appOpenIdGAIOS;
#endif
            AppOpenAd.Load(id, adRequest,
                (AppOpenAd ad, LoadAdError error) =>
                {
                    // if error is not null, the load request failed.
                    if (error != null || ad == null)
                    {
                        MediationManager.Instance.DebugError("GMA > app open ad failed to load an ad " +
                                                             "with error : " +
                                                             error);
                        _countAttemptAppOpen++;
                        var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptAppOpen));
                        DelayCall(retryDelay, LoadAppOpenAd);
                        return;
                    }

                    MediationManager.Instance.DebugLog("GMA > App open ad loaded with response : " +
                                                       ad.GetResponseInfo());
                    _appOpenAd = ad;
                    RegisterAppOpenEventHandlers(ad);
                });
#endif
        }

#if GOOGLE_MOBILE_ADS_ENABLE
        private void ApplicationAwakeEvent()
        {
            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        }

        private bool IsAppOpenAdAvailable()
        {
            return _appOpenAd != null && _appOpenAd.CanShowAd();
        }
#endif

        private IEnumerator Wait02S()
        {
            yield return new WaitForSeconds(0.2f);
            if (_isShowingAds)
            {
                _isShowingAds = false;
                yield break;
            }

            if (MediationManager.Instance.CanShowAppOpen())
            {
                ShowAppOpenAd();
            }
        }

        public bool InternalCanShowAppOpen(string placementId)
        {
            //nếu chưa hết thời gian cooldown thì break
            if (!IsOverTimeCoolDown()) return false;
            return _settings.minLevelShowAoaMax <= LevelData.Instance.level;
        }

        private bool IsOverTimeCoolDown()
        {
            return (DateTime.Now - _showAppOpenTime).TotalSeconds >= _settings.cooldownTimeGa;
        }

        private void ShowAppOpenAd()
        {
            if (IsRemoveAds()) return;
#if GOOGLE_MOBILE_ADS_ENABLE
            if (IsAppOpenAdAvailable())
            {
                MediationManager.Instance.DebugLog("Showing app open ad.");
                _appOpenAd.Show();
                _showAppOpenTime = DateTime.Now;
            }
            else
            {
                MediationManager.Instance.DebugError("App open ad is not ready yet.");
            }
#endif
        }
#if GOOGLE_MOBILE_ADS_ENABLE
        public void OnAppStateChanged(AppState state)
        {
            if (IsRemoveAds()) return;
            MediationManager.Instance.DebugLog("---------OnAppStateChanged : " + state);
            if (state == AppState.Foreground)
            {
                GMAEvent.Instance.StartCoroutine(Wait02S());
            }
        }

        private void RegisterAppOpenEventHandlers(AppOpenAd ad)
        {
            ad.OnAdPaid += OnAppOpenAdPaid;
            ad.OnAdFullScreenContentClosed += OnAppOpenAdFullScreenContentClosed;
            ad.OnAdFullScreenContentFailed += OnAppOpenAdFullScreenContentFailed;
        }

        private void UnregisterAppOpenEventHandlers(AppOpenAd ad)
        {
            ad.OnAdPaid -= OnAppOpenAdPaid;
            ad.OnAdFullScreenContentClosed -= OnAppOpenAdFullScreenContentClosed;
            ad.OnAdFullScreenContentFailed -= OnAppOpenAdFullScreenContentFailed;
        }

        void OnAppOpenAdPaid(AdValue adValue)
        {
            MediationManager.Instance.DebugLog("GMA > App open OnAppOpenAdPaid.");
            var adapterResponseInfo = _appOpenAd.GetResponseInfo().GetLoadedAdapterResponseInfo();
            OnAdPaid(adValue, AdType.AppOpen, adapterResponseInfo);
        }

        void OnAppOpenAdFullScreenContentFailed(AdError error)
        {
            MediationManager.Instance.DebugError("GMA > App open ad failed to open full screen content with error : " +
                                                 error);
            LoadAppOpenAd();
        }
#endif
        void OnAppOpenAdFullScreenContentClosed()
        {
            MediationManager.Instance.DebugLog("GMA > App open ad full screen content closed.");
            MediationManager.Instance.AdsClosed(AdType.AppOpen, "AppOpen");
            LoadAppOpenAd();
        }

        #endregion

        #region Intertitial

        private string _idAd;
        private int _countAttemptInterstitial = 0;
#if GOOGLE_MOBILE_ADS_ENABLE
        private InterstitialAd _interstitialAd;

        private void RegisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdPaid += OnInterstitialAdPaid;
            ad.OnAdClicked += OnInterstitialAdClicked;
            ad.OnAdFullScreenContentOpened += OnInterstitialAdFullScreenContentOpened;
            ad.OnAdFullScreenContentClosed += OnInterstitialAdFullScreenContentClosed;
            ad.OnAdFullScreenContentFailed += OnInterstitialAdFullScreenContentFailed;
        }

        private void UnregisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdPaid -= OnInterstitialAdPaid;
            ad.OnAdClicked -= OnInterstitialAdClicked;
            ad.OnAdFullScreenContentOpened -= OnInterstitialAdFullScreenContentOpened;
            ad.OnAdFullScreenContentClosed -= OnInterstitialAdFullScreenContentClosed;
            ad.OnAdFullScreenContentFailed -= OnInterstitialAdFullScreenContentFailed;
        }

        private void OnInterstitialAdPaid(AdValue adValue)
        {
            var adapterResponseInfo = _interstitialAd.GetResponseInfo().GetLoadedAdapterResponseInfo();
            OnAdPaid(adValue, AdType.Interstitial, adapterResponseInfo);
        }

        private void OnInterstitialAdFullScreenContentFailed(AdError obj)
        {
            MediationManager.Instance.DebugLog("GMA > OnIntertitialAdFullScreenContentFailed");
            new FAdDisplayFailedLog(AdType.Interstitial, _placementId, "IronSource", LevelData.Instance.level)
                .Send();
            _onInterstitialFailed?.Invoke();
            DelayCall(0.5f, LoadInterstitial);
        }

        private void OnInterstitialAdFullScreenContentClosed()
        {
            MediationManager.Instance.DebugLog("GMA > OnInterstitialAdFullScreenContentClosed");
            MediationManager.Instance.AdsClosed(AdType.Interstitial, _placementId);
            _onInterstitialClosed?.Invoke();
            var time = (int)(DateTime.Now - _dateTimeTimeShow).TotalSeconds;
            var adapterResponseInfo = _interstitialAd.GetResponseInfo().GetLoadedAdapterResponseInfo();
            new CSAdFinish(_idAd, AdType.Interstitial.ToString(), _placementId, adapterResponseInfo.AdSourceName,
                "Admob", _interstitialAd.GetAdUnitID(), 0, time).Send();

            //log FActionLog
            var dict = new Dictionary<string, string>
            {
                { "ad_where", _placementId },
                { "ad_network", adapterResponseInfo.AdSourceName }
            };
            FActionLogManager.Log($"finish_{AdType.Interstitial.ToString()}", dict);

            DelayCall(0.5f, LoadInterstitial);
        }
#endif

        private void OnInterstitialAdClicked()
        {
            MediationManager.Instance.DebugLog("GMA > OnInterstitialClickedEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_CLICKED);
            _hasClick = true;
        }


        private void OnInterstitialAdFullScreenContentOpened()
        {
            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_SHOW);
            FalconMmpLog.LogEvent(FalconMmpLog.MMP_INTERS_DISPLAYED, new Dictionary<string, string>
            {
                { "af_level", LevelData.Instance.level + "" }
            });
#if GOOGLE_MOBILE_ADS_ENABLE
            var adapterResponseInfo = _interstitialAd.GetResponseInfo().GetLoadedAdapterResponseInfo();
            //log FActionLog
            var dict = new Dictionary<string, string>
            {
                { "ad_where", _placementId },
                { "ad_network", adapterResponseInfo.AdSourceName }
            };
            FActionLogManager.Log($"start_{AdType.Interstitial.ToString()}", dict);
#endif
            MediationManager.Instance.NumberShowInterstitial++;
        }

        public bool IsInterstitialReady()
        {
            if (_useMultiCall)
                return MultiCall.Instance.IsInterstitialReady();
#if GOOGLE_MOBILE_ADS_ENABLE
            return _interstitialAd != null && _interstitialAd.CanShowAd();
#else
            return false;
#endif
        }

        private void ShowInterstitial(
            string where, Action onInterstitialClosed = null, Action onFail = null,
            bool needAdBreak = false, bool showForRewarded = false)
        {
            if (_settings == null) return;
            if (!_settings.UseInterstitialGa) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
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

/*
            if ((float)eCPM_inter < FalconConfig.Instance<FalconMediationConfig>().f_ecpm_show_inter)
            {
                MediationManager.Instance.DebugLog("GMA == ecpm_inter < " + FalconConfig.Instance<FalconMediationConfig>().f_ecpm_show_inter);
                return;
            }
*/
            MediationManager.Instance.DebugLog("GMA > ShowInterstitial");
            _placementId = where;
            _isShowingAds = true;
            var id = _settings.interstitialIdGAAndroid;
#if UNITY_IOS
            id = _settings.interstitialIdGAIOS;
#endif
            _onInterstitialClosed = onInterstitialClosed;
            _onInterstitialFailed = onFail;
            _hasClick = false;
            _dateTimeTimeShow = DateTime.Now;
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
                _idAd = Guid.NewGuid().ToString();
                if (_useMultiCall)
                    MultiCall.Instance.ShowInterstitial(where, onInterstitialClosed, onFail);
#if GOOGLE_MOBILE_ADS_ENABLE
                var adapterResponseInfo = _interstitialAd.GetResponseInfo().GetLoadedAdapterResponseInfo();
                new CSAdStart(_idAd, AdType.Interstitial.ToString(), where, adapterResponseInfo.AdSourceName,
                    "Admob", _interstitialAd.GetAdUnitID()).Send();
                new FAdCalledLog(AdType.Interstitial, where, "Admob", LevelData.Instance.level)
                    .Send();
                _interstitialAd.Show();
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

        private void LoadInterstitial(string id)
        {
#if GOOGLE_MOBILE_ADS_ENABLE
            if (_interstitialAd != null)
            {
                UnregisterInterstitialEvents(_interstitialAd);
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }

            MediationManager.Instance.DebugLog("GMA > Loading the interstitial ad.");
            // create our request used to load the ad.
            var adRequest = new AdRequest();
            // send the request to load the ad.
            InterstitialAd.Load(id, adRequest,
                (InterstitialAd ad, LoadAdError error) =>
                {
                    // if error is not null, the load request failed.
                    if (error != null || ad == null)
                    {
                        MediationManager.Instance.DebugError(
                            "GMA > interstitial ad failed to load an ad with error : " + error);
                        FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_LOAD_FAILED, error.ToString());
                        _countAttemptInterstitial++;
                        var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptInterstitial));
                        DelayCall(retryDelay, LoadInterstitial);
                        return;
                    }

                    MediationManager.Instance.DebugLog("GMA > Interstitial ad loaded with response : " +
                                                       ad.GetResponseInfo());
                    _interstitialAd = ad;
                    RegisterInterstitialEvents(ad);

                    _countAttemptInterstitial = 0;
                    FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_LOAD_SUCCESS);
                });
#endif
        }

        public void IgnoreNextAoa()
        {
            _isShowingAds = true;
        }

        public void LoadInterstitial()
        {
            if (!_settings.UseInterstitialGa) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            var id = _settings.interstitialIdGAAndroid;
#if UNITY_IOS
            id = _settings.interstitialIdGAIOS;
#endif
            LoadInterstitial(id);
        }

        #endregion

        #region Banner collapsible

        private int _countAttemptCollapsibleBanner = 0;

        public bool IsBannerReady()
        {
            return _bannerIsLoaded;
        }

        public void ShowCollapsibleBanner()
        {
            if (!_settings.UseCollapsibleBannerGa) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            MediationManager.Instance.DebugLog("GMA > ShowCollapsibleBanner");
#if GOOGLE_MOBILE_ADS_ENABLE
            _collapsibleBannerView?.Show();
#endif
        }

        public void HideCollapsibleBanner()
        {
            MediationManager.Instance.DebugLog("Hiding Collapsible banner view.");
#if GOOGLE_MOBILE_ADS_ENABLE
            _collapsibleBannerView?.Hide();
#endif
        }
#if GOOGLE_MOBILE_ADS_ENABLE
        private BannerView _collapsibleBannerView;
#endif
        private void CreateCollapsibleBannerView()
        {
            if (!_settings.UseCollapsibleBannerGa) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            MediationManager.Instance.DebugLog("GMA > Creating Collapsible banner view");
#if GOOGLE_MOBILE_ADS_ENABLE
            if (_collapsibleBannerView != null)
            {
                DestroyCollapsibleBanner();
            }

            var adaptiveSize = AdSize.GetPortraitAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

            var id = _settings.CollapsibleBannerIdAndroid;
#if UNITY_IOS
            id = _settings.CollapsibleBannerIdIOS;
#endif
            _collapsibleBannerView = new BannerView(id, adaptiveSize, AdPosition.Bottom);
            RegisterCollapsibleBannerEvents();
#endif
        }

#if GOOGLE_MOBILE_ADS_ENABLE
        private void DestroyCollapsibleBanner()
        {
            if (_collapsibleBannerView == null) return;
            MediationManager.Instance.DebugLog("Destroying Collapsible banner view.");
            _collapsibleBannerView.Destroy();
            _collapsibleBannerView = null;
            UnregisterCollapsibleBannerEvents();
        }

        private void RegisterCollapsibleBannerEvents()
        {
            _collapsibleBannerView.OnBannerAdLoaded += OnCollapsibleBannerAdLoadedEvent;
            _collapsibleBannerView.OnBannerAdLoadFailed += OnCollapsibleBannerAdLoadFailedEvent;
            _collapsibleBannerView.OnAdPaid += OnCollapsibleBannerAdPaid;
        }

        private void UnregisterCollapsibleBannerEvents()
        {
            _collapsibleBannerView.OnBannerAdLoaded -= OnCollapsibleBannerAdLoadedEvent;
            _collapsibleBannerView.OnBannerAdLoadFailed -= OnCollapsibleBannerAdLoadFailedEvent;
            _collapsibleBannerView.OnAdPaid -= OnCollapsibleBannerAdPaid;
        }

        private void OnCollapsibleBannerAdPaid(AdValue adValue)
        {
            var adapterResponseInfo = _collapsibleBannerView.GetResponseInfo().GetLoadedAdapterResponseInfo();
            OnAdPaid(adValue, AdType.CollapsibleBanner, adapterResponseInfo);
        }

        void OnCollapsibleBannerAdLoadFailedEvent(LoadAdError errorInfo)
        {
            MediationManager.Instance.DebugError("GMA > Collapsible Banner view failed to load an ad with error : " +
                                                 errorInfo);
            _countAttemptCollapsibleBanner++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptCollapsibleBanner));
            DelayCall(retryDelay, LoadCollapsibleBanner);
        }

        void OnCollapsibleBannerAdLoadedEvent()
        {
            MediationManager.Instance.DebugLog("GMA > Collapsible Banner view loaded an ad");
            MediationManager.Instance.DebugLog(_collapsibleBannerView.IsCollapsible()
                ? "Banner is collapsible."
                : "Banner is not collapsible.");
            _countAttemptCollapsibleBanner = 0;
        }
#endif

        public void LoadCollapsibleBanner()
        {
#if GOOGLE_MOBILE_ADS_ENABLE
            if (_collapsibleBannerView == null)
            {
                CreateCollapsibleBannerView();
            }

            // create our request used to load the ad.
            var adRequest = new AdRequest();
            adRequest.Extras.Add("collapsible", "bottom");
            adRequest.Extras.Add("collapsible_request_id", Guid.NewGuid().ToString());

            // send the request to load the ad.
            MediationManager.Instance.DebugLog("GMA Collapsible banner -> Loading banner ad.");
            _collapsibleBannerView?.LoadAd(adRequest);
#endif
        }

        #endregion

        #region Banner

        private int _countAttemptBanner = 0;

        public void ShowBanner(string placement)
        {
            MediationManager.Instance.DebugLog("Call > ShowBanner");
            _callShowBanner = true;
            _placementIdBanner = placement;
            if (_settings == null) return;
            if (!_settings.UseBannerGa) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            if (IsRemoveAds()) return;
            MediationManager.Instance.DebugLog("GMA > ShowBanner");
#if GOOGLE_MOBILE_ADS_ENABLE
            _bannerView?.Show();
#endif
        }

        public void HideBanner(bool fromInit = false)
        {
            MediationManager.Instance.DebugLog("Call > HideBanner");
            if (!fromInit)
            {
                _callShowBanner = false;
            }

            if (_settings == null) return;
            MediationManager.Instance.DebugLog("Hiding banner view.");
#if GOOGLE_MOBILE_ADS_ENABLE
            _bannerView?.Hide();
#endif
        }
#if GOOGLE_MOBILE_ADS_ENABLE
        private BannerView _bannerView;
#endif
        private void CreateBannerView()
        {
            if (!_settings.UseBannerGa) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            MediationManager.Instance.DebugLog("GMA > Creating banner view");
#if GOOGLE_MOBILE_ADS_ENABLE
            if (_bannerView != null)
            {
                DestroyBanner();
            }

            var adaptiveSize = AdSize.GetPortraitAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

            var id = _settings.bannerIdGAAndroid;
#if UNITY_IOS
            id = _settings.bannerIdGAIOS;
#endif
            _bannerView = new BannerView(id, adaptiveSize, AdPosition.Bottom);
            RegisterBannerEvents();
#endif
        }

        private bool _callShowBanner;
        private bool _bannerIsLoaded;
#if GOOGLE_MOBILE_ADS_ENABLE
        private void DestroyBanner()
        {
            if (_bannerView == null) return;
            MediationManager.Instance.DebugLog("Destroying banner view.");
            _bannerView.Destroy();
            _bannerView = null;
            UnregisterBannerEvents();
        }


        private void RegisterBannerEvents()
        {
            _bannerView.OnBannerAdLoaded += OnBannerAdLoadedEvent;
            _bannerView.OnBannerAdLoadFailed += OnBannerAdLoadFailedEvent;
            _bannerView.OnAdPaid += OnBannerAdPaid;
            _bannerView.OnAdFullScreenContentOpened += OnBannerShow;
            _bannerView.OnAdFullScreenContentClosed += OnBannerHide;
        }

        private void OnBannerHide()
        {
            MediationManager.Instance.onBannerHide?.Invoke();
        }

        private void OnBannerShow()
        {
            MediationManager.Instance.onBannerShow?.Invoke();
        }

        private void UnregisterBannerEvents()
        {
            _bannerView.OnBannerAdLoaded -= OnBannerAdLoadedEvent;
            _bannerView.OnBannerAdLoadFailed -= OnBannerAdLoadFailedEvent;
            _bannerView.OnAdPaid -= OnBannerAdPaid;
            _bannerView.OnAdFullScreenContentOpened -= OnBannerShow;
            _bannerView.OnAdFullScreenContentClosed -= OnBannerHide;
        }

        private void OnBannerAdPaid(AdValue adValue)
        {
            var adapterResponseInfo = _bannerView.GetResponseInfo().GetLoadedAdapterResponseInfo();
            OnAdPaid(adValue, AdType.Banner, adapterResponseInfo);
        }

        void OnBannerAdLoadFailedEvent(LoadAdError errorInfo)
        {
            MediationManager.Instance.DebugError("GMA > Banner view failed to load an ad with error : " + errorInfo);
            _countAttemptBanner++;
            _bannerIsLoaded = false;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptBanner));
            DelayCall(retryDelay, LoadBanner);
        }

        void OnBannerAdLoadedEvent()
        {
            MediationManager.Instance.DebugLog("GMA > Banner view loaded an ad");
            MediationManager.Instance.DebugLog(_bannerView.IsCollapsible()
                ? "Banner is collapsible."
                : "Banner is not collapsible.");
            _countAttemptBanner = 0;
            _bannerIsLoaded = true;
            if (_callShowBanner)
            {
                ShowBanner(_placementIdBanner);
                _callShowBanner = false;
            }
        }

#endif

        public void LoadBanner()
        {
#if GOOGLE_MOBILE_ADS_ENABLE
            if (_bannerView == null)
            {
                CreateBannerView();
            }

            // create our request used to load the ad.
            var adRequest = new AdRequest();

            // send the request to load the ad.
            MediationManager.Instance.DebugLog("GMA banner -> Loading banner ad.");
            _bannerView?.LoadAd(adRequest);
#endif
        }

        #endregion

        #region Rewarded

        private int _countAttemptRewarded;
#if GOOGLE_MOBILE_ADS_ENABLE
        private RewardedAd _rewardedAd;
        private IAdsService _adsServiceImplementation;

        private void RegisterRewardedEventHandlers(RewardedAd ad)
        {
            ad.OnAdPaid += OnRewardedAdPaid;
            ad.OnAdClicked += OnRewardedAdClicked;
            ad.OnAdFullScreenContentOpened += OnRewardedAdFullScreenContentOpened;
            ad.OnAdFullScreenContentClosed += OnRewardedAdFullScreenContentClosed;
            ad.OnAdFullScreenContentFailed += OnRewardedAdFullScreenContentFailed;
        }

        void UnregisterRewardedEvents(RewardedAd ad)
        {
            ad.OnAdPaid -= OnRewardedAdPaid;
            ad.OnAdClicked -= OnRewardedAdClicked;
            ad.OnAdFullScreenContentOpened -= OnRewardedAdFullScreenContentOpened;
            ad.OnAdFullScreenContentClosed -= OnRewardedAdFullScreenContentClosed;
            ad.OnAdFullScreenContentFailed -= OnRewardedAdFullScreenContentFailed;
        }

        private void OnRewardedAdPaid(AdValue adValue)
        {
            var adapterResponseInfo = _rewardedAd.GetResponseInfo().GetLoadedAdapterResponseInfo();
            OnAdPaid(adValue, AdType.Reward, adapterResponseInfo);
        }
#endif
        private void LoadRewarded(string id)
        {
#if GOOGLE_MOBILE_ADS_ENABLE
            if (_rewardedAd != null)
            {
                UnregisterRewardedEvents(_rewardedAd);
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }

            MediationManager.Instance.DebugLog("GMA > Loading the rewarded ad.");
            // create our request used to load the ad.
            var adRequest = new AdRequest();
            // send the request to load the ad.
            RewardedAd.Load(id, adRequest,
                (RewardedAd ad, LoadAdError error) =>
                {
                    // if error is not null, the load request failed.
                    if (error != null || ad == null)
                    {
                        MediationManager.Instance.DebugError(
                            "GMA > rewarded ad failed to load an ad with error : " + error);
                        _countAttemptRewarded++;
                        var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptRewarded));
                        DelayCall(retryDelay, LoadRewardedAd);
                        return;
                    }

                    MediationManager.Instance.DebugLog("GMA > Rewarded ad loaded with response : " +
                                                       ad.GetResponseInfo());
                    _rewardedAd = ad;
                    RegisterRewardedEventHandlers(ad);

                    _countAttemptRewarded = 0;
                    FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_LOAD);
                });
#endif
        }

        public void LoadRewardedAd()
        {
            if (!_settings.UseInterstitialGa) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            var id = _settings.interstitialIdGAAndroid;
#if UNITY_IOS
                id = _settings.interstitialIdGAIOS;
#endif
            LoadRewarded(id);
        }

        public bool IsRewardedVideoReady()
        {
            if (_settings == null) return false;
            if (!_settings.UseRewardedGa) return false;
            if (_useMultiCall)
                return MultiCall.Instance.IsRewardedVideoReady();
#if GOOGLE_MOBILE_ADS_ENABLE
            return _rewardedAd != null && _rewardedAd.CanShowAd();
#else
            return false;
#endif
        }

        public float GetBannerHeightInPixels()
        {
            return 0;
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

            MediationManager.Instance.DebugLog("GMA > Rewarded ad ShowRewardedVideo");
            _onRewardedComplete = onRewardedComplete;
            _onRewardedFailed = onFail;

            _isShowingAds = true;
            _hasClick = false;
            _dateTimeTimeShow = DateTime.Now;
            _placementId = where;
            _idAd = Guid.NewGuid().ToString();
#if GOOGLE_MOBILE_ADS_ENABLE
            var adapterResponseInfo = _rewardedAd.GetResponseInfo().GetLoadedAdapterResponseInfo();
            new CSAdStart(_idAd, AdType.Reward.ToString(), where, adapterResponseInfo.AdSourceName,
                "Admob", _rewardedAd.GetAdUnitID()).Send();
            new FAdCalledLog(AdType.Interstitial, where, "Admob", LevelData.Instance.level)
                .Send();
            if (_useMultiCall)
            {
                MultiCall.Instance.ShowRewardedVideo(where, onRewardedComplete, onFail);
            }
            else
            {
                _rewardedAd.Show(null);
            }
#endif
            FalconMmpLog.LogEvent(FalconMmpLog.MMP_REWARDED_SHOW, new Dictionary<string, string>
            {
                { "af_level", LevelData.Instance.level + "" }
            });
        }

        private void OnRewardedAdFullScreenContentOpened()
        {
            MediationManager.Instance.DebugLog("GMA > Rewarded ad OnRewardedAdDisplayedEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_SHOW_SUCCESS);
            FalconMmpLog.LogEvent(FalconMmpLog.MMP_REWARDED_DISPLAYED, new Dictionary<string, string>
            {
                { "af_level", LevelData.Instance.level + "" }
            });
#if GOOGLE_MOBILE_ADS_ENABLE
            var adapterResponseInfo = _rewardedAd.GetResponseInfo().GetLoadedAdapterResponseInfo();
            //log FActionLog
            var dict = new Dictionary<string, string>
            {
                { "ad_where", _placementId },
                { "ad_network", adapterResponseInfo.AdSourceName }
            };
            FActionLogManager.Log($"start_{AdType.Reward.ToString()}", dict);
#endif
        }
#if GOOGLE_MOBILE_ADS_ENABLE
        private void OnRewardedAdFullScreenContentFailed(AdError error)
        {
            MediationManager.Instance.DebugLog("GMA > Rewarded ad OnRewardedAdFailedToDisplayEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_SHOW_FAIL, error.ToString());
            _onRewardedFailed?.Invoke();
            new FAdDisplayFailedLog(AdType.Reward, _placementId, "IronSource", LevelData.Instance.level)
                .Send();
            DelayCall(0.5f, LoadRewardedAd);
        }
#endif
        private void OnRewardedAdClicked()
        {
            MediationManager.Instance.DebugLog("GMA > Rewarded ad OnRewardedAdClickedEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_CLICK);
            _hasClick = true;
        }

        private void OnRewardedAdFullScreenContentClosed()
        {
            MediationManager.Instance.DebugLog("GMA > Rewarded ad OnRewardedAdHiddenEvent");
            MediationManager.Instance.AdsClosed(AdType.Reward, _placementId);
#if GOOGLE_MOBILE_ADS_ENABLE
            var adapterResponseInfo = _rewardedAd.GetResponseInfo().GetLoadedAdapterResponseInfo();
            var time = (int)(DateTime.Now - _dateTimeTimeShow).TotalSeconds;
            new CSAdFinish(_idAd, AdType.Reward.ToString(), _placementId, adapterResponseInfo.AdSourceName, "Admob",
                _rewardedAd.GetAdUnitID(), 0, time).Send();

            //log FActionLog
            var dict = new Dictionary<string, string>
            {
                { "ad_where", _placementId },
                { "ad_network", adapterResponseInfo.AdSourceName }
            };
            FActionLogManager.Log($"finish_{AdType.Reward.ToString()}", dict);
#endif

            _onRewardedComplete?.Invoke();
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_COMPLETE);

            DelayCall(0.5f, LoadRewardedAd);
        }

        #endregion

        public void UnregisterEvent()
        {
        }

        private void DelayCall(float time, Action action)
        {
            GMAEvent.Instance.StartCoroutine(RetryDelay());

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