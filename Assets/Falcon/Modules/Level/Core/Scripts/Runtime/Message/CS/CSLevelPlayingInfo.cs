// /*
//  * Author: quanph
//  * Email: quanph@falcongames.com
//  * Company: Falcon Games
//  * Date: 2026 - 01 - 23
//  */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage(("cs_level_playing_info"))]
    public class CSLevelPlayingInfo : CSMessage
    {
        public int level;
        public string md5LevelContent;
        public string levelParam;
        public int timeFromLevelStart;
        public string info;
        
        public CSLevelPlayingInfo(int level, string md5LevelContent, string levelParam, int timeFromLevelStart, string info)
        {
            this.level = level;
            this.md5LevelContent = md5LevelContent;
            this.levelParam = levelParam;
            this.timeFromLevelStart = timeFromLevelStart;
            this.info = info;
        }
    }
}