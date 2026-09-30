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
    /// Custom JSON converter cho mảng ABaseElementPackConfig[].
    /// Sử dụng khi deserialize cấu hình các pack với kiểu cụ thể dựa trên idGroup.
    /// </summary>
    public class ElementPackArrayConverter : JsonConverter<ABaseElementPackConfig[]>
    {
        private readonly string _idGroup;

        /// <summary>
        /// Tạo một converter với idGroup cụ thể (được dùng để lookup kiểu dữ liệu).
        /// </summary>
        /// <param name="idGroup">Khóa định danh nhóm pack.</param>
        public ElementPackArrayConverter(string idGroup)
        {
            _idGroup = idGroup;
        }
        
        /// <summary>
        /// Serialize mảng ABaseElementPackConfig[] như bình thường.
        /// </summary>
        public override void WriteJson(JsonWriter writer, ABaseElementPackConfig[] value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        /// <summary>
        /// Deserialize JSON thành mảng ABaseElementPackConfig[], sử dụng kiểu cụ thể theo idGroup.
        /// </summary>
        public override ABaseElementPackConfig[] ReadJson(JsonReader reader, Type objectType, ABaseElementPackConfig[] existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);
            var result = new List<ABaseElementPackConfig>();

            foreach (var item in array)
            {
                var type = PacksManager.GetConfigType(_idGroup);
                if (type == null)
                    throw new JsonSerializationException($"Unknown element pack type for idGroup: {_idGroup}");

                var instance = (ABaseElementPackConfig)Activator.CreateInstance(type);
                serializer.Populate(item.CreateReader(), instance);
                result.Add(instance);
            }

            return result.ToArray();
        }
    }
}