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
    public class FResourceLog : AFalconLog
    {
        public ResourceParam param;

        [Preserve]
        public FResourceLog()
        {
        }

        public FResourceLog(ResourceParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public FResourceLog(
            FlowType flowType, string itemType, string currency, string itemId, long amount,
            int? currentLevel = null, FParam param = null
        )
        {
            if (param is ResourceParam resourceParam)
            {
                resourceParam.flowType = flowType;
                resourceParam.itemType = itemType;
                resourceParam.currency = currency;
                resourceParam.itemId = itemId;
                resourceParam.amount = amount;
                resourceParam.currentLevel = currentLevel;
                this.param = resourceParam;
            }
            else
            {
                this.param = new OldCodeSupportResourceParam
                {
                    flowType = flowType,
                    itemType = itemType,
                    currency = currency,
                    itemId = itemId,
                    amount = amount,
                    currentLevel = currentLevel,
                    extraMeta = param?.ToDictionary()
                };
            }

            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_resource_data";

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