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
    /// Mốc xin ad từ mediation (hợp đồng §B) — điểm vào của phễu fill.
    /// Ad hiển thị được sẽ có log impression mang cùng <c>adViewId</c>; request không có
    /// impression tương ứng chính là view fill-fail (không cần event fail riêng).
    /// </summary>
    [Serializable]
    public class FAdRequestLog : AAdLog
    {
        public AdViewParam param;

        [Preserve]
        public FAdRequestLog()
        {
        }

        public FAdRequestLog(AdViewParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_ad_request_data";

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
    /// Mốc gọi hiển thị ad — khoảng giữa request và impression. Show mà không có impression mang
    /// cùng <c>adViewId</c> nghĩa là hiển thị hỏng (display failure).
    /// </summary>
    [Serializable]
    public class FAdShowLog : AAdLog
    {
        public AdShowParam param;

        /// <summary>
        /// Từ lúc xin ad tới lúc gọi hiển thị (ms) — SDK-core tự đo. Null khi lần show này không đi
        /// từ một request nào của SDK (không bịa 0).
        /// </summary>
        [FKey(RemoveIfNull = true)] public int? requestToShowMs;

        [Preserve]
        public FAdShowLog()
        {
        }

        public FAdShowLog(AdShowParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_ad_show_data";

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
    /// Mốc ad đóng lại, người chơi quay về game — mốc cuối của vòng đời một lần xem ad.
    /// </summary>
    [Serializable]
    public class FAdCloseLog : AAdLog
    {
        public AdCloseParam param;

        /// <summary>Ad nằm trên màn hình bao lâu (giây) — SDK-core tự đo từ mốc show.</summary>
        [FKey(RemoveIfNull = true)] public int? shownDurationSec;

        [Preserve]
        public FAdCloseLog()
        {
        }

        public FAdCloseLog(AdCloseParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_ad_close_data";

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
