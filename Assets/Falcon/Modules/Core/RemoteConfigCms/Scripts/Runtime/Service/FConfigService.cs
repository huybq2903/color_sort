/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    public class FConfigService : IConfigUpdatedReact
    {
        private readonly IFConfigRepository _configRepository;

        public FConfigService(IFConfigRepository configRepository)
        {
            _configRepository = configRepository;
        }

        public Dictionary<string, object> Configs => _configRepository.Configs;

        public Task Init(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void OnConfigUpdated()
        {
        }
    }
}