/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class FLevelLog : AFalconLog
    {
        public ALevelParamV2 param;
        public int failCount;
        public int playCount;

        /// <summary>
        /// Số trận thắng liên tiếp tính đến log này (xuyên level + xuyên session, SDK tự tính —
        /// Fail reset, Skip/drop trong suốt).
        /// <br/>Đây là chuỗi PHÂN TÍCH chuẩn fleet: một định nghĩa cho mọi game, server replay
        /// Pass/Fail là kiểm lại được. Nó KHÔNG phải chuỗi TÍNH NĂNG của game (kiểu
        /// first-attempt-only, drop cũng cắt, có milestone...) — thứ đó là state của game, báo
        /// bằng <c>Label.User("streak_status", n)</c> + funnel milestone, đừng nhét vào đây:
        /// mỗi game một định nghĩa là cột này mất so-chéo-game và mất luôn tính tự-kiểm.
        /// </summary>
        public int winStreak;

        /// <summary>Như <see cref="winStreak"/> cho chuỗi thua — Pass reset, Skip/drop trong suốt.</summary>
        public int loseStreak;

        /// <summary>
        /// Nhãn của MÀN này (§D11) — SDK-core tự đóng dấu từ kho nhãn vật, game khai qua
        /// <c>FalconBigDataController.Label.Level(...)</c> chứ không set ở đây.
        /// <br/>Object PHẲNG, giữ nguyên kiểu JSON: <c>{"cluster":"hard_1","tier":3}</c>.
        /// Vắng mặt khi màn chưa có nhãn; gỡ nhãn = thôi kèm key (không có "bản tin gỡ").
        /// Trần 256B — nó đi kèm MỌI level event, stream dày nhất hệ.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> levelLabels;

        [Preserve]
        public FLevelLog()
        {
        }

        public FLevelLog(ALevelParamV2 param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public FLevelLog(
            int currentLevel, string difficulty, LevelStatus status, TimeSpan duration,
            int? wave = null, FParam param = null
        )
        {
            if (param is LevelParam levelParam)
            {
                levelParam.currentLevel = currentLevel;
                levelParam.difficulty = difficulty;
                levelParam.status = status;
                levelParam.duration = duration;
                levelParam.wave = wave;
                this.param = levelParam;
            }
            else
            {
                this.param = new OldCodeSupportLevelParam
                {
                    currentLevel = currentLevel,
                    difficulty = difficulty,
                    status = status,
                    duration = duration,
                    wave = wave,
                    extraMeta = param?.ToDictionary()
                };
            }

            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => "f_sdk_level_data";

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