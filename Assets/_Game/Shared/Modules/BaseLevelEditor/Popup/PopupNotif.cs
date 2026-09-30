// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-26

using System;
using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace Falcon.Shared.BaseLevelEditor
{
    public class PopupNotif : PopupBase
    {
        private const float MinW   = 240f;
        private const float MaxW   = 480f;
        private const float HPad   = 32f;  // horizontal padding on each side
        private const float VPad   = 16f;  // vertical gap above/below content and buttons
        private const float BtnW   = 100f;

        private string _content;

        public void SetContent(string content)
        {
            _content = content;
        }

        public override void Draw(ImGui gui)
        {
            var rowH = gui.GetRowHeight();

            var innerSp   = gui.Style.Layout.InnerSpacing;
            var titleBarH = rowH + innerSp * 2f;

            var contentSettings = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f, true);
            var textSize = gui.MeasureTextSize(_content ?? "", in contentSettings,
                                               new Vector2(MaxW - HPad * 2f, 0f));
            var w = Mathf.Clamp(textSize.x + HPad * 2f, MinW, MaxW);
            w = Mathf.Max(w, BtnW + HPad * 2f);
            var h = titleBarH + VPad + textSize.y + VPad + rowH + VPad;

            var screen = gui.Canvas.ScreenSize;
            var rect   = new ImRect((screen.x - w) / 2f, (screen.y - h) / 2f, w, h);

            gui.BeginWindow("Thông báo", rect, ImWindowFlag.NoResizing | ImWindowFlag.NoMoving | ImWindowFlag.NoCloseButton);

            var layoutW = gui.GetLayoutWidth();

            gui.AddSpacing(VPad);
            var contentRect = gui.AddLayoutRect(layoutW, textSize.y);
            gui.Canvas.Text(_content, new Color32(148, 163, 184, 255), contentRect, in contentSettings);
            gui.AddSpacing(VPad);

            gui.BeginHorizontal(layoutW, rowH);
            gui.AddSpacing((layoutW - BtnW) / 2f);
            var okClicked = gui.Button("OK", new ImSize(BtnW, rowH));
            gui.EndHorizontal();

            gui.EndWindow();

            if (okClicked) { IsOpen = false; }
        }
    }
}
