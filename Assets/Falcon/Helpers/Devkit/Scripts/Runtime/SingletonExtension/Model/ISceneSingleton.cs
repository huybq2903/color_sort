/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ISceneSingleton : IMySingleton
    {
        void OnNewScene(Scene oldScene, Scene newScene);
    }
}