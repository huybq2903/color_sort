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

        private readonly Action _onNewLevel;
        private readonly Func<bool> _onOpenLevel;

        public PopupStart(Action onNewLevel, Func<bool> onOpenLevel)
        {
            _onNewLevel = onNewLevel;
            _onOpenLevel = onOpenLevel;
        }

        public override void Draw(ImGui gui)
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
    }
}