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
        private const float H = 170f;

        private readonly Func<string> _text;

        public PopupLoading(Func<string> text) => _text = text;

        public override void Draw(ImGui gui)
        {
            var screen = gui.Canvas.ScreenSize;
            var rect = new ImRect((screen.x - W) / 2f, (screen.y - H) / 2f, W, H);
            gui.BeginWindow("Đang xử lý", rect, ImWindowFlag.NoResizing | ImWindowFlag.NoMoving | ImWindowFlag.NoCloseButton);

            var dots = new string('.', 1 + (int)(Time.realtimeSinceStartup * 3f) % 3);
            var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
            var line = gui.AddLayoutRect(gui.GetLayoutWidth(), gui.GetRowHeight() * 2f);
            gui.Canvas.Text($"{_text()}{dots}", new Color32(226, 232, 240, 255), line, in settings);

            var hint = gui.AddLayoutRect(gui.GetLayoutWidth(), gui.GetRowHeight());
            gui.Canvas.Text("Vui lòng chờ, ảnh lớn có thể mất vài giây", new Color32(148, 163, 184, 255), hint, in settings);
            gui.EndWindow();
        }
    }
}
