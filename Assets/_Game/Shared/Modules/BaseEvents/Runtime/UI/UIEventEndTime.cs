/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-14
 */

using System;
using System.Linq;
using Falcon.Helpers.EventBus;
using Falcon.Shared.Common.Time;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Falcon.Shared.BaseEvents
{
    public class UIEventEndTime : MonoBehaviour
    {
        [ValueDropdown(nameof(GetKeyEventList)), SerializeField] private string keyEvent;
        
        private TMP_Text _txtTime;
        private bool _active = true;
        
        private const string GET_LOCALIZED_STRING = "falcon.modules.uloc.get_localized_string";
        public event Action OnFinish;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                if (!_active) 
                    WrapperTime.RemoveAction(keyEvent + "_end_time", DoCountTimer);
                else
                    WrapperTime.AddAction(keyEvent + "_end_time", DoCountTimer);
            }
        }
        
        private void Awake()
        {
            _txtTime = GetComponentInChildren<TMP_Text>();
        }

        private void OnEnable()
        {
            if (!_active) return;
            if (!WrapperTime.AddAction(keyEvent + "_end_time", DoCountTimer))
            {
                SetText(GameRequest<string, string>.Request(GET_LOCALIZED_STRING, "completed"));
            }
        }
        
        private void OnDisable()
        {
            WrapperTime.RemoveAction(keyEvent + "_end_time", DoCountTimer);
        }

        private void DoCountTimer(long time)
        {
            if (time <= 0)
            {
                SetText(GameRequest<string, string>.Request(GET_LOCALIZED_STRING, "completed"));
                OnFinish?.Invoke();
                return;
            }
            SetText(time.ToTime());
        }
        
        public void SetText(string text)
        {
            _txtTime ??= GetComponentInChildren<TMP_Text>();
            _txtTime.SetText(text);
        }
        
        private static string[] GetKeyEventList()
        {
            return EventsRegister.DictWrapper.Select(e => e.Key).ToArray();
        }
    }
}