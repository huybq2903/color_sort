/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-27
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Falcon.Shared.BaseInGame
{
    public class PropertyDataConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dict = (Dictionary<string, PropertyData>)value ?? new Dictionary<string, PropertyData>();
            var jo = new JObject();

            foreach (var (key, data) in dict)
            {
                jo[key] = JObject.FromObject(data, serializer);
            }

            jo.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new Dictionary<string, PropertyData>();
            var jo = JObject.Load(reader);

            foreach (var prop in jo.Properties())
            {
                var typeName = prop.Name;

                var targetType = PropertyDataTypeRegistry.Get(typeName);
                if (targetType == null)
                    continue;

                var propertyData = (PropertyData)prop.Value.ToObject(targetType, serializer);
                result[typeName] = propertyData;
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
            return typeof(PropertyData).IsAssignableFrom(valueType);
        }
    }
}