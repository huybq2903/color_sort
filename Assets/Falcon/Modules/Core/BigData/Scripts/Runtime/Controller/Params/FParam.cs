/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */
using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public abstract class FParam
    {
        public const string UNKNOWN = "Unknown";
        public virtual Dictionary<string, object> ToDictionary()
        {
            return FKeyService.Encode(this);
        }

        public virtual void CorrectValues()
        {
        }

        #region Check Params

        protected void CheckNonNull(object obj, string fieldName)
        {
            if (obj == null)
                Debug.LogError(
                    $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-null");
        }

        protected string CheckNonBlank(string str, string fieldName)
        {
            if (!string.IsNullOrWhiteSpace(str)) return str;
            Debug.LogError(
                $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-blank");
            return UNKNOWN;
        }

        protected TimeSpan CheckTimeSpanNonNegative(TimeSpan i, string fieldName)
        {
            if (i.TotalMilliseconds >= 0) return i;
            Debug.LogError(
                $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
            return TimeSpan.Zero;
        }

        protected int CheckNumberNonNegative(int i, string fieldName)
        {
            if (i >= 0) return i;
            Debug.LogError(
                $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
            return 0;
        }

        protected long CheckNumberNonNegative(long i, string fieldName)
        {
            if (i >= 0) return i;
            Debug.LogError(
                $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
            return 0;
        }

        protected float CheckNumberNonNegative(float i, string fieldName)
        {
            if (i >= 0) return i;
            Debug.LogError(
                $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
            return 0;
        }

        protected double CheckNumberNonNegative(double i, string fieldName)
        {
            if (i >= 0) return i;
            Debug.LogError(
                $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
            return 0;
        }

        protected decimal CheckNumberNonNegative(decimal i, string fieldName)
        {
            if (i >= 0) return i;
            Debug.LogError(
                $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
            return 0;
        }

        protected int? CheckNumberNonNegative(int? i, string fieldName)
        {
            switch (i)
            {
                case null:
                    return null;
                case >= 0:
                    return i;
                default:
                    Debug.LogError(
                        $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
                    return 0;
            }
        }

        protected long? CheckNumberNonNegative(long? i, string fieldName)
        {
            switch (i)
            {
                case null:
                    return null;
                case >= 0:
                    return i;
                default:
                    Debug.LogError(
                        $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
                    return 0;
            }
        }

        protected float? CheckNumberNonNegative(float? i, string fieldName)
        {
            switch (i)
            {
                case null:
                    return null;
                case >= 0:
                    return i;
                default:
                    Debug.LogError(
                        $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
                    return 0;
            }
        }

        protected double? CheckNumberNonNegative(double? i, string fieldName)
        {
            switch (i)
            {
                case null:
                    return null;
                case >= 0:
                    return i;
                default:
                    Debug.LogError(
                        $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
                    return 0;
            }
        }

        protected decimal? CheckNumberNonNegative(decimal? i, string fieldName)
        {
            switch (i)
            {
                case null:
                    return null;
                case >= 0:
                    return i;
                default:
                    Debug.LogError(
                        $"Dwh Log invalid field: the value of field {fieldName} of {GetType().Name} must be non-negative, input value '{i}'");
                    return 0;
            }
        }

        #endregion
    }
}