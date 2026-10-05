/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-25
 */

using System;
using System.Collections.Generic;
using Falcon.Shared.BaseInGame;
using Falcon.Shared.Common;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using Imui.Style;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Falcon.Shared.BaseLevelEditor
{
    public class LevelEditorMainUI : ImuiRoot, IEditorManager
    {
        protected LevelEditorSaveLoad _saveLoad;
        protected LevelEditorPopup _popupManager;
        protected PopupStart _popupStart;
        protected PopupConfirm _popupConfirm;
        protected PopupNotif _popupNotif;

        // Cơ chế Property/Inspector dùng chung: dict type entity -> inspector (giống CustomEditor).
        private readonly Dictionary<Type, IEntityInspector> _inspectors = new();

        // Log 1 dòng dưới cùng: log sau đè log trước; quá dài thì Ellipsis tự cắt "…".
        private static string _log;

        /// <summary>Ghi 1 dòng log vào panel dưới cùng (đè dòng cũ).</summary>
        public static void Log(string msg) => _log = msg;

        // Toast cảnh báo nhanh nổi dưới hàng tên file, tự tắt sau ToastSeconds; bổ sung cho thanh log
        private static string _toast;
        private static float _toastStart;
        private const float ToastSeconds = 3f, ToastFade = 0.4f;

        /// <summary>Hiện toast (chỉ toast, không ghi log).</summary>
        public static void Toast(string msg)
        {
            _toast = msg;
            _toastStart = Time.unscaledTime;
        }

        /// <summary>Cảnh báo cần người dùng chú ý ngay: ghi log và hiện toast.</summary>
        public static void Warn(string msg)
        {
            Log(msg);
            Toast(msg);
        }

        /// <summary>Độ rộng Property panel — override nếu muốn.</summary>
        protected virtual float RightW => 300f;

        /// <summary>Entity đang được focus — derived trả về selection của mình.</summary>
        protected virtual IEntityDataContainer CurrentSelection => null;

        /// <summary>Đăng ký inspector theo TargetType của nó.</summary>
        protected void RegisterInspector(IEntityInspector inspector) => _inspectors[inspector.TargetType] = inspector;

        // Property panel cố định bên phải, vẽ inspector cho CurrentSelection.
        protected virtual void DrawRightPanel()
        {
            var screen = Gui.Canvas.ScreenSize;
            var menuH = Gui.Style.Layout.InnerSpacing * 2f + Gui.GetRowHeight();
            var logH = Gui.GetRowHeight(); // chừa chỗ cho thanh log dưới cùng
            var panelH = screen.y - menuH - 10 - logH;
            var rect = new ImRect(screen.x - RightW - 10, 10 + logH, RightW, panelH);
            DrawInspectorPanel(rect, CurrentSelection);
        }

        /// <summary>Lookup inspector theo type của selected rồi paint vào rect.</summary>
        protected virtual void DrawInspectorPanel(ImRect rect, IEntityDataContainer selected)
        {
            if (selected == null || !_inspectors.TryGetValue(selected.GetType(), out var inspector)) return;

            Gui.BeginWindow($"{inspector.TargetType.Name} ({selected.BaseData.id})", rect, ImWindowFlag.NoMovingAndResizing);
            inspector.Target = selected;
            inspector.OnInspectorGUI(Gui);
            Gui.EndWindow();
        }

        protected virtual void Awake()
        {
            LevelEditorManager.Register(this);
        }

        public virtual void Initialized()
        {
            _saveLoad = LevelEditorManager.Get<LevelEditorSaveLoad>();
            _popupManager = LevelEditorManager.Get<LevelEditorPopup>();
            _popupStart = new PopupStart(OnNewLevel, OnOpenLevel);
            _popupConfirm = new PopupConfirm("Có thay đổi chưa lưu. Tiếp tục?");
            _popupNotif = new PopupNotif();
            Gui.SetTheme(ImThemeBuiltin.Dark());
            Gui.Style.MenuBar = DarkMenuBarStyle(Gui);
            Gui.Style.Window.ContentPadding = 12f; // chừa lề nội dung trong mọi window
            MenuBarRegistry.Initialize();
        }

        protected override void OnDrawGui()
        {
            if (_saveLoad.LevelName == null)
            {
                _popupManager.Open(_popupStart);
                return;
            }

            DrawMenuBar();
            DrawNameFile();
            DrawRightPanel();
            DrawLogBar();
            DrawToast();
        }

        // Hộp cảnh báo đỏ giữa trên cùng, mờ dần ở cuối thời gian hiện
        protected virtual void DrawToast()
        {
            if (string.IsNullOrEmpty(_toast)) return;
            var age = Time.unscaledTime - _toastStart;
            if (age > ToastSeconds)
            {
                _toast = null;
                return;
            }
            var alpha = Mathf.Clamp01((ToastSeconds - age) / ToastFade);
            var rowH = Gui.GetRowHeight();
            var screen = Gui.Canvas.ScreenSize;
            var menuBarH = Gui.Style.Layout.InnerSpacing * 2f + rowH;
            var settings = new ImTextSettings(Gui.Style.Layout.TextSize, 0.5f, 0.5f, true);
            var maxW = Mathf.Min(560f, screen.x - 40f);
            var size = Gui.MeasureTextSize(_toast, in settings, new Vector2(maxW - 32f, 0f));
            var w = Mathf.Min(maxW, size.x + 32f);
            var h = size.y + 16f;
            var rect = new ImRect((screen.x - w) / 2f, screen.y - menuBarH - rowH - 8f - h, w, h);

            Gui.Canvas.PushOrder(ImWindow.WINDOW_ORDER_OFFSET * 8); // nổi trên mọi panel
            Gui.Canvas.Rect(rect, new Color32(127, 29, 29, (byte)(235 * alpha)));
            Gui.Canvas.RectOutline(rect, new Color32(252, 165, 165, (byte)(255 * alpha)), 1.5f);
            Gui.Canvas.Text(_toast, new Color32(255, 255, 255, (byte)(255 * alpha)), rect, in settings);
            Gui.Canvas.PopOrder();
        }

        // Panel 1 dòng sát đáy màn hình, hiển thị _log (Ellipsis tự "…" khi tràn).
        protected virtual void DrawLogBar()
        {
            var rowH = Gui.GetRowHeight();
            var screen = Gui.Canvas.ScreenSize;
            var rect = new ImRect(0f, 0f, screen.x, rowH);

            Gui.Canvas.Rect(rect, new Color32(15, 23, 42, 200));                  // nền
            Gui.Canvas.RectOutline(rect, new Color32(255, 255, 255, 40), 1f);     // viền

            var labelRect = new ImRect(rect.X + 8f, rect.Y, rect.W - 16f, rect.H);
            var textSettings = new ImTextSettings(Gui.Style.Layout.TextSize, 0f, 0.5f, false, ImTextOverflow.Ellipsis);
            Gui.Canvas.Text(_log, new Color32(226, 232, 240, 255), labelRect, in textSettings);
        }

        protected virtual void DrawMenuBar()
        {
            Gui.BeginMenuBar();

            if (ImMenuBar.Button(Gui, "New")) ConfirmDiscardChanges(OnNewLevel);
            if (ImMenuBar.Button(Gui, "Open")) ConfirmDiscardChanges(() => OnOpenLevel());
            if (ImMenuBar.Button(Gui, "Save")) OnSave();
            if (ImMenuBar.Button(Gui, "Save As")) OnSaveAs();

            foreach (var node in MenuBarRegistry.TopLevel)
            {
                if (node.IsLeaf)
                {
                    if (ImMenuBar.Button(Gui, node.Label)) node.Contributor.OnClick();
                }
                else if (ImMenuBar.BeginItem(Gui, node.Label))
                {
                    DrawMenuChildren(node);
                    ImMenuBar.EndItem(Gui);
                }
            }

            Gui.EndMenuBar();
        }

        protected virtual void DrawNameFile()
        {
            var rowH = Gui.GetRowHeight();
            var innerSp = Gui.Style.Layout.InnerSpacing;
            var screen = Gui.Canvas.ScreenSize;
            var menuBarH = innerSp * 2f + rowH;

            const float hSp = 12f;
            const float btnW = 88f;
            const float textW = 220f;
            const float totalW = hSp + textW + hSp + btnW + hSp;

            var x = (screen.x - totalW) / 2f;
            var y = screen.y - menuBarH - rowH;

            var textRect = new ImRect(x + hSp, y, textW, rowH);
            var btnRect = new ImRect(x + hSp + textW + hSp, y, btnW, rowH);

            // Đóng khung tên file
            Gui.Canvas.Rect(textRect, new Color32(15, 23, 42, 180));                 // nền
            Gui.Canvas.RectOutline(textRect, new Color32(255, 255, 255, 40), 1f);    // viền

            var dirty = _saveLoad && _saveLoad.HasUnsavedChanges;
            var label = $"{_saveLoad.LevelName}{(dirty ? " *" : "")}";          // * nếu chưa lưu

            var labelRect = new ImRect(textRect.X + 8f, textRect.Y, textRect.W - 16f, textRect.H);
            var textSettings = new ImTextSettings(Gui.Style.Layout.TextSize, 0f, 0.5f, false, ImTextOverflow.Ellipsis);
            Gui.Canvas.Text(label, new Color32(255, 255, 255, 255), labelRect, in textSettings);

            if (Gui.Button("Play (P)", btnRect)) OnPlay();
        }

        protected virtual void ConfirmDiscardChanges(Action onContinue)
        {
            if (!_saveLoad.HasUnsavedChanges)
            {
                onContinue?.Invoke();
                return;
            }
            _popupConfirm.SetAction(null, onContinue);
            _popupManager.Open(_popupConfirm);
        }

        protected virtual void DrawMenuChildren(MenuNode node)
        {
            foreach (var child in node.Children)
            {
                if (child.IsLeaf)
                {
                    if (Gui.Menu(child.Label)) child.Contributor.OnClick();
                }
                else if (Gui.BeginMenu(child.Label))
                {
                    DrawMenuChildren(child);
                    Gui.EndMenu();
                }
            }
        }

        protected virtual void OnPlay()
        {
            _saveLoad.SaveTempToPlay();
            DataTempExtensions<string>.Set(GameModeKeys.MODE, GameModeKeys.EDITOR_PLAYTEST);
            SceneManager.LoadScene("GameScene");
        }

        protected virtual void OnNewLevel() => _saveLoad.NewFile();

        protected virtual bool OnOpenLevel()
        {
            _saveLoad.OpenFile();
            return _saveLoad.LevelName != null;
        }

        protected virtual void OnSave()
        {
            _saveLoad.SaveFileImmediately(out var failed);
            if (failed.Count > 0)
            {
                _popupNotif.SetContent($"Không thể lưu:\n{string.Join("\n", failed)}");
                _popupManager.Open(_popupNotif);
            }
        }

        protected virtual void OnSaveAs()
        {
            _saveLoad.SaveFileAs(out var failed);
            if (failed.Count > 0)
            {
                _popupNotif.SetContent($"Không thể lưu:\n{string.Join("\n", failed)}");
                _popupManager.Open(_popupNotif);
            }
        }

        private static ImStyleMenuBar DarkMenuBarStyle(ImGui gui)
        {
            var style = gui.Style.MenuBar;
            style.Box.BackColor = new Color32(10, 14, 26, 255);    // #0A0E1A — darker than bg
            style.Box.BorderColor = new Color32(255, 255, 255, 20);  // rgba(255,255,255,0.08)
            style.ItemNormal.Normal.BackColor = new Color32(0, 0, 0, 0);
            style.ItemNormal.Normal.FrontColor = new Color32(148, 163, 184, 255); // #94A3B8 muted
            style.ItemNormal.Hovered.BackColor = new Color32(30, 41, 59, 255);   // #1E293B
            style.ItemNormal.Hovered.FrontColor = new Color32(248, 250, 252, 255); // #F8FAFC
            style.ItemActive.Normal.BackColor = new Color32(30, 41, 59, 255);
            style.ItemActive.Normal.FrontColor = new Color32(248, 250, 252, 255);
            return style;
        }
    }
}
