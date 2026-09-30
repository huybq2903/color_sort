/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-10
 */

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Custom JSON converter cho đối tượng PacksInfo.
    /// Hỗ trợ deserialize đúng kiểu dữ liệu theo idGroup (dùng để xác định kiểu config và userdata).
    /// </summary>
    public class PacksInfoConverter : JsonConverter<PacksInfo>
    {
        /// <summary>
        /// Ghi đối tượng PacksInfo thành JSON (config + userData).
        /// </summary>
        public override void WriteJson(JsonWriter writer, PacksInfo value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("config");
            serializer.Serialize(writer, value.config);

            writer.WritePropertyName("userData");
            serializer.Serialize(writer, value.userData);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Đọc đối tượng JSON thành PacksInfo, sử dụng idGroup để biết kiểu dữ liệu cụ thể.
        /// </summary>
        public override PacksInfo ReadJson(JsonReader reader, Type objectType, PacksInfo existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            // Lấy token
            var configToken = jo["config"];
            var userDataToken = jo["userData"];

            if (configToken == null)
            {
                Debug.LogWarning("PacksInfoConverter: Missing configToken");
                return null;
            }

            if (userDataToken == null)
            {
                Debug.LogWarning("PacksInfoConverter: Missing userDataToken");
                return null;
            }

            // Lấy idGroup từ config
            var idGroup = configToken["idGroup"]?.ToString();
            if (string.IsNullOrEmpty(idGroup))
            {
                Debug.LogWarning("PacksInfoConverter: Missing or invalid idGroup in config");
                return null;
            }

            // Lấy kiểu thực thi tương ứng từ register
            var userDataType = PacksManager.GetUserDataType(idGroup);
            
            if (userDataType == null)
            {
                Debug.LogWarning($"PacksInfoConverter: Unknown or unregistered userData type for idGroup: {idGroup}");
                return null;
            }

            // Tạo instance
            var configInstance = new PacksConfig();
            var userDataInstance = (IPackUserData)Activator.CreateInstance(userDataType);

            // Deserialize config với converter riêng cho packs[]
            var configJson = configToken.ToString();
            var customSettings = new JsonSerializerSettings
            {
                Converters = { new ElementPackArrayConverter(idGroup) }
            };
            JsonConvert.PopulateObject(configJson, configInstance, customSettings);

            // Deserialize userData như bình thường
            serializer.Populate(userDataToken.CreateReader(), userDataInstance);

            return new PacksInfo
            {
                config = configInstance,
                userData = userDataInstance
            };
        }
    }
}