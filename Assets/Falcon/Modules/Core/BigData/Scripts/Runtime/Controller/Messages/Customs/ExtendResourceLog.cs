/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-28
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    ///     Class log resource theo cấu trúc bên CSKH hay sử dụng
    /// </summary>
    [Serializable]
    public class ExtendResourceLog : FResourceLog
    {
        [Preserve]
        public ExtendResourceLog()
        {
        }

        public ExtendResourceLog(ResourceParam param) : base(param)
        {
        }

        public ExtendResourceLog(
            FlowType flowType, string itemType, string currency, string itemId, 
            long amount, long valueBefore,long valueAfter,
            int? currentLevel = null, Dictionary<string, object> detail = null, FParam param = null
        )
        {
            if (param is ResourceParam resourceParam)
            {
                // CLONE chứ không ghi vào object của caller: một context thường được dùng CHUNG
                // cho cả mẻ grant (foreach Grant(..., context)) — mutate là caller lãnh
                // side-effect sau vòng lặp, và wire hiện chỉ thoát nạn nhờ DataWrapper serialize
                // ngay tại Enqueue (ai đổi sang serialize-lúc-gửi là cả mẻ thành bản sao của vế
                // cuối). Soát owner 05/09.
                var clone = resourceParam is OldCodeSupportResourceParam oldSupport
                    ? new OldCodeSupportResourceParam { extraMeta = oldSupport.extraMeta }
                    : new ResourceParam();

                // Ngữ cảnh của caller — giữ nguyên vẹn từ context
                clone.resourceWhen = resourceParam.resourceWhen;
                clone.resourceWhere = resourceParam.resourceWhere;
                clone.exchangeId = resourceParam.exchangeId;
                clone.adViewId = resourceParam.adViewId;
                clone.transactionId = resourceParam.transactionId;

                // Số đo của GIAO DỊCH này — từ đối số chuỗi gọi
                clone.flowType = flowType;
                clone.itemType = itemType;
                clone.currency = currency;
                clone.itemId = itemId;
                clone.amount = amount;
                clone.currentLevel = currentLevel;
                clone.valueBefore = valueBefore;
                clone.valueAfter = valueAfter;

                clone.detail = MergeDetail(detail, resourceParam.detail);
                this.param = clone;
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
                    detail = detail,
                    valueBefore = valueBefore,
                    valueAfter = valueAfter,
                    extraMeta = param?.ToDictionary()
                };
            }

            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        /// <summary>
        /// Trộn TỪNG KEY hai nguồn detail (chốt owner 05/09, thay luật đè-toàn-cục cũ): key chỉ
        /// một bên có thì giữ; trùng key thì ĐỐI SỐ (thông tin tại-giao-dịch của chuỗi gọi) thắng
        /// — cùng khuôn PutIfAbsent/ApplyContext của cả hệ. Không mutate nguồn nào: context có
        /// thể đang được dùng chung cho cả mẻ.
        /// </summary>
        private static Dictionary<string, object> MergeDetail(
            Dictionary<string, object> fromCall, Dictionary<string, object> fromContext)
        {
            if (fromContext == null) return fromCall;
            if (fromCall == null) return new Dictionary<string, object>(fromContext);

            var merged = new Dictionary<string, object>(fromContext);
            foreach (var (key, value) in fromCall) merged[key] = value;
            return merged;
        }
    }
}