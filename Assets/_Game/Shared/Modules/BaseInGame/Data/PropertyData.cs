using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Falcon.Helpers.FReflection;
using Newtonsoft.Json;

namespace Falcon.Shared.BaseInGame
{
    // --- MODULE LEVEL (PROPERTY DATA) ---

    /// <summary>
    /// Base class cho các Module dữ liệu trong Level (VD: SnakesProperty, SettingsProperty).
    /// </summary>
    public abstract class PropertyData : IFReflection
    {
        public string type { get; }
        
        public T As<T>() where T : PropertyData
        {
            return this as T;
        }

        protected PropertyData()
        {
            type = GetType().GetCustomAttribute<PropertyDataTypeAttribute>()?.Type;
        }
    }

    /// <summary>
    /// Helper class cho các Module chứa một danh sách Entity (VD: Danh sách Rắn, Danh sách hành khách).
    /// </summary>
    /// <typeparam name="T">Loại Entity con (phải kế thừa EntityData)</typeparam>
    public abstract class MultipleEntityDataProperty<T> : PropertyData where T : EntityData
    {
        // Tên field này sẽ cố định trong JSON là "list".
        [JsonProperty("list")]
        public List<T> list = new();
        
        public Dictionary<string, T> BuildDictionary()
        {
            var dict = new Dictionary<string, T>();
            foreach (var entity in list)
            {
                dict[entity.id] = entity;
            }
            return dict;
        }
    }

    /// <summary>
    /// Helper class cho các Module chỉ chứa 1 object dữ liệu (VD: Boss, LevelSettings)
    /// Nếu bắt buộc phải tạo EntityData để truyền vào behaviour thì dùng
    /// Không thì cứ kế thừa trực tiếp từ PropertyData
    /// </summary>
    public abstract class SingleEntityDataProperty<T> : PropertyData where T : EntityData
    {
        [JsonProperty("data")]
        public T data;
    }

    // --- ATTRIBUTES & REGISTRY ---

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PropertyDataTypeAttribute : Attribute
    {
        public string Type { get; }
        public PropertyDataTypeAttribute(string type)
        {
            Type = type;
        }
    }

    public static class PropertyDataTypeRegistry
    {
        private static Dictionary<string, Type> _map;
 
        public static Type Get(string typeName)
        {
            if (_map == null) Initialize();
            return _map.GetValueOrDefault(typeName);
        }

        private static void Initialize()
        {
            _map = new Dictionary<string, Type>();
            var entityTypes = FReflection.Instance.GetTypes()
                .Where(t => typeof(PropertyData).IsAssignableFrom(t) && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<PropertyDataTypeAttribute>() != null);

            foreach (var type in entityTypes)
            {
                var attr = type.GetCustomAttribute<PropertyDataTypeAttribute>();
                if (attr == null)
                    continue;
                _map[attr.Type] = type;
            }
        }
    }
}