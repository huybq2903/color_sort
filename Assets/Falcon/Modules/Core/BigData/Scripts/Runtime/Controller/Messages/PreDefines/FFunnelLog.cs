/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using UnityEngine;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class FFunnelLog : AFunnelLog
    {
        [Preserve]
        public FFunnelLog()
        {
        }

        public FFunnelLog(FunnelParam param) : base(param)
        {
            var decorInfo = FunnelLogService.Instance.Check(this);
            if (!decorInfo.valid) Debug.LogError(decorInfo.error);
        }

        protected FFunnelLog(
            string funnelName, string action, int priority,
            int? currentLevel = null, FParam logParams = null
        ) : base(funnelName, action, priority, currentLevel, logParams)
        {
            var decorInfo = FunnelLogService.Instance.Check(this);
            if (!decorInfo.valid) Debug.LogError(decorInfo.error);
        }
    }
}