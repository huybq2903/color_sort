using UnityEngine;
/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
namespace Falcon.Modules.Core.Network
{
    public class NetworkSettings : ScriptableObject
    {
        [SerializeField] private string serverIp = "34.170.143.65"; //Server thật
                                                                    // [SerializeField] private string serverIp = "192.168.98.199"; //Server local máy Trung
        [SerializeField] private string context = "falcon_puzzle";
        [SerializeField] private int port = 11125;
        [SerializeField] private int portUdp = 11126;
        [SerializeField] private int portIap = 8025;

        private static NetworkSettings _networkConstant;

        private static NetworkSettings Instance
        {
            get
            {
                if (_networkConstant == null)
                {
                    _networkConstant = Resources.Load<NetworkSettings>("NetworkSettings");
                }

                return _networkConstant;
            }
        }

        public static string GetServerIp()
        {
            if (Instance == null)
            {
                Debug.LogError("You have to create setting file first! \"Puzzle/Network/Network Settings\"");
                return "";
            }

            return Instance.serverIp;
        }

        public static string GetServerContext()
        {
            if (Instance == null)
            {
                Debug.LogError("You have to create setting file first! \"Puzzle/Network/Network Settings\"");
                return "";
            }

            return Instance.context;
        }

        public static int GetServerPort()
        {
            if (Instance == null)
            {
                Debug.LogError("You have to create setting file first! \"Puzzle/Network/Network Settings\"");
                return 0;
            }

            return Instance.port;
        }

        public static int GetServerPortUDP()
        {
            if (Instance == null)
            {
                Debug.LogError("You have to create setting file first! \"Puzzle/Network/Network Settings\"");
                return 0;
            }

            return Instance.portUdp;
        }

        public static int GetServerPortIAP()
        {
            if (Instance == null)
            {
                Debug.LogError("You have to create setting file first! \"Puzzle/Network/Network Settings\"");
                return 0;
            }

            return Instance.portIap;
        }
    }
}
