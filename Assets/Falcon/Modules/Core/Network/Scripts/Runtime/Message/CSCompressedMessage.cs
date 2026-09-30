/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-07


using System;

namespace Falcon.Modules.Core.Network
{
    [FAMessage("cs_compressed_message")]
    public class CSCompressedMessage : CSMessage
    {
        public string cs_event;
        public string cs_data;
        
        public CSCompressedMessage()
        {
            this.cs_event = string.Empty;
            this.cs_data = string.Empty;
        }
        public CSCompressedMessage(string csEvent, string csData)
        {
            this.cs_event = csEvent;
            this.cs_data = csData;
        }
    }
}