/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-09-17


using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage(("cs_level_ready"))]
    public class CSLevelReady : CSMessageWaitLoginSuccess
    {
        public int level;
        public string md5LevelData;
        public string levelParam;
        
        public CSLevelReady(int level)
        {
            this.level = level;
            this.md5LevelData = FLevelManager.Instance.GetLevelDataMd5(level);
            this.levelParam = FLevelManager.Instance.GetLevelParam(level);
        }
    }
}