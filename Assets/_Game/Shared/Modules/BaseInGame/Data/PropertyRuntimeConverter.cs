/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-04
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Falcon.Shared.BaseInGame
{
    public class PropertyRuntimeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dict = (Dictionary<string, PropertyRuntime>)value ?? new Dictionary<string, PropertyRuntime>();
            var jo = new JObject();

            foreach (var (key, data) in dict)
            {
                jo[key] = JObject.FromObject(data, serializer);
            }

            jo.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new Dictionary<string, PropertyRuntime>();
            var jo = JObject.Load(reader);

            foreach (var prop in jo.Properties())
            {
                var typeName = prop.Name;

                var targetType = PropertyRuntimeTypeRegistry.Get(typeName);
                if (targetType == null)
                    continue;

                var propertyRuntime = (PropertyRuntime)prop.Value.ToObject(targetType, serializer);
                result[typeName] = propertyRuntime;
            }

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            if (!objectType.IsGenericType)
                return false;

            var genericTypeDef = objectType.GetGenericTypeDefinition();
            if (genericTypeDef != typeof(Dictionary<,>))
                return false;

            var valueType = objectType.GetGenericArguments()[1];
            return typeof(PropertyRuntime).IsAssignableFrom(valueType);
        }
    }
}
