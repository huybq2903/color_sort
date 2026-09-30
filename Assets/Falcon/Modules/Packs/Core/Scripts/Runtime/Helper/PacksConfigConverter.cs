/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Custom JSON converter cho lớp PacksConfig.
    /// Hỗ trợ deserialize đúng kiểu phần tử (element) dựa theo idGroup.
    /// </summary>
    public class PacksConfigConverter : JsonConverter<PacksConfig>
    {
        /// <summary>
        /// Ghi đối tượng PacksConfig thành JSON, bao gồm idGroup và danh sách elements.
        /// </summary>
        public override void WriteJson(JsonWriter writer, PacksConfig value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("idGroup");
            writer.WriteValue(value.idGroup);

            writer.WritePropertyName("elements");
            serializer.Serialize(writer, value.elements);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Đọc đối tượng JSON thành PacksConfig, xác định kiểu phần tử bằng cách tra cứu từ idGroup.
        /// </summary>
        public override PacksConfig ReadJson(JsonReader reader, Type objectType, PacksConfig existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            // Lấy idGroup
            var idGroup = jo["idGroup"]?.ToString();
            if (string.IsNullOrEmpty(idGroup))
                throw new JsonSerializationException("Missing or invalid idGroup in config");

            // Tạo đối tượng config
            var config = new PacksConfig
            {
                idGroup = idGroup
            };

            // Lấy type cụ thể của element từ idGroup
            var elementType = PacksManager.GetConfigType(idGroup);
            if (elementType == null)
                throw new JsonSerializationException($"Unknown or unregistered element type for idGroup: {idGroup}");

            // Deserialize từng phần tử trong mảng elements
            var elementsToken = jo["elements"] as JArray;
            if (elementsToken == null)
                throw new JsonSerializationException("Missing elements array in config");

            var elementList = new List<ABaseElementPackConfig>();
            foreach (var item in elementsToken)
            {
                var instance = (ABaseElementPackConfig)Activator.CreateInstance(elementType);
                serializer.Populate(item.CreateReader(), instance);
                elementList.Add(instance);
            }

            config.elements = elementList.ToArray();
            return config;
        }
    }
}
