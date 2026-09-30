/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-09
 */
using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.RemoteConfig;

namespace Falcon.Modules.ABTestingByServerLogic.Scripts.Runtime
{
    public class ServerAbTestCustomInfo: IFCustomInfoRepository
    {
        public const string AB_TESTING_VALUE = "abTestingValue";
        public const string AB_TESTING_VARIABLE = "abTestingVariable";
        private readonly FConfigService _configService;
        private readonly ABTestingManager _abTestingManager = ABTestingManager.Instance;

        public ServerAbTestCustomInfo(FConfigService configService)
        {
            _configService = configService;
        }

        public Dictionary<string, object> GetInfo()
        {
            var abTestingValue = !string.IsNullOrWhiteSpace(_abTestingManager.Ab_testing_value) ? _abTestingManager.Ab_testing_value : _configService.AbTestingString;
            var runningAbTesting = !string.IsNullOrWhiteSpace(_abTestingManager.Ab_testing_campaign) ? _abTestingManager.Ab_testing_campaign : _configService.RunningAbTesting;
            return new Dictionary<string, object>
            {
                { AB_TESTING_VALUE, abTestingValue },
                { AB_TESTING_VARIABLE, runningAbTesting }
            };
        }
    }
    
    public class DefaultAbTestCustomInfoDisabler : ISingletonShellSourceDisabler
    {
        public int Priority => 0;

        public IEnumerable<Type> DisablingSources
        {
            get
            {
                yield return typeof(FAbTestCustomInfo);
            }
        }
    }
}