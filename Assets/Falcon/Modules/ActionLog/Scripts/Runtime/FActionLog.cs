using System;
using System.Collections.Generic;
using Falcon.Modules.Core.BigData;

namespace Falcon.Modules.ActionLog.Runtime
{
    public class FActionLog : AFalconLog
    {
        public String action;
        public Dictionary<String, String> param;
        public long timestamp;
        
        public FActionLog(String action, Dictionary<String, String> param)
        {
            LogParams(action, param);
            this.action = action;
            this.param = param;
            this.timestamp = DateTime.Now.Ticks;
        }
        
        public override string Event => "action_log";
    }
}