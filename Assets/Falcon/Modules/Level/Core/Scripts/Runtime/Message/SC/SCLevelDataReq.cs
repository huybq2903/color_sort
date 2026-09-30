/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-11


using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage("sc_level_data_req")]
    public class SCLevelDataReq : SCMessage
    {
        public string md5LevelData;
        public override void OnData()
        {
            
        }
    }
}