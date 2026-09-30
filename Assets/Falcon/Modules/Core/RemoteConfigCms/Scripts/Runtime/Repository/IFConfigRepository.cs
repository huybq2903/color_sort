/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    public interface IFConfigRepository : IMySingleton
    {
        Dictionary<string, object> Configs { get; }
        void Save(Dictionary<string, object> configs);
    }
}