// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-04-21

using System;
using System.Collections.Generic;
using System.Linq;

namespace Falcon.Shared.Common
{
    public static class Center
    {
        private static readonly Dictionary<Type, object> _dic = new();
        
        public static T Get<T>() where T : class
        {
            var t = _dic.ContainsKey(typeof(T)) ? _dic[typeof(T)] as T : default;
            return t;
        }

        public static object Get(Type type) => _dic.GetValueOrDefault(type);

        public static T GetOrCreate<T>() where T : class, new()
        {
            T t;
            if (_dic.ContainsKey(typeof(T)))
                t = _dic[typeof(T)] as T;
            else
            {
                t = new T();
                _dic.Add(typeof(T), t);
                if (t is IInitialize init)
                    init.OnInitialize();
            }

            return t;
        }
        
        public static object GetOrCreate(Type type)
        {
            object t;
            if (_dic.TryGetValue(type, out var obj))
                t = obj;
            else
            {
                t = Activator.CreateInstance(type);
                _dic.Add(type, t);
                if (t is IInitialize init)
                    init.OnInitialize();
            }

            return t;
        }
        
        public static IEnumerable<T> All<T>() where T : class
        {
            return from o in _dic where o.Value is T select o.Value as T;
        }

        public static void Remove<T>() where T : class
        {
            if (_dic.ContainsKey(typeof(T)))
                _dic.Remove(typeof(T));
        }
        
        public static void Remove(Type type)
        {
            _dic.Remove(type);
        }
    }
    
    public interface IInitialize
    {
        void OnInitialize();
    }
}