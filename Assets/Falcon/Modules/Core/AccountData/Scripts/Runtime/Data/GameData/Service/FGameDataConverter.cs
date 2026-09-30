/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-15


using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Falcon.Modules.Core.AccountData 
{
    public class FGameDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(FGameData).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            // Load object từ JSON
            JObject jo = JObject.Load(reader);

            // Lấy tên type
            string typeName = jo["__type"]?.ToString();
            if (string.IsNullOrEmpty(typeName))
                return null;

            // Tìm kiểu thực sự từ registry
            Type targetType = FGameDataRegistry.Instance.Get(typeName);
            if (targetType == null)
                return null;

            // Deserialize thành đúng subclass của FGameData
            return jo.ToObject(targetType, serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Mặc định serialize như bình thường
            serializer.Serialize(writer, value);
        }
    }
}