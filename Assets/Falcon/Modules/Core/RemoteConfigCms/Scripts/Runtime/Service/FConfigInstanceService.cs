/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    public class FConfigInstanceService : IConfigUpdatedReact
    {
        private readonly ConcurrentDictionary<Type, IFalconConfigCms> _cache = new();

        private readonly LazyVal<string> _configJsonStringBase;

        public FConfigInstanceService(IFConfigRepository configRepository)
        {
            _configJsonStringBase = new(() => configRepository.Configs.ToJson());
        }

        public T GetInstance<T>() where T : IFalconConfigCms, new()
        {
            return (T)_cache.Compute(typeof(T), config => config ?? CreateInstance<T>());
        }

        private T CreateInstance<T>() where T : IFalconConfigCms, new()
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