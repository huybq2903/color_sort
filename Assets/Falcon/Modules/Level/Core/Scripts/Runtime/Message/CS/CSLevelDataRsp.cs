/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-11


using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage(("cs_level_data_rsp"))]
    public class CSLevelDataRsp : CSMessage
    {
        public int level;
        public string levelData;
        public string levelParam;
        public int levelDifficulty;
    }
}