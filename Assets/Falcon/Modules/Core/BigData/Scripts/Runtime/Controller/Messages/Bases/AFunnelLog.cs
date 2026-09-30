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
    public class AFunnelLog : AFalconLog
    {
        public FunnelParam param;
        public string funnelDay;

        /// <summary>
        /// Hình dạng van ĐANG ÁP tại client lúc log — SDK tự đóng trong decor
        /// (<see cref="FunnelShapeWire.ToWireName"/>), game không đụng. Không phải hằng số
        /// thiết kế mà là TRẠNG THÁI client có thể sai — chính vì thế nó mang tin: catalog giữ
        /// shape chuẩn, field này chở shape thực tế, lệch nhau = quên Register (DQ bắt được ngay
        /// ca funnel rơi về luật ftue trượt trắng, khỏi nội suy từ hành vi stream).
        /// </summary>
        public string funnelShape;

        [Preserve]
        protected AFunnelLog()
        {
        }

        public AFunnelLog(FunnelParam param)
        {
            this.param = param;
        }

        protected AFunnelLog(
            string funnelName, string action, int priority,
            int? currentLevel = null, FParam logParams = null
        )
        {
            if (logParams is FunnelParam funnelParam)
            {
                funnelParam.funnelName = funnelName;
                funnelParam.action = action;
                funnelParam.priority = priority;
                funnelParam.currentLevel = currentLevel;
                param = funnelParam;
            }
            else
            {
                param = new OldCodeSupportFunnelParam()
                {
                    funnelName = funnelName,
                    action = action,
                    priority = priority,
                    currentLevel = currentLevel,
                    extraMeta = logParams?.ToDictionary()
                };
            }

            AnalyticLogger.Instance.Info($"{GetType().Name}:{param.ToJson()}");
            param.CorrectValues();
        }

        public override string Event => "f_sdk_funnel_data";

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