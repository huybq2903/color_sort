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
    public class FInAppLog : AIapCheckoutLog
    {
        public InAppParam param;

        /// <summary>
        ///     Lần hiển thị offer dẫn tới giao dịch này — SDK-core tự đóng dấu khi productId khớp
        ///     offer đang hiển thị (§C). Không quy được thì vắng mặt, KHÔNG đoán bừa.
        /// </summary>
        [FKey(RemoveIfNull = true)] public string offerImpressionId;

        [Preserve]
        public FInAppLog()
        {
        }


        public FInAppLog(InAppParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }


        public FInAppLog(
            string productId, decimal localizedPrice, string isoCurrencyCode,
            string where, string transactionId, string purchaseToken = null,
            int? currentLevel = null, string purchaseMethod = null, FParam param = null
        )
        {
            if (param is InAppParam inAppParam)
            {
                inAppParam.productId = productId;
                inAppParam.localizedPrice = localizedPrice;
                inAppParam.isoCurrencyCode = isoCurrencyCode;
                inAppParam.where = where;
                inAppParam.transactionId = transactionId;
                inAppParam.purchaseToken = purchaseToken;
                inAppParam.currentLevel = currentLevel;
                inAppParam.purchaseMethod = purchaseMethod;
                this.param = inAppParam;
            }
            else
            {
                this.param = new OldCodeSupportInAppParam()
                {
                    productId = productId,
                    localizedPrice = localizedPrice,
                    isoCurrencyCode = isoCurrencyCode,
                    where = where,
                    transactionId = transactionId,
                    purchaseToken = purchaseToken,
                    currentLevel = currentLevel,
                    extraMeta = param?.ToDictionary()
                };
            }

            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_in_app_data";

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