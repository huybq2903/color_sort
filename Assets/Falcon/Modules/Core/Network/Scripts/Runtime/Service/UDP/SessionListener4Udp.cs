using System.Collections;
using NUnit.Framework.Constraints;
using UnityEngine;

namespace Falcon.Modules.Core.Network
{
    public class SessionListener4Udp : ISessionListener
    {
        public void OnSessionReset()
        {
            Debug.Log("START SETTING UP UDP!!!");
            CoroutineRunner.Instance.StartCoroutine(SetupUdp());
        }

        public void OnFirstSession()
        {
            Debug.Log("START SETTING UP UDP!!!");
            CoroutineRunner.Instance.StartCoroutine(SetupUdp());
        }

        public static bool setupUdpSuccess = false;
        
        IEnumerator SetupUdp()
        {
            while (!setupUdpSuccess)
            {
                new CSUdpInit().UDP().Send();
                yield return new WaitForSecondsRealtime(5);
            }
        }

        public void OnChannelDisconnected(FChannel channel)
        {
        }
    }
}