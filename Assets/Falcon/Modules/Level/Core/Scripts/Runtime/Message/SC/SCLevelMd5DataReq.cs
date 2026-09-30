/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-11


using System.Collections.Generic;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage("sc_level_md5_data_req")]
    public class SCLevelMd5DataReq : SCMessage
    {
        public List<int> levels;

        public override void OnData()
        {
            Dictionary<int, string> level2Md5Data = new Dictionary<int, string>();
            Dictionary<int, string> level2Param = new Dictionary<int, string>();
            Dictionary<int, int> level2Difficulty = new Dictionary<int, int>();
            if (levels == null) return;
            foreach (var level in levels)
            {
                string levelData = FLevelManager.Instance.GetLevelData(level);
                string levelParam = FLevelManager.Instance.GetLevelParam(level);
                int levelDifficulty = FLevelManager.Instance.GetLevelDifficulty(level);
                level2Md5Data[level] = Md5Utils.GetMd5First5Char(levelData);
                level2Param[level] = levelParam;
                level2Difficulty[level] = levelDifficulty;
            }

            new CSLevelMd5DataRsp(level2Md5Data, level2Param, level2Difficulty, ABLevelValue.GetABLevelsValue()).Send();
        }
    }
}