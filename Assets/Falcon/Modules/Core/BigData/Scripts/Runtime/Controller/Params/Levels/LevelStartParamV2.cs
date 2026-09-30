/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public class LevelStartParamV2 : ALevelParamV2
    {
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, int> preBoostersUsed;

        public override void CorrectValues()
        {
            if (preBoostersUsed == null) return;
            foreach (var (key, value) in preBoostersUsed.Where(e => e.Value < 0).ToList())
            {
                Debug.LogError(
                    $"Dwh Log invalid field: the value of pre-booster {key} of {GetType().Name} must be non-negative, input value '{value}'");
                preBoostersUsed.Remove(key);
            }
        }
        
        public override LevelStatus Status => LevelStatus.Start;
    }
}