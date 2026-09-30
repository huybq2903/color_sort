/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-19


using Falcon.Modules.Core.AccountData;

namespace Falcon.Modules.Level.Core
{
    [FGameDataType("level_data")]
    public class LevelData : FGameData<LevelData>
    {
        public int level = 1;
        public int playTimes = 0;
        public bool startCurrentLevel =  false;
    }

    [FGameDataType("ab_level_info")]
    public class AbLevelInfo : FGameData<AbLevelInfo>
    {
        public string filterID;
        public string abVariant;
    }
}