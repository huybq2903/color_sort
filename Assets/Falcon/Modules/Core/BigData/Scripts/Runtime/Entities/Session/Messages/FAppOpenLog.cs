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
    /// Mốc app vào foreground (hợp đồng §B). Bản Cold đồng thời là mốc MỞ PHIÊN — entity session
    /// (session_uid) trước đây chỉ có id mà không có event mở; xem EntityLifecycle-Design.md.
    /// SDK-core tự bắn, dev game không phải gọi gì.
    /// </summary>
    [Serializable]
    public class FAppOpenLog : AFalconLog
    {
        public AppOpenParam param;

        [Preserve]
        public FAppOpenLog()
        {
        }

        public FAppOpenLog(AppOpenParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_app_open_data";

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
