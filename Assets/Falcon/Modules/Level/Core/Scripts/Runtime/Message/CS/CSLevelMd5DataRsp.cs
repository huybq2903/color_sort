/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-11


using System.Collections.Generic;
using Falcon.Modules.ABTestingByServerLogic.Scripts.Runtime;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage("cs_level_md5_data_rsp")]
    public class CSLevelMd5DataRsp : CSMessage
    {
        public Dictionary<int, string> level2Md5Data;
        public Dictionary<int, string> level2Param;
        public Dictionary<int, int> level2Difficulty;
        public string ab_levels_value;
        
        public CSLevelMd5DataRsp() { }
        
        public CSLevelMd5DataRsp(Dictionary<int, string> level2Md5Data, Dictionary<int, string> level2Param, Dictionary<int, int> level2Difficulty, string ab_levels_value)
        {
            this.level2Md5Data = level2Md5Data;
            this.level2Param = level2Param;
            this.level2Difficulty = level2Difficulty;
            if (!string.IsNullOrEmpty(ABTestingManager.Instance.Ab_testing_campaign) && ABTestingManager.Instance.Ab_testing_campaign.Contains("ab_levels_by_server"))
                this.ab_levels_value = ABTestingManager.Instance.Ab_testing_value;
            else
                this.ab_levels_value = ab_levels_value;
        }
    }
}