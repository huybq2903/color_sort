/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface IFAppInfoRepository : IMySingleton
    {
        /// <summary>
        /// Gets the package name (bundle ID) of the application.
        /// </summary>
        string PackageName { get; }

        /// <summary>
        /// Gets the human-readable name of the game or application.
        /// </summary>
        string GameName { get; }

        /// <summary>
        /// Gets the platform on which the application is currently running (e.g., "iOS", "Android", "Windows").
        /// </summary>
        string Platform { get; }

        /// <summary>
        /// Gets the version string of the currently running application.
        /// </summary>
        string AppVersion { get; }
    }
}