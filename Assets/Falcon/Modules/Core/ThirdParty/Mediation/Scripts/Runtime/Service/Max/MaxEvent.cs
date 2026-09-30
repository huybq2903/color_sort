/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class MaxEvent : MonoBehaviour
    {
        private static MaxEvent _instance;

        public static MaxEvent Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("FalconMaxEvent");
                    _instance = obj.AddComponent<MaxEvent>();
                }

                return _instance;
            }
        }

        void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public Action<bool> applicationPauseEvent;

        private void OnApplicationPause(bool pauseStatus)
        {
            applicationPauseEvent?.Invoke(pauseStatus);
        }
    }
}