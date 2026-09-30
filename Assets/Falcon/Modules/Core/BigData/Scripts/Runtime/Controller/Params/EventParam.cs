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
    public class EventParam : FParam
    {
        [NotNull] public string eventName = UNKNOWN;

        [NotNull] public string eventWhere = UNKNOWN;
        [NotNull] public string eventWhen = UNKNOWN;
        [FKey(RemoveIfNull = true)] public int? currentLevel;

        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> param;

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(param));
            return dictionary.PutIfAbsentAndNotNull("paramsStr", param?.ToJson());
        }

        public override void CorrectValues()
        {
            eventName = CheckNonBlank(eventName, nameof(eventName));
            eventWhere = CheckNonBlank(eventWhere, nameof(eventWhere));
            eventWhen = CheckNonBlank(eventWhen, nameof(eventWhen));
            currentLevel = CheckNumberNonNegative(currentLevel, nameof(currentLevel));
        }
    }

    [Serializable]
    public class OldCodeSupportEventParam : EventParam
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