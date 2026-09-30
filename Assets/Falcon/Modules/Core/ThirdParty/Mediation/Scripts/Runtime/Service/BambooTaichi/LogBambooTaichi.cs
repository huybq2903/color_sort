using System;
using UnityEngine;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.RemoteConfigCms;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Modules.Core.ThirdParty.Firebase.Scripts.Runtime;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class LogBambooTaichi
    {
        private const string _BAM_BOO_COUNT_TRACKER_COUNT = "BamBoo_Count_Tracker_Count";
        private const string _BAM_BOO_COUNT_TRACKER_TOTAL_FOR_COUNT = "BamBoo_Count_Tracker_Total_For_Count";

        private const string _BAM_BOO_V3_COUNT_TRACKER_COUNT = "BamBoo_3_0_Count_Tracker_Count";
        private const string _BAM_BOO_V3_COUNT_TRACKER_TOTAL_FOR_COUNT = "BamBoo_3_0_Count_Tracker_Total_For_Count";
        private const string _BAM_BOO_V3_COUNT_TRACKER_CONTINUOUS = "BamBoo_3_0_Count_Tracker_Continuous";

        private const string _TAI_CHI_2_0_COUNT_TRACKER_TOTAL_FOR_THRESHOLD =
            "TaiChi_2_0_Count_Tracker_Total_For_Threshold";

        private const string _TAI_CHI_4_0_COUNT_TRACKER_TOTAL_FOR_THRESHOLD =
            "TaiChi_4_0_Count_Tracker_Total_For_Threshold";

        private const string _TAI_CHI_4_0_COUNT_TRACKER_IS_UNLOCKED = "TaiChi_4_0_Count_Tracker_Is_Unlocked";

        private List<CountTracker> _countTrackers;
        private List<ThresholdTracker> _thresholdTaiChi2Trackers;
        private List<ThresholdTracker> _thresholdTaiChi4Trackers;
        private AdsCountEventsWrapper _adsCountEventsWrapper;
        private AdsThresholdEventsWrapper _adsThresholdTaiChi2EventsWrapper;
        private AdsThresholdEventsWrapper _adsThresholdTaiChi4EventsWrapper;

        //bb2
        private List<CountTracker3> _countTrackers3;
        private AdsCountEvents3Wrapper _adsCountEvents3Wrapper;

        private static LogBambooTaichi _instance;

        public static LogBambooTaichi Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LogBambooTaichi();
                    _instance.ProgressRemoteConfig();
                }

                return _instance;
            }
        }

        private void ProgressRemoteConfig()
        {
            InitTrackersBamBoo();
            InitTrackersBamBoo3();
            InitTrackersTaiChi2();
            InitTrackersTaiChi4();
        }

        private void InitTrackersBamBoo3()
        {
            try
            {
                var bamBooThreshold = FConfigControllerCms.Instance.Config<MediationConfig>().bamBoo3AdsThreshold;
                if (!string.IsNullOrEmpty(bamBooThreshold))
                {
                    _adsCountEvents3Wrapper = JsonUtility.FromJson<AdsCountEvents3Wrapper>(bamBooThreshold);
                    _countTrackers3 = new List<CountTracker3>(_adsCountEvents3Wrapper.ads_count_events.Length);
                    foreach (var t in _adsCountEvents3Wrapper.ads_count_events)
                    {
                        _countTrackers3.Add(
                            new CountTracker3(t.count, t.name, t.rewarded, t.interstitial, t.continuous));
                    }

                    for (int i = 0; i < _countTrackers3.Count; i++)
                    {
                        _countTrackers3[i].count = SaveLoadHandler.Load(
                            $"{_BAM_BOO_V3_COUNT_TRACKER_COUNT}_{_countTrackers3[i].eventName}", 0);
                        _countTrackers3[i].totalForCount = SaveLoadHandler.Load(
                            $"{_BAM_BOO_V3_COUNT_TRACKER_TOTAL_FOR_COUNT}_{_countTrackers3[i].eventName}", 0d);
                        _countTrackers3[i].continuousActive = SaveLoadHandler.Load(
                            $"{_BAM_BOO_V3_COUNT_TRACKER_CONTINUOUS}_{_countTrackers3[i].eventName}", false);
                    }
                }
                else
                {
                    MediationManager.Instance.DebugLog("bb 3 is empty");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void InitTrackersBamBoo()
        {
            try
            {
                var bamBooThreshold = FConfigControllerCms.Instance.Config<MediationConfig>().bamBooAdsThreshold;
                if (!string.IsNullOrEmpty(bamBooThreshold))
                {
                    _adsCountEventsWrapper = JsonUtility.FromJson<AdsCountEventsWrapper>(bamBooThreshold);
                    _countTrackers = new List<CountTracker>(_adsCountEventsWrapper.ads_count_events.Length);
                    foreach (var t in _adsCountEventsWrapper.ads_count_events)
                    {
                        _countTrackers.Add(new CountTracker(t.count, t.name));
                    }

                    for (int i = 0; i < _countTrackers.Count; i++)
                    {
                        if (SaveLoadHandler.ExistsKey($"{_BAM_BOO_COUNT_TRACKER_COUNT}_{_countTrackers[i].eventName}"))
                        {
                            _countTrackers[i].count = SaveLoadHandler.Load(
                                $"{_BAM_BOO_COUNT_TRACKER_COUNT}_{_countTrackers[i].eventName}", 0);
                            _countTrackers[i].totalForCount = SaveLoadHandler.Load(
                                $"{_BAM_BOO_COUNT_TRACKER_TOTAL_FOR_COUNT}_{_countTrackers[i].eventName}", 0d);
                        }
                        else
                        {
                            _countTrackers[i].count = SaveLoadHandler.Load($"{_BAM_BOO_COUNT_TRACKER_COUNT}_{i}", 0);
                            _countTrackers[i].totalForCount =
                                SaveLoadHandler.Load($"{_BAM_BOO_COUNT_TRACKER_TOTAL_FOR_COUNT}_{i}", 0d);
                        }
                    }
                }
                else
                {
                    MediationManager.Instance.DebugLog("bamboo is empty");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void InitTrackersTaiChi2()
        {
            try
            {
                var taiChi2Threshold = FConfigControllerCms.Instance.Config<MediationConfig>().taiChi2AdsThreshold;
                if (!string.IsNullOrEmpty(taiChi2Threshold))
                {
                    _adsThresholdTaiChi2EventsWrapper =
                        JsonUtility.FromJson<AdsThresholdEventsWrapper>(taiChi2Threshold);
                    _thresholdTaiChi2Trackers =
                        new List<ThresholdTracker>(_adsThresholdTaiChi2EventsWrapper.ads_threshold_events.Length);
                    foreach (var t in _adsThresholdTaiChi2EventsWrapper.ads_threshold_events)
                    {
                        _thresholdTaiChi2Trackers.Add(new ThresholdTracker(t.value, t.name, false));
                    }

                    for (int i = 0; i < _thresholdTaiChi2Trackers.Count; i++)
                    {
                        if (SaveLoadHandler.ExistsKey(
                                $"{_TAI_CHI_2_0_COUNT_TRACKER_TOTAL_FOR_THRESHOLD}_{_thresholdTaiChi2Trackers[i].eventName}"))
                        {
                            _thresholdTaiChi2Trackers[i].totalForThreshold = SaveLoadHandler.Load(
                                $"{_TAI_CHI_2_0_COUNT_TRACKER_TOTAL_FOR_THRESHOLD}_{_thresholdTaiChi2Trackers[i].eventName}",
                                0d);
                        }
                        else
                        {
                            _thresholdTaiChi2Trackers[i].totalForThreshold =
                                SaveLoadHandler.Load($"{_TAI_CHI_2_0_COUNT_TRACKER_TOTAL_FOR_THRESHOLD}_{i}", 0d);
                        }
                    }
                }
                else
                {
                    MediationManager.Instance.DebugLog("taichi2 is empty");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void InitTrackersTaiChi4()
        {
            try
            {
                var taiChi4Threshold = FConfigControllerCms.Instance.Config<MediationConfig>().taiChi4AdsThreshold;
                if (!string.IsNullOrEmpty(taiChi4Threshold))
                {
                    _adsThresholdTaiChi4EventsWrapper =
                        JsonUtility.FromJson<AdsThresholdEventsWrapper>(taiChi4Threshold);
                    _thresholdTaiChi4Trackers =
                        new List<ThresholdTracker>(_adsThresholdTaiChi4EventsWrapper.ads_threshold_events.Length);
                    foreach (var t in _adsThresholdTaiChi4EventsWrapper.ads_threshold_events)
                    {
                        _thresholdTaiChi4Trackers.Add(new ThresholdTracker(t.value, t.name, t.continuous));
                    }

                    for (int i = 0; i < _thresholdTaiChi4Trackers.Count; i++)
                    {
                        _thresholdTaiChi4Trackers[i].totalForThreshold = SaveLoadHandler.Load(
                            $"{_TAI_CHI_4_0_COUNT_TRACKER_TOTAL_FOR_THRESHOLD}_{_thresholdTaiChi4Trackers[i].eventName}",
                            0d);
                        _thresholdTaiChi4Trackers[i].isUnlocked = SaveLoadHandler.Load(
                            $"{_TAI_CHI_4_0_COUNT_TRACKER_IS_UNLOCKED}_{_thresholdTaiChi4Trackers[i].eventName}",
                            false);
                    }
                }
                else
                {
                    MediationManager.Instance.DebugLog("taichi3 is empty");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public void Log(double value, AdType type)
        {
            if (type == AdType.Interstitial || type == AdType.Reward || type == AdType.AppOpen)
            {
                if (_countTrackers3 != null)
                {
                    for (int i = 0; i < _countTrackers3.Count; i++)
                    {
                        var tracker = _countTrackers3[i];
                        if (tracker.continuousActive && tracker.continuous)
                        {
                            FalconFirebaseLog.LogBamBoo(tracker.eventName, value);
                        }
                        else
                        {
                            if ((tracker.rewarded && type == AdType.Reward) ||
                                (tracker.interstitial && type == AdType.Interstitial))
                            {
                                tracker.count++;
                                tracker.totalForCount += value;
                                if (tracker.count >= tracker.limit)
                                {
                                    FalconFirebaseLog.LogBamBoo(tracker.eventName, tracker.totalForCount);
                                    tracker.count = 0;
                                    tracker.totalForCount = 0;
                                    if (tracker.continuous)
                                    {
                                        tracker.continuousActive = true;
                                        SaveLoadHandler.Save(
                                            $"{_BAM_BOO_V3_COUNT_TRACKER_CONTINUOUS}_{tracker.eventName}",
                                            tracker.continuousActive);
                                    }
                                }

                                SaveLoadHandler.Save($"{_BAM_BOO_V3_COUNT_TRACKER_COUNT}_{tracker.eventName}",
                                    tracker.count);
                                SaveLoadHandler.Save($"{_BAM_BOO_V3_COUNT_TRACKER_TOTAL_FOR_COUNT}_{tracker.eventName}",
                                    tracker.totalForCount);
                            }
                        }
                    }
                }

                if (type == AdType.Interstitial || type == AdType.Reward)
                {
                    if (_countTrackers != null)
                    {
                        for (int i = 0; i < _countTrackers.Count; i++)
                        {
                            var tracker = _countTrackers[i];
                            tracker.count++;
                            tracker.totalForCount += value;
                            if (tracker.count >= tracker.limit)
                            {
                                FalconFirebaseLog.LogBamBoo(tracker.eventName, tracker.totalForCount);
                                tracker.count = 0;
                                tracker.totalForCount = 0;
                            }

                            SaveLoadHandler.Save($"{_BAM_BOO_COUNT_TRACKER_COUNT}_{tracker.eventName}", tracker.count);
                            SaveLoadHandler.Save($"{_BAM_BOO_COUNT_TRACKER_TOTAL_FOR_COUNT}_{tracker.eventName}",
                                tracker.totalForCount);
                        }
                    }

                    if (_thresholdTaiChi2Trackers != null)
                    {
                        for (int i = 0; i < _thresholdTaiChi2Trackers.Count; i++)
                        {
                            var tracker = _thresholdTaiChi2Trackers[i];
                            tracker.totalForThreshold += value;
                            while (tracker.totalForThreshold >= tracker.threshold)
                            {
                                FalconFirebaseLog.LogTaiChi(tracker.eventName, tracker.threshold);
                                tracker.totalForThreshold -= tracker.threshold;
                            }

                            SaveLoadHandler.Save(
                                $"{_TAI_CHI_2_0_COUNT_TRACKER_TOTAL_FOR_THRESHOLD}_{tracker.eventName}",
                                tracker.totalForThreshold);
                        }
                    }

                    if (_thresholdTaiChi4Trackers != null)
                    {
                        for (int i = 0; i < _thresholdTaiChi4Trackers.Count; i++)
                        {
                            var tracker = _thresholdTaiChi4Trackers[i];
                            if (tracker.continuous)
                            {
                                if (!tracker.isUnlocked)
                                {
                                    tracker.totalForThreshold += value;
                                    if (tracker.totalForThreshold >= tracker.threshold)
                                    {
                                        tracker.isUnlocked = true;
                                        FalconFirebaseLog.LogTaiChi(tracker.eventName, tracker.totalForThreshold);
                                        SaveLoadHandler.Save(
                                            $"{_TAI_CHI_4_0_COUNT_TRACKER_IS_UNLOCKED}_{tracker.eventName}",
                                            tracker.isUnlocked);
                                    }
                                }
                                else
                                {
                                    FalconFirebaseLog.LogTaiChi(tracker.eventName, value);
                                }
                            }
                            else
                            {
                                tracker.totalForThreshold += value;
                                while (tracker.totalForThreshold >= tracker.threshold)
                                {
                                    FalconFirebaseLog.LogTaiChi(tracker.eventName, tracker.threshold);
                                    tracker.totalForThreshold -= tracker.threshold;
                                }
                            }

                            SaveLoadHandler.Save(
                                $"{_TAI_CHI_4_0_COUNT_TRACKER_TOTAL_FOR_THRESHOLD}_{tracker.eventName}",
                                tracker.totalForThreshold);
                        }
                    }
                }
            }
        }

        private class CountTracker
        {
            public readonly int limit;
            public int count;
            public double totalForCount;
            public readonly string eventName;

            public CountTracker(int limit, string eventName)
            {
                this.limit = limit;
                count = 0;
                totalForCount = 0;
                this.eventName = eventName;
            }
        }

        private class CountTracker3
        {
            public readonly int limit;
            public int count;
            public double totalForCount;
            public readonly string eventName;
            public readonly bool rewarded;
            public readonly bool interstitial;
            public readonly bool continuous;
            public bool continuousActive;

            public CountTracker3(int limit, string eventName, bool rewarded, bool interstitial, bool continuous)
            {
                this.limit = limit;
                count = 0;
                totalForCount = 0;
                this.eventName = eventName;
                this.rewarded = rewarded;
                this.interstitial = interstitial;
                this.continuous = continuous;
                continuousActive = false;
            }
        }

        private class ThresholdTracker
        {
            public readonly double threshold;
            public double totalForThreshold;
            public readonly string eventName;
            public bool isUnlocked;
            public bool continuous;

            public ThresholdTracker(double threshold, string eventName, bool continuous)
            {
                this.threshold = threshold;
                totalForThreshold = 0;
                this.eventName = eventName;
                isUnlocked = false;
                this.continuous = continuous;
            }
        }
    }

    [Serializable]
    public class AdsCountEvents3Wrapper
    {
        public AdsCountEvent3[] ads_count_events;
    }

    [Serializable]
    public class AdsCountEvent3
    {
        public string name;
        public int count;
        public bool rewarded; //true : áp dụng với rewarded
        public bool interstitial; //true : áp dụng với interstitial
        public bool continuous;
    }

    [Serializable]
    public class AdsCountEventsWrapper
    {
        public AdsCountEvent[] ads_count_events;
    }

    [Serializable]
    public class AdsCountEvent
    {
        public string name;
        public int count;
    }

    [Serializable]
    public class AdsThresholdEventsWrapper
    {
        public AdsThresholdEvent[] ads_threshold_events;
    }

    [Serializable]
    public class AdsThresholdEvent
    {
        public string name;
        public double value;
        public bool continuous;
    }
}