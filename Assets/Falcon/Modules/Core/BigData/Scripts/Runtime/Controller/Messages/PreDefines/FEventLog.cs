/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class FEventLog : AFalconLog
    {
        public EventParam param;

        [Preserve]
        public FEventLog()
        {
        }

        public FEventLog(EventParam param)
        {
            AnalyticLogger.Instance.Info($"{GetType().Name}:{param.ToJson()}");
            param.CorrectValues();
            this.param = param;
        }

        public FEventLog(string eventName, Dictionary<string, object> param = null, 
            int? currentLevel = null, FParam logParams = null
            )
        {
            if (logParams is EventParam eventParam)
            {
                eventParam.eventName = eventName;
                eventParam.param = param;
                eventParam.currentLevel = currentLevel;
                this.param = eventParam;
            }
            else
            {
                this.param = new OldCodeSupportEventParam()
                {
                    eventName = eventName,
                    param = param,
                    currentLevel = currentLevel,
                    extraMeta = logParams?.ToDictionary()
                };
            }

            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_event_data";

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Remove(nameof(param));
            foreach (var (key, value) in param.ToDictionary())
                result.PutIfAbsentAndNotNull(key, value);
            return result;
        }
    }
}