using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Newtonsoft.Json;
using Falcon.Shared.Common;

namespace Falcon.Shared.BaseInGame
{
    public class LevelRuntime
    {
        public string hash;
        [JsonConverter(typeof(PropertyRuntimeConverter))]
        public Dictionary<string, PropertyRuntime> properties = new();

        public virtual T GetProperty<T>() where T : PropertyRuntime
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<PropertyRuntimeTypeAttribute>();
            if (attr == null) return null;
            return properties.TryGetValue(attr.Type, out var entity) ? entity.As<T>() : null;
        }

        public virtual T GetOrCreateProperty<T>() where T : PropertyRuntime, new()
        {
            var property = GetProperty<T>();
            if (property != null)
                return property;

            property = new T();
            SetProperty(property);
            return property;
        }

        public virtual void SetProperty(PropertyRuntime property)
        {
            properties[property.type] = property;
        }

        public virtual string ToJson() => JsonConvert.SerializeObject(this);

        public static LevelRuntime FromJson(string json) => JsonConvert.DeserializeObject<LevelRuntime>(json);


        public static bool HasLevelRuntime(string hash, out LevelRuntime levelRuntime)
        {
            var json = SaveLoadHandler.Load<string>(GameKeys.LEVEL_RUNTIME);
            levelRuntime = json != null ? FromJson(json) : null;

            if (levelRuntime != null && levelRuntime.properties.Count > 0 && levelRuntime.hash == hash)
            {
                return true;
            }

            levelRuntime = null;
            return false;
        }
    }

    public static class Md5Utils
    {
        static string Normalize(string s)
        {
            if (s == null) return "";
            if (s.Length > 0 && s[0] == '\uFEFF') s = s.Substring(1);
            return s.Replace("\r\n", "\n");
        }

        public static string GetMd5First5Char(string input)
        {
            try
            {
                string canonical = Normalize(input);
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                    return Convert.ToBase64String(hash).Substring(0, 5);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}