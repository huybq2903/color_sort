/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-11
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Falcon.Shared.Common
{
    public class GlobalButtonListener : MonoBehaviour
    {
        private static event Action<Button> ButtonReleased;

        private readonly List<RaycastResult> _raycastResults = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (FindFirstObjectByType<GlobalButtonListener>(FindObjectsInactive.Include)) return;

            var listenerObject = new GameObject(nameof(GlobalButtonListener));
            DontDestroyOnLoad(listenerObject);
            listenerObject.AddComponent<GlobalButtonListener>();
        }

        public static void Register(Action<Button> callback)
        {
            ButtonReleased += callback;
        }

        public static void Unregister(Action<Button> callback)
        {
            ButtonReleased -= callback;
        }

        // Raised by ButtonClickForwarder from the real Button.onClick, so this fires
        // only when the click actually executes (release on the same button, etc.).
        internal static void NotifyClicked(Button button)
        {
            ButtonReleased?.Invoke(button);
        }

        private void Update()
        {
            // On press, hook the pressed button's real onClick (once). The hook then
            // drives ButtonReleased, so it is perfectly in sync with the actual click.
            if (!TryGetPointerDownPosition(out var position)) return;

            var button = FindButton(RaycastTopmost(position));
            if (button) ButtonClickForwarder.Ensure(button);
        }

        private static Button FindButton(GameObject target)
        {
            return target ? target.GetComponentInParent<Button>() : null;
        }

        private GameObject RaycastTopmost(Vector2 position)
        {
            var eventSystem = EventSystem.current;
            if (!eventSystem) return null;

            var pointerData = new PointerEventData(eventSystem) { position = position };
            _raycastResults.Clear();
            eventSystem.RaycastAll(pointerData, _raycastResults);

            return _raycastResults.Count > 0 ? _raycastResults[0].gameObject : null;
        }

        private static bool TryGetPointerDownPosition(out Vector2 position)
        {
            // Pointer.current là chuột hoặc ngón tay, tuỳ device đang dùng
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                position = pointer.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }
    }

    [DisallowMultipleComponent]
    internal sealed class ButtonClickForwarder : MonoBehaviour
    {
        private Button _button;

        public static void Ensure(Button button)
        {
            if (button.TryGetComponent<ButtonClickForwarder>(out _)) return;

            button.gameObject.AddComponent<ButtonClickForwarder>().Bind(button);
        }

        private void Bind(Button button)
        {
            _button = button;
            _button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (_button) GlobalButtonListener.NotifyClicked(_button);
        }

        private void OnDestroy()
        {
            if (_button) _button.onClick.RemoveListener(OnClick);
        }
    }
}
