/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-07


using System;

namespace Falcon.Modules.Core.Network
{
    [FAMessage("sc_compressed_message")]
    public class SCCompressedMessage : SCMessage
    {
        public string sc_event;
        public string sc_data;

        public override void OnData()
        {
            //do nothing
        }
    }
}