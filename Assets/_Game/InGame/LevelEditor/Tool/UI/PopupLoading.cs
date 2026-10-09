using System;
using Falcon.Shared.BaseLevelEditor;
using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Popup chờ chặn thao tác: chữ động (dấu chấm chạy) và số giây đã chờ.</summary>
    public class PopupLoading : PopupBase
    {
        private const float W = 440f;
        private const float H = 200f;

        private readonly Func<string> _text;

        public PopupLoading(Func<string> text) => _text = text;

        public override void Draw(ImGui gui)
        {
            var screen = gui.Canvas.ScreenSize;
            var rect = new ImRect((screen.x - W) / 2f, (screen.y - H) / 2f, W, H);
            gui.BeginWindow("Đang xử lý", rect, ImWindowFlag.NoResizing | ImWindowFlag.NoMoving | ImWindowFlag.NoCloseButton);

            var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
            gui.AddLayoutRect(1f, 16f); // cách xa tiêu đề popup
            DrawSpinner(gui, gui.AddLayoutRect(gui.GetLayoutWidth(), 44f));
            var line = gui.AddLayoutRect(gui.GetLayoutWidth(), gui.GetRowHeight());
            gui.Canvas.Text(_text(), new Color32(226, 232, 240, 255), line, in settings);

            var hint = gui.AddLayoutRect(gui.GetLayoutWidth(), gui.GetRowHeight());
            gui.Canvas.Text("Vui lòng chờ, ảnh lớn có thể mất vài giây", new Color32(148, 163, 184, 255), hint, in settings);
            gui.EndWindow();
        }

        // Vòng chấm xoay: 12 chấm quanh tâm, chấm đầu sáng nhất rồi mờ dần theo chiều quay
        private static void DrawSpinner(ImGui gui, ImRect area)
        {
            const int dots = 12;
            var c = new Vector2(area.X + area.W / 2f, area.Y + area.H / 2f);
            var head = (int)(Time.realtimeSinceStartup * 12f) % dots;
            for (var i = 0; i < dots; i++)
            {
                var a = i * Mathf.PI * 2f / dots;
                var age = (head - i + dots) % dots; // 0 = chấm đầu
                var alpha = (byte)(255f * Mathf.Lerp(1f, 0.12f, age / (dots - 1f)));
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 15f;
                gui.Canvas.Rect(new ImRect(p.x - 3f, p.y - 3f, 6f, 6f), new Color32(125, 211, 252, alpha));
            }
        }
    }
}
