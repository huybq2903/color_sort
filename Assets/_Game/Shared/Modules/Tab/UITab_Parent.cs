/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-06
 */

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.Tab
{
    public class UITab_Parent : MonoBehaviour
    {
        public UITab_Child[] lsChild;
        public Button[] lsButton;
        public Func<int, bool> ConditionSelect;
        public bool activeOnEnable = true;

        public int CurrentIndexTab { get; private set; }
        
        private UITab_Child _currentTab;
        private bool _initialized;
        private Action _onInitialized;
        
        private void Awake()
        {
            foreach (var child in lsChild) child.DeActive();

            for (var i = 0; i < lsButton.Length; i++)
            {
                var index = i;
                lsButton[i].onClick.AddListener(() =>
                {
                    if (ConditionSelect != null && !ConditionSelect.Invoke(index)) return;
                    Active(index);
                });
            }

            _initialized = true;
            _onInitialized?.Invoke();
        }

        private void OnEnable()
        {
            if (activeOnEnable) Active(0);
        }

        public void Active(int index)
        {
            if (!_initialized)
            {
                _onInitialized = () => Active(index);
                return;
            }
            
            CurrentIndexTab = index;
            _currentTab?.DeActive();
            _currentTab = lsChild[index];
            _currentTab.Active();
        }
    }
}