using System.Collections.Generic;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.ActionLog.Runtime
{
    [FAMessage("cs_action_log")]
    public class CSActionLog : CSMessageWaitLoginSuccess
    {
        public string funnel_type;
        public string action;
        public Dictionary<string, string> param;
        public long timestamp = System.DateTime.Now.Ticks;
        public CSActionLog(string funnel_type, string action, Dictionary<string, string> param)
        {
            this.funnel_type = funnel_type;
            this.action = action;
            this.param = param;
        }
    }
}