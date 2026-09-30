/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System;
using System.Collections.Generic;
using Falcon.Modules.Core.AccountData;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    [FGameDataType("ab_test_data")]
    [Serializable]
    public class PlayerAbTest : FGameData<PlayerAbTest>
    {
        public string runningAbTesting;
        public Dictionary<string, bool> campaignMeta;
        public Dictionary<string, object> nonTestConfigs;
        public Dictionary<string, object> testingConfigs;
    }
}