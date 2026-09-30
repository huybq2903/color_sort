/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */
using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class SessionParam : FParam
    {
        [NotNull] public string gameMode = UNKNOWN;
        public TimeSpan sessionTime;
        [FKey(RemoveIfNull = true)] public int? currentLevel;

        public override void CorrectValues()
        {
            gameMode = CheckNonBlank(gameMode, nameof(gameMode));
            currentLevel = CheckNumberNonNegative(currentLevel, nameof(currentLevel));
            sessionTime = CheckTimeSpanNonNegative(sessionTime, nameof(sessionTime));
        }
        
        public override Dictionary<string, object> ToDictionary()
        {
            return base.ToDictionary().Put(nameof(sessionTime), (long)sessionTime.TotalSeconds);
        }
    }

    [Serializable]
    public class OldCodeSupportSessionParam : SessionParam
    {
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> extraMeta = null;

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }
    }
}