/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;

namespace Falcon.Modules.Core.Network
{
    [FAMessage("f_sc_close_session")]
    internal class SCCloseSession : SCMessage
    {
        public int reason;
        
        public override void OnData()
        {

        }
    }
}

