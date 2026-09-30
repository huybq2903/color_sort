/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-11


using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage("cs_level_verify_req")]
    public class CSLevelVerifyReq : CSMessage
    {
        public string md5LevelData;
        public CSLevelVerifyReq(){}
        public CSLevelVerifyReq(string md5LevelData)
        {
            this.md5LevelData = md5LevelData;
        }
    }
}