/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-16
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.EventBus;
using Falcon.Shared.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.BaseEvents
{
    public class UIEventNotify : MonoBehaviour
    {
        [SerializeField] private GameObject goNotify;

        [ValueDropdown(nameof(GetKeyEventList)), SerializeField] private string keyEvent;
        
        [ValueDropdown(nameof(GetKeyNotifiesList)), SerializeField] private string[] keyNotify;

        private IWrapperEvent _wrapper;

        private void OnEnable()
        {
            GameEvent<object>.Register(keyEvent, OnChangeData, this);
            _wrapper = Center.Get(EventsRegister.GetType(keyEvent)) as IWrapperEvent;
            OnChangeData(null);
        }

        private void OnDisable()
        {
            GameEvent<object>.Unregister(keyEvent, OnChangeData, this);
        }

        protected virtual void OnChangeData(object data)
        {
            if (goNotify) goNotify.SetActive(Notify > 0);
        }

        public int Notify
        {
            get
            {
                if (_wrapper == null || keyNotify == null || _wrapper.Notifies == null) return 0;

                var sum = 0;
                foreach (var key in keyNotify)
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    if (_wrapper.Notifies.TryGetValue(key, out var func) && func != null)
                        sum += func.Invoke();
                }
                return sum;
            }
        }

        private static string[] GetKeyEventList()
        {
            return EventsRegister.DictWrapper.Select(e => e.Key).ToArray();
        }

        private IEnumerable<string> GetKeyNotifiesList()
        {
            if (string.IsNullOrEmpty(keyEvent)) return Array.Empty<string>();
            if (!EventsRegister.DictWrapperNotifies.TryGetValue(keyEvent, out var arr) || arr == null)
                return Array.Empty<string>();
            return arr;
        }
    }
}