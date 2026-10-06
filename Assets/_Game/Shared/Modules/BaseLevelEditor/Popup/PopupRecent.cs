using Imui.Controls;
using Imui.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falcon.Shared.BaseLevelEditor
{
    /// <summary>Popup Open Recent: chỉ có lưới level gần đây, đóng bằng nút X ở góc hoặc phím Esc.</summary>
    public class PopupRecent : PopupBase
    {
        private const float MaxW = 780f;
        private const float Aspect = 0.75f; // chiều cao tối thiểu = 75% chiều rộng cho popup bớt dẹt
        private const float WinPad = 16f;

        private readonly RecentGrid _grid;

        public PopupRecent(RecentGrid grid) => _grid = grid;

        public override void Draw(ImGui gui)
        {
            var screen = gui.Canvas.ScreenSize;
            var rowH = gui.GetRowHeight();
            var w = Mathf.Min(MaxW, screen.x - 40f);
            var content = _grid.MeasureHeight(gui, w - WinPad * 2f, false) + rowH * 2f + WinPad * 2f; // + thanh tiêu đề
            var h = Mathf.Min(screen.y - 40f, Mathf.Max(content, w * Aspect));
            var rect = new ImRect((screen.x - w) / 2f, (screen.y - h) / 2f, w, h);
            var escBlocked = _grid.WantsEscape;

            var open = true;
            var oldPad = gui.Style.Window.ContentPadding;
            gui.Style.Window.ContentPadding = WinPad;
            gui.BeginWindow("Open Recent", ref open, rect, ImWindowFlag.NoResizing | ImWindowFlag.NoMoving);
            _grid.Draw(gui, false);
            gui.EndWindow();
            gui.Style.Window.ContentPadding = oldPad;

            var kb = Keyboard.current;
            var esc = !escBlocked && kb != null && kb.escapeKey.wasPressedThisFrame;
            if (!open || esc || _grid.TakeCloseRequest()) IsOpen = false;
        }
    }
}
