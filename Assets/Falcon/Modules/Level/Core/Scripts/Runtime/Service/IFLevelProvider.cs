/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-11


using Falcon.Helpers.Devkit;

namespace Falcon.Modules.Level.Core
{
    public interface IFLevelProvider : IMySingleton
    {
        public string GetLevelData(int level);
        public string GetLevelParam(string levelData);
        public bool IsValidLevelData(string levelData);
    }
}