/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class LevelParam : ALevelParamV2
    {
        public LevelStatus status;
        public TimeSpan duration;

        [FKey(RemoveIfNull = true)] public int? movesUsed;
        [FKey(RemoveIfNull = true)] public int? score;
        [FKey(RemoveIfNull = true)] public int? wave;
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, int> boostersUsed;

        /// <summary>
        /// Người chơi thực hiện được bao nhiêu % của level rồi (từ 0 -> 100%)
        /// Mặc định Start -> 0%, Pass -> 100% 
        /// </summary>
        public int levelProgress;

        public override void CorrectValues()
        {
            currentLevel = CheckNumberNonNegative(currentLevel, "currentLevel");
            difficulty = CheckNonBlank(difficulty, "difficulty");
            duration = CheckTimeSpanNonNegative(duration, "duration");
            wave = CheckNumberNonNegative(wave, "wave");
            score = CheckNumberNonNegative(score, "score");
            movesUsed = CheckNumberNonNegative(movesUsed, "movesUsed");

            if (status.IsPass())
            {
                levelProgress = 100;
            } else if (status.IsStart())
            {
                levelProgress = 0;
            }
            else
            {
                levelProgress = CheckNumberNonNegative(levelProgress, "levelPercentage");
            }

            if (boostersUsed == null) return;
            foreach (var (key, value) in boostersUsed.AsEnumerable().Where(e => e.Value < 0))
            {
                Debug.LogError(
                    $"Dwh Log invalid field: the value of booster {key} of {GetType().Name} must be non-negative, input value '{value}'");
                boostersUsed.Remove(key);
            }
        }

        public override Dictionary<string, object> ToDictionary()
        {
            return base.ToDictionary().Put(nameof(duration), (long)duration.TotalSeconds);
        }
        
        public override LevelStatus Status => status;
    }

    [Serializable]
    /// <summary>
    /// Đường vào cho code cũ gọi <c>FLevelLog</c> bằng đối số rời — không thêm field nào, chỉ tồn
    /// tại để phân biệt "param do game dựng" với "param SDK dựng hộ từ đối số rời".
    /// <br/><c>extraMeta</c> nằm ở <see cref="ALevelParamV2"/> cho cả họ; khai lại ở đây thì
    /// <c>FKeyService</c> (quét field public, kể cả field bị che) thấy HAI field cùng tên và ghi đè
    /// nhau theo thứ tự <c>GetFields</c> — cùng lỗi đã dính với <c>purchaseAttemptId</c>.
    /// </summary>
    public class OldCodeSupportLevelParam : LevelParam
    {
    }
}