/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class FKeyService
    {
        public static Dictionary<string, object> Encode(object obj)
        {
            var result = new Dictionary<string, object>();
            var defaultAttribute = new FKeyAttribute();

            foreach (var fieldInfo in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var attribute = fieldInfo.GetCustomAttribute<FKeyAttribute>() ?? defaultAttribute;
                if (attribute.Ignore) continue;
                
                var propertyName = attribute.Name ?? fieldInfo.Name;

                var value = fieldInfo.GetValue(obj);
                if (value != null || !attribute.RemoveIfNull) result[propertyName] = value;
            }

            return result;
        }
    }
}