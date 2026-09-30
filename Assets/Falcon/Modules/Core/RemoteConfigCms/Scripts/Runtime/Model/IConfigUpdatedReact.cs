/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    public interface IConfigUpdatedReact : IMySingleton
    {
        void OnConfigUpdated();
    }
}