using System.Diagnostics;
using Falcon.Helpers.Singleton;
using UnityEngine;

namespace Falcon.Modules.ActionLog.Runtime
{
    public class FActionLogMono : PersistentSingleton<FActionLogMono>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAppStart()
        {
            FActionLogMono instance = FActionLogMono.Instance;
        }

        void Start()
        {
            FActionLogManager.Log("app_start");
        }
    }
}