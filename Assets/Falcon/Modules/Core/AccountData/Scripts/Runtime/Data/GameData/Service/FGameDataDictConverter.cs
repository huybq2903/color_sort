/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-09

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

namespace Falcon.Modules.Core.AccountData 
{
    public class FGameDataDictConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            typeof(FGameData).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new Dictionary<string, FGameData>();
            var jo = JObject.Load(reader);

            foreach (var prop in jo.Properties())
            {
                var value = prop.Value;
                var typeName = value["__type"]?.ToString();

                if (typeName == null)
                    continue;

                var targetType = FGameDataRegistry.Instance.Get(typeName);
                if (targetType == null)
                    continue;

                var gameData = (FGameData)value.ToObject(targetType, serializer);
                result[prop.Name] = gameData;
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dict = (Dictionary<string, FGameData>)value;
            var jo = new JObject();

            foreach (var kv in dict)
            {
                var data = kv.Value;
                var token = JObject.FromObject(data, serializer);

                var type = data.GetType();
                var attr = type.GetCustomAttribute<FGameDataTypeAttribute>();
                if (attr == null || attr.Skip)
                    continue;
                token["__type"] = attr.TypeName;
                jo[kv.Key] = token;
            }

            jo.WriteTo(writer);
        }
    }
}