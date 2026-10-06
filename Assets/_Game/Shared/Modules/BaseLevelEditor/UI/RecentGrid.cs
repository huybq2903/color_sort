using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falcon.Shared.BaseLevelEditor
{
    /// <summary>Lưới thẻ level gần đây dùng chung cho PopupStart và PopupRecent: mở, đổi tên, xoá khỏi danh sách, xoá file.</summary>
    public class RecentGrid
    {
        private const float MinCard = 170f;
        private const float Gap = 12f;
        private const float Pad = 6f;
        private const float DotsW = 34f;
        private const float EmptyH = 200f;

        private static readonly Color32 White = new(224, 224, 224, 255);
        private static readonly Color32 Muted = new(148, 163, 184, 255);
        private static readonly Color32 Danger = new(248, 113, 113, 255);
        private static readonly Color32 Accent = new(17, 121, 200, 255);

        private readonly LevelEditorPopup _popups;
        private readonly Action<string, Action> _open; // (đường dẫn, gọi khi đã mở xong)
        private readonly Func<string> _currentPath;
        private readonly Dictionary<string, Texture2D> _thumbs = new();

        private int _thumbVersion = -1;
        private RecentLevels.Entry _menuEntry;
        private bool _menuOpen;
        private Vector2 _menuSource;
        private RecentLevels.Entry _renaming;
        private string _renameText, _renameError;
        private bool _focusRename;
        private Action _pending;
        private bool _closeRequested;

        public RecentGrid(LevelEditorPopup popups, Action<string, Action> open, Func<string> currentPath)
        {
            _popups = popups;
            _open = open;
            _currentPath = currentPath;
        }

        /// <summary>Đang đổi tên hoặc mở menu ⋯: Esc phải đóng cái đó trước, không đóng popup.</summary>
        public bool WantsEscape => _renaming != null || _menuOpen;

        /// <summary>True một lần sau khi level được mở xong, popup chứa lưới nên tự đóng.</summary>
        public bool TakeCloseRequest()
        {
            var r = _closeRequested;
            _closeRequested = false;
            return r;
        }

        public float MeasureHeight(ImGui gui, float width, bool showTitle = true)
        {
            var n = RecentLevels.Items.Count;
            var rowH = gui.GetRowHeight();
            var header = n > 0 || showTitle ? rowH + Gap : 0f;
            if (n == 0) return header + EmptyH;
            var cols = Columns(width);
            var rows = (n + cols - 1) / cols;
            return header + rows * (CardHeight(width / cols - Gap, rowH) + Gap);
        }

        /// <summary>showTitle=false: bỏ chữ tiêu đề ở đầu lưới (popup đã có tiêu đề riêng), chỉ giữ nút Xoá danh sách.</summary>
        public void Draw(ImGui gui, bool showTitle = true)
        {
            SyncThumbs();
            var items = RecentLevels.Items;
            var width = gui.GetLayoutWidth();
            var rowH = gui.GetRowHeight();

            if (items.Count > 0 || showTitle)
            {
                DrawHeader(gui, width, rowH, items.Count, showTitle);
                gui.AddSpacing(Gap);
            }

            if (items.Count == 0)
            {
                DrawEmpty(gui, width, rowH);
            }
            else
            {
                var cols = Columns(width);
                var cell = width / cols;
                var cellH = CardHeight(cell - Gap, rowH) + Gap;
                for (var i = 0; i < items.Count; i += cols)
                {
                    gui.BeginHorizontal();
                    for (var k = i; k < Mathf.Min(i + cols, items.Count); k++)
                        DrawCard(gui, items[k], gui.AddLayoutRectWithSpacing(cell, cellH), rowH);
                    gui.EndHorizontal();
                }
            }

            DrawMenu(gui);
            var run = _pending;
            _pending = null;
            run?.Invoke(); // thao tác đổi danh sách chạy sau khi vẽ xong để không sửa list giữa vòng lặp
            HandleKeys();
        }

        private static int Columns(float width) => Mathf.Max(1, (int)((width + Gap) / (MinCard + Gap)));

        private static float ThumbHeight(float cardW) => (cardW - Pad * 2f) * 0.58f;

        private static float CardHeight(float cardW, float rowH) => Pad + ThumbHeight(cardW) + rowH * 2f + Pad;

        private void DrawHeader(ImGui gui, float width, float rowH, int count, bool showTitle)
        {
            var r = gui.AddLayoutRect(width, rowH);
            var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0f, 0.5f);
            if (showTitle) gui.Canvas.Text("Recent", White, r, in settings);
            const float btnW = 130f;
            if (count > 0 && gui.Button("Xoá danh sách", new ImRect(r.Right - btnW, r.Y, btnW, r.H))) _pending = RecentLevels.Clear;
        }

        private static void DrawEmpty(ImGui gui, float width, float rowH)
        {
            var r = gui.AddLayoutRect(width, Mathf.Max(EmptyH, gui.GetLayoutHeight() - Gap)); // trạng thái trống lấp đầy chỗ còn lại
            gui.Canvas.RectOutline(r, new Color32(85, 85, 85, 255), 1f, 6f);
            var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
            gui.Canvas.Text("Chưa có level gần đây", Muted, r, in settings);
        }

        private void DrawCard(ImGui gui, RecentLevels.Entry e, ImRect cell, float rowH)
        {
            var card = new ImRect(cell.X + Gap / 2f, cell.Y + Gap / 2f, cell.W - Gap, cell.H - Gap);
            var exists = RecentLevels.Exists(e);
            var renaming = _renaming == e;
            var thumbH = ThumbHeight(card.W);
            var thumb = new ImRect(card.X + Pad, card.Top - Pad - thumbH, card.W - Pad * 2f, thumbH);
            var nameRect = new ImRect(thumb.X, thumb.Y - rowH, thumb.W, rowH);
            var timeRect = new ImRect(thumb.X, nameRect.Y - rowH, thumb.W, rowH);
            var dots = new ImRect(thumb.Right - 6f - DotsW, thumb.Top - 6f - rowH, DotsW, rowH);
            var mouse = gui.Input.MousePosition;

            var cardClicked = false;
            if (renaming)
            {
                gui.Canvas.Rect(card, new Color32(64, 64, 64, 255));
            }
            else
            {
                gui.BeginReadOnly(!exists);
                cardClicked = gui.Button(gui.GetControlId(e.path), card, out _);
                gui.EndReadOnly();
            }

            DrawThumb(gui, e, thumb, exists);

            if (renaming)
            {
                DrawRename(gui, nameRect, timeRect);
            }
            else
            {
                var text = new ImTextSettings(gui.Style.Layout.TextSize, 0f, 0.5f, false, ImTextOverflow.Ellipsis);
                gui.Canvas.Text(Path.GetFileNameWithoutExtension(e.path), exists ? White : Muted, nameRect, in text);
                var sub = exists ? RecentLevels.TimeAgo(e.opened) + (IsCurrent(e) ? " · đang mở" : "") : "File đã bị dời hoặc xoá";
                gui.Canvas.Text(sub, exists ? Muted : Danger, timeRect, in text);
            }

            if (IsCurrent(e)) gui.Canvas.RectOutline(card, Accent, 2f, 6f);

            var showDots = !renaming && (card.Contains(mouse) || (_menuOpen && _menuEntry == e));
            var dotsClicked = showDots && gui.Button(gui.GetControlId(e.path + "#dots"), "···", dots);
            if (dotsClicked)
            {
                _menuEntry = e;
                _menuOpen = true;
                _menuSource = mouse;
            }
            else if (cardClicked && !dots.Contains(mouse))
            {
                _pending = () => Open(e);
            }
        }

        private void DrawThumb(ImGui gui, RecentLevels.Entry e, ImRect thumb, bool exists)
        {
            var tex = exists ? ThumbFor(e) : null;
            if (tex)
            {
                gui.Image(tex, thumb, true);
            }
            else
            {
                gui.Canvas.Rect(thumb, new Color32(43, 43, 43, 255));
                var center = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
                gui.Canvas.Text(exists ? "Not preview" : "Not found", exists ? Muted : Danger, thumb, in center);
            }

            gui.Canvas.RectOutline(thumb, new Color32(28, 28, 28, 255), 1f);
        }

        private void DrawRename(ImGui gui, ImRect nameRect, ImRect hintRect)
        {
            var id = gui.GetControlId("recent_rename");
            ref var state = ref gui.Storage.Get<ImTextEditState>(id);
            if (_focusRename)
            {
                gui.SetActiveControl(id);
                state.Caret = _renameText.Length;
                _focusRename = false;
            }

            if (gui.TextEdit(id, ref _renameText, ref state, nameRect, false, 120)) _renameError = null;

            var hint = new ImTextSettings(gui.Style.Layout.TextSize * 0.85f, 0f, 0.5f, false, ImTextOverflow.Ellipsis);
            gui.Canvas.Text(_renameError ?? "Enter để lưu · Esc để huỷ", _renameError == null ? Muted : Danger, hintRect, in hint);
            if (_renameError != null) gui.Canvas.RectOutline(nameRect, Danger, 1.5f);
        }

        private void DrawMenu(ImGui gui)
        {
            if (!_menuOpen || _menuEntry == null) return;
            var e = _menuEntry;
            var exists = RecentLevels.Exists(e);
            var current = IsCurrent(e);

            if (!gui.BeginContextMenu(gui.GetControlId("recent_menu"), _menuSource, ref _menuOpen)) return;

            gui.BeginReadOnly(!exists || current);
            if (gui.Menu("Mở")) _pending = () => Open(e);
            if (gui.Menu("Đổi tên")) _pending = () => StartRename(e);
            gui.EndReadOnly();

            gui.BeginReadOnly(!exists);
            if (gui.Menu("Hiện trong thư mục")) _pending = () => Reveal(e);
            gui.EndReadOnly();

            if (gui.Menu("Sao chép đường dẫn")) _pending = () => Copy(e);
            gui.Separator();
            if (gui.Menu("Xoá khỏi danh sách")) _pending = () => RecentLevels.Remove(e);

            gui.BeginReadOnly(!exists || current);
            if (gui.Menu("Xoá file…")) _pending = () => ConfirmDelete(e);
            gui.EndReadOnly();

            gui.EndContextMenu();
        }

        private void HandleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null || _renaming == null) return;
            if (kb.escapeKey.wasPressedThisFrame) _renaming = null;
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) CommitRename();
        }

        private void Open(RecentLevels.Entry e)
        {
            if (RecentLevels.Exists(e)) _open(e.path, () => _closeRequested = true);
        }

        private void StartRename(RecentLevels.Entry e)
        {
            _renaming = e;
            _renameText = Path.GetFileNameWithoutExtension(e.path);
            _renameError = null;
            _focusRename = true;
        }

        private void CommitRename()
        {
            if (RecentLevels.Rename(_renaming, _renameText, out _renameError)) _renaming = null;
        }

        private static void Reveal(RecentLevels.Entry e)
        {
            if (Application.platform is RuntimePlatform.WindowsEditor or RuntimePlatform.WindowsPlayer)
                Process.Start("explorer.exe", $"/select,\"{e.path}\"");
            else
                Application.OpenURL("file://" + Path.GetDirectoryName(e.path));
        }

        private static void Copy(RecentLevels.Entry e)
        {
            GUIUtility.systemCopyBuffer = e.path;
            LevelEditorMainUI.Log("Đã sao chép đường dẫn: " + e.path);
        }

        private void ConfirmDelete(RecentLevels.Entry e)
        {
            var popup = new PopupConfirm($"Xoá file {Path.GetFileName(e.path)} khỏi ổ đĩa?\nFile sẽ được chuyển vào Thùng rác của Windows.", "Huỷ", "Xoá file");
            popup.SetAction(null, () =>
            {
                if (!RecentLevels.DeleteFile(e, out var error)) LevelEditorMainUI.Warn("Không xoá được file: " + error);
            });
            _popups.Open(popup);
        }

        private bool IsCurrent(RecentLevels.Entry e)
        {
            var cur = _currentPath?.Invoke();
            return !string.IsNullOrEmpty(cur) && string.Equals(Path.GetFullPath(cur), e.path, StringComparison.OrdinalIgnoreCase);
        }

        private Texture2D ThumbFor(RecentLevels.Entry e)
        {
            var p = RecentLevels.ThumbPath(e);
            if (p == null) return null;
            if (_thumbs.TryGetValue(p, out var cached)) return cached;

            Texture2D tex = null;
            if (File.Exists(p))
            {
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(p)))
                {
                    UnityEngine.Object.Destroy(tex);
                    tex = null;
                }
            }

            _thumbs[p] = tex;
            return tex;
        }

        private void SyncThumbs()
        {
            if (_thumbVersion == RecentLevels.Version) return;
            foreach (var t in _thumbs.Values)
                if (t) UnityEngine.Object.Destroy(t);
            _thumbs.Clear();
            _thumbVersion = RecentLevels.Version;
        }
    }
}
