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
    public class GMAEvent : MonoBehaviour
    {
        private static GMAEvent _instance;

        public static GMAEvent Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("FalconGMAEvent");
                    _instance = obj.AddComponent<GMAEvent>();
                }

                return _instance;
            }
        }

        public Action applicationAwakeEvent;

        void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterEvent()
        {
            applicationAwakeEvent?.Invoke();
        }
    }
}