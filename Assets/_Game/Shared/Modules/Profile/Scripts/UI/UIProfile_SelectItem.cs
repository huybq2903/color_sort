/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-08
 */

using System;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.UI;
using Falcon.Shared.Common;

namespace Game.Shared.Profile
{
    public class UIProfile_SelectItem : MonoBehaviour
    {
        [SerializeField] private GameObject goSelected, goLock;

        private UIProfileElement _profileElement;
        private int _id, _indexInGrid;
        private Action<int, int> _onChosen;
        private bool _isUnlock;

        private void Awake()
        {
            _profileElement = GetComponentInChildren<UIProfileElement>();
            var button = GetComponentInChildren<Button>();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (_isUnlock)
            {
                _onChosen?.Invoke(_id, _indexInGrid);
            }
            else
            {
                GameEvent<string>.Emit(GameKeys.TOAST_OPEN, "not_unlock_yet");
            }
        }

        public void SetData(int id, int indexInGrid, Action<int, int> onChosen)
        {
            _id = id;
            _indexInGrid = indexInGrid;
            _onChosen = onChosen;

            _profileElement.ActiveItem(_id);
            goLock.SetActive(!_isUnlock);
        }

        public void SetSelected(bool state)
        {
            goSelected.SetActive(state);
            goSelected.transform.localScale = state ? Vector3.one * 1.1f : Vector3.one;
            _profileElement.transform.localScale = state ? Vector3.one * 1.1f : Vector3.one;
            if (state)
                goLock.SetActive(false);
            else
                goLock.SetActive(!_isUnlock);
        }

        public void SetUnlock(bool state) => _isUnlock = state;
    }
}