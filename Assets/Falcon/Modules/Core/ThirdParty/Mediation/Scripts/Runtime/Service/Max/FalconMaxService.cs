/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.ActionLog.Runtime;
using Falcon.Modules.Core.BigData;
using Falcon.Modules.Core.RemoteConfigCms;
using Falcon.Modules.Core.ThirdParty.Firebase.Scripts.Runtime;
using Falcon.Modules.Core.ThirdParty.Mmp.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Modules.Core.Utils.Time.Runtime;
using Falcon.Modules.Level.Core;
#if MAX_ENABLE
using Falcon.Modules.Core.SaveLoad.Runtime;
#endif
#if AMAZON_ENABLE && !UNITY_EDITOR
using AmazonAds;
#endif
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class FalconMaxService : IAdsService
    {
        private const string _INTERSTITIAL_DISPLAYED = "MEDIATION_INTERSTITIAL_DISPLAYED";
        private const string _REWARDED_DISPLAYED = "MEDIATION_REWARDED_DISPLAYED";
        private const string _INTERSTITIAL_CLOSED = "MEDIATION_INTERSTITIAL_CLOSED";
        private const string _REWARDED_CLOSED = "MEDIATION_REWARDED_CLOSED";
        private const string _MAX = "Max";

        //giá trị dự phòng cho ad_where, để không log null nếu callback về mà chưa qua hàm show nào
        private const string _PLACEMENT_UNKNOWN = "unknown";
        private string _placementIdBanner;
        private string _placementIdRewarded;
        private string _placementIdInterstitial;
        private bool _isShowForRewarded;
        private bool _isShowingAds;
        private const string _KEY_FOR_SEGMENT_MAX = "key_for_segment_max";
        private const string _KEY_DECISION_POLICY = "decision_policy_load_ad";
        private const string _DECISION_POLICY_NO_MULTICALL = "client-nomulticall";
        private IFDeviceInfoRepository _deviceInfoRepository = MySingletonService.Instance<IFDeviceInfoRepository>();

        private bool _closedRewarded, _receivedRewarded;

        private SOMediationSetting _settings;

        private static bool? _isInitialized;

        private Action _onRewardedComplete;
        private Action _onRewardedFailed;
        private Action _onRewardedHidden;
        private Action _onInterstitialClosed;
        private Action _onInterstitialFailed;
        private DateTime _dateTimeTimeShow;
        private bool _hasClick;

        private bool _useMultiCall;
        private float _timeBreak;

        private bool _callShowBanner;

        private bool _bannerIsLoaded;
        private bool _bannerIsShow;

        private DateTime _showAppOpenTime;

        private double _eCpmInterstitial;
        private double _eCpmRewarded;

        //Tại 1 thời điểm chỉ có đúng 1 quảng cáo fullscreen được show.
        //Cờ này bật khi bắt đầu 1 lượt show (và cả khi SDK báo displayed, phòng luồng show nào chưa gán),
        //rồi tắt ở callback hidden đầu tiên. Nhờ vậy hidden bắn lặp - do SDK gửi 2 lần hoặc do handler
        //bị đăng ký chồng - sẽ không gửi thêm CSAdFinish/action log nào nữa.
        private bool _hasShowSession;

        public virtual void Init(Action actionFromUser = null)
        {
            MediationManager.Instance.DebugLog("Init max");
            if (_isInitialized.HasValue)
            {
                return;
            }

            // MaxSdk.SetVerboseLogging(true);

            var settings = Resources.Load<SOMediationSetting>("SOMediationSetting");
            _settings = settings;

            _useMultiCall = MultiCall.Instance.ListAdBidFloors.Count > 0 && !MultiCall.Instance.IsUseMobileData() &&
                            !MultiCall.Instance.IsLowEndDevice();
            // if (_useMultiCall)
            // {
            //     MultiCall.Instance.ProcessConfigFromServer(MediationType.Max);
            // }
            // else
            // {
            //     MediationManager.Instance.DebugLog("isLowEnd Device : " + MultiCall.Instance.IsLowEndDevice());
            // }

            var idBanner = _settings.bannerIdMaxAndroid;
#if UNITY_IOS
            idBanner = _settings.bannerIdMaxIOS;
#endif
            if (!string.IsNullOrWhiteSpace(idBanner))
            {
                if (_useMultiCall)
                {
                    MultiCall.Instance.SetIdBanner(idBanner);
                }
            }

            var idAppOpen = _settings.AppOpenIdMaxAndroid;
#if UNITY_IOS
            idAppOpen = _settings.AppOpenIdMaxIOS;
#endif
            if (!string.IsNullOrWhiteSpace(idAppOpen))
            {
                if (_useMultiCall)
                {
                    MultiCall.Instance.SetIdAppOpen(idAppOpen);
                }
            }

            if (_useMultiCall)
            {
                MultiCall.Instance.SetIdInterstitialRewarded();
                MultiCall.Instance.ProcessAdUnitId(MediationType.Max);
            }

            GameEvent<object>.Register(MediationManager.EVENT_START_PURCHASE, Action, null);

            void Action(object obj)
            {
                _isShowingAds = true;
            }

            _timeBreak = FConfigControllerCms.Instance.Config<MediationConfig>().timeBreak;

            _isInitialized = false;

            var appIdAmazon = settings.appIdAmazonMaxAndroid;
#if UNITY_IOS
            appIdAmazon = settings.appIdAmazonMaxIOS;
#endif
#if AMAZON_ENABLE && !UNITY_EDITOR
            AmazonAds.Amazon.Initialize(appIdAmazon);
#if UNITY_IOS
            AmazonAds.Amazon.SetAPSPublisherExtendedIdFeatureEnabled(true);
#endif
#endif

#if MAX_ENABLE
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                _isInitialized = true;
                if (_useMultiCall)
                {
                    MultiCall.Instance.CallAfterInit();
                }

                if (_settings.UseAppOpenMax && !IsRemoveAds())
                {
                    RegisterAppOpenEvents();
                    ShowAppOpenAdIfReady();
                }

                if (_settings.UseMrecMax)
                {
                    RegisterMrectEvents();
                }

                if (_settings.UseBannerMax)
                {
                    CreateBannerView();
                    // ShowBanner();
                    HideBanner(true);
                    RegisterBannerEvents();
                }

                if (_settings.UseInterstitialMax)
                {
                    if (!_useMultiCall)
                        LoadInterstitial();
                    RegisterInterstitialEvents();
                }

                if (_settings.UseRewardedMax)
                {
                    if (!_useMultiCall)
                        LoadRewardedAd();
                    RegisterRewardedEvent();
                }

                actionFromUser?.Invoke();
                if (_useMultiCall)
                    MultiCall.Instance.OnMediationInitCompleted();
            };
            var userId = _deviceInfoRepository.DeviceId;
            MaxSdk.SetUserId(userId);
            var campaignId = GameData4Appsflyer.Instance.campaignID;
            if (!string.IsNullOrEmpty(campaignId))
            {
                var old = SaveLoadHandler.Load(_KEY_FOR_SEGMENT_MAX, "");
                if (!old.Equals(campaignId))
                {
                    MaxSegmentCollection collection = MaxSegmentCollection.Builder()
                        .AddSegment(new MaxSegment(1,
                            new List<int> { EncodeStringToInt(campaignId) }))
                        .Build();
                    MaxSdk.SetSegmentCollection(collection);
                    SaveLoadHandler.Save(_KEY_FOR_SEGMENT_MAX, campaignId);
                }
            }

            EventStartInit();
#endif
        }

        private void EventStartInit()
        {
            MediationManager.Instance.DebugLog("Max Initializing.");
#if MAX_ENABLE
            if (_useMultiCall)
            {
                MultiCall.Instance.CallBeforeInit();
                MaxSdk.InitializeSdk(MultiCall.Instance.GetInitParams());
            }
            else
            {
                MaxSdk.InitializeSdk();
            }
#endif
        }

#if MAX_ENABLE
        private void RegisterMrectEvents()
        {
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMrecAdRevenuePaidEvent;
        }

        private void OnMrecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdRevenuePaidEvent(adUnitId, AdType.MREC, adInfo);
        }

        private void RegisterAppOpenEvents()
        {
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenDismissedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenAdRevenuePaidEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenLoadFailedEvent;
            MaxEvent.Instance.applicationPauseEvent += OnApplicationPause;
        }

        private void OnAppOpenLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            MediationManager.Instance.DebugLog("Max > AppOpen load failed : " + errorInfo.Message);
        }

        private void OnAppOpenLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("Max > AppOpen loaded an ad");
        }

        private void OnAppOpenAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdRevenuePaidEvent(adUnitId, AdType.AppOpen, adInfo);
        }

        public int EncodeStringToInt(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                // Lấy 4 byte đầu để tạo int
                return BitConverter.ToInt32(hashBytes, 0);
            }
        }

        private void OnAdRevenuePaidEvent(string adUnit, AdType type, MaxSdkBase.AdInfo adInfo)
        {
            //log to client data
            AdLogClient(adInfo, type);
            //log to bigdata
            AdLog(adInfo, type);
            if (!_useMultiCall)
            {
                //log to adInfo
                AdLogInfo(adInfo, type);
            }

            //log to taichi/bamboo
            LogBambooTaichi.Instance.Log(adInfo.Revenue, type);

            var adRevenue = new AdRevenueInfo
            {
                adUnit = adUnit,
                network = adInfo.NetworkName,
                format = adInfo.AdFormat,
                revenue = adInfo.Revenue,
                placement = adInfo.Placement
            };

            //user chơi lần đầu (session đầu tiên) + quảng cáo đầu tiên --> hoãn log firebase/mmp,
            //để log cùng lúc với khi CSAdFinish được gửi
            if (!_usedDeferFirstAdRevenue && IsFirstAdOfFirstSession(type))
            {
                _usedDeferFirstAdRevenue = true;
                _pendingAdRevenue = adRevenue;
                MediationManager.Instance.DebugLog("MAX > delay log firebase/mmp of first ad until CSAdFinish");
                return;
            }

            //đề phòng quảng cáo đầu tiên không nhận được sự kiện đóng --> gửi nốt log đang chờ
            FlushPendingAdRevenue();
            LogAdRevenue(adRevenue);
        }

        //quảng cáo đầu tiên của user chơi lần đầu (chỉ hoãn với loại quảng cáo có gửi CSAdFinish)
        private bool _usedDeferFirstAdRevenue;
        private AdRevenueInfo _pendingAdRevenue;

        private class AdRevenueInfo
        {
            public string adUnit;
            public string network;
            public string format;
            public double revenue;
            public string placement;
        }

        private static bool IsFirstAdOfFirstSession(AdType type)
        {
            if (type != AdType.Interstitial && type != AdType.Reward) return false;
            return FPlayerInfoService.Instance.Session.SessionId == 1;
        }

        //log firebase/mmp của quảng cáo đầu tiên, gọi cùng lúc với khi gửi CSAdFinish
        private void FlushPendingAdRevenue()
        {
            if (_pendingAdRevenue == null) return;
            var adRevenue = _pendingAdRevenue;
            _pendingAdRevenue = null;
            LogAdRevenue(adRevenue);
        }

        private void LogAdRevenue(AdRevenueInfo adRevenue)
        {
            //log firebase
            FalconFirebaseLog.LogImpression(_MAX, adRevenue.network, adRevenue.adUnit, adRevenue.format,
                adRevenue.revenue);
            //log mmp
            FalconMmpLog.LogRevenue(adRevenue.network, adRevenue.format, adRevenue.revenue, adRevenue.adUnit,
                adRevenue.placement);
            if (!GameData4AntiFraude.Instance.sendDataToAppsflyer)
            {
                GameData4AntiFraude.Instance.sendDataToAppsflyer = true;
                GameData4AntiFraude.Instance.UpdateToServer();
            }
        }

        private void AdLogInfo(MaxSdkBase.AdInfo adInfo, AdType type)
        {
            double timeShow = 0;
            string placementId = _placementIdRewarded;
            switch (type)
            {
                case AdType.Interstitial:
                    timeShow = (DateTime.Now - _dateTimeTimeShow).TotalSeconds;
                    placementId = _placementIdInterstitial;
                    break;
                case AdType.Reward:
                    timeShow = (DateTime.Now - _dateTimeTimeShow).TotalSeconds;
                    break;
            }

            new CSAdInfo(_idAd, Placement(placementId), type.ToString(), adInfo.NetworkName, _MAX, (int)timeShow,
                    adInfo.Revenue)
                .Send();
        }

        private void AdLogClient(MaxSdkBase.AdInfo adInfo, AdType type)
        {
            if (GameData4AdInfo.Instance.adTypeToInfo == null)
            {
                GameData4AdInfo.Instance.adTypeToInfo = new Dictionary<AdType, AdInfo>();
            }

            if (GameData4AdInfo.Instance.adTypeToInfo.ContainsKey(type))
            {
                GameData4AdInfo.Instance.adTypeToInfo[type].ltv += adInfo.Revenue;
                GameData4AdInfo.Instance.adTypeToInfo[type].count++;
            }
            else
            {
                GameData4AdInfo.Instance.adTypeToInfo.Add(type, new AdInfo { count = 1, ltv = adInfo.Revenue });
            }

            GameData4AdInfo.Instance.UpdateToServer();
        }

        private void AdLog(MaxSdkBase.AdInfo impressionData, AdType type)
        {
            if (type == AdType.Banner)
            {
                if (string.IsNullOrWhiteSpace(_placementIdBanner))
                {
                    _placementIdBanner = "Banner";
                }

                MySingletonService.Instance<BannerLogService>().Log(_placementIdBanner, impressionData.RevenuePrecision,
                    "", impressionData.Revenue, impressionData.NetworkName, _MAX, LevelData.Instance.level);
                return;
            }

            double timeShow = (DateTime.Now - _dateTimeTimeShow).TotalSeconds;
            string placementId = _placementIdInterstitial;
            if (type == AdType.Reward)
            {
                placementId = _placementIdRewarded;
            }
            new ExtendAdLog(type, Placement(placementId), impressionData.RevenuePrecision, "", impressionData.Revenue,
                impressionData.NetworkName, _MAX, timeShow, _hasClick, LevelData.Instance.level /*, new TimeParam()
                {
                    adTime = a
                }*/).Send();
            _hasClick = false;
        }

        private void OnAppOpenDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            var id = _settings.AppOpenIdMaxAndroid;
#if UNITY_IOS
            id = _settings.AppOpenIdMaxIOS;
#endif
            MaxSdk.LoadAppOpenAd(id);
            MediationManager.Instance.AdsClosed(AdType.AppOpen, "AppOpen");
        }

        public void OnApplicationPause(bool pauseStatus)
        {
            if (_settings.UseAppOpenMax && !pauseStatus && !IsRemoveAds())
            {
                MaxEvent.Instance.StartCoroutine(Wait02S());
            }
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
                ShowAppOpenAdIfReady();
            }
        }

        public bool InternalCanShowAppOpen(string placementId)
        {
            //nếu chưa hết thời gian cooldown thì break
            if (!IsOverTimeCoolDown())
            {
                MediationManager.Instance.DebugLog("Is over time cool down");
                return false;
            }

            if (_settings.minLevelShowAoaMax > LevelData.Instance.level)
            {
                MediationManager.Instance.DebugLog("level < min level for show");
                return false;
            }

            return _settings.minLevelShowAoaMax <= LevelData.Instance.level;
        }

        private bool IsOverTimeCoolDown()
        {
            return (DateTime.Now - _showAppOpenTime).TotalSeconds >= _settings.cooldownTimeMax;
        }

        //không show app open ở lần tiếp resume theo
        public void IgnoreNextAoa()
        {
            _isShowingAds = true;
        }

        public void ShowAppOpenAdIfReady()
        {
            if (IsRemoveAds())
            {
                MediationManager.Instance.DebugLog("MAX > remove Ads don't show app open");
                return;
            }

            var id = _settings.AppOpenIdMaxAndroid;
#if UNITY_IOS
            id = _settings.AppOpenIdMaxIOS;
#endif
#if MAX_ENABLE
            if (MaxSdk.IsAppOpenAdReady(id))
            {
                MediationManager.Instance.DebugLog("MAX > show open app");
                MaxSdk.ShowAppOpenAd(id);
                _showAppOpenTime = DateTime.Now;
            }
            else
            {
                MediationManager.Instance.DebugLog("MAX > load open app");
                MaxSdk.LoadAppOpenAd(id);
            }
#endif
        }

        #region Intertitial

        private int _countAttemptInterstitial = 0;

        private void RegisterInterstitialEvents()
        {
#if MAX_ENABLE
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialAdRevenuePaidEvent;
#endif
        }

#if MAX_ENABLE
        private void OnInterstitialAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdRevenuePaidEvent(adUnitId, AdType.Interstitial, adInfo);
        }

        private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // MediationManager.Instance.DebugLog("MAX > Loaded event intertitial : " + adUnitId);
            _eCpmInterstitial = adInfo.Revenue;
            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_LOAD_SUCCESS);
            if (_useMultiCall) return; //multi call đã xử lý
            LogAdLoadSuccess(adUnitId, adInfo);
            _countAttemptInterstitial = 0;
            _networkNameInterstitial = adInfo.NetworkName;
        }

        private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // MediationManager.Instance.DebugLog("MAX > Load failed event intertitial : " + adUnitId + " ---- : " +
            //                                    errorInfo.Message);
            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_LOAD_FAILED, errorInfo.ToString());
            if (_useMultiCall) return; //multi call đã xử lý
            LogAdLoadFailed(adUnitId, errorInfo);
            _countAttemptInterstitial++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptInterstitial));
            DelayCall(retryDelay, LoadInterstitial);
        }

        private string _idAd;

        private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // MediationManager.Instance.DebugLog("MAX > OnInterstitialDisplayedEvent intertitial");
            _hasShowSession = true;

            //log FActionLog trước, để start_ không bị mất nếu Firebase/MMP/LevelData ném exception
            var dict = new Dictionary<string, string>
            {
                { "ad_where", Placement(_placementIdInterstitial) },
                { "ad_network", adInfo.NetworkName }
            };
            FActionLogManager.Log($"start_{AdType.Interstitial.ToString()}", dict);

            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_SHOW);
            FalconMmpLog.LogEvent(FalconMmpLog.MMP_INTERS_DISPLAYED, new Dictionary<string, string>
            {
                { "af_level", LevelData.Instance.level + "" }
            });

            if (!_useMultiCall)
                MediationManager.Instance.NumberShowInterstitial++;
        }

        private void OnInterstitialAdFailedToDisplayEvent(
            string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // MediationManager.Instance.DebugLog("MAX > OnInterstitialAdFailedToDisplayEvent intertitial");
            if (_useMultiCall) return; //multi call đã xử lý
            _onInterstitialFailed?.Invoke();
            new FAdDisplayFailedLog(AdType.Interstitial, _placementIdInterstitial, _MAX, LevelData.Instance.level)
                .Send();
            DelayCall(0.5f, LoadInterstitial);
        }

        private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // MediationManager.Instance.DebugLog("MAX > OnInterstitialClickedEvent intertitial");
            FalconFirebaseLog.Log(FalconFirebaseLog.AD_INTER_CLICKED);
            _hasClick = true;
        }

        private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // MediationManager.Instance.DebugLog("MAX > OnInterstitialHiddenEvent intertitial");

            if (ConsumeShowSession())
            {
                if (!_useMultiCall)
                {
                    var time = (int)(DateTime.Now - _dateTimeTimeShow).TotalSeconds;
                    new CSAdFinish(_idAd, AdType.Interstitial.ToString(), Placement(_placementIdInterstitial),
                        adInfo.NetworkName, _MAX, adUnitId, adInfo.Revenue, time).Send();
                }

                MediationManager.Instance.AdsClosed(AdType.Interstitial, _placementIdInterstitial);
                //log FActionLog
                var dict = new Dictionary<string, string>
                {
                    { "ad_where", Placement(_placementIdInterstitial) },
                    { "ad_network", adInfo.NetworkName }
                };
                FActionLogManager.Log($"finish_{AdType.Interstitial.ToString()}", dict);
            }
            else
            {
                MediationManager.Instance.DebugError(
                    "MAX > bỏ qua log hidden interstitial lặp của lượt show đã kết thúc : " + adUnitId);
            }

            //log firebase/mmp của quảng cáo đầu tiên (nếu đang hoãn) cùng lúc với CSAdFinish
            FlushPendingAdRevenue();

            _eCpmInterstitial = -100;
            if (_isShowForRewarded)
            {
                GameEvent.Emit(_REWARDED_CLOSED);
            }
            else
            {
                GameEvent.Emit(_INTERSTITIAL_CLOSED);
            }

            if (_useMultiCall) return; //multi call đã xử lý
            _onInterstitialClosed?.Invoke();
            DelayCall(0.5f, LoadInterstitial);
        }

#endif

        public bool IsInterstitialReady()
        {
            if (_settings == null) return false;
            if (!_settings.UseInterstitialMax) return false;
            if (_isInitialized == null || !_isInitialized.Value) return false;
            var id = _settings.interstitialIdMaxAndroid;
#if UNITY_IOS
            id = _settings.interstitialIdMaxIOS;
#endif
            if (_useMultiCall) return MultiCall.Instance.IsInterstitialReady();
#if MAX_ENABLE
            return MaxSdk.IsInterstitialReady(id);
#else
            return false;
#endif
        }

        public void ShowInterstitial(
            string where, Action onInterstitialClosed = null, Action onFail = null,
            bool needAdBreak = false)
        {
            ShowInterstitial(where, onInterstitialClosed, onFail, needAdBreak, false);
        }

        private void ShowInterstitial(
            string where, Action onInterstitialClosed = null, Action onFail = null,
            bool needAdBreak = false, bool showForRewarded = false)
        {
            if (_settings == null)
            {
                MediationManager.Instance.DebugError("ShowInterstitial setting is null");
                onFail?.Invoke();
                return;
            }

            //chuẩn hoá 1 lần ở đây, mọi log phía sau (CSAdStart, FAdCalledLog, multicall) dùng chung giá trị này
            where = Placement(where);

            if (IsRemoveAds() && !showForRewarded)
            {
                onInterstitialClosed?.Invoke();
                return;
            }

            if (!_settings.UseInterstitialMax)
            {
                MediationManager.Instance.DebugError("_settings.UseInterstitialMax = false");
                onFail?.Invoke();
                return;
            }

            if (!_isInitialized.HasValue || !_isInitialized.Value)
            {
                MediationManager.Instance.DebugError("_isInitialized.HasValue = false || _isInitialized.Value = false");
                onFail?.Invoke();
                return;
            }

            if (!IsInterstitialReady())
            {
                onFail?.Invoke();
                LoadInterstitial();
                return;
            }

            MediationManager.Instance.DebugLog("MAX > ShowInterstitial");
            _placementIdInterstitial = where;
            _isShowingAds = true;
            _isShowForRewarded = showForRewarded;
            var id = _settings.interstitialIdMaxAndroid;
#if UNITY_IOS
            id = _settings.interstitialIdMaxIOS;
#endif
            _dateTimeTimeShow = DateTime.Now;
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
                _hasShowSession = true;
#if MAX_ENABLE
                if (_useMultiCall)
                    MultiCall.Instance.ShowInterstitial(where, onInterstitialClosed, onFail, showForRewarded);
                else
                {
                    _idAd = Guid.NewGuid().ToString();
                    new CSAdStart(_idAd, AdType.Interstitial.ToString(), where,
                        _networkNameInterstitial, _MAX, id).Send();

                    new FAdCalledLog(AdType.Interstitial, where, _MAX, LevelData.Instance.level)
                        .Send();
                    FsnAdManager.Instance.ShowInterstitialThenNative(
                        showInterstitial: callbacks =>
                        {
                            _onInterstitialClosed = callbacks.OnHidden;
                            _onInterstitialFailed = callbacks.OnDisplayFailed;
                            MaxSdk.ShowInterstitial(id);
                        },
                        onSuccess: () =>
                        {
                            onInterstitialClosed?.Invoke();
                        }, //success là hàm callback sẽ đc gọi sau khi xem xong quảng cáo interstitial, do user tự code
                        onFailed: () =>
                        {
                            onFail?.Invoke();
                        } //failure là hàm callback sẽ được gọi nếu quảng cáo không show được
                    );

                    if (showForRewarded)
                    {
                        GameEvent.Emit(_REWARDED_DISPLAYED);
                    }
                    else
                    {
                        GameEvent.Emit(_INTERSTITIAL_DISPLAYED);
                    }
                }
#endif

                FalconMmpLog.LogEvent(FalconMmpLog.MMP_INTERS_SHOW, new Dictionary<string, string>
                {
                    { "af_level", LevelData.Instance.level + "" }
                });
            }
        }

        public void LoadInterstitial()
        {
            if (!_settings.UseInterstitialMax) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            // MediationManager.Instance.DebugLog("MAX > LoadInterstitial");
            var id = _settings.interstitialIdMaxAndroid;
#if UNITY_IOS
            id = _settings.interstitialIdMaxIOS;
#endif
#if MAX_ENABLE
            MaxSdk.LoadInterstitial(id);
            LogAdRequest(id, AdType.Interstitial);
#endif
        }

        #endregion

        #region Banner

        private int _countAttemptBanner = 0;

        public bool IsBannerReady()
        {
            return _bannerIsLoaded;
        }

        public void LoadCollapsibleBanner()
        {
            MediationManager.Instance.DebugError("max not support collapsible Banner --> call from Google");
            // throw new NotImplementedException();
        }

        public void ShowCollapsibleBanner()
        {
            MediationManager.Instance.DebugError("max not support collapsible Banner --> call from Google");
            // throw new NotImplementedException();
        }

        public void HideCollapsibleBanner()
        {
            MediationManager.Instance.DebugError("max not support collapsible Banner --> call from Google");
            // throw new NotImplementedException();
        }

        public void ShowBanner(string placementId)
        {
            MediationManager.Instance.DebugLog("Call > ShowBanner");
            _callShowBanner = true;
            _placementIdBanner = placementId;
            if (_settings == null) return;
            if (!_settings.UseBannerMax) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            if (IsRemoveAds()) return;
            MediationManager.Instance.DebugLog("MAX > ShowBanner");
            var id = _settings.bannerIdMaxAndroid;
#if UNITY_IOS
            id = _settings.bannerIdMaxIOS;
#endif
#if MAX_ENABLE
            MaxSdk.ShowBanner(id);
#endif
            if (_bannerIsLoaded)
            {
                _bannerIsShow = true;
                MediationManager.Instance.onBannerShow?.Invoke();
                if (_settings.useButtonCloseBanner)
                {
                    float bannerHeightPx = GetBannerHeightInPixels();
                    BannerCloseButtonUI.Show(bannerHeightPx);
                }
            }
        }

        public float GetBannerHeightInPixels()
        {
            var heightPx = 0f;
#if MAX_ENABLE
            float heightDp = MaxSdkUtils.GetAdaptiveBannerHeight();
            heightPx = heightDp * (Screen.dpi / 160f);
#endif
            return heightPx;
        }

        public void HideBanner(bool fromInit = false)
        {
            MediationManager.Instance.DebugLog("Call > HideBanner");
            if (!fromInit)
            {
                _callShowBanner = false;
            }

            if (_settings == null) return;
            if (!_settings.UseBannerMax) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            MediationManager.Instance.DebugLog("MAX > HideBanner");
            var id = _settings.bannerIdMaxAndroid;
#if UNITY_IOS
            id = _settings.bannerIdMaxIOS;
#endif
#if MAX_ENABLE
            MaxSdk.HideBanner(id);
#endif
            if (_bannerIsShow)
            {
                _bannerIsShow = false;
                MediationManager.Instance.onBannerHide?.Invoke();
                BannerCloseButtonUI.Hide();
            }
        }

#if AMAZON_ENABLE && !UNITY_EDITOR
        private APSBannerAdRequest bannerAdRequest;
#endif
        public void CreateBannerView()
        {
            if (!_settings.UseBannerMax) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            MediationManager.Instance.DebugLog("MAX > Creating banner view");

#if AMAZON_ENABLE && !UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(_settings.bannerIdAmazonMaxAndroid)) return;
            if (bannerAdRequest != null) bannerAdRequest.DestroyFetchManager();
            var size = GetSizeAdaptiveBanner();
            var idAmazon = _settings.bannerIdAmazonMaxAndroid;
            var id = _settings.bannerIdMaxAndroid;
#if UNITY_IOS
            idAmazon = _settings.bannerIdAmazonMaxIOS;
            id = _settings.bannerIdMaxIOS;
#endif
            bannerAdRequest =
                new APSBannerAdRequest((int)size.x, (int)size.y, idAmazon, new AdNetworkInfo(ApsAdNetwork.MAX));
            bannerAdRequest.onFailedWithError += (adError) =>
            {
#if MAX_ENABLE
                MaxSdk.SetBannerLocalExtraParameter(id, "amazon_ad_error", adError.GetAdError());
#endif
                LoadBanner();
            };
            bannerAdRequest.onSuccess += (adResponse) =>
            {
#if MAX_ENABLE
                MaxSdk.SetBannerLocalExtraParameter(id, "amazon_ad_response", adResponse.GetResponse());
#endif
                LoadBanner();
            };

            bannerAdRequest.LoadAd();
#else
            LoadBanner();
#endif
        }

        private Vector2 GetSizeAdaptiveBanner()
        {
            // Lấy chiều rộng màn hình (theo pixels, vì Max SDK dùng pixel)
            int screenWidth = Screen.width;

            // Tính chiều cao banner dựa theo chiều rộng (Google gợi ý: 50dp cho phones, 90dp cho tablets)
            // Với Unity, ta lấy DPI thực để convert dp -> px
            float dpi = Screen.dpi;
            if (dpi == 0) dpi = 160; // fallback nếu không xác định được DPI

            // 50dp tương đương:
            int heightPx = Mathf.RoundToInt(50 * (dpi / 160f));

            // Cập nhật kích thước banner
            return new Vector2(screenWidth, heightPx);
        }

        public void DestroyBanner()
        {
            if (!_settings.UseBannerMax) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            UnregisterBannerEvents();
            var id = _settings.bannerIdMaxAndroid;
#if UNITY_IOS
            id = _settings.bannerIdMaxIOS;
#endif
#if MAX_ENABLE
            MaxSdk.DestroyBanner(id);
#endif
#if AMAZON_ENABLE && !UNITY_EDITOR
            bannerAdRequest.Dispose();
#endif
        }

        private void RegisterBannerEvents()
        {
#if MAX_ENABLE
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;
            //remove for performance
            // MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            // MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
            // MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;
#endif
        }

        private void UnregisterBannerEvents()
        {
#if MAX_ENABLE
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerAdRevenuePaidEvent;
            //remove for performance
            // MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClickedEvent;
            // MaxSdkCallbacks.Banner.OnAdExpandedEvent -= OnBannerAdExpandedEvent;
            // MaxSdkCallbacks.Banner.OnAdCollapsedEvent -= OnBannerAdCollapsedEvent;
#endif
        }
#if MAX_ENABLE
        private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdRevenuePaidEvent(adUnitId, AdType.Banner, adInfo);
        }

        void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("Max > Banner view loaded an ad");
            _bannerIsLoaded = true;
            _countAttemptBanner = 0;
            MediationManager.Instance.onBannerLoaded?.Invoke();
            if (_callShowBanner)
            {
                ShowBanner(_placementIdBanner);
                _callShowBanner = false;
            }
        }

        void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            MediationManager.Instance.DebugLog("Max > Banner view failed to load an ad with error : " +
                                               errorInfo.Message);
            _countAttemptBanner++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptBanner));
            DelayCall(retryDelay, LoadBanner);
            _bannerIsLoaded = false;
        }
        /*
                void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
                {
                    MediationManager.Instance.DebugLog("Max > Banner view was clicked.");
                }

                void OnBannerAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
                {
                    MediationManager.Instance.DebugLog("Max > Banner view Expanded.");
                }

                void OnBannerAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
                {
                    MediationManager.Instance.DebugLog("Max > Banner view collapsed.");
                }
                */
#endif
        public void LoadBanner()
        {
            MediationManager.Instance.DebugLog("Max > LoadBanner");
            var id = _settings.bannerIdMaxAndroid;
#if UNITY_IOS
            id = _settings.bannerIdMaxIOS;
#endif
#if MAX_ENABLE
            MaxSdk.CreateBanner(id, new MaxSdkBase.AdViewConfiguration(MaxSdkBase.AdViewPosition.BottomCenter));
#endif
        }

        #endregion

        #region Rewarded

        private int _countAttemptRewarded;

        public void LoadRewardedAd()
        {
            if (!_settings.UseRewardedMax) return;
            if (!_isInitialized.HasValue || !_isInitialized.Value) return;
            MediationManager.Instance.DebugLog("Max > LoadRewardedAd");
            var id = _settings.rewardedIdMaxAndroid;
#if UNITY_IOS
            id = _settings.rewardedIdMaxIOS;
#endif
#if MAX_ENABLE
            MaxSdk.LoadRewardedAd(id);
            LogAdRequest(id, AdType.Reward);
#endif
        }

        public bool IsRewardedVideoReady()
        {
            if (_settings == null) return false;
            if (!_settings.UseRewardedMax) return false;
            var id = _settings.rewardedIdMaxAndroid;
#if UNITY_IOS
            id = _settings.rewardedIdMaxIOS;
#endif
            if (_useMultiCall) return MultiCall.Instance.IsRewardedVideoReady();
#if MAX_ENABLE
            return MaxSdk.IsRewardedAdReady(id);
#endif
            return false;
        }

        private void RegisterRewardedEvent()
        {
#if MAX_ENABLE
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
#endif
        }

#if MAX_ENABLE
        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdRevenuePaidEvent(adUnitId, AdType.Reward, adInfo);
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("Max > Rewarded ad OnRewardedAdLoadedEvent");
            _eCpmRewarded = adInfo.Revenue;
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_LOAD);
            if (_useMultiCall) return;
            LogAdLoadSuccess(adUnitId, adInfo);
            _countAttemptRewarded = 0;
            _networkNameRewarded = adInfo.NetworkName;
        }

        private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            MediationManager.Instance.DebugLog(
                "Max > Rewarded ad OnRewardedAdLoadFailedEvent ---- " + errorInfo.Message);
            if (_useMultiCall) return;
            LogAdLoadFailed(adUnitId, errorInfo);
            _countAttemptRewarded++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _countAttemptRewarded));
            DelayCall(retryDelay, LoadRewardedAd);
        }
#endif
        private string _networkNameRewarded;
        private string _networkNameInterstitial;

        public void ShowRewardedVideo(
            string where, Action onRewardedComplete = null, Action onFail = null,
            bool showInterstitialInstead = true)
        {
            if (_settings == null)
            {
                onFail?.Invoke();
                return;
            }

            //1 lượt rewarded có thể bị đổi thành interstitial: so sánh eCpm ở ngay dưới,
            //rewarded chưa sẵn sàng, hoặc multicall tự chọn interstitial bên trong MultiCallProvider.
            //Gán placement cho cả 2 loại để log của loại thực sự được show ra không bị null.
            where = Placement(where);
            _placementIdRewarded = where;
            _placementIdInterstitial = where;

            if (!_useMultiCall)
            {
                if (_eCpmInterstitial > _eCpmRewarded && showInterstitialInstead)
                {
                    ShowInterstitial(where, onRewardedComplete, onFail, false, true);
                    return;
                }
            }

            if (!IsRewardedVideoReady())
            {
                if (showInterstitialInstead)
                {
                    MediationManager.Instance.DebugLog(" Reward not available, show Interstitial instead");
                    if (IsInterstitialReady())
                    {
                        ShowInterstitial(where, onRewardedComplete, onFail, false, true);
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

            MediationManager.Instance.DebugLog("Max > Rewarded ad ShowRewardedVideo");
            _closedRewarded = false;
            _receivedRewarded = false;

            _isShowingAds = true;
            _hasShowSession = true;
            _dateTimeTimeShow = DateTime.Now;
            var id = _settings.rewardedIdMaxAndroid;
#if UNITY_IOS
            id = _settings.rewardedIdMaxIOS;
#endif
#if MAX_ENABLE
            if (_useMultiCall)
                MultiCall.Instance.ShowRewardedVideo(where, onRewardedComplete, onFail);
            else
            {
                _idAd = Guid.NewGuid().ToString();
                new CSAdStart(_idAd, AdType.Reward.ToString(), where, _networkNameRewarded, _MAX, id).Send();
                new FAdCalledLog(AdType.Reward, where, _MAX, LevelData.Instance.level)
                    .Send();
                FsnAdManager.Instance.ShowRewardedThenNative(
                    showRewarded: callbacks =>
                    {
                        _onRewardedComplete = callbacks.OnRewardReceived;
                        _onRewardedFailed = callbacks.OnDisplayFailed;
                        _onRewardedHidden = callbacks.OnHidden;
                        MaxSdk.ShowRewardedAd(id);
                    },
                    onSuccess: () =>
                    {
                        onRewardedComplete?.Invoke();
                    }, //success là action được gọi khi user xem xong Rewarded Video, trả thưởng cho user, hồi sinh, …
                    onFailed: () =>
                    {
                        onFail?.Invoke();
                    } //failure là action được gọi khi quảng cáo bị lỗi không hiển thị được
                );

                GameEvent.Emit(_REWARDED_DISPLAYED);
            }
#endif
            FalconMmpLog.LogEvent(FalconMmpLog.MMP_REWARDED_SHOW, new Dictionary<string, string>
            {
                { "af_level", LevelData.Instance.level + "" }
            });
        }

#if MAX_ENABLE
        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("Max > Rewarded ad OnRewardedAdDisplayedEvent");
            _hasShowSession = true;

            //log FActionLog trước, để start_ không bị mất nếu Firebase/MMP/LevelData ném exception
            var dict = new Dictionary<string, string>
            {
                { "ad_where", Placement(_placementIdRewarded) },
                { "ad_network", adInfo.NetworkName }
            };
            FActionLogManager.Log($"start_{AdType.Reward.ToString()}", dict);

            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_SHOW_SUCCESS);
            FalconMmpLog.LogEvent(FalconMmpLog.MMP_REWARDED_DISPLAYED, new Dictionary<string, string>
            {
                { "af_level", LevelData.Instance.level + "" }
            });
        }

        private void OnRewardedAdFailedToDisplayEvent(
            string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("Max > Rewarded ad OnRewardedAdFailedToDisplayEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_SHOW_FAIL, errorInfo.ToString());
            if (_useMultiCall) return;
            _onRewardedFailed?.Invoke();
            new FAdDisplayFailedLog(AdType.Reward, _placementIdRewarded, _MAX, LevelData.Instance.level)
                .Send();
            DelayCall(0.5f, LoadRewardedAd);
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            MediationManager.Instance.DebugLog("Max > Rewarded ad OnRewardedAdClickedEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_CLICK, adInfo.Placement);
            _hasClick = true;
        }

        private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _closedRewarded = true;
            MediationManager.Instance.DebugLog("Max > Rewarded ad OnRewardedAdHiddenEvent");
            if (ConsumeShowSession())
            {
                if (!_useMultiCall)
                {
                    var time = (int)(DateTime.Now - _dateTimeTimeShow).TotalSeconds;
                    new CSAdFinish(_idAd, AdType.Reward.ToString(), Placement(_placementIdRewarded), adInfo.NetworkName,
                        _MAX, adUnitId, adInfo.Revenue, time).Send();
                }

                //log FActionLog
                var dict = new Dictionary<string, string>
                {
                    { "ad_where", Placement(_placementIdRewarded) },
                    { "ad_network", adInfo.NetworkName }
                };
                FActionLogManager.Log($"finish_{AdType.Reward.ToString()}", dict);
            }
            else
            {
                MediationManager.Instance.DebugError(
                    "MAX > bỏ qua log hidden rewarded lặp của lượt show đã kết thúc : " + adUnitId);
            }

            //log firebase/mmp của quảng cáo đầu tiên (nếu đang hoãn) cùng lúc với CSAdFinish
            FlushPendingAdRevenue();

            _eCpmRewarded = -100;
            GameEvent.Emit(_REWARDED_CLOSED);
            if (_useMultiCall) return;
            _onRewardedHidden?.Invoke();
            CheckReceivedRewarded();
            DelayCall(0.5f, LoadRewardedAd);
        }

        private void OnRewardedAdReceivedRewardEvent(
            string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            _receivedRewarded = true;
            MediationManager.Instance.DebugLog("Max > Rewarded ad OnRewardedAdReceivedRewardEvent");
            FalconFirebaseLog.Log(FalconFirebaseLog.ADS_REWARD_COMPLETE, adInfo.Placement);
            if (_useMultiCall) return;
            _onRewardedComplete?.Invoke();
            CheckReceivedRewarded();
        }

        private void CheckReceivedRewarded()
        {
            if (_closedRewarded && _receivedRewarded)
            {
                //tiêu thụ 2 cờ (được reset lại ở ShowRewardedVideo) để callback lặp
                //không tính thêm 1 lượt xem nữa vào cap của placement
                _closedRewarded = false;
                _receivedRewarded = false;
                MediationManager.Instance.AdsClosed(AdType.Reward, _placementIdRewarded);
            }
        }
#endif

        #endregion

        #region CSAdRequestLog

#if MAX_ENABLE
        //Khi chạy multicall thì MultiCallProvider đã tự đếm request/lỗi/thành công.
        //Khi _useMultiCall = false (đang dùng 3G, máy low-end, hoặc không có bid floor) thì không có ai đếm,
        //nên LogInstance không có dữ liệu và CSAdRequestLog không bao giờ được gửi. Phần dưới bù cho trường hợp đó.
        private void LogAdRequest(string adUnitId, AdType adType)
        {
            if (_useMultiCall) return;
            if (string.IsNullOrWhiteSpace(adUnitId)) return;

            var item = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId);
            if (item == null)
            {
                item = new AdRequestLogItem
                {
                    adUnitId = adUnitId,
                    adType = adType.ToString().ToLower(),
                    decisionPolicy = SaveLoadHandler.Load(_KEY_DECISION_POLICY, _DECISION_POLICY_NO_MULTICALL),
                    numRequest = 0,
                    errorDetail = new Dictionary<string, int>(),
                    adSuccessInfos = new List<AdSuccessInfo>()
                };
                LogInstance.Instance.AddAdLog(item);
            }

            item.numRequest++;
        }

        private void LogAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (_useMultiCall) return;

            var item = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId);
            if (item == null)
            {
                MediationManager.Instance.DebugError("don't exists adRequestLogItem load failed id : " + adUnitId);
                return;
            }

            var code = errorInfo.Code.ToString();
            if (!item.errorDetail.ContainsKey(code))
            {
                item.errorDetail.Add(code, 0);
            }

            item.errorDetail[code]++;
        }

        private void LogAdLoadSuccess(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (_useMultiCall) return;

            var item = LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId);
            if (item == null)
            {
                MediationManager.Instance.DebugError("don't exists adRequestLogItem loaded id : " + adUnitId);
                return;
            }

            item.adSuccessInfos.Add(new AdSuccessInfo
            {
                index = item.numRequest,
                revenueUsd = adInfo.Revenue,
                time = new DateTimeOffset(TimeUtils.UTCNow).ToUnixTimeMilliseconds(),
                adMediation = _MAX,
                adNetwork = adInfo.NetworkName
            });
        }
#endif

        #endregion

        public void UnregisterEvent()
        {
        }

        private void DelayCall(float time, Action action)
        {
            MaxEvent.Instance.StartCoroutine(RetryDelay());

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

        //ad_where luôn phải là placement được truyền vào hàm show.
        //Chỉ rơi về _PLACEMENT_UNKNOWN khi callback của SDK về mà chưa có lượt show nào,
        //để log không bị null/rỗng và nhìn vào là biết ngay có luồng chưa gán placement.
        private static string Placement(string placementId)
        {
            return string.IsNullOrWhiteSpace(placementId) ? _PLACEMENT_UNKNOWN : placementId;
        }

        //true nếu đây là callback hidden đầu tiên của lượt show hiện tại.
        //Chỉ chặn phần log, không chặn phần load lại/callback game để luồng quảng cáo không bị kẹt.
        private bool ConsumeShowSession()
        {
            if (!_hasShowSession) return false;
            _hasShowSession = false;
            return true;
        }
    }
}

public class TimeParam : FParam
{
    public int adTime;
}