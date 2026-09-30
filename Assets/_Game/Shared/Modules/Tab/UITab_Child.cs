/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-06
 */

using System;
using Sirenix.Utilities;
using UnityEngine;

namespace Falcon.Shared.Tab
{
    public class UITab_Child : MonoBehaviour
    {
        [SerializeField] private GameObject[] lsObjs;
        [SerializeField] private GameObject btnEnable, btnDisable;

        public event Action OnActive, OnDeActive;
        
        public void Active()
        {
            lsObjs.ForEach(item => item.SetActive(true));
            btnEnable.SetActive(true);
            btnDisable.SetActive(false);
            OnActive?.Invoke();
        }

        public void DeActive()
        {
            lsObjs.ForEach(item => item.SetActive(false));
            btnEnable.SetActive(false);
            btnDisable.SetActive(true);
            OnDeActive?.Invoke();
        }
    }
}