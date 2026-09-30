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
    [FAMessage("sc_levels")]
    public class SCLevels : SCMessage
    {
        public string filterId;
        public string abVariant;
        public Dictionary<int, string> level2Data;
        public Dictionary<int, string> level2Param;
        public Dictionary<int, int> level2Difficulty;
        public override void OnData()
        {
            FLevelManager.Instance.SaveLevels(level2Data, level2Param, level2Difficulty);
            FLevelManager.Instance.SaveAbInformation(filterId, abVariant);
        }
    }
}