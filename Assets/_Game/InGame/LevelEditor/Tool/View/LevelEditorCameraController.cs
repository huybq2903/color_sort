using System;
using Falcon.Shared.BaseLevelEditor;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Camera editor: cuộn = zoom về con trỏ, giữ chuột giữa = pan, FitTo = vừa khung.</summary>
    public class LevelEditorCameraController : MonoBehaviour, IEditorManager
    {
        [SerializeField] private Camera cam;
        [SerializeField] private float zoomStep = 0.1f;
        [SerializeField] private float minOrthographicSize = 0.5f;
        [SerializeField] private float maxOrthographicSize = 100f;
        [SerializeField, Tooltip("Chừa lề quanh tranh khi Vừa khung.")] private float fitMargin = 1.15f;

        private LevelEditorInputHandler _input;
        private bool _panning;
        private Vector2 _lastScreen;
        private IDisposable _subScroll, _subDown, _subUp, _subMove;

        public void Initialized()
        {
            if (!cam) cam = Camera.main;
            _input = LevelEditorManager.Get<LevelEditorInputHandler>();
            if (_input == null) return;
            _subScroll = _input.ScrollWheel.Subscribe(OnScroll);
            _subDown = _input.PointerDown.Subscribe(e =>
            {
                if (e.Button != 2) return;
                _panning = true;
                _lastScreen = e.ScreenPosition;
            });
            _subUp = _input.PointerUp.Subscribe(e => { if (e.Button == 2) _panning = false; });
            _subMove = _input.PointerMove.Subscribe(OnPointerMove);
        }

        // leftInset: phần bề ngang màn hình (0..1) bị panel bên trái che; tranh được canh vào vùng còn lại
        public void FitTo(Bounds b, float leftInset = 0f)
        {
            if (!cam || b.size.y <= 0f) return;
            var free = Mathf.Clamp(1f - leftInset, 0.2f, 1f);
            var size = Mathf.Clamp(Mathf.Max(b.extents.y, b.extents.x / (cam.aspect * free)) * fitMargin, minOrthographicSize, maxOrthographicSize);
            cam.orthographicSize = size;
            cam.transform.position = new Vector3(b.center.x - leftInset * size * cam.aspect, b.center.y, cam.transform.position.z);
        }

        private void OnScroll(float dy)
        {
            if (!cam || !cam.orthographic || Mathf.Abs(dy) < 0.01f) return;
            var screen = _input.CurrentPointer.ScreenPosition;
            var before = ScreenToWorld(screen);
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * (1f - Mathf.Sign(dy) * zoomStep), minOrthographicSize, maxOrthographicSize);
            var delta = before - ScreenToWorld(screen);
            delta.z = 0f;
            cam.transform.position += delta; // giữ điểm dưới con trỏ đứng yên
        }

        private void OnPointerMove(LevelEditorPointerEvent e)
        {
            if (!_panning || !cam) return;
            if (Mouse.current != null && !Mouse.current.middleButton.isPressed)
            {
                _panning = false; // thả nút trên panel UI thì sự kiện up bị chặn
                return;
            }
            var delta = ScreenToWorld(_lastScreen) - ScreenToWorld(e.ScreenPosition);
            delta.z = 0f;
            cam.transform.position += delta;
            _lastScreen = e.ScreenPosition;
        }

        private Vector3 ScreenToWorld(Vector2 screen) => cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));

        private void OnDestroy()
        {
            _subScroll?.Dispose();
            _subDown?.Dispose();
            _subUp?.Dispose();
            _subMove?.Dispose();
        }
    }
}
