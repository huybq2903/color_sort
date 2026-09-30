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
    public abstract class LevelProgressParamV2 : ALevelParamV2
    {
        [FKey(RemoveIfNull = true)] public int? score;
        [FKey(RemoveIfNull = true)] public int? movesUsed;
        public TimeSpan duration;
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, int> boostersUsed;
        
        /// <summary>
        /// Người chơi thực hiện được bao nhiêu % của level rồi (từ 0 -> 100%)
        /// Mặc định Start -> 0%, Pass -> 100% 
        /// </summary>
        public int levelProgress;
        
        public override void CorrectValues()
        {
            base.CorrectValues();
            duration = CheckTimeSpanNonNegative(duration, nameof(duration));
            score = CheckNumberNonNegative(score, nameof(score));
            movesUsed = CheckNumberNonNegative(movesUsed, nameof(movesUsed));
            levelProgress = CheckNumberNonNegative(levelProgress, nameof(levelProgress));

            if (levelProgress > 100)
            {
                Debug.LogError(
                    $"Dwh Log invalid field: the value of levelProgress must be <=100, input value '{levelProgress}'");
                levelProgress = 100;
            }

            if (boostersUsed == null) return;
            foreach (var (key, value) in boostersUsed.Where(e => e.Value < 0).ToList())
            {
                Debug.LogError(
                    $"Dwh Log invalid field: the value of booster {key} of {GetType().Name} must be non-negative, input value '{value}'");
                boostersUsed.Remove(key);
            }
        }
        
        public override Dictionary<string, object> ToDictionary()
        {
            return base.ToDictionary()
                .Put(nameof(duration), (long)duration.TotalSeconds);
        }
    }

    public class LevelPassParamV2 : LevelProgressParamV2
    {
        public LevelPassParamV2()
        {
            levelProgress = 100;
        }
        public override LevelStatus Status => LevelStatus.Pass;
        
    }
    
    public class LevelFailParamV2 : LevelProgressParamV2
    {
        /// <summary>
        /// Thua VÌ SAO — hợp đồng §D, chỉ có trên Fail.
        /// Theo §H4: không biết thì để null (vắng mặt khỏi payload), KHÔNG điền bừa.
        /// </summary>
        [FKey(RemoveIfNull = true)] public LevelFailReason? failReason;

        public override LevelStatus Status => LevelStatus.Fail;
    }
    
    public class LevelHeartBeatParamV2 : LevelProgressParamV2
    {
        public override LevelStatus Status => LevelStatus.HeartBeat;
    }
}