using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("sc_response_clan")]
    public class SCResponse_Clan : SCMessage
    {
        public bool success;
        public string message;
        public override void OnData()
        {
        }
    }
}
