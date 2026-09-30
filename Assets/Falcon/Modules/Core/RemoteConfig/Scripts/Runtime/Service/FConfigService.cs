/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    public class FConfigService : IConfigUpdatedReact, IInit
    {
        private readonly IFConfigRepository _configRepository;
        private readonly LazyVal<String> _abTestingString;

        public FConfigService(IFConfigRepository configRepository)
        {
            _configRepository = configRepository;
            _abTestingString = new(() =>
            {
                var builder = new StringBuilder();
                foreach (var configObj in new Dictionary<string, object>(configRepository.TestingConfigs))
                    builder.Append(Convert.ToString(configObj.Key, CultureInfo.InvariantCulture))
                        .Append(":")
                        .Append(Convert.ToString(configObj.Value, CultureInfo.InvariantCulture))
                        .Append("_");
                if (builder.Length > 0) builder.Length--;

                return builder.ToString();
            });
        }
        public string AbTestingString => _abTestingString.Value;
        public string RunningAbTesting => _configRepository.RunningAbTesting;
        public Dictionary<string, object> Configs => _configRepository.Configs;
        public Dictionary<string, object> NonTestConfigs => _configRepository.NonTestConfigs;
        public Dictionary<string, object> TestingConfigs => _configRepository.TestingConfigs;
        public Dictionary<string, bool> CampaignMeta => _configRepository.CampaignMeta;
        
        public void OnConfigUpdated()
        {
            _abTestingString.Reset();
        }

        public Task Init(CancellationToken cancellationToken = default)
        {
            _abTestingString.Reset();
            return Task.CompletedTask;
        }
    }
}