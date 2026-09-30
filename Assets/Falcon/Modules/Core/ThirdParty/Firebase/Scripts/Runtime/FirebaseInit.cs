/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */


#if FIREBASE_ENABLE && !UNITY_EDITOR
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.ThirdParty.Ump.Runtime;
using Firebase;
using UnityEngine;
using Firebase.Analytics;
//using Firebase.Crashlytics;
using Firebase.Extensions;

namespace Falcon.Modules.Core.ThirdParty.Firebase.Scripts.Runtime
{
    public class FirebaseInit : AutoSingleton<FirebaseInit>
    {
        private const string EVENT_BUS_LEVEL_START = "event_bus_level_start";
        private const string EVENT_BUS_LEVEL_COMPLETE = "event_bus_level_complete";
        private const string EVENT_BUS_LEVEL_FAILED = "event_bus_level_failed";

        public static bool isInitialize;
        private static bool _initStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            _ = Instance;
        }

        private void Start()
        {
            Init();
        }

        protected override void Awake()
        {
            base.Awake();
            RegisterEvent();
        }

        private void RegisterEvent()
        {
            GameEvent<int>.Register(EVENT_BUS_LEVEL_START, ActionStart, null);
            GameEvent<(string currentLevel, string timePlayed, int score)>.Register(EVENT_BUS_LEVEL_COMPLETE,
                ActionComplete, null);
            GameEvent<(string currentLevel, string failCount)>.Register(EVENT_BUS_LEVEL_FAILED, ActionFailed, null);
        }

        private LazyVal<IFDeviceInfoRepository> DeviceInfoRepository =
            new(MySingletonService.Instance<IFDeviceInfoRepository>);

        void SetFirebaseUserId()
        {
            var userId = DeviceInfoRepository.Value.DeviceId;
            FirebaseAnalytics.SetUserId(userId);
        }

        private void Init()
        {
            if (_initStarted) return;
            _initStarted = true;
            FirebaseApp.CheckAndFixDependenciesAsync()
                .ContinueWithOnMainThread(task =>
                {
                    var dependencyStatus = task.Result;
                    if (dependencyStatus == DependencyStatus.Available)
                    {
                        isInitialize = true;
                        Debug.Log("Firebase init success");
                        SetFirebaseUserId();
                    FirebaseApp app = FirebaseApp.DefaultInstance;

                    //Crashlytics.IsCrashlyticsCollectionEnabled = true;
                    
                    // When this property is set to true, Crashlytics will report all
                    // uncaught exceptions as fatal events. This is the recommended behavior.
                    //Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                    }
                    else
                    {
                        Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                    }
                });
        }

        private void ActionFailed((string currentLevel, string failCount) obj)
        {
            FalconFirebaseLog.Log(FalconFirebaseLog.LEVEL_FAIL, obj.currentLevel, obj.failCount);
        }

        private void ActionComplete((string currentLevel, string timePlayed, int score) obj)
        {
            FalconFirebaseLog.Log(FalconFirebaseLog.LEVEL_COMPLETE, obj.currentLevel, obj.timePlayed);
        }

        private void ActionStart(int currentLevel)
        {
            FalconFirebaseLog.Log(FalconFirebaseLog.LEVEL_START, "" + currentLevel);
        }
    }
}

#endif