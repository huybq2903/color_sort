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
    public class FAdLog : AAdLog
    {
        public int typeCount;
        public AdParam param;
        public double? adLtv;

        /// <summary>
        /// Độ trễ fill: từ lúc xin ad tới lúc ad lên hình (ms) — SDK-core tự đo (§D2). Server cũng
        /// tự trừ timestamp giữa <c>ad_request</c> và event này được; đây là bản client đo sẵn làm
        /// fallback. Vắng mặt khi impression không đi từ request nào của SDK.
        /// </summary>
        [FKey(RemoveIfNull = true)] public int? fillLatencyMs;

        /// <summary>
        /// Số impression mà bản tin này gộp (§G) — chỉ khác 1 ở dòng banner đã nén
        /// (<see cref="BannerLogService"/>). Server đếm impression banner phải dùng
        /// <c>sum(impressionCount)</c> chứ không phải <c>count(*)</c>.
        /// </summary>
        public int impressionCount = 1;

        [Preserve]
        public FAdLog()
        {
        }

        public FAdLog(AdParam param)
        {
            AnalyticLogger.Instance.Info($"{GetType().Name}:{param.ToJson()}");
            param.CorrectValues();
            this.param = param;
        }

        public FAdLog(AdType type, string adWhere, string adPrecision, string adCountry, double adRev,
            string adNetwork, string adMediation, int? currentLevel = null, FParam param = null)
        {
            if (param is AdParam adParam)
            {
                adParam.type = type;
                adParam.adWhere = adWhere;
                adParam.adPrecision = adPrecision;
                adParam.adCountry = adCountry;
                adParam.adRev = adRev;
                adParam.adNetwork = adNetwork;
                adParam.adMediation = adMediation;
                adParam.currentLevel = currentLevel;
                this.param = adParam;
            }
            else
            {
                this.param = new OldCodeSupportAdParam
                {
                    type = type,
                    adWhere = adWhere,
                    adPrecision = adPrecision,
                    adCountry = adCountry,
                    adRev = adRev,
                    adNetwork = adNetwork,
                    adMediation = adMediation,
                    currentLevel = currentLevel,
                    extraMeta = param?.ToDictionary()
                };
            }

            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_ads_data";

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