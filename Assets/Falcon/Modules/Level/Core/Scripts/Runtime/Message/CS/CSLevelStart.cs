/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-07-18


using System.Collections.Generic;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage(("cs_level_start"))]
    public class CSLevelStart : CSMessageWaitLoginSuccess
    {
        public int level;
        public string md5LevelData;
        public string levelParam;

        /// <summary>
        /// Tham số thêm của game cho lần chơi này — game server đọc theo key, không có schema
        /// cứng. Null = không gửi. Key đừng trùng field sẵn có của message (level, md5LevelData...).
        /// </summary>
        public Dictionary<string, object> extraMeta;
        
        public CSLevelStart(int level)
        {
            this.level = level;
            this.md5LevelData = FLevelManager.Instance.GetLevelDataMd5(level);
            this.levelParam = FLevelManager.Instance.GetLevelParam(level);
        }

        public CSLevelStart(int level, string md5LevelData, string levelParam)
        {
            this.level = level;
            this.md5LevelData = md5LevelData;
            this.levelParam = levelParam;
        }
    }
}