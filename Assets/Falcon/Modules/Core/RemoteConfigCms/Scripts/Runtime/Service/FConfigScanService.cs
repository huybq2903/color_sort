/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.FReflection;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    public class FConfigScanService : IConfigUpdatedReact
    {
        private readonly LazyVal<Dictionary<string, object>> _configEntries;

        public FConfigScanService(IFConfigRepository configService)
        {
            _configEntries = new LazyVal<Dictionary<string, object>>(() => LoadConfigEntries(configService));
        }

        public Dictionary<string, object> ConfigEntries => _configEntries.Value;

        private static Dictionary<string, object> LoadConfigEntries(IFConfigRepository configRepository)
        {
            ISet<Type> types = new HashSet<Type>(FReflection.Instance.GetTypes()
                .Where(type => typeof(IFalconConfigCms).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract));

            Dictionary<string, object> result = new();
            foreach (var type in types)
            {
                try
                {
                    var instance = Activator.CreateInstance(type);
                    result.AddAll(instance.ToJson().JsonToObj<Dictionary<string, object>>());
                }
                catch (Exception e)
                {
                    UtilLogger.Instance.Error($"Error on analyzing instance of type {type.FullName} to get entries", e);
                }
            }

            return result.AddAll(configRepository.Configs);
        }

        public void OnConfigUpdated()
        {
            ISet<Type> types = new HashSet<Type>(FReflection.Instance.GetTypes()
                .Where(type => typeof(IFalconConfigCms).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract));
            foreach (var type in types)
            {
                try
                {
                    var instance = Activator.CreateInstance(type);
                    var fields = instance.GetType().GetFields();
                    foreach (var field in fields)
                    {
                        try
                        {
                            var fieldValue = ConfigEntries[field.Name];
                            var convertedValue = Convert.ChangeType(fieldValue, field.FieldType);
                            field.SetValue(instance, convertedValue);
                        }
                        catch (Exception e)
                        {
                            // ignored
                        }
                    }

                    var methodInfo = type.GetMethod("OnData");
                    methodInfo?.Invoke(instance, null);
                }
                catch (Exception e)
                {
                    UtilLogger.Instance.Error($"Error on analyzing instance of type {type.FullName} to get entries", e);
                }
            }

            _configEntries.Reset();
        }

        public Task Init(CancellationToken cancellationToken = default)
        {
            _configEntries.Reset();
            return Task.CompletedTask;
        }
    }
}