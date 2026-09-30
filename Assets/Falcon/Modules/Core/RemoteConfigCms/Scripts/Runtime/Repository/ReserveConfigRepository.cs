/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    public class ReserveConfigRepository : IFConfigRepository
    {
        private const string kConfigKey = "FALCON_CONFIG_VER_2_CMS_";
        private readonly LazyVal<Dictionary<string, object>> _configs;
        private readonly BasicPoolData<ConfigResponse> _response;

        public ReserveConfigRepository(IDataPool dataPool)
        {
            _response = new BasicPoolData<ConfigResponse>(dataPool, kConfigKey, new ConfigResponse());
            _configs = new LazyVal<Dictionary<string, object>>(
                () => _response.Value.Configs);
        }

        public Dictionary<string, object> Configs => _configs.Value;

        public void Save(Dictionary<string, object> configs)
        {
            var a = new ConfigResponse()
            {
                configs = configs
            };
            _response.Value = a;
            _configs.Reset();
        }

        public Task Init(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}