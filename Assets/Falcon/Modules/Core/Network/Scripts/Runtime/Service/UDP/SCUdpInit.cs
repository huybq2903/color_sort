using UnityEngine;

namespace Falcon.Modules.Core.Network
{
    [FAMessage("sc_udp_init")]
    public class SCUdpInit : SCMessage
    {
        public override void OnData()
        {
            SessionListener4Udp.setupUdpSuccess = true;
            Debug.Log("SETUP UDP SUCCESS!!!");
        }
    }
}