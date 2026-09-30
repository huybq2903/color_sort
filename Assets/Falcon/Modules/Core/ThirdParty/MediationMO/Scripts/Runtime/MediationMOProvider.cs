/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-21
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using Falcon.Modules.Core.Utils.Time.Runtime;
using Falcon.Modules.Level.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.MediationMO.Runtime
{
    public class MediationMOProvider : IMediationProvider
    {
        private const string _FORMAT_DATE = "yyyy-MM-dd";
        private static MediationMOProvider _instance;
        public static MediationMOProvider Instance => _instance ??= new MediationMOProvider();

        private bool _initialized;

        public int Priority => 1;
        public string Name => "MediationMOProvider";
        private const string _ADS_MANAGER_DICTIONARY = "falcon_ads_manager_dictionary_v3";

        private MediationMOProvider()
        {
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            MediationManager.Instance.Register(_instance);
        }

        public bool CanShowAppOpen(string placementId)
        {
            var status =
                GetStatusAdsFromPlacement(placementId, true); //ko bị ảnh hưởng bởi biên interval between Inter + Video
            Debug.Log(" ======= status : " + status.statusShowAds);
            return status.statusShowAds == StatusShowAds.Active;
        }

        public void InitFromManager()
        {
            GameEvent<int>.Register(FLevelManager.EVENT_BUS_LEVEL_READY, OnStartSceneGame, null);
            if (_dictionaryAdsManager.Count == 0)
            {
                LoadFromCache();
            }

            GameEvent<Dictionary<string, SOAdsMOSetting.AdsGroupConfig>>.Register(
                SCAdsMoConfigV3.RECEIVED_ADS_CONFIG, OnReceivedAdsConfig, null);
        }

        private void OnReceivedAdsConfig(Dictionary<string, SOAdsMOSetting.AdsGroupConfig> adsConfigs)
        {
            _dictionaryAdsManager = LoadAdsGroupConfigFromJson(adsConfigs);
            SaveLoadHandler.Save(_ADS_MANAGER_DICTIONARY, adsConfigs.ToJson());
        }

        private Dictionary<string, SOAdsMOSetting.AdsManager> _dictionaryAdsManager = new(); //key = group

        private const string _ADS_LOAD_FROM_REMOTE_CONFIG = "falcon_ads_load_from_remote_config_v3";
        private const string _TIME_CLOSED_REWARDED = "falcon_time_closed_rewarded";
        private const string _TIME_CLOSED_INTERSTITIAL = "falcon_time_closed_interstitial";
        private const string _TIME_CLOSED_APP_OPEN = "falcon_time_closed_app_open";

        private void OnStartSceneGame(int obj)
        {
            foreach (var adsManager in _dictionaryAdsManager)
            {
                foreach (var adsValue in adsManager.Value.adsGroupValue.dictionaryAdsValue)
                {
                    adsValue.Value.levelCount = 0;
                }
            }
        }

        private void LoadFromCache()
        {
            var json = SaveLoadHandler.Load(_ADS_MANAGER_DICTIONARY, "");
            if (string.IsNullOrEmpty(json))
            {
                var setting = Resources.Load<SOAdsMOSetting>("SOAdsMOV3Setting");
                if (setting == null)
                {
                    Debug.LogError(
                        "SOAdsSetting doesn't exists. Go to 'Falcon/Modules/ThirdParty/Ads MO Settings' to config.");
                    return;
                }

                var jsonConfig = setting.ConfigAds;

                try
                {
                    var dtoDict =
                        JsonConvert.DeserializeObject<Dictionary<string, SOAdsMOSetting.ListAdsGroupConfigDto>>(
                            jsonConfig);
                    var jsonDictionary = dtoDict.ToDictionary(
                        kv => kv.Key,
                        kv => new SOAdsMOSetting.AdsGroupConfig
                        {
                            group = kv.Value.group,
                            configAds = kv.Value.configAds?.ToDictionary(x => x.placementId, x => x.adsConfigV3)
                                        ?? new Dictionary<string, SOAdsMOSetting.AdsConfigV3>(),
                            adsRewards = kv.Value.adsRewards ?? new List<SOAdsMOSetting.AdsReward>(),
                            maxTotalAds = kv.Value.maxTotalAds
                        }
                    );

                    _dictionaryAdsManager = LoadAdsGroupConfigFromJson(jsonDictionary);
                    SaveLoadHandler.Save(_ADS_MANAGER_DICTIONARY, jsonDictionary.ToJson());
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }
            }
            else
            {
                var adsConfigs = JsonConvert.DeserializeObject<Dictionary<string, SOAdsMOSetting.AdsGroupConfig>>(json);
                _dictionaryAdsManager = LoadAdsGroupConfigFromJson(adsConfigs);
            }
        }

        private Dictionary<string, SOAdsMOSetting.AdsManager> LoadAdsGroupConfigFromJson(
            Dictionary<string, SOAdsMOSetting.AdsGroupConfig> json)
        {
            Dictionary<string, SOAdsMOSetting.AdsManager> dic = SaveLoadHandler.Load(_ADS_LOAD_FROM_REMOTE_CONFIG,
                new Dictionary<string, SOAdsMOSetting.AdsManager>());
            _timeRewardedClosed = SaveLoadHandler.Load(_TIME_CLOSED_REWARDED, _timeRewardedClosed);
            _timeInterstitialClosed = SaveLoadHandler.Load(_TIME_CLOSED_INTERSTITIAL, _timeInterstitialClosed);
            _timeAppOpenClosed = SaveLoadHandler.Load(_TIME_CLOSED_APP_OPEN, _timeAppOpenClosed);

            foreach (var kvp in json)
            {
                if (dic.TryGetValue(kvp.Key, out var value))
                {
                    value.adsGroupConfig = kvp.Value;
                    value.adsGroupValue.timeServer = TimeUtils.Now;
                    foreach (var adsValue in value.adsGroupValue.dictionaryAdsValue)
                    {
                        adsValue.Value.sessionCount = 0;
                    }
                }
                else
                {
                    var b = new SOAdsMOSetting.AdsGroupValue();
                    var a = new Dictionary<string, SOAdsMOSetting.AdsValue>();
                    foreach (var adsConfigV3 in kvp.Value.configAds)
                    {
                        a.Add(adsConfigV3.Key, new SOAdsMOSetting.AdsValue
                        {
                            dayCount = 0,
                            sessionCount = 0,
                            levelCount = 0,
                            level = 0,
                            currentDay = DateTime.Now.Date,
                            timeClosed = DateTime.Now.AddDays(-1)
                        });
                    }

                    b.dictionaryAdsValue = a;
                    b.group = kvp.Key;
                    b.timeServer = TimeUtils.Now;
                    if (GameData4MOInfo.Instance.viewCountInfo == null)
                    {
                        GameData4MOInfo.Instance.viewCountInfo = new Dictionary<string, AdsMoInfo>();
                    }

                    if (!GameData4MOInfo.Instance.viewCountInfo.ContainsKey(kvp.Key))
                    {
                        GameData4MOInfo.Instance.viewCountInfo.Add(kvp.Key, new AdsMoInfo
                        {
                            group = kvp.Key,
                            dateTime = b.timeServer.ToString(_FORMAT_DATE)
                        });
                    }
                    else
                    {
                        GameData4MOInfo.Instance.viewCountInfo[kvp.Key].dateTime = b.timeServer.ToString(_FORMAT_DATE);
                    }

                    GameData4MOInfo.Instance.UpdateToServer();

                    dic[kvp.Key] = new SOAdsMOSetting.AdsManager
                    {
                        adsGroupConfig = kvp.Value,
                        adsGroupValue = b
                    };
                }
            }

            return dic;
        }

        public int GetMaxViewByGroupId(string groupId)
        {
            foreach (var adsManager in _dictionaryAdsManager)
            {
                if (adsManager.Key.Equals(groupId))
                {
                    return adsManager.Value.adsGroupConfig.maxTotalAds;
                }
            }

            return -1;
        }

        public int GetViewCountByGroupId(string groupId)
        {
            if (GameData4MOInfo.Instance.viewCountInfo != null &&
                GameData4MOInfo.Instance.viewCountInfo.ContainsKey(groupId))
            {
                return GameData4MOInfo.Instance.viewCountInfo[groupId].viewCount;
            }

            return -1;
        }

        public void ShowBanner(string placement)
        {
            var adsConfigV3 = GetAdsConfigFromPlacementId(placement);
            if (adsConfigV3 != null)
            {
                if (LevelData.Instance.level >= adsConfigV3.levelUnlock && adsConfigV3.isActive)
                {
                    MediationManager.Instance.GetAdsService().ShowBanner(placement);
                }
            }
            else
            {
                MediationManager.Instance.GetAdsService().ShowBanner(placement);
            }
        }

        private DateTime _timeRewardedClosed = DateTime.Now.AddDays(-1);
        private DateTime _timeInterstitialClosed = DateTime.Now.AddDays(-1);
        private DateTime _timeAppOpenClosed = DateTime.Now.AddDays(-1);

        private bool CanShowInterstitial(string placementId)
        {
            var status = GetStatusAdsFromPlacement(placementId, false);
            return status.statusShowAds == StatusShowAds.Active;
        }

        public void ShowInterstitial(
            string placementId, Action onInterstitialClosed = null, Action onFail = null, bool needAdBreak = false)
        {
            if (!CanShowInterstitial(placementId))
            {
                onInterstitialClosed?.Invoke();
                return;
            }

            MediationManager.Instance.GetAdsService()
                .ShowInterstitial(placementId, onInterstitialClosed, onFail, needAdBreak);
        }

        private SOAdsMOSetting.AdsGroupConfig GetAdsGroupConfigFromPlacementId(string placementId)
        {
            foreach (var adsManager in _dictionaryAdsManager)
            {
                foreach (var adsConfigV3 in adsManager.Value.adsGroupConfig.configAds)
                {
                    if (adsConfigV3.Key.Equals(placementId))
                    {
                        return adsManager.Value.adsGroupConfig;
                    }
                }
            }

            return null;
        }

        private SOAdsMOSetting.AdsGroupValue GetAdsGroupValueFromPlacementId(string placementId)
        {
            foreach (var adsManager in _dictionaryAdsManager)
            {
                foreach (var adsValue in adsManager.Value.adsGroupValue.dictionaryAdsValue)
                {
                    if (adsValue.Key.Equals(placementId))
                    {
                        return adsManager.Value.adsGroupValue;
                    }
                }
            }

            return null;
        }

        private SOAdsMOSetting.AdsValue GetAdsValueFromPlacementId(string placementId)
        {
            foreach (var adsManager in _dictionaryAdsManager)
            {
                foreach (var adsValue in adsManager.Value.adsGroupValue.dictionaryAdsValue)
                {
                    if (adsValue.Key.Equals(placementId))
                    {
                        return adsValue.Value;
                    }
                }
            }

            var adsValue1 = new SOAdsMOSetting.AdsValue
            {
                dayCount = 0,
                sessionCount = 0,
                levelCount = 0,
                level = 0,
                currentDay = DateTime.Now.Date,
                timeClosed = DateTime.Now.AddDays(-1)
            };
            var adsGroupConfig = GetAdsGroupConfigFromPlacementId(placementId);
            _dictionaryAdsManager[adsGroupConfig.group].adsGroupValue.dictionaryAdsValue.Add(placementId, adsValue1);
            return adsValue1;
        }

        private SOAdsMOSetting.AdsConfigV3 GetAdsConfigFromPlacementId(string placementId)
        {
            foreach (var adsManager in _dictionaryAdsManager)
            {
                foreach (var adsConfigV3 in adsManager.Value.adsGroupConfig.configAds)
                {
                    if (adsConfigV3.Key.Equals(placementId))
                    {
                        return adsConfigV3.Value;
                    }
                }
            }

            return null;
        }


        public void AdsClosed(AdType adType, string placementId)
        {
            var adsGroupValue = GetAdsGroupValueFromPlacementId(placementId);
            if (adsGroupValue == null) return;
            var adsValue = GetAdsValueFromPlacementId(placementId);
            var level = LevelData.Instance.level;
            if (adsValue.level != level)
            {
                adsValue.level = level;
                adsValue.levelCount = 0;
            }

            if (adsValue.currentDay != TimeUtils.Now.Date)
            {
                adsValue.currentDay = TimeUtils.Now.Date;
                adsValue.dayCount = 0;
            }

            adsValue.dayCount++;
            adsValue.currentDay = TimeUtils.Now.Date;
            adsValue.levelCount++;
            adsValue.level = level;
            adsValue.sessionCount++;
            adsValue.timeClosed = TimeUtils.Now;

            if (GameData4MOInfo.Instance.viewCountInfo.ContainsKey(adsGroupValue.group))
            {
                var a = GameData4MOInfo.Instance.viewCountInfo[adsGroupValue.group];
                //tính ngày ở thời điểm hiện tại, so sánh với ngày trong gamedata4moinfo
                var dateNow = TimeUtils.Now.ToString(_FORMAT_DATE);
                var dateNowFromData = a.dateTime;
                if (!dateNow.Equals(dateNowFromData))
                {
                    a.viewCount = 0;
                    a.dateTime = dateNow;
                }

                a.viewCount++;
            }

            GameData4MOInfo.Instance.UpdateToServer();
            if (adType == AdType.Interstitial)
            {
                _timeInterstitialClosed = DateTime.Now;
                SaveLoadHandler.Save(_TIME_CLOSED_INTERSTITIAL, _timeInterstitialClosed);
            }
            else if (adType == AdType.Reward)
            {
                _timeRewardedClosed = DateTime.Now;
                SaveLoadHandler.Save(_TIME_CLOSED_REWARDED, _timeRewardedClosed);
            }
            else if (adType == AdType.AppOpen)
            {
                _timeAppOpenClosed = DateTime.Now;
                SaveLoadHandler.Save(_TIME_CLOSED_APP_OPEN, _timeAppOpenClosed);
            }

            SaveLoadHandler.Save(_ADS_LOAD_FROM_REMOTE_CONFIG, _dictionaryAdsManager);
        }

        private bool CanShowRewarded(string placementId)
        {
            var status = GetStatusAdsFromPlacement(placementId);
            return status.statusShowAds == StatusShowAds.Active;
        }

        public void ShowRewardedVideo(
            string placementId, Action onRewardedComplete = null, Action onFail = null,
            bool showInterstitialInstead = true)
        {
            if (!CanShowRewarded(placementId))
            {
                MediationManager.Instance.DebugError("Can't show rewarded at : " + placementId);
                return;
            }

            MediationManager.Instance.GetAdsService()
                .ShowRewardedVideo(placementId, onRewardedComplete, onFail, showInterstitialInstead);
        }

        private static int SecondsUntilNextUtcDay(DateTime timeServer)
        {
            var nextMidnightUtc = new DateTime(timeServer.Year, timeServer.Month, timeServer.Day, 0, 0, 0).AddDays(1);
            var timePassed = (TimeUtils.Now - timeServer).TotalSeconds;
            var secondsLeft = Math.Ceiling((nextMidnightUtc - timeServer).TotalSeconds) - timePassed;
            return (int)Math.Max(0, secondsLeft);
        }

        private int GetRemainingTimeByGroup(string groupId)
        {
            foreach (var adsManager in _dictionaryAdsManager)
            {
                if (adsManager.Key.Equals(groupId))
                {
                    return SecondsUntilNextUtcDay(adsManager.Value.adsGroupValue.timeServer);
                }
            }

            return -1;
        }

        public StatusAds GetStatusAdsFromPlacement(string placementId, bool isRewarded = true)
        {
            var statusAds = new StatusAds
            {
                remainingTime = -1,
                statusShowAds = StatusShowAds.Active,
                remainingAttempts = -1
            };
            var adsGroupConfig = GetAdsGroupConfigFromPlacementId(placementId);
            if (adsGroupConfig == null)
            {
                return statusAds;
            }

            //kiểm tra ngày hiện tại, so với ngày trong data4gameviewcount
            var dateNow = TimeUtils.Now.ToString(_FORMAT_DATE);
            var a = GameData4MOInfo.Instance.viewCountInfo[adsGroupConfig.group];
            var dateNowFromData = a.dateTime;
            if (!dateNowFromData.Equals(dateNow))
            {
                a.viewCount = 0;
            }

            //lấy giá trị viewcount so sánh
            if (adsGroupConfig.maxTotalAds != -1 && a.viewCount >= adsGroupConfig.maxTotalAds)
            {
                statusAds.statusShowAds = StatusShowAds.DayLimitGroup;
                statusAds.remainingTimeForGroup = GetRemainingTimeByGroup(adsGroupConfig.group);
                return statusAds;
            }

            var adsConfig = GetAdsConfigFromPlacementId(placementId);
            var adsValue = GetAdsValueFromPlacementId(placementId);

            var level = LevelData.Instance.level;
            if (!LevelData.Instance.startCurrentLevel)
            {
                level--;
            }

            if (adsConfig.isActive)
            {
                if (level >= adsConfig.levelUnlock)
                {
                    if (adsConfig.sessionLimit == -1 || adsConfig.sessionLimit > adsValue.sessionCount)
                    {
                        if (adsValue.currentDay.Date != DateTime.Now.Date)
                        {
                            adsValue.dayCount = 0;
                        }

                        if (adsConfig.dayLimit == -1 || adsConfig.dayLimit > adsValue.dayCount)
                        {
                            if (adsConfig.intervalLevel == -1 ||
                                adsConfig.intervalLevel <= Mathf.Abs(level - adsValue.level))
                            {
                                if (adsConfig.intervalTime == -1 || adsConfig.intervalTime <=
                                    (DateTime.Now - adsValue.timeClosed).TotalSeconds)
                                {
                                    if (adsValue.level != level)
                                    {
                                        adsValue.levelCount = 0;
                                    }

                                    if (adsConfig.levelLimit == -1 || adsConfig.levelLimit > adsValue.levelCount)
                                    {
                                        if (isRewarded)
                                        {
                                            statusAds.statusShowAds = StatusShowAds.Active;
                                        }
                                        else
                                        {
                                            if ((DateTime.Now - _timeRewardedClosed).TotalSeconds <
                                                adsConfig.intervalBetweenIv)
                                            {
                                                statusAds.statusShowAds = StatusShowAds.IntervalBetweenIv;
                                                statusAds.remainingTime = adsConfig.intervalBetweenIv -
                                                                          (DateTime.Now - _timeRewardedClosed)
                                                                          .TotalSeconds;
                                            }
                                            else if ((DateTime.Now - _timeInterstitialClosed).TotalSeconds <
                                                     adsConfig.intervalTime)
                                            {
                                                statusAds.statusShowAds = StatusShowAds.IntervalTime;
                                                statusAds.remainingTime = adsConfig.intervalTime -
                                                                          (DateTime.Now - _timeInterstitialClosed)
                                                                          .TotalSeconds;
                                            }
                                            else
                                            {
                                                statusAds.statusShowAds = StatusShowAds.Active;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        statusAds.statusShowAds = StatusShowAds.LevelLimit;
                                    }
                                }
                                else
                                {
                                    statusAds.statusShowAds = StatusShowAds.IntervalTime;
                                    statusAds.remainingTime = adsConfig.intervalTime -
                                                              (DateTime.Now - adsValue.timeClosed).TotalSeconds;
                                }
                            }
                            else
                            {
                                statusAds.statusShowAds = StatusShowAds.IntervalLevel;
                            }
                        }
                        else
                        {
                            statusAds.statusShowAds = StatusShowAds.DayLimit;
                        }
                    }
                    else
                    {
                        statusAds.statusShowAds = StatusShowAds.SessionLimit;
                    }
                }
                else
                {
                    statusAds.statusShowAds = StatusShowAds.LevelUnlock;
                }
            }
            else
            {
                statusAds.statusShowAds = StatusShowAds.DeActive;
            }

            List<int> values = new()
            {
                adsConfig.sessionLimit == -1
                    ? adsConfig.sessionLimit
                    : adsConfig.sessionLimit - adsValue.sessionCount,
                adsConfig.dayLimit == -1 ? adsConfig.dayLimit : adsConfig.dayLimit - adsValue.dayCount,
                adsConfig.levelLimit == -1 ? adsConfig.levelLimit : adsConfig.levelLimit - adsValue.levelCount
            };

            int minValue = -1;
            foreach (int v in values)
            {
                if (v == -1) continue;
                if (minValue == -1 || v < minValue)
                    minValue = v;
            }

            statusAds.remainingAttempts = minValue;


            return statusAds;
        }

        public RewardedCallback GetRewardedCallbackFromGroupId(string groupId)
        {
            var listReward = new List<Reward>();
            foreach (var adsManager in _dictionaryAdsManager)
            {
                if (adsManager.Key.Equals(groupId))
                {
                    var adsGroupConfig = adsManager.Value.adsGroupConfig;
                    var viewCount =
                        GameData4MOInfo.Instance.viewCountInfo[adsGroupConfig.group].viewCount +
                        1; //+ 1 để so sánh với mốc trên cms
                    foreach (var adsReward in adsGroupConfig.adsRewards)
                    {
                        listReward.Clear();
                        foreach (var reward in adsReward.rewards)
                        {
                            listReward.Add(reward);
                        }

                        if (viewCount >= adsReward.fromViewCount && viewCount <= adsReward.toViewCount)
                        {
                            return new RewardedCallback { listReward = listReward };
                        }
                    }

                    break;
                }
            }

            return new RewardedCallback { listReward = listReward };
        }
    }

    public static class MediationMoProviderBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            MediationMOProvider.Instance.Initialize();
        }
    }
}