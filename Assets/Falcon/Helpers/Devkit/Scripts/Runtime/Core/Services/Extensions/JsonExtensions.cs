/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class JsonExtensions
    {
        private static readonly JsonSerializerSettings NormSettings = new()
        {
            DateFormatString = "yyyy-MM-dd HH:mm:sszzz"
        };
        
        private static readonly JsonSerializerSettings StrictSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            DateFormatString = "yyyy-MM-dd HH:mm:sszzz"
        };

        public static T JsonToObj<T>(this string jsonStr)
        {
            return JsonConvert.DeserializeObject<T>(jsonStr, NormSettings);
        }
        
        public static object JsonToObj(this string jsonStr, Type type)
        {
            return JsonConvert.DeserializeObject(jsonStr, type, NormSettings);
        }

        public static T JsonToObjStrict<T>(this string jsonStr)
        {
            return JsonConvert.DeserializeObject<T>(jsonStr, StrictSettings);
        }

        public static object JsonToObjStrict(this string jsonStr, Type type)
        {
            return JsonConvert.DeserializeObject(jsonStr, type, StrictSettings);
        }

        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj, NormSettings);
        }

        public static string ToJsonStrict(this object obj)
        {
            return JsonConvert.SerializeObject(obj, StrictSettings);
        }
    }
}