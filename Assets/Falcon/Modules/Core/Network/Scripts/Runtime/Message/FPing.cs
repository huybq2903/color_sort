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
    [FAMessage("f_ping")]
    internal class FPing : CSMessage, ICSBinary
    {
        public string scene_name;
        public FPing()
        {
            scene_name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        public byte[] ToBytes()
        {
            FBinaryWriter writer = initWriter(128);
            writer.WriteString(scene_name);
            return writer.ToArray();
        }
    }
}

