/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-19


using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Level4Profile
{
    [FAMessage(("cs_level_info_4_profile_req"))]
    public class CSLevelInfo4ProfileReq : CSMessage
    {
        public int code;
        public CSLevelInfo4ProfileReq() { }
        public CSLevelInfo4ProfileReq(int code)
        {
            this.code = code;
        }
    }
}