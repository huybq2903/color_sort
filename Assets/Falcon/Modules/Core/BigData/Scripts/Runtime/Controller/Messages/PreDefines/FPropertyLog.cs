/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using Falcon.Helpers.Devkit;
using UnityEngine;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class FPropertyLog : AFalconLog
    {
        public string pName;
        public string pValue;
        public int priority;

        // ReSharper disable once InconsistentNaming
        [FKey(RemoveIfNull = true)]public int? currentLevel;

        [Preserve]
        public FPropertyLog()
        {
        }

        public FPropertyLog(string pName, string pValue, int priority, int? currentLevel = null)
        {
            LogParams(pName, pValue, priority, currentLevel);
            this.pName = pName;
            this.pValue = pValue;

            this.priority = CheckNumberNonNegative(priority, nameof(priority));
            this.currentLevel = currentLevel;
        }
        
        protected int CheckNumberNonNegative(int i, string fieldName)
        {
            if (i >= 0) return i;
            Debug.LogError(
                $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
            return 0;
        }

        public override string Event => "f_sdk_property_data";
    }
}