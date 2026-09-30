/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.RemoteConfigCms;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Modules.Core.Utils.Time.Runtime;
using Falcon.Modules.Level.Core;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public abstract class MultiCallProvider<T> : IMultiCallProvider where T : MultiCallProvider<T>, new()
    {
        private const string _INTERSTITIAL_DISPLAYED = "MEDIATION_INTERSTITIAL_DISPLAYED";
        private const string _REWARDED_DISPLAYED = "MEDIATION_REWARDED_DISPLAYED";
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }

                return _instance;
            }
        }

        protected abstract int IndexProvider { get; }
        private int _indexProvider;
        protected abstract MediationType MediationType { get; }
        private MediationType _mediationType;


        private bool _initialized;

        protected Action onRewardedComplete;
        protected Action onRewardedFailed;
        protected Action onRewardedHidden;
        protected Action onInterstitialClosed;
        protected Action onInterstitialFailed;

        protected bool closedRewarded, receivedRewarded;

        private SOMediationSetting _setting;

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterProvider();
        }

        protected virtual void RegisterProvider()
        {
        }

        protected bool enableLog;

        public int GetIndex()
        {
            return IndexProvider;
        }

        public void RegisterEvent()
        {
            MediationManager.Instance.DebugLog("provider : " + IndexProvider);
            enableLog = FConfigControllerCms.Instance.Config<MultiCallConfig>().enableLogMultiCall;
            LoadInterstitial();
            RegisterInterstitialEvents();
            LoadRewarded();
            RegisterRewardedEvents();
            Application.lowMemory += OnLowMemory;
        }

        protected virtual void RegisterInterstitialEvents()
        {
        }

        protected virtual void RegisterRewardedEvents()
        {
        }

        protected bool throttled;

        private void OnLowMemory()
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
            if (throttled) return;
            throttled = true;
            var ram = SystemInfo.systemMemorySize / 1024f;
            var maxAds = MultiCall.Instance.dictionaryInterstitial.Values.Count(x => x.eCpm != -100) +
                         MultiCall.Instance.dictionaryRewarded.Values.Count(x => x.eCpm != -100);
            new MThrottledLog(ram, maxAds, IndexProvider).Send();
        }

        /// <summary>
        /// Load rewarded video ads
        /// </summary>
        public virtual void LoadRewarded()
        {
        }

        protected virtual void InternalLoadRewarded(string adUnitId)
        {
        }

        protected virtual void ResetMultiplier(string adUnitId)
        {
        }

        protected void LoadRewardedAd(string adUnitId)
        {
            //kiểm tra có đang dùng mobile data ko
            if (MultiCall.Instance.IsUseMobileData())
            {
                throttled = true;
            }

            if (MultiCall.Instance.dictionaryRewarded.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryRewarded[adUnitId];
                MediationManager.Instance.DebugLog("Load Rewarded Ad : " + adUnitId);

                foreach (var multiCall in MultiCall.Instance.dictionaryRewarded)
                {
                    //id default thì bỏ qua
                    if (a.multiplier == 0) break;
                    if (multiCall.Value.eCpm >= 0 && multiCall.Value.multiplier >= a.multiplier && a.multiplier != -1)
                    {
                        //đã load được qc có multiplier > id này
                        MediationManager.Instance.DebugLog("đã load được qc : " + multiCall.Key + " x " +
                                                           multiCall.Value.multiplier + " > " + adUnitId + " x " +
                                                           a.multiplier);
                        ResetMultiplier(adUnitId);
                        return;
                    }
                }

                //chưa có quảng cáo
                //đang ko trong trạng thái chờ callback quảng cáo

                if (!IsRewardedVideoReady(adUnitId) && !a.isWaiting)
                {
                    if (!throttled || (throttled && a.index == 0))
                    {
                        InternalLoadRewarded(adUnitId);

                        a.timeLoad = TimeUtils.UTCNow;
                        a.countLoadAttempt++;
                        a.isWaiting = true;
                        if (LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId) == null)
                        {
                            var adRequestLogItem = new AdRequestLogItem()
                            {
                                adUnitId = adUnitId,
                                adType = AdType.Reward.ToString().ToLower(),
                                decisionPolicy = SaveLoadHandler.Load("decision_policy_load_ad",
                                    "client-" + FConfigControllerCms.Instance.Config<MultiCallConfig>()
                                        .moMulProviderIndexMax),
                                numRequest = 0,
                                errorDetail = new Dictionary<string, int>(),
                                adSuccessInfos = new List<AdSuccessInfo>()
                            };
                            LogInstance.Instance.AddAdLog(adRequestLogItem);
                        }

                        if (LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId) != null)
                        {
                            LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId).numRequest++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check Rewarded ready to show or not
        /// </summary>
        public bool IsRewardedVideoReady()
        {
#if MAX_ENABLE
            foreach (var call in MultiCall.Instance.dictionaryRewarded)
            {
                if (IsRewardedVideoReady(call.Key))
                {
                    return true;
                }
            }
#endif
            return false;
        }

        protected virtual bool IsRewardedVideoReady(string adUnitId)
        {
            return false;
        }

        protected string idAd;
        protected string placementId;

        //giá trị dự phòng cho ad_where, để không log null nếu callback về mà chưa qua hàm show nào
        protected const string PLACEMENT_UNKNOWN = "unknown";

        protected static string Placement(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? PLACEMENT_UNKNOWN : value;
        }

        //Bật khi bắt đầu 1 lượt show, tắt ở callback hidden đầu tiên.
        //Hidden bắn lặp sẽ không gửi thêm CSAdFinish, nhưng phần load lại vẫn chạy bình thường.
        private bool _hasShowSession;

        protected bool ConsumeShowSession()
        {
            if (!_hasShowSession) return false;
            _hasShowSession = false;
            return true;
        }

        protected void OpenShowSession()
        {
            _hasShowSession = true;
        }

        protected virtual void InternalShowRewarded(string adUnitId, string where)
        {
        }

        /// <summary>
        /// Show rewarded ads
        /// </summary>
        /// <param name="where">Position show ads. eg: home, end game...</param>
        public void ShowRewardedVideo(string where, Action onRewardedComplete, Action onFailed)
        {
            //gán ngay từ đầu: lượt này có thể được show bằng interstitial ở nhánh dưới,
            //mọi log (CSAdStart/CSAdFinish/CSAdInfo) đều đọc từ placementId này
            placementId = Placement(where);
            var highestEcpmEntry = MultiCall.Instance.dictionaryRewarded.OrderByDescending(pair => pair.Value.eCpm)
                .FirstOrDefault();
            var highestEcpmEntryInterstitial =
                MultiCall.Instance.dictionaryInterstitial.OrderByDescending(pair => pair.Value.eCpm)
                    .FirstOrDefault();
            if (highestEcpmEntry.Value.eCpm == -100 && highestEcpmEntryInterstitial.Value.eCpm == -100)
            {
                //ads don't exists
                MediationManager.Instance.DebugLog("ecpm = -100 -> reload all");
                LoadRewarded();
                onFailed?.Invoke();
            }
            else
            {
#if MAX_ENABLE
                MediationManager.Instance.DebugLog("ecpm Rewarded : " + highestEcpmEntry.Value.eCpm +
                                                   " -- ecpm inters : " + highestEcpmEntryInterstitial.Value.eCpm);
                if (highestEcpmEntry.Value.eCpm >= highestEcpmEntryInterstitial.Value.eCpm)
                {
                    MediationManager.Instance.DebugLog("Show rewarded ads");
                    var a = MultiCall.Instance.dictionaryRewarded[highestEcpmEntry.Key];
                    MultiCall.Instance.dateTimeTimeShow = TimeUtils.UTCNow;
                    a.eCpm = -100;
                    OpenShowSession();
                    idAd = Guid.NewGuid().ToString();
                    new CSAdStart(idAd, AdType.Reward.ToString(), placementId, a.networkName, MediationType.ToString(),
                        highestEcpmEntry.Key).Send();
                    new FAdCalledLog(AdType.Reward, placementId, MediationType.ToString(), LevelData.Instance.level)
                        .Send();
                    FsnAdManager.Instance.ShowRewardedThenNative(
                        showRewarded: callbacks =>
                        {
                            this.onRewardedComplete = callbacks.OnRewardReceived;
                            onRewardedFailed = callbacks.OnDisplayFailed;
                            onRewardedHidden = callbacks.OnHidden;
                            InternalShowRewarded(highestEcpmEntry.Key, placementId);
                        },
                        onSuccess: () =>
                        {
                            onRewardedComplete?.Invoke();
                        }, //success là action được gọi khi user xem xong Rewarded Video, trả thưởng cho user, hồi sinh, …
                        onFailed: () =>
                        {
                            onFailed?.Invoke();
                        } //failure là action được gọi khi quảng cáo bị lỗi không hiển thị được
                    );
                    GameEvent.Emit(_REWARDED_DISPLAYED);
                }
                else
                {
                    MediationManager.Instance.DebugLog("Show Inter ads instead rewarded");
                    ShowInterstitial(placementId, onRewardedComplete, onFailed, true);
                }
#endif
            }
        }

        #region interstitial

        /// <summary>
        /// Check interstitial ads ready or not
        /// </summary>
        public bool IsInterstitialReady()
        {
            foreach (var call in MultiCall.Instance.dictionaryInterstitial)
            {
                if (IsInterstitialReady(call.Key))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual bool IsInterstitialReady(string adUnitId)
        {
            return false;
        }

        protected virtual void InternalShowInterstitial(string adUnitId)
        {
        }

        /// <summary>
        /// Show Interstitial ads
        /// </summary>
        public void ShowInterstitial(string where, Action onInterstitialClosed, Action onFailed, bool showForRewarded)
        {
            placementId = Placement(where);
            var highestEcpmEntry = MultiCall.Instance.dictionaryInterstitial
                .OrderByDescending(pair => pair.Value.eCpm).FirstOrDefault();
            if (highestEcpmEntry.Value.eCpm == -100)
            {
                //ads don't exists
                MediationManager.Instance.DebugLog("ecpm = -100 -> reload all");
                LoadInterstitial();
                onFailed?.Invoke();
            }
            else
            {
                var a = MultiCall.Instance.dictionaryInterstitial[highestEcpmEntry.Key];
                MultiCall.Instance.dateTimeTimeShow = TimeUtils.UTCNow;
                a.eCpm = -100;
                OpenShowSession();
                idAd = Guid.NewGuid().ToString();
                new CSAdStart(idAd, AdType.Interstitial.ToString(), placementId, a.networkName,
                    MediationType.ToString(), highestEcpmEntry.Key).Send();
                new FAdCalledLog(AdType.Interstitial, placementId, MediationType.ToString(), LevelData.Instance.level)
                    .Send();
                FsnAdManager.Instance.ShowInterstitialThenNative(
                    showInterstitial: callbacks =>
                    {
                        this.onInterstitialClosed = callbacks.OnHidden;
                        onInterstitialFailed = callbacks.OnDisplayFailed;
                        InternalShowInterstitial(highestEcpmEntry.Key);
                    },
                    onSuccess: () =>
                    {
                        onInterstitialClosed?.Invoke();
                    }, //success là hàm callback sẽ đc gọi sau khi xem xong quảng cáo interstitial, do user tự code
                    onFailed: () =>
                    {
                        onFailed?.Invoke();
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
        }

        /// <summary>
        /// Load interstitial ads to show
        /// </summary>
        public virtual void LoadInterstitial()
        {
        }

        protected virtual void InternalLoadInterstitial(string adUnitId)
        {
        }

        protected void LoadInterstitialAd(string adUnitId)
        {
            //kiểm tra xem có đang dùng 3G ko
            if (MultiCall.Instance.IsUseMobileData())
            {
                throttled = true;
            }

            if (MultiCall.Instance.dictionaryInterstitial.ContainsKey(adUnitId))
            {
                var a = MultiCall.Instance.dictionaryInterstitial[adUnitId];
                // MediationManager.Instance.DebugLog("Load Interstitial Ad : " + adUnitId);

                foreach (var multiCall in MultiCall.Instance.dictionaryInterstitial)
                {
                    //id default thì bỏ qua
                    if (a.multiplier == 0) break;
                    if (multiCall.Value.eCpm >= 0 && multiCall.Value.multiplier > a.multiplier && a.multiplier != -1)
                    {
                        //đã load được qc có multiplier > id này
                        // MediationManager.Instance.DebugLog("đã load được qc : " + multiCall.Key + " x " +
                        //                                    multiCall.Value.multiplier + " > " + adUnitId + " x " +
                        //                                    a.multiplier);
                        ResetMultiplier(adUnitId);
                        return;
                    }
                }

                //chưa có quảng cáo
                //đang ko trong trạng thái chờ callback quảng cáo

                if (!IsInterstitialReady(adUnitId) && !a.isWaiting)
                {
                    if (!throttled || (throttled && a.index == 0))
                    {
                        InternalLoadInterstitial(adUnitId);

                        a.timeLoad = TimeUtils.UTCNow;
                        a.countLoadAttempt++;
                        a.isWaiting = true;
                        if (LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId) == null)
                        {
                            var adRequestLogItem = new AdRequestLogItem()
                            {
                                adUnitId = adUnitId,
                                adType = AdType.Interstitial.ToString().ToLower(),
                                decisionPolicy = SaveLoadHandler.Load("decision_policy_load_ad",
                                    "client-" + FConfigControllerCms.Instance.Config<MultiCallConfig>()
                                        .moMulProviderIndexMax),
                                numRequest = 0,
                                errorDetail = new Dictionary<string, int>(),
                                adSuccessInfos = new List<AdSuccessInfo>()
                            };
                            LogInstance.Instance.AddAdLog(adRequestLogItem);
                        }

                        if (LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId) != null)
                        {
                            LogInstance.Instance.GetAdRequestLogItemByAdUnitId(adUnitId).numRequest++;
                        }
                    }
                }
            }
        }

        #endregion
    }
}