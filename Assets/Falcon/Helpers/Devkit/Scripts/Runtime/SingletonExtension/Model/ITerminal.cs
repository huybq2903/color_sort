/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */


// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ITerminal : IMySingleton
    {
        /// <summary>
        ///     Called after all other FMainObj.OnGameStop.
        ///     Called in main thread.
        /// </summary>
        void OnPostStop();
    }
}