/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    public class FAbTestCustomInfo : IFCustomInfoRepository
    {
        public const string AB_TESTING_VALUE = "abTestingValue";
        public const string AB_TESTING_VARIABLE = "abTestingVariable";
        private readonly FConfigService _configService;

        public FAbTestCustomInfo(FConfigService configService)
        {
            _configService = configService;
        }

        public Dictionary<string, object> GetInfo()
        {
            return new Dictionary<string, object>
            {
                { AB_TESTING_VALUE, _configService.AbTestingString },
                { AB_TESTING_VARIABLE, _configService.RunningAbTesting }
            };
        }
    }
}