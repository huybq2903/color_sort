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
    public class FSessionLog : AFalconLog
    {
        public SessionParam param;
        public long modeTotalTime;

        /// <summary>
        /// FPS trung bình của quãng chơi mà bản tin này khai (§D7) — SDK-core tự đo, game không set.
        /// Muốn gộp cả phiên thì cân theo <see cref="SessionParam.sessionTime"/> của từng bản tin.
        /// </summary>
        [FKey(RemoveIfNull = true)] public float? avgFps;

        /// <summary>Số cú tụt khung nặng trong quãng (ngưỡng <see cref="PerfSampleState.FRAME_DROP_SEC"/>).</summary>
        [FKey(RemoveIfNull = true)] public int? frameDropCount;

        /// <summary>Số lần hệ điều hành cảnh báo thiếu bộ nhớ (onTrimMemory / didReceiveMemoryWarning).</summary>
        [FKey(RemoveIfNull = true)] public int? memoryWarningCount;

        [Preserve]
        public FSessionLog()
        {
        }

        public FSessionLog(SessionParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public FSessionLog(
            string gameMode, TimeSpan sessionTime,
            int? currentLevel = null, FParam param = null
        )
        {
            if (param is SessionParam sessionParam)
            {
                sessionParam.gameMode = gameMode;
                sessionParam.sessionTime = sessionTime;
                sessionParam.currentLevel = currentLevel;
                this.param = sessionParam;
            }
            else
            {
                this.param = new OldCodeSupportSessionParam()
                {
                    gameMode = gameMode,
                    sessionTime = sessionTime,
                    currentLevel = currentLevel,
                    extraMeta = param?.ToDictionary()
                };
            }
        }

        public override string Event => "f_sdk_session_data";

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