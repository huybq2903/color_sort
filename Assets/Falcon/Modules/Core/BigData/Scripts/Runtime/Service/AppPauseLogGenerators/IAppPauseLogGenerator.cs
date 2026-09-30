/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-17
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public interface IAppPauseLogGenerator : IMySingleton
    {
        IEnumerable<IDataLog> GetLogsOnAppPause();
    }
}