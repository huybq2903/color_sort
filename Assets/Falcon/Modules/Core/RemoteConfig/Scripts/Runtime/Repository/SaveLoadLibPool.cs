/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.SaveLoad.Runtime;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    public class SaveLoadLibPool : IDataPool, ITerminal, ISingletonServiceReady
    {
        private const String SAVE_LOAD_LIB_KEY= "F_BIGDATA_POOL";
        private readonly ConcurrentDictionary<string, string> _cache;
        private readonly MyLock _lock = new();
        public SaveLoadLibPool()
        {
            try
            {
                _cache = new ConcurrentDictionary<string, string>(SaveLoadHandler.Load(SAVE_LOAD_LIB_KEY,
                    new Dictionary<string, string>()));
            }
            catch (Exception e)
            {
                BaseSystemLogger.Instance.Error("Failed to load data", e);
                _cache = new ConcurrentDictionary<string, string>();
            }
        }

        public T GetOrDefault<T>(string key, T defaultValue)
        {
            if (!_cache.TryGetValue(key, out var valStr)) return defaultValue;
            try
            {
                return valStr.JsonToObjStrict<T>();
            }
            catch (Exception e)
            {
                BaseSystemLogger.Instance.Warning(e);
                return defaultValue;
            }
        }

        public T GetOrSet<T>(string key, T valueIfNotExist)
        {
            var result = valueIfNotExist;
            _cache.Compute(key, valStr =>
            {
                if (valStr == null) return valueIfNotExist.ToJsonStrict();
                try
                {
                    result = valStr.JsonToObjStrict<T>();
                    return valStr;
                }
                catch (Exception e)
                {
                    BaseSystemLogger.Instance.Warning(e);
                    return valueIfNotExist.ToJsonStrict();
                }
            });
            return result;
        }

        public bool HasKey(string key)
        {
            return _cache.ContainsKey(key);
        }

        public T Compute<T>(string key, Func<T, T> function) where T : class
        {
            T result = null;
            _cache.Compute(key, valStr =>
            {
                if (valStr != null)
                    try
                    {
                        result = valStr.JsonToObjStrict<T>();
                    }
                    catch (Exception e)
                    {
                        BaseSystemLogger.Instance.Warning(e);
                    }

                if (function != null) result = function(result);

                return result?.ToJsonStrict();
            });
            return result;
        }

        public T? Compute<T>(string key, Func<T?, T?> function) where T : struct
        {
            T? result = null;
            _cache.Compute(key, valStr =>
            {
                if (valStr != null)
                    try
                    {
                        result = valStr.JsonToObjStrict<T>();
                    }
                    catch (Exception e)
                    {
                        BaseSystemLogger.Instance.Warning(e);
                    }

                if (function != null) result = function(result);

                return result?.ToJsonStrict();
            });
            return result;
        }

        public void Save<T>(string key, T value)
        {
            var json = value.ToJsonStrict();
            _cache[key] = json;
        }

        public void Delete(string key)
        {
            _cache.Remove(key, out _);
        }

#if UNITY_EDITOR
        public void Clear()
        {
            SaveLoadHandler.DeleteKey(SAVE_LOAD_LIB_KEY);
            _cache.Clear();
        }
#endif
        
        public void OnSingletonServiceReady()
        {
            new RepeatAction(() => TrySyncFile(), TimeSpan.FromMinutes(5)).Schedule();
        }
        
        public void OnPostStop()
        {
            if (TrySyncFile()) BaseSystemLogger.Instance.Info("SaveLoadLibPool: Save Data finished");
        }

        public bool TrySync()
        {
            return TrySyncFile();
        }

        public bool TrySyncFile()
        {
            if (!_lock.TryLock(out var key)) return false;
            using (key)
            {
                SaveLoadHandler.Save(SAVE_LOAD_LIB_KEY,new Dictionary<string, string>(_cache));
            }

            return true;
        }
    }
    
    public class ReserveDataPoolDisabler : ISingletonShellSourceDisabler
    {
        public int Priority => 0;

        public IEnumerable<Type> DisablingSources
        {
            get
            {
                yield return typeof(FDataPool);
            }
        }
    }
}