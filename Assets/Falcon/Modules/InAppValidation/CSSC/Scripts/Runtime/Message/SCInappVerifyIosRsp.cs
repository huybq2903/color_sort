/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-18
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.InAppValidation.CSSC.Runtime
{
    [FAMessage("sc_inapp_verify_ios_rsp")]
    public class SCInappVerifyIosRsp : SCMessage
    {
        public int status;
        public override void OnData()
        {
            
        }
    }
}