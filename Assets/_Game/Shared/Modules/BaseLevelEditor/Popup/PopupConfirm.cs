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
    public class PopupConfirm : PopupBase
    {
        private const float MinW  = 240f;
        private const float MaxW  = 480f;
        private const float HPad  = 32f;
        private const float VPad  = 16f;
        private const float BtnSp = 16f;

        private readonly string _content;
        private Action _onBack;
        private Action _onContinue;

        public PopupConfirm(string content)
        {
            _content = content;
        }

        public void SetAction(Action onBack, Action onContinue)
        {
            _onBack     = onBack;
            _onContinue = onContinue;
        }

        public override void Draw(ImGui gui)
        {
            var rowH      = gui.GetRowHeight();
            var innerSp   = gui.Style.Layout.InnerSpacing;
            var titleBarH = rowH + innerSp * 2f;

            var contentSettings = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f, true);
            var textSize = gui.MeasureTextSize(_content ?? "", in contentSettings,
                                               new Vector2(MaxW - HPad * 2f, 0f));
            var w = Mathf.Clamp(textSize.x + HPad * 2f, MinW, MaxW);
            var h = titleBarH + VPad + textSize.y + VPad + rowH + VPad;

            var screen = gui.Canvas.ScreenSize;
            var rect   = new ImRect((screen.x - w) / 2f, (screen.y - h) / 2f, w, h);

            gui.BeginWindow("Thông báo", rect, ImWindowFlag.NoResizing | ImWindowFlag.NoMoving | ImWindowFlag.NoCloseButton);

            var layoutW = gui.GetLayoutWidth();
            var btnW    = (layoutW - BtnSp * 3f) / 2f;

            gui.AddSpacing(VPad);
            var contentRect = gui.AddLayoutRect(layoutW, textSize.y);
            gui.Canvas.Text(_content, new Color32(148, 163, 184, 255), contentRect, in contentSettings);
            gui.AddSpacing(VPad);

            gui.BeginHorizontal(layoutW, rowH);
            gui.AddSpacing(BtnSp);
            var backClicked     = gui.Button("Quay về", new ImSize(btnW, rowH));
            gui.AddSpacing(BtnSp);
            var continueClicked = gui.Button("Tiếp tục", new ImSize(btnW, rowH));
            gui.AddSpacing(BtnSp);
            gui.EndHorizontal();

            gui.EndWindow();

            if (backClicked)     { _onBack?.Invoke();     IsOpen = false; return; }
            if (continueClicked) { _onContinue?.Invoke(); IsOpen = false; return; }
        }
    }
}
