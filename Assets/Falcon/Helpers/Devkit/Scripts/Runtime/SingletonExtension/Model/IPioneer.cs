/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */


// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface IPioneer: IMySingleton
    {
        /// <summary>
        ///     Called before all other FMainObj.OnGameContinue.
        ///     Called in main thread.
        /// </summary>
        void OnPreContinue();
    }
}