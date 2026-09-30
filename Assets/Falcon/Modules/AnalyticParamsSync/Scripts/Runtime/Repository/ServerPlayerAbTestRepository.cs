/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.RemoteConfig;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    [Primary]
    public class ServerPlayerAbTestRepository : IFConfigRepository
    {
        private readonly LazyVal<Dictionary<string, object>> _configs;
        private readonly ReserveConfigRepository _reserveRepository;

        public ServerPlayerAbTestRepository(ReserveConfigRepository reserveRepository)
        {
            _reserveRepository = reserveRepository;
            _configs = new LazyVal<Dictionary<string, object>>(() =>
                new Dictionary<string, object>().AddAll(TestingConfigs).AddAll(NonTestConfigs));
        }

        public async Task Init(CancellationToken cancellationToken = default)
        {
            if (RemoteUncertain())
            {
                if(!await AccountLoginListener.WaitLogin()) return;
                if (RemoteUncertain()) OverwriteRemoteData();
            }

            _configs.Reset();
        }

        public string RunningAbTesting => PlayerAbTest.Instance.runningAbTesting ?? _reserveRepository.RunningAbTesting;
        public Dictionary<string, object> Configs => _configs.Value;
        public Dictionary<string, object> NonTestConfigs => _reserveRepository.NonTestConfigs;

        public Dictionary<string, object> TestingConfigs =>
            PlayerAbTest.Instance.testingConfigs ?? _reserveRepository.TestingConfigs;

        public Dictionary<string, bool> CampaignMeta =>
            PlayerAbTest.Instance.campaignMeta ?? _reserveRepository.CampaignMeta;

        public void Save(ConfigResponse config)
        {
            _reserveRepository.Save(config);
            OverwriteRemoteData();
            _configs.Reset();
        }

        private void OverwriteRemoteData()
        {
            PlayerAbTest.Instance.runningAbTesting = _reserveRepository.RunningAbTesting;
            PlayerAbTest.Instance.campaignMeta = _reserveRepository.CampaignMeta;
            PlayerAbTest.Instance.testingConfigs = _reserveRepository.TestingConfigs;
            PlayerAbTest.Instance.UpdateToServer();
        }

        private static bool RemoteUncertain()
        {
            var playerGeneralData = PlayerAbTest.Instance;
            return playerGeneralData.runningAbTesting == null
                   || playerGeneralData.campaignMeta == null
                   || playerGeneralData.testingConfigs == null;
        }
    }
}