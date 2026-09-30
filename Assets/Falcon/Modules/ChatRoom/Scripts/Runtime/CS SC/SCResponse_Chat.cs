using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("sc_response_chatroom")]
    public class SCResponse_Chat : SCMessage
    {
        public bool success;
        public string message;
        public override void OnData()
        {
        }
    }
}
