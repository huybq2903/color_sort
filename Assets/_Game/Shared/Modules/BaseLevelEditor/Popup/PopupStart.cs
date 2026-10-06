// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-25

using System;
using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace Falcon.Shared.BaseLevelEditor
{
    public class PopupStart : PopupBase
    {
        private const float W = 360f;
        private const float H = 160f;
        private const float MaxW = 960f;
        private const float Aspect = 0.7f; // chiều cao tối thiểu = 70% chiều rộng cho popup bớt dẹt
        private const float LeftW = 200f;
        private const float MidGap = 18f;
        private const float WinPad = 16f;

        private readonly Action _onNewLevel;
        private readonly Func<bool> _onOpenLevel;
        private readonly RecentGrid _recent;

        public PopupStart(Action onNewLevel, Func<bool> onOpenLevel, RecentGrid recent = null)
        {
            _onNewLevel = onNewLevel;
            _onOpenLevel = onOpenLevel;
            _recent = recent;
        }

        public override void Draw(ImGui gui)
        {
            if (_recent == null) DrawCompact(gui);
            else DrawWithRecent(gui);
        }

        // Bản gốc chỉ có hai nút, dùng khi không có lưới Recent
        private void DrawCompact(ImGui gui)
        {
            var screen = gui.Canvas.ScreenSize;
            var rect = new ImRect((screen.x - W) / 2f, (screen.y - H) / 2f, W, H);

            gui.BeginWindow("Level Editor", rect, ImWindowFlag.NoResizing | ImWindowFlag.NoMoving | ImWindowFlag.NoCloseButton);

            const float hSp = 16f; // horizontal spacing between/around buttons
            var rowH    = gui.GetRowHeight();
            var layoutW = gui.GetLayoutWidth();
            var layoutH = gui.GetLayoutHeight();
            var vGap    = (layoutH - rowH * 2f) / 3f; // equal top / middle / bottom gaps
            var btnW    = (layoutW - hSp * 3f) / 2f;

            var subtitleSettings = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
            var subtitleColor = new Color32(148, 163, 184, 255);
            gui.AddSpacing(vGap);
            var subtitleRect = gui.AddLayoutRect(layoutW, rowH);
            gui.Canvas.Text("New or Open Level", subtitleColor, subtitleRect, in subtitleSettings);
            gui.AddSpacing(vGap);

            gui.BeginHorizontal(layoutW, rowH);
            gui.AddSpacing(hSp);
            var newClicked  = gui.Button("NEW LEVEL",  new ImSize(btnW, rowH));
            gui.AddSpacing(hSp);
            var openClicked = gui.Button("OPEN LEVEL", new ImSize(btnW, rowH));
            gui.AddSpacing(hSp);
            gui.EndHorizontal();

            gui.EndWindow();

            if (newClicked)  { _onNewLevel?.Invoke(); IsOpen = false; return; }
            if (openClicked && _onOpenLevel?.Invoke() == true) { IsOpen = false; }
        }

        // NEW/OPEN ở cột trái, lưới level gần đây ở cột phải
        private void DrawWithRecent(ImGui gui)
        {
            var screen = gui.Canvas.ScreenSize;
            var rowH = gui.GetRowHeight();
            var w = Mathf.Min(MaxW, screen.x - 40f);
            var rightW = w - WinPad * 2f - LeftW - MidGap;
            var content = _recent.MeasureHeight(gui, rightW) + rowH * 2f + WinPad * 2f;
            var h = Mathf.Min(screen.y - 40f, Mathf.Max(content, w * Aspect));
            var rect = new ImRect((screen.x - w) / 2f, (screen.y - h) / 2f, w, h);

            var oldPad = gui.Style.Window.ContentPadding;
            gui.Style.Window.ContentPadding = WinPad;
            gui.BeginWindow("Level Editor", rect, ImWindowFlag.NoResizing | ImWindowFlag.NoMoving | ImWindowFlag.NoCloseButton);

            var layoutW = gui.GetLayoutWidth();
            var layoutH = gui.GetLayoutHeight();
            var newClicked = false;
            var openClicked = false;

            gui.BeginHorizontal(layoutW, layoutH);

            gui.BeginVertical(LeftW, layoutH);
            var sub = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
            gui.Canvas.Text("New or Open Level", new Color32(148, 163, 184, 255), gui.AddLayoutRect(LeftW, rowH), in sub);
            gui.AddSpacing(12f);
            newClicked = gui.Button("NEW LEVEL", new ImSize(LeftW, rowH));
            gui.AddSpacing(12f);
            openClicked = gui.Button("OPEN LEVEL", new ImSize(LeftW, rowH));
            gui.EndVertical();

            gui.AddSpacing(MidGap);

            gui.BeginVertical(layoutW - LeftW - MidGap, layoutH);
            _recent.Draw(gui);
            gui.EndVertical();

            gui.EndHorizontal();

            gui.EndWindow();
            gui.Style.Window.ContentPadding = oldPad;

            if (newClicked) { _onNewLevel?.Invoke(); IsOpen = false; return; }
            if (_recent.TakeCloseRequest()) { IsOpen = false; return; }
            if (openClicked && _onOpenLevel?.Invoke() == true) { IsOpen = false; }
        }
    }
}
