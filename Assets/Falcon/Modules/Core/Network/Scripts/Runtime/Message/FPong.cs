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
    [FAMessage("f_pong")]
    internal class FPong : SCMessage, ISCBinary
    {

        public override void OnData()
        {

        }

        public void FromBytes(byte[] bytes)
        {
            FBinaryReader reader = initReader(bytes);
        }
    }
}

