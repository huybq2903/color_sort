/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    public class FConfigInstanceService : IConfigUpdatedReact, IInit
    {
        private readonly ConcurrentDictionary<Type, IFalconConfig> _cache = new();

        private readonly LazyVal<string> _configJsonStringBase;

        public FConfigInstanceService(IFConfigRepository configRepository)
        {
            _configJsonStringBase = new(() => configRepository.Configs.ToJson());
        }
        
        public T GetInstance<T>() where T : IFalconConfig, new()
        {
            return (T)_cache.Compute(typeof(T), config => config ?? CreateInstance<T>());
        }

        private T CreateInstance<T>() where T : IFalconConfig, new()
        {
            try
            {
                return _configJsonStringBase.Value.JsonToObj<T>();
            }
            catch (Exception e)
            {
                BaseSystemLogger.Instance.Error(e);
                return new T();
            }
        }

        public void OnConfigUpdated()
        {
            _cache.Clear();
            _configJsonStringBase.Reset();
        }

        public Task Init(CancellationToken cancellationToken = default)
        {
            _configJsonStringBase.Reset();
            return Task.CompletedTask;
        }
    }
}