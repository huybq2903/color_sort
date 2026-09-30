using System;
using System.Collections.Generic;
using Falcon.Modules.Core.RemoteConfig;

namespace Falcon.Modules.ActionLog.Runtime
{
    public class FActionLogManager
    {
        public static void Log(String action, Dictionary<String, String> param){
            if (FConfigController.Instance.Config<FActionRemoteConfig>().enableActionLog == 1)
                new FActionLog(action, param).Send();
        }
        
        public static void Log(String action){
            if (FConfigController.Instance.Config<FActionRemoteConfig>().enableActionLog == 1)
            new FActionLog(action, null).Send();
        }
    }
}