/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using Falcon.Helpers.Devkit;
using UnityEngine;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class FFilteredFunnelLog : AFunnelLog
    {

        [FKey(Ignore = true)]public bool logValid = true;

        [Preserve]
        public FFilteredFunnelLog()
        {
        }

        public FFilteredFunnelLog(FunnelParam param) : base(param)
        {
            var decorInfo = FunnelLogService.Instance.Check(this);
            if (!decorInfo.valid)
            {
                logValid = false;
                Debug.LogError(decorInfo.error);
            }
            else
            {
                logValid = true;
            }
        }

        public FFilteredFunnelLog(string funnelName, string action, int priority, int? currentLevel = null, FParam logParams = null) 
            : base(funnelName, action, priority, currentLevel, logParams)
        {
            var decorInfo = FunnelLogService.Instance.Check(this);
            if (!decorInfo.valid)
            {
                logValid = false;
                Debug.LogError(decorInfo.error);
            }
            else
            {
                logValid = true;
            }
        }

        /// <summary>
        /// Bước sai luật thì bản tin bị chặn ngay tại funnel của pipeline — mọi đường vào đều chịu,
        /// kể cả khi ai đó cầm log này gọi thẳng <c>Controller.Send</c> hay <c>Enqueue</c>.
        /// </summary>
        public override bool IsSendable => logValid;
    }
}