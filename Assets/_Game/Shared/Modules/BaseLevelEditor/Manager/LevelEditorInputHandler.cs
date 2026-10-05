using System;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

// ReSharper disable once CheckNamespace
namespace Falcon.Shared.BaseLevelEditor
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Shift = 1,
        Ctrl = 2,
        Alt = 4
    }

    public readonly struct LevelEditorHotkey
    {
        public Key Key { get; }
        public KeyModifiers Modifiers { get; }

        public LevelEditorHotkey(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            Key = key;
            Modifiers = modifiers;
        }
    }

    public readonly struct LevelEditorPointerEvent
    {
        public Vector2 ScreenPosition { get; }
        public Vector3 WorldPosition { get; }
        public KeyModifiers Modifiers { get; }
        public int Button { get; }

        public LevelEditorPointerEvent(
            Vector2 screenPosition,
            Vector3 worldPosition,
            KeyModifiers modifiers,
            int button)
        {
            ScreenPosition = screenPosition;
            WorldPosition = worldPosition;
            Modifiers = modifiers;
            Button = button;
        }
    }

    public class LevelEditorInputHandler : MonoBehaviour, IEditorManager
    {
        private Camera targetCamera;
        private readonly Subject<LevelEditorPointerEvent> _pointerDown = new();
        private readonly Subject<LevelEditorPointerEvent> _pointerUp = new();
        private readonly Subject<LevelEditorPointerEvent> _pointerMove = new(); 
        private readonly Subject<LevelEditorHotkey> _hotkeyDown = new();        
        private readonly Subject<float> _scrollWheel = new();
        private IDisposable subBtnPress;
        private bool _lastLeftPressed;
        private bool _lastRightPressed;
        private bool _lastMiddlePressed;
        private bool _leftDownOnCanvas, _rightDownOnCanvas, _middleDownOnCanvas; // lần nhấn đang giữ đã phát sự kiện nhấn chưa
        private bool _isPointerOverUI;
        private bool _pointerCaptured;

        private Vector2 _currentScreen;
        private Vector3 _currentWorld;
        private Vector2 _lastScreen;
        private KeyModifiers _lastModifiers;

        public Observable<LevelEditorPointerEvent> PointerDown => _pointerDown;
        public Observable<LevelEditorPointerEvent> PointerUp => _pointerUp;
        public Observable<LevelEditorPointerEvent> PointerMove => _pointerMove;
        public Observable<LevelEditorHotkey> HotKeyDown(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            return _hotkeyDown.Where(h => h.Key == key && h.Modifiers == modifiers);
        }
        public Observable<float> ScrollWheel => _scrollWheel;

        // true: đang kéo, rê qua panel UI không làm đứt thao tác
        public void SetPointerCaptured(bool captured) => _pointerCaptured = captured;

        public LevelEditorPointerEvent CurrentPointer => new(_currentScreen, _currentWorld, _lastModifiers, -1);

        private void Awake()
        {
            targetCamera = Camera.main;
        }

        private void Update()
        {
            _isPointerOverUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
        }
        
        private void OnEnable()
        {
            InputSystem.onEvent += OnInputEvent;
            subBtnPress = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
            InitializeMouseCache();
        }

        private void OnDisable()
        {
            InputSystem.onEvent -= OnInputEvent;
            subBtnPress?.Dispose();
            subBtnPress = null;
        }

        private void OnDestroy()
        {
            _pointerDown.Dispose();
            _pointerUp.Dispose();
            _pointerMove.Dispose();
            _hotkeyDown.Dispose();
            _scrollWheel.Dispose();
        }
        
        private void InitializeMouseCache()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            _lastModifiers = GetModifiers();
            _currentScreen = mouse.position.ReadValue();
            _currentWorld = GetMouseWorldPosition();
            _lastScreen = _currentScreen;
            _lastLeftPressed = mouse.leftButton.isPressed;
            _lastRightPressed = mouse.rightButton.isPressed;
            _lastMiddlePressed = mouse.middleButton.isPressed;
        }

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device is not Mouse mouse) return;
            var targeted = IsTargetedToGameView();
            if (targeted) UpdatePointerCache(mouse, eventPtr);
            HandleMouseButtons(mouse, eventPtr, targeted); // chuột trên UI vẫn theo dõi trạng thái nút, chỉ không phát sự kiện nhấn
            if (targeted) HandleMouseScroll(mouse, eventPtr);
        }

        private void UpdatePointerCache(Mouse mouse, InputEventPtr eventPtr)
        {
            _lastModifiers = GetModifiers();
            if (!mouse.position.ReadValueFromEvent(eventPtr, out _currentScreen))
            {
                _currentScreen = mouse.position.ReadValue();
            }
            _currentWorld = GetMouseWorldPosition();

            if (_currentScreen != _lastScreen)
            {
                _pointerMove.OnNext(CreatePointerEvent(-1));
                _lastScreen = _currentScreen;
            }
        }

        private LevelEditorPointerEvent CreatePointerEvent(int button)
        {
            return new LevelEditorPointerEvent(_currentScreen, _currentWorld, _lastModifiers, button);
        }

        private void HandleMouseButtons(Mouse mouse, InputEventPtr eventPtr, bool targeted)
        {
            HandleButton(mouse.leftButton, eventPtr, 0, targeted, ref _lastLeftPressed, ref _leftDownOnCanvas);
            HandleButton(mouse.rightButton, eventPtr, 1, targeted, ref _lastRightPressed, ref _rightDownOnCanvas);
            HandleButton(mouse.middleButton, eventPtr, 2, targeted, ref _lastMiddlePressed, ref _middleDownOnCanvas);
        }

        // Nhấn chỉ phát khi chuột đang trên canvas; nhả chỉ phát nếu lần nhấn đó đã phát. Nhấn trên UI rồi kéo ra canvas thì không bị coi là nhấn mới
        private void HandleButton(ButtonControl button, InputEventPtr eventPtr, int id, bool targeted, ref bool last, ref bool downOnCanvas)
        {
            if (!button.ReadValueFromEvent(eventPtr, out var value)) return;
            var pressed = value >= InputSystem.settings.defaultButtonPressPoint;
            if (pressed && !last && targeted)
            {
                downOnCanvas = true;
                _pointerDown.OnNext(CreatePointerEvent(id));
            }
            else if (!pressed && last && downOnCanvas)
            {
                downOnCanvas = false;
                _pointerUp.OnNext(CreatePointerEvent(id));
            }

            last = pressed;
        }

        private void HandleMouseScroll(Mouse mouse, InputEventPtr eventPtr)
        {
            if (!IsMouseTargetedToGameView(mouse, eventPtr))
            {
                return;
            }

            if (!mouse.scroll.ReadValueFromEvent(eventPtr, out var scroll))
            {
                return;
            }

            if (Mathf.Abs(scroll.y) > Mathf.Epsilon)
            {
                _scrollWheel.OnNext(scroll.y);
            }
        }
        
        private void OnAnyButtonPressed(InputControl control)
        {
            if (control is not KeyControl keyControl)
                return;
            
            if (!IsHotkeyTargetedToGameView())
                return;

            if (keyControl.keyCode is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift)
                return;
            
            _lastModifiers = GetModifiers();
            _hotkeyDown.OnNext(new LevelEditorHotkey(keyControl.keyCode, _lastModifiers));
        }
        
        private bool IsMouseTargetedToGameView(Mouse mouse, InputEventPtr eventPtr)
        {
            var pos = _currentScreen;
            if (mouse.position.ReadValueFromEvent(eventPtr, out var eventPos))
            {
                pos = eventPos;
            }

            if (pos.x < 0f || pos.y < 0f || pos.x > Screen.width || pos.y > Screen.height)
            {
                return false;
            }

            return IsTargetedToGameView();
        }

        private bool IsHotkeyTargetedToGameView()
        {
            if (ImuiRoot.AnyControlActive)
            {
                return false;
            }

            return IsWindowTargetedToGameView();
        }

        private bool IsTargetedToGameView()
        {
            if (_isPointerOverUI && !_pointerCaptured)
            {
                return false;
            }

            return IsWindowTargetedToGameView();
        }

        private bool IsWindowTargetedToGameView()
        {
            if (!Application.isFocused)
            {
                return false;
            }

#if UNITY_EDITOR
            var window = UnityEditor.EditorWindow.mouseOverWindow;
            if (window == null || window.GetType().Name != "GameView")
            {
                return false;
            }
#endif

            return true;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (!targetCamera)
            {
                return Vector3.zero;
            }

            var worldPos = targetCamera.ScreenToWorldPoint(new Vector3(_currentScreen.x, _currentScreen.y, 0f));
            worldPos.z = 0;
            return worldPos;
        }

        private static KeyModifiers GetModifiers()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return KeyModifiers.None;
            }

            var modifiers = KeyModifiers.None;

            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                modifiers |= KeyModifiers.Shift;
            }

            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            {
                modifiers |= KeyModifiers.Ctrl;
            }

            if (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed)
            {
                modifiers |= KeyModifiers.Alt;
            }

            return modifiers;
        }
    }
}