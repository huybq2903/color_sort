using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Falcon.Shared.BaseInGame
{
    public class LevelData
    {
        /// <summary>
        /// Danh sách các Module dữ liệu trong Level (rắn, hành khách, bus, blocks, ...)
        /// </summary>
        [JsonConverter(typeof(PropertyDataConverter))]
        public Dictionary<string, PropertyData> properties = new();
        
        public virtual T GetProperty<T>() where T : PropertyData
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<PropertyDataTypeAttribute>();
            if (attr == null) return null;
            return properties.TryGetValue(attr.Type, out var entity) ? entity.As<T>() : null;
        }
        
        public virtual void SetProperty(PropertyData property)
        {
            properties[property.type] = property;
        }

        public virtual LevelData Clone() => FromJson(ToJson());

        private static readonly JsonSerializerSettings PublicFieldsSettings = new()
        {
            ContractResolver = new PublicFieldsResolver()
        };

        public virtual string ToJson() => JsonConvert.SerializeObject(this, PublicFieldsSettings);
        
        public static LevelData FromJson(string json) => JsonConvert.DeserializeObject<LevelData>(json, PublicFieldsSettings);
    }
    
    public class PublicFieldsResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            return objectType
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                .Cast<MemberInfo>()
                .ToList();
        }
    }
}
