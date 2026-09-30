/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;

namespace Falcon.Modules.Core.Network
{
    [FAMessage("cs_sc_response")]
    public class CSSCResponse : SCMessage
    {
        public bool success;
        public string code;
        public string message;


        public override void OnData()
        {
            FCallbackManager.Instance.OnResponse(this);
        }

        public CSSCResponse() { }

        public CSSCResponse(string messageId, bool success, string message, string code)
        {
            this.messageId = messageId;
            this.success = success;
            this.message = message;
            this.code = code;
        }
    }
}

