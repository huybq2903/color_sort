/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.TryGetValue(key, out var val)) return val;
            val = value;
            dict.Add(key, val);

            return val;
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> value)
        {
            if (dict.TryGetValue(key, out var val)) return val;
            val = value.Invoke();
            dict.Add(key, val);

            return val;
        }


        public static TDict AddAll<TKey, TValue, TDict>(this TDict dict, Dictionary<TKey, TValue> dictToAdd)
            where TDict : IDictionary<TKey, TValue>
        {
            foreach (var (key, value) in dictToAdd) dict[key] = value;
            return dict;
        }
        
        public static Dictionary<TK, TV> Put<TK, TV>(this Dictionary<TK, TV> dict, TK key, TV value)
        {
            dict[key] = value;
            return dict;
        }

        public static Dictionary<TK, TV> PutIfNotNull<TK, TV>(this Dictionary<TK, TV> dict, TK key, TV value)
        {
            if (!Equals(value, null)) dict[key] = value;
            return dict;
        }

        public static Dictionary<TK, TV> PutIfAbsent<TK, TV>(this Dictionary<TK, TV> dict, TK key, TV value)
        {
            dict.TryAdd(key, value);
            return dict;
        }

        public static Dictionary<TK, TV> PutIfAbsentAndNotNull<TK, TV>(this Dictionary<TK, TV> dict, TK key, TV value)
        {
            if (!dict.ContainsKey(key) && !Equals(value, null)) dict[key] = value;
            return dict;
        }
        
        public static Dictionary<TK, TV> ReplaceKey<TK, TV>(this Dictionary<TK, TV> dict, TK oldKey, TK newKey)
        {
            if(dict.Remove(oldKey, out var val)) dict[newKey] = val;
            return dict;
        }
        
        public static Dictionary<TK, TV> RemoveKey<TK, TV>(this Dictionary<TK, TV> dict, TK key)
        {
            dict.Remove(key);
            return dict;
        }
    }
}