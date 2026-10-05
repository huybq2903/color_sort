using System;
using System.Linq;
using Falcon.InGame.Core;
using Falcon.Shared.BaseLevelEditor;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>MainUI Color Sort: panel Picture bên trái (ảnh nguồn, công cụ, bảng màu, xem, thống kê).</summary>
    public class ColorSortEditorUI : LevelEditorMainUI
    {
        private const float LeftW = 460f;
        private const float MaxSwatch = 36f;
        private const float ThumbMaxH = 240f;
        private const int PerRow = 5;
        private static readonly int[] SmoothOptions = { 1, 3, 5, 7 };
        private static readonly string[] ModeLabels = { "Chọn (V)", "Vẽ (D)", "Tô (B)" };
        private static readonly string[] DrawLabels = { "Tách (1)", "Vẽ biên (2)" }; // Nét (3) tạm ẩn, bổ sung sau

        private LevelEditorPicture _picture;
        private PopupLoading _loading;

        public override void Initialized()
        {
            base.Initialized();
            _picture = LevelEditorManager.Get<LevelEditorPicture>();
            _loading = new PopupLoading(() => _picture.BusyText);
        }

        protected override void OnDrawGui()
        {
            base.OnDrawGui();
            if (_saveLoad.LevelName == null || _picture == null) return;
            DrawPicturePanel();
            if (_picture.IsBusy && !_loading.IsOpen) _popupManager.Open(_loading);
            else if (!_picture.IsBusy && _loading.IsOpen) _popupManager.Close(_loading);
        }

        private void DrawPicturePanel()
        {
            var screen = Gui.Canvas.ScreenSize;
            var menuH = Gui.Style.Layout.InnerSpacing * 2f + Gui.GetRowHeight();
            var logH = Gui.GetRowHeight();
            var rect = new ImRect(10f, 10f + logH, LeftW, screen.y - menuH - 10f - logH);
            _picture.ViewInset = (rect.X + rect.W) / screen.x;

            Gui.BeginWindow("Picture", rect, ImWindowFlag.NoMovingAndResizing);
            try
            {
                DrawSourceSection();
                Gui.Separator("Chế độ");
                DrawModeSection();
                if (_picture.SelectedCount > 0) DrawSelection();
                else if (_picture.SelectedLine >= 0) Gui.Text("Đang chọn 1 nét · Delete để xoá", true);
                Gui.Separator("Bảng màu");
                DrawPalette();
                Gui.Separator("Xem");
                DrawViewSection();
                Gui.Separator("Thống kê");
                var p = _picture.Picture;
                Gui.Text(p == null
                    ? "Chưa có tranh"
                    : $"{p.regions.Count} mảnh · {p.regions.Select(r => r.colorId).Distinct().Count()} màu · cát {p.regions.Sum(r => r.value)} · {p.width}x{p.height}");
            }
            finally
            {
                Gui.EndWindow(); // luôn đóng window để Imui không lệch stack Begin/End
            }
        }

        private void DrawSourceSection()
        {
            var s = _picture.Settings;
            Gui.BeginHorizontal();
            if (_picture.Picture == null && Gui.Button("Import Image...")) _picture.ImportImage();
            if ((_picture.Picture != null || _picture.HasSource) && Gui.Button("Clear")) _picture.Clear();
            Gui.EndHorizontal();
            Gui.Text(_picture.HasSource ? $"Ảnh: {s.sourceName}" : "Chưa nạp ảnh", true);
            var tex = _picture.SourceTexture;
            if (tex)
            {
                var w = Gui.Layout.GetAvailableWidth();
                Gui.Image(tex, new ImSize(w, Mathf.Min(ThumbMaxH, w * tex.height / tex.width)), true);
            }
            if (_picture.HasSource) DrawFrameSection(s);
            if (_picture.HasSource && Gui.Button("Generate")) _picture.Generate();
        }

        // Khung đầu ra: chỉ đổi được khi chưa có tranh; ảnh nằm trong khung, phần dư là nền
        private void DrawFrameSection(GenSettings s)
        {
            Gui.Separator("Khung");
            if (_picture.Picture != null)
            {
                Gui.Text($"Khung: {s.frameW}x{s.frameH}", true);
                return;
            }
            s.bgColorId = -1; // màu nền luôn tự động
            s.fitCover = true; // luôn phủ kín khung
            var before = (s.frameW, s.frameH, s.fitCover);
            var fieldH = Gui.GetRowHeight();
            Gui.BeginHorizontal();
            Gui.Text("W");
            Gui.NumericEdit(ref s.frameW, new ImSize(64f, fieldH), default, 1, LevelEditorPicture.MinFrame, LevelEditorPicture.MaxFrame);
            Gui.Text("H");
            Gui.NumericEdit(ref s.frameH, new ImSize(64f, fieldH), default, 1, LevelEditorPicture.MinFrame, LevelEditorPicture.MaxFrame);
            if (Gui.Button("Theo tỉ lệ ảnh")) _picture.FitFrameToImage();
            Gui.EndHorizontal();
            if (before != (s.frameW, s.frameH, s.fitCover)) _picture.RefreshPreview();
        }

        // Mảnh đang chọn (click, Shift thêm bớt, kéo khung): gộp, bỏ chọn, chia mảnh
        private void DrawSelection()
        {
            Gui.Separator($"Đang chọn: {_picture.SelectedCount} mảnh");
            Gui.BeginHorizontal();
            if (_picture.SelectedCount >= 2 && Gui.Button("Gộp (M)")) _picture.MergeSelected();
            if (Gui.Button("Bỏ chọn (Esc)")) _picture.ClearSelection();
            Gui.EndHorizontal();
            DrawSubdivide();
        }

        // Chia mảnh đang chọn: số mảnh sau khi chia, mặc định gợi ý theo diện tích mảnh
        private void DrawSubdivide()
        {
            var n = _picture.SplitCount;
            Gui.Text($"Số mảnh: {n}", true);
            if (Gui.Slider(ref n, Subdivide.MinCount, Subdivide.MaxCount)) _picture.SplitCount = n;
            if (Gui.Button("Chia mảnh (K)")) _picture.SubdivideSelected();
        }

        // Chế độ Chọn / Vẽ / Tô; khi Vẽ thì thêm hàng chọn kiểu vẽ
        private void DrawModeSection()
        {
            DrawChoiceRow(ModeLabels, (int)_picture.Mode, i => _picture.SetMode((PictureMode)i));
            if (_picture.Mode == PictureMode.Draw) DrawChoiceRow(DrawLabels, (int)_picture.Draw, i => _picture.SetDrawKind((DrawKind)i));
        }

        // Hàng nút lựa chọn một trong nhiều; viền vàng = đang dùng
        private void DrawChoiceRow(string[] labels, int current, Action<int> pick)
        {
            var rowH = Gui.GetRowHeight();
            var gap = Gui.Style.Layout.Spacing;
            var w = (Gui.Layout.GetAvailableWidth() - gap * (labels.Length - 1)) / labels.Length;
            Gui.BeginHorizontal();
            for (var i = 0; i < labels.Length; i++)
            {
                var r = Gui.AddLayoutRectWithSpacing(w, rowH);
                if (Gui.Button(Gui.GetControlId(labels[i]), labels[i], r)) pick(i);
                if (i == current) Gui.Canvas.RectOutline(r, new Color32(250, 204, 21, 255), 2f, 4f);
            }
            Gui.EndHorizontal();
        }

        // Thứ tự hiển thị: mỗi hàng 5 màu cùng họ (id màu giữ nguyên)
        private static readonly int[] PaletteOrder =
        {
            0, 27, 35, 10, 15,   9, 17, 21, 8, 37,   1, 19, 32, 11, 26,   2, 36, 14, 31, 30,
            4, 34, 18, 3, 25,   16, 39, 33, 5, 22,   7, 38, 6, 24, 20,   12, 29, 28, 13, 23,
        };

        // Lưới cố định 5 cột x 8; ô đang chọn viền trắng, số trên ô = số mảnh dùng màu đó
        private void DrawPalette()
        {
            var counts = _picture.ColorCounts();
            var colors = ColorPalette.Colors;
            var swatch = Mathf.Min(MaxSwatch, (Gui.Layout.GetAvailableWidth() - Gui.Style.Layout.Spacing * (PerRow - 1)) / PerRow);
            for (var k = 0; k < colors.Length; k++)
            {
                var i = k < PaletteOrder.Length ? PaletteOrder[k] : k;
                if (k % PerRow == 0) Gui.BeginHorizontal();
                var r = Gui.AddLayoutRectWithSpacing(swatch, swatch);
                Color c = colors[i];
                if (Gui.ColorButton(0xC0100000u + (uint)i, c, r)) _picture.PickPaletteColor(i);
                if (i == _picture.CurrentColorId) Gui.Canvas.RectOutline(r, new Color32(255, 255, 255, 255), 3f);
                if (counts.TryGetValue(i, out var n)) FitNumber(n.ToString(), TextColorOn(c), r);
                if (k % PerRow == PerRow - 1 || k == colors.Length - 1) Gui.EndHorizontal();
            }
        }

        private void DrawViewSection()
        {
            var showColor = _picture.ShowColor;
            if (Gui.Checkbox(ref showColor, "Hiện màu (tắt = xám)")) _picture.SetShowColor(showColor);
            var glass = _picture.ShowGlass;
            if (Gui.Checkbox(ref glass, "Hiệu ứng kính")) _picture.SetShowGlass(glass);
            var values = _picture.ShowValues;
            if (Gui.Checkbox(ref values, "Hiện số cát")) _picture.SetShowValues(values);
            if (_picture.HasSource && _picture.Picture != null)
            {
                var alpha = _picture.SourceAlpha;
                Gui.Text($"Độ đậm ảnh gốc: {alpha:0.00} (0 = ẩn)", true);
                if (Gui.Slider(ref alpha, 0f, 1f)) _picture.SetSourceAlpha(alpha);
            }
        }

        private void FitNumber(ReadOnlySpan<char> text, Color32 color, ImRect rect)
        {
            var ts = new ImTextSettings(Gui.Style.Layout.TextSize, 0.5f, 0.5f);
            ts.Size = Gui.AutoSizeTextSlow(text, ts, new Vector2(rect.W - 4f, rect.H), 8f);
            Gui.Canvas.Text(text, color, rect, in ts);
        }

        private static Color32 TextColorOn(Color bg) =>
            bg.r * 0.299f + bg.g * 0.587f + bg.b * 0.114f > 0.6f ? new Color32(15, 23, 42, 255) : new Color32(248, 250, 252, 255);
    }
}
