/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-15
 */

using DG.Tweening;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Falcon.Modules.UI.UITooltip.Runtime
{
    public class UITooltip : MonoBehaviour
    {
        [SerializeField] private GameObject element;

        private GameObject _goTrigger;
        private CanvasGroup _canvasGroup;
        private bool _active;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            GameEvent<(string, int)[]>.Register("falcon.modules.ui.tooltip", Open, this);
            GameEvent<string>.Register("falcon.modules.ui.tooltip", Open, this);
        }

        private void OnDisable()
        {
            GameEvent<(string, int)[]>.Unregister("falcon.modules.ui.tooltip", Open, this);
            GameEvent<string>.Unregister("falcon.modules.ui.tooltip", Open, this);
            _goTrigger = null;
            _active = false;
        }

        private void Open(string content)
        {
            if (Open())
            {
                element.SetActive(true);
                element.transform.position = _goTrigger.transform.position;
                element.SendMessage("Active", content, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void Open((string, int)[] data)
        {
            if (Open())
            {
                element.SetActive(true);
                element.transform.position = _goTrigger.transform.position;
                element.SendMessage("Active", data, SendMessageOptions.DontRequireReceiver);
            }
        }

        private bool Open()
        {
            // Tween scale nằm ở element, complete ở đây kẻo mở lại trong 0.2s là scale kẹt dở dang.
            element.transform.DOComplete();

            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (_goTrigger == selected)
            {
                CloseAndForget();
                return false;
            }

            _goTrigger = selected;
            if (_goTrigger == null)
            {
                Close();
                _active = false;
                return false;
            }

            _active = true;
            _canvasGroup.alpha = 1;
            return true;
        }

        private void CloseAndForget()
        {
            Close();
            _goTrigger = null;
            _active = false;
        }

        private void Close()
        {
            _canvasGroup.alpha = 0;
            element.SendMessage("DeActive", SendMessageOptions.DontRequireReceiver);
            element.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_active) return;

            // Nút trigger bị tắt (đóng popup, despawn pool) thì tooltip phải biến theo.
            if (_goTrigger == null || !_goTrigger.activeInHierarchy)
            {
                CloseAndForget();
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame) Close();
            else if (pointer.press.wasReleasedThisFrame) OnEndTouch();
        }

        private void OnEndTouch()
        {
            if (_goTrigger != null)
            {
                _goTrigger = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            }

            if (_canvasGroup.alpha != 0) return;
            _active = false;
        }
    }
}
