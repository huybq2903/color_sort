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
    public interface IFConfigRepository : IInit
    {
        string RunningAbTesting { get; }
        Dictionary<string, object> Configs { get; }
        Dictionary<string, object> NonTestConfigs { get; }
        Dictionary<string, object> TestingConfigs { get; }
        Dictionary<string, bool> CampaignMeta { get; }
        void Save(ConfigResponse config);
    }
}