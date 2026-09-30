/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class FConfigController : MySingleton<FConfigController>
    {
        private readonly FConfigInitService _configInitService;
        private readonly FConfigInstanceService _configInstanceService;
        private readonly FConfigService _configService;
        private readonly FConfigScanService _scanService;

        public FConfigController(FConfigService configService, 
            FConfigInitService configInitService,
            FConfigInstanceService configInstanceService,
            FConfigScanService scanService)
        {
            _configService = configService;
            _configInitService = configInitService;
            _configInstanceService = configInstanceService;
            _scanService = scanService;
        }

        public string AbTestingString => _configService.AbTestingString;
        public string RunningAbTesting => _configService.RunningAbTesting;
        public Dictionary<string, object> Configs => _configService.Configs;
        public Dictionary<string, object> NonTestConfigs => _configService.NonTestConfigs;
        public Dictionary<string, object> TestingConfigs => _configService.TestingConfigs;
        public Dictionary<string, bool> CampaignMeta => _configService.CampaignMeta;
        public Dictionary<string, object> ConfigEntries => _scanService.ConfigEntries;
        public ExecState InitState => _configInitService.InitState;

        public T Config<T>() where T : IFalconConfig, new()
        {
            return _configInstanceService.GetInstance<T>();
        }

        /// <inheritdoc cref="FConfigInitService.TryFetch"/>
        public Task<bool> TryFetch(CancellationToken cancellationToken = default)
        {
            return _configInitService.TryFetch(cancellationToken);
        }

        public event Action OnUpdateFromNet
        {
            add => _configInitService.OnUpdateFromNet += value;
            remove => _configInitService.OnUpdateFromNet -= value;
        }
    }
}