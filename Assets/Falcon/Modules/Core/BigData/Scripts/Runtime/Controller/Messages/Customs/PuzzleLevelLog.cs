/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-26
 */
using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class PuzzleLevelLog : FLevelLog
    {
        [Preserve]
        public PuzzleLevelLog()
        {
        }

        public PuzzleLevelLog(LevelParam param) : base(param)
        {
        }


        public PuzzleLevelLog(
            int currentLevel, string currentLevelId, string difficulty, 
            LevelStatus status, TimeSpan duration, Dictionary<string, int> boostersUsed = null, 
            int? score = null, int? elo = null, int? movesUsed = null, FParam param = null
        )
        {
            // elo giữ trong chữ ký để code cũ khỏi phải sửa, nhưng KHÔNG lên level event nữa: nó
            // tả người chơi chứ không tả lượt (§H6). Đây là constructor nên không bắn được bản tin
            // nhãn từ đây — chỉ đường cho dev.
            if (elo.HasValue)
                AnalyticLogger.Instance.Warning(
                    "PuzzleLevelLog(elo:) bỏ qua — elo nay là nhãn NGƯỜI CHƠI. Gọi một lần " +
                    "FalconBigDataController.Level.SetUserElo(giá trị) mỗi khi elo đổi.");

            if (param is LevelParam @params)
            {
                @params.currentLevel = currentLevel;
                @params.difficulty = difficulty;
                @params.status = status;
                
                @params.duration = duration;
                @params.currentLevelId = currentLevelId;
                @params.wave = null;
                @params.boostersUsed = boostersUsed;
                @params.score = score;
                @params.movesUsed = movesUsed;
                this.param = @params;
            }
            else
            {
                this.param = new OldCodeSupportLevelParam
                {
                    currentLevel = currentLevel,
                    difficulty = difficulty,
                    status = status,
                    duration = duration,
                    currentLevelId = currentLevelId,
                    wave = null,
                    boostersUsed = boostersUsed,
                    score = score,
                    movesUsed = movesUsed,
                    extraMeta = param?.ToDictionary()
                };
            }
        }
    }
}