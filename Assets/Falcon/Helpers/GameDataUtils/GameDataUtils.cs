/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-25
*/

using System;
using System.Reflection;
using UnityEngine;

namespace Falcon.Helpers.GameDataUtils
{
    public static class GameDataUtils
    {
        public static void PrintFieldValues(object obj)
        {
            Type type = obj.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name;
                object fieldValue = field.GetValue(obj);
                Debug.Log($"{fieldName} = {fieldValue}");
            }
        }

        public static object GetFieldValueByName(object obj, string fieldName)
        {
            Type type = obj.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        public static void SetFieldValueByName(object obj, string fieldName, object value)
        {
            Type type = obj.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

            if (field == null)
            {
                Debug.LogWarning($"Field '{fieldName}' not found in type '{type.Name}'.");
                return;
            }

            try
            {
                object convertedValue = Convert.ChangeType(value, field.FieldType);
                field.SetValue(obj, convertedValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set field '{fieldName}' with value '{value}': {ex.Message}");
            }
        }
    }
}
