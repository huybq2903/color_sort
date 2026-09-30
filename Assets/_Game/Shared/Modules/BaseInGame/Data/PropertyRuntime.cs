using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Falcon.Helpers.FReflection;
using Newtonsoft.Json;

namespace Falcon.Shared.BaseInGame
{
    // --- MODULE LEVEL (PROPERTY RUNTIME) ---

    /// <summary>
    /// Base class for runtime property modules in a level.
    /// </summary>
    public abstract class PropertyRuntime : IFReflection
    {
        public string type { get; }

        public T As<T>() where T : PropertyRuntime
        {
            return this as T;
        }

        protected PropertyRuntime()
        {
            type = GetType().GetCustomAttribute<PropertyRuntimeTypeAttribute>()?.Type;
        }
    }

    /// <summary>
    /// Helper for property modules containing a list of runtime entities.
    /// </summary>
    public abstract class MultipleEntityRuntimeProperty<T> : PropertyRuntime where T : EntityRuntime
    {
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
    /// Helper for property modules containing a single runtime entity.
    /// </summary>
    public abstract class SingleEntityRuntimeProperty<T> : PropertyRuntime where T : EntityRuntime
    {
        [JsonProperty("data")]
        public T data;
    }

    // --- ATTRIBUTES & REGISTRY ---

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PropertyRuntimeTypeAttribute : Attribute
    {
        public string Type { get; }

        public PropertyRuntimeTypeAttribute(string type)
        {
            Type = type;
        }
    }

    public static class PropertyRuntimeTypeRegistry
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
                .Where(t => typeof(PropertyRuntime).IsAssignableFrom(t) && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<PropertyRuntimeTypeAttribute>() != null);

            foreach (var type in entityTypes)
            {
                var attr = type.GetCustomAttribute<PropertyRuntimeTypeAttribute>();
                if (attr == null)
                    continue;
                _map[attr.Type] = type;
            }
        }
    }
}
