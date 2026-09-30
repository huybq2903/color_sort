/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Mở một lượt mua (gọi launchBillingFlow / StoreKit payment) — hợp đồng §D4.
    /// Đây là điểm vào của phễu checkout: hiện tại server chỉ thấy mua-XONG nên abandonment mù hoàn toàn.
    /// </summary>
    [Serializable]
    public class FIapStartPurchaseLog : AIapCheckoutLog
    {
        public IapPurchaseAttemptParam param;

        [Preserve]
        public FIapStartPurchaseLog()
        {
        }

        public FIapStartPurchaseLog(IapPurchaseAttemptParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_iap_start_purchase_data";

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Remove(nameof(param));
            foreach (var (key, value) in param.ToDictionary())
                result.PutIfAbsentAndNotNull(key, value);
            return result;
        }
    }

    /// <summary>
    /// Lượt mua kết thúc bằng thất bại/huỷ/treo — hợp đồng §D4. Mang cùng
    /// <see cref="AIapCheckoutLog.purchaseAttemptId"/> với log mở lượt để khớp phễu.
    /// </summary>
    [Serializable]
    public class FIapPurchaseFailLog : AIapCheckoutLog
    {
        public IapPurchaseFailParam param;

        [Preserve]
        public FIapPurchaseFailLog()
        {
        }

        public FIapPurchaseFailLog(IapPurchaseFailParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_iap_purchase_fail_data";

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
