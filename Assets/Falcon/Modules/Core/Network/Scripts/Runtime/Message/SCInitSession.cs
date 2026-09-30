/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.Modules.Core.Network
{
    [FAMessage("f_sc_init_session")]
    internal class SCInitSession : SCMessage
    {
        public const int STATE_NEW_SESSION = 0;
        public const int STATE_OLD_SESSION = 1;
        public int state;
        public int udpPort;

        public override void OnData()
        {

        }

        public int GetState()
        {
            return state;
        }
    }
}

