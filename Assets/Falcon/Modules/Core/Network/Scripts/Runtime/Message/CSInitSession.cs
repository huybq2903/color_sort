/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Falcon.Modules.Core.Network
{
    [FAMessage("f_cs_init_session")]
    internal class CSInitSession : CSMessage
    {

        public CSInitSession(string sessionId)
        {
            this.sessionId = sessionId;
        }

    }
}

