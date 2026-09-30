/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 */
// 2026-02-11

using System.Collections.Generic;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FAMessage("cs_ad_request_log")]
    public class CSAdRequestLog : CSMessageWaitLoginSuccess
    {
        public List<AdRequestLogItem> logs;
        public long timeStartLog;
        public long timeEndLog;

        public CSAdRequestLog(List<AdRequestLogItem> logs, long timeStartLog, long timeEndLog)
        {
            this.logs = logs;
            this.timeStartLog = timeStartLog;
            this.timeEndLog = timeEndLog;
        }
    }

    public class AdSuccessInfo
    {
        public int index;
        public double revenueUsd;
        public long time;
        public string adNetwork;
        public string adMediation;
    }

    public class AdRequestLogItem
    {
        public string adUnitId;
        public string adType;
        public string decisionPolicy;
        public int numRequest;
        public Dictionary<string, int> errorDetail;
        public List<AdSuccessInfo> adSuccessInfos;
    }
}