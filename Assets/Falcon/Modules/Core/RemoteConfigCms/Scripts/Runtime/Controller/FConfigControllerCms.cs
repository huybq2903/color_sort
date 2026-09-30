/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class FConfigControllerCms : MySingleton<FConfigControllerCms>
    {
        private readonly FConfigInitService _configInitService;
        private readonly FConfigInstanceService _configInstanceService;
        private readonly FConfigService _configService;
        private readonly FConfigScanService _scanService;

        public FConfigControllerCms(
            FConfigService configService,
            FConfigInitService configInitService,
            FConfigInstanceService configInstanceService,
            FConfigScanService scanService)
        {
            _configService = configService;
            _configInitService = configInitService;
            _configInstanceService = configInstanceService;
            _scanService = scanService;
        }

        public Dictionary<string, object> Configs => _configService.Configs;
        public Dictionary<string, object> ConfigEntries => _scanService.ConfigEntries;
        public ExecState InitState => _configInitService.InitState;

        public T Config<T>() where T : IFalconConfigCms, new()
        {
            return _configInstanceService.GetInstance<T>();
        }

        public event Action OnUpdateFromNet
        {
            add => _configInitService.OnUpdateFromNet += value;
            remove => _configInitService.OnUpdateFromNet -= value;
        }
    }
}