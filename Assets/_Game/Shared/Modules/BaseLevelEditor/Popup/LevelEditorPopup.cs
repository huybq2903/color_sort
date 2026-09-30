// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-25

using System;
using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using Imui.Style;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.BaseLevelEditor
{
    public class LevelEditorPopup : ImuiRoot, IEditorManager
    {
        private readonly List<PopupBase> _activePopups = new();
        private readonly Queue<PopupBase> _addQueue = new();

        private void Awake()
        {
            LevelEditorManager.Register(this);
        }

        public void Initialized()
        {
            var theme = ImThemeBuiltin.Dark();
            theme.Spacing = 0;
            Gui.SetTheme(theme);
        }

        public void Open(PopupBase popup)
        {
            if (_activePopups.Contains(popup)) return;
            popup.IsOpen = true;
            _addQueue.Enqueue(popup);
        }

        public void Close(PopupBase popup)
        {
            popup.IsOpen = false;
        }

        protected override void OnDrawGui()
        {
            while (_addQueue.Count > 0)
                _activePopups.Add(_addQueue.Dequeue());

            // Forward order: popup 0 drawn first (lowest z), last popup drawn last (highest z).
            // Window i gets canvas order (i+1)*WINDOW_ORDER_OFFSET from the window manager.
            // Scrim for popup i is placed just below its window: (i+1)*WINDOW_ORDER_OFFSET - 1.
            for (var i = 0; i < _activePopups.Count; i++)
            {
                Gui.BeginPopup();
                Gui.Canvas.PushOrder((i + 1) * ImWindow.WINDOW_ORDER_OFFSET - 1);
                Gui.Canvas.Rect(Gui.Canvas.ScreenRect, new Color32(0, 0, 0, 240));
                Gui.Canvas.PopOrder();
                _activePopups[i].Draw(Gui);
                Gui.EndPopup();
            }

            for (var i = _activePopups.Count - 1; i >= 0; i--)
            {
                if (!_activePopups[i].IsOpen) _activePopups.RemoveAt(i);
            }
        }
    }

    [Serializable]
    public abstract class PopupBase
    {
        public bool IsOpen { get; set; }
        public abstract void Draw(ImGui gui);
    }
}