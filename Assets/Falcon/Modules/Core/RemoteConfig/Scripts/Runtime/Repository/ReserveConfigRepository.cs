/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    public class ReserveConfigRepository : IFConfigRepository
    {
        private const string kConfigKey = "FALCON_CONFIG_VER_2_";

        private readonly LazyVal<Dictionary<string, object>> _configs;

        private readonly LazyVal<Dictionary<string, object>> _nonTestConfigs;

        private readonly BasicPoolData<ConfigResponse> _response;

        private readonly LazyVal<Dictionary<string, object>> _testingConfigs;

        public ReserveConfigRepository(IDataPool dataPool)
        {
            _response = new BasicPoolData<ConfigResponse>(dataPool, kConfigKey, new ConfigResponse());
            _configs = new LazyVal<Dictionary<string, object>>(() => _response.Value.Configs);
            _nonTestConfigs = new LazyVal<Dictionary<string, object>>(() => _response.Value.NonTestConfigs());
            _testingConfigs = new LazyVal<Dictionary<string, object>>(() => _response.Value.TestingConfigs());
        }

        public string RunningAbTesting => _response.Value.runningAbTesting;
        public Dictionary<string, object> Configs => _configs.Value;
        public Dictionary<string, object> NonTestConfigs => _nonTestConfigs.Value;
        public Dictionary<string, object> TestingConfigs => _testingConfigs.Value;
        public Dictionary<string, bool> CampaignMeta => _response.Value.CampaignMeta;

        public void Save(ConfigResponse config)
        {
            _response.Value = config;
            _configs.Reset();
            _nonTestConfigs.Reset();
            _testingConfigs.Reset();
        }

        public Task Init(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}