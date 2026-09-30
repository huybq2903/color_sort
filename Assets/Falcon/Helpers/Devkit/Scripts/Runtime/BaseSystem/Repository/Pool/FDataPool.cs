/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class FDataPool : IDataPool, ITerminal, ISingletonServiceReady
    {
        private static readonly string kOldFilePath = Path.Combine("Sdk", "Data.txt");
        private static readonly string kDataFilePath = Path.Combine("Sdk", "DataNew.txt");
        private readonly ConcurrentDictionary<string, string> _cache;

        private readonly IFFile _file;
        private readonly MyLock _lock = new();

        public FDataPool(FLocalFileRepository fLocalFileRepository)
        {
            try
            {
                _file = fLocalFileRepository.GetFile(kDataFilePath).AesSimpleEncrypt();
                var oldFile = fLocalFileRepository.GetFile(kOldFilePath);
                if (!_file.Exists() && oldFile.Exists())
                {
                    _file.Save(oldFile.LoadString(), new Base64Encoding());
                    BaseSystemLogger.Instance.Info("Transfer old data to newly encrypted file");
                }

                if (_file.Exists())
                {
                    var fileData = _file.LoadJson<Dictionary<string, string>>(new Base64Encoding()) ??
                                   new Dictionary<string, string>();

                    fileData = fileData
                        .Where(f => f.Value != null)
                        .ToDictionary(x => x.Key, x => x.Value);
                    _cache = new ConcurrentDictionary<string, string>(fileData);
                }
                else
                {
                    _cache = new ConcurrentDictionary<string, string>();
                }
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
            if (_file.Exists()) _file.Delete();
            _cache.Clear();
        }
#endif

        public void OnSingletonServiceReady()
        {
            new RepeatAction(() => TrySyncFile(), TimeSpan.FromMinutes(5)).Schedule();
        }

        public void OnPostStop()
        {
            if (TrySyncFile()) BaseSystemLogger.Instance.Info("FDataPool: Save Data finished");
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
                _file.Save(new Dictionary<string, string>(_cache), new Base64Encoding());
            }

            return true;
        }
    }
}