/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-19


using System.Security.Cryptography.X509Certificates;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Level4Profile
{
    [FAMessage(("sc_level_info_4_profile_rsp"))]
    public class SCLevelInfo4ProfileRsp : SCMessage
    {
        public int code;
        public int max_level;
        public int one_time_shot;
        public int win_strike;
        public override void OnData()
        {
            
        }
    }
}