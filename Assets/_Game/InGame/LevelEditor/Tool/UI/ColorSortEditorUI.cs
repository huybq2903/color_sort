using System;
using System.Collections;
using System.Linq;
using Falcon.InGame.Core;
using Falcon.Shared.BaseInGame;
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
        private static readonly string[] CutKindLabels = { "Tách", "Vẽ lỗ" };
        private static readonly string[] ValueLabels = { "50", "100", "150", "200", "250", "300" };

        private LevelEditorPicture _picture;
        private readonly LineData _lineEntity = new();
        private readonly HoleData _holeEntity = new();
        private static readonly PictureInfoData InfoEntity = new();
        private static readonly NullData NoneEntity = new() { id = "none" };
        private PopupLoading _loading;
        private const float UpdateCheckSeconds = 1800f; // 30 phút hỏi GitHub một lần
        private readonly AppUpdater _updater = new();

        public override void Initialized()
        {
            base.Initialized();
            _picture = LevelEditorManager.Get<LevelEditorPicture>();
            RegisterPictureInspectors();
            _loading = new PopupLoading(() => _picture.BusyText);
            _saveLoad.ThumbnailCapture = () => PictureThumbnail.Render(_picture.Picture);
#if !UNITY_EDITOR
            StartCoroutine(CheckUpdatesRepeatedly());
#endif
        }

        // Có bản mới chỉ hiện thành mục "Update vX" trên menu bar, không bật popup
        private IEnumerator CheckUpdatesRepeatedly()
        {
            var wait = new WaitForSecondsRealtime(UpdateCheckSeconds);
            while (true)
            {
                yield return _updater.Check();
                yield return wait;
            }
        }

        private void StartUpdate() => ConfirmDiscardChanges(() => StartCoroutine(_updater.Apply()));

        protected override void DrawMenuBarExtra()
        {
            if (_updater.Tag != null && ImMenuBar.Button(Gui, $"Update {_updater.Tag}")) StartUpdate();
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

            Gui.BeginWindow("Picture", rect, ImWindowFlag.NoMovingAndResizing);
            try
            {
                DrawSourceSection();
                Gui.Separator("Xem");
                DrawViewSection();
            }
            finally
            {
                Gui.EndWindow(); // luôn đóng window để Imui không lệch stack Begin/End
            }
        }

        // Inspector bên phải: mỗi trạng thái của tranh (mảnh, đường cắt, nét, thông tin chung) là một inspector đăng ký theo kiểu selection
        protected override IEntityDataContainer CurrentSelection
        {
            get
            {
                if (_picture == null) return null;
                var p = _picture.Picture;
                if (p == null) return PictureSelection.Of<NoPictureSelection>(NoneEntity);
                var first = _picture.PrimarySelected;
                if (_picture.EditingHole >= 0 && first >= 0 && first < p.regions.Count)
                {
                    _holeEntity.region = first;
                    _holeEntity.hole = _picture.EditingHole;
                    _holeEntity.id = $"{p.regions[first].id}:h{_holeEntity.hole + 1}";
                    return PictureSelection.Of<HoleSelection>(_holeEntity);
                }
                if (_picture.SelectedCount > 0 && first >= 0 && first < p.regions.Count) return PictureSelection.Of<PieceSelection>(p.regions[first]);
                if (_picture.InspectedLine >= 0 || _picture.IsLineTool)
                {
                    var li = _picture.InspectedLine;
                    _lineEntity.index = li;
                    _lineEntity.id = li >= 0 && p.lineIds != null && li < p.lineIds.Count ? p.lineIds[li] : "new";
                    return PictureSelection.Of<LineSelection>(_lineEntity);
                }
                return PictureSelection.Of<PictureInfoSelection>(InfoEntity);
            }
        }

        private void RegisterPictureInspectors()
        {
            RegisterInspector(new PictureInspector<NoPictureSelection>(() => Gui.Text("Chưa có tranh", true)));
            RegisterInspector(new PictureInspector<PieceSelection>(() => DrawPieceInspector(_picture.Picture)));
            RegisterInspector(new PictureInspector<HoleSelection>(() => DrawHoleInspector(_holeEntity)));
            RegisterInspector(new PictureInspector<LineSelection>(() => DrawLineInspector(_picture.Picture)));
            RegisterInspector(new PictureInspector<PictureInfoSelection>(() => DrawPictureInspector(_picture.Picture)));
        }

        private void DrawInspectorHeader(Color32 swatch, string title, string sub)
        {
            var rowH = Gui.GetRowHeight();
            Gui.BeginHorizontal();
            var r = Gui.AddLayoutRectWithSpacing(rowH * 1.4f, rowH * 1.4f);
            Gui.Canvas.Rect(r, swatch);
            Gui.Canvas.RectOutline(r, new Color32(255, 255, 255, 60), 1f);
            Gui.Text(title);
            Gui.EndHorizontal();
            if (!string.IsNullOrEmpty(sub)) Gui.Text(sub, true);
        }

        private void DrawPieceInspector(PictureProperty p)
        {
            var sel = _picture.SelectedCount;
            var idx = _picture.SelectedIndex;
            var colors = p.regions.Where((r, i) => _picture.IsSelected(i)).Select(r => r.colorId).Distinct().ToList();
            var swatch = colors.Count == 1 ? ColorPalette.Get(colors[0]) : new Color32(120, 130, 150, 255);
            DrawInspectorHeader(swatch, sel == 1 ? $"Mảnh #{idx + 1}" : $"{sel} mảnh", colors.Count == 1 ? $"Màu #{colors[0]}" : $"{colors.Count} màu khác nhau");

            Gui.Separator("Bảng màu (bấm để đổi màu)");
            DrawPalette(_picture.PickPaletteColor, false);

            Gui.Separator("Số cát");
            var known = _picture.TryGetSelectedValue(out var value);
            var chip = known ? value / PieceValue.Step - 1 : -1;
            DrawChoiceRow(ValueLabels, chip, i => _picture.SetSelectedValue((i + 1) * PieceValue.Step));
            var rowH = Gui.GetRowHeight();
            Gui.BeginHorizontal();
            if (Gui.Button("-", new ImSize(44f, rowH))) _picture.StepSelectedValue(-1);
            Gui.Text(known ? value.ToString() : "—");
            if (Gui.Button("+", new ImSize(44f, rowH))) _picture.StepSelectedValue(1);
            Gui.EndHorizontal();

            Gui.Text(sel == 1 ? $"Gợi ý theo diện tích: {_picture.SuggestedValue(idx)}" : "Gợi ý theo diện tích khác nhau từng mảnh", true);

            DrawSelection();
        }

        // Đường cắt của mảnh đang chọn: rãnh đen chỉnh ngay lúc đặt điểm (dải đen mờ trên đường cắt) và hai nút chọn kết quả Tách / Vẽ lỗ ở dưới
        private void DrawCutSection(PictureProperty p)
        {
            Gui.Text(_picture.Splitting ? "Đang cắt: Shift+click đặt điểm, bấm gần điểm đầu để khép kín · Enter cắt · Esc huỷ" : "Shift+click để đặt điểm cắt mảnh này · Enter cắt", true);
            if (_picture.CutAsHole)
                Gui.Text("Vẽ lỗ: đường khép kín, phần trong vòng bị khoét thành lỗ. Lỗ không có cát và không được chạm biên mảnh.", true);
            else if (!p.gen.inkGaps)
                Gui.Text("Tranh không có khe nét: đường cắt dùng viền chung của hai mảnh.", true);
            else
            {
                Gui.Text("Rãnh đen giữa hai mảnh sau khi tách", true);
                DrawGrooveEditor(_picture.CutThickness, v => _picture.CutThickness = v);
                Gui.Text(_picture.PenAnchorCount >= 2 ? "Dải đen mờ trên đường cắt là rãnh sẽ tạo ra." : "Đặt ít nhất 2 điểm cắt để thấy dải rãnh.", true);
            }
            DrawChoiceRow(CutKindLabels, _picture.CutAsHole ? 1 : 0, i => _picture.CutAsHole = i == 1);
        }

        // -, ô số, +, thanh trượt cho độ rộng rãnh (ô lưới); set được gọi mỗi khi giá trị đổi
        private void DrawGrooveEditor(int current, Action<int> set)
        {
            var rowH = Gui.GetRowHeight();
            var v = current;
            Gui.BeginHorizontal();
            if (Gui.Button("-", new ImSize(44f, rowH))) v = Mathf.Max(0, v - 1);
            Gui.NumericEdit(ref v, new ImSize(80f, rowH), default, 1, 0, 80);
            if (Gui.Button("+", new ImSize(44f, rowH))) v = Mathf.Min(80, v + 1);
            Gui.Text("ô lưới");
            Gui.EndHorizontal();
            Gui.Slider(ref v, 0, 40);
            if (v != current) set(v);
        }

        // Nét đang chọn: thông tin và độ dày của nét đó; chưa chọn nét nào mà đang ở bút vẽ: độ dày áp cho nét sắp vẽ
        private void DrawLineInspector(PictureProperty p)
        {
            var sel = _picture.InspectedLine;
            var editing = sel >= 0 && sel < p.lines.Count;
            DrawInspectorHeader(new Color32(15, 23, 42, 255), editing ? $"Nét #{sel + 1}" : "Nét sắp vẽ", editing ? (_picture.SelectedLine >= 0 ? "Delete để xoá nét" : "Đang sửa nét: Enter chốt · Esc huỷ") : "Độ dày áp cho nét vẽ tiếp theo");
            var isDot = false;
            if (editing)
            {
                var l = p.lines[sel];
                var len = 0f;
                for (var k = 0; k + 3 < l.Length; k += 2) len += Vector2.Distance(new Vector2(l[k], l[k + 1]), new Vector2(l[k + 2], l[k + 3])) / Mathf.Max(1, p.unit);
                isDot = sel < p.lineWidths.Count && p.lineWidths[sel] > 0 && l.Length == 4;
                Gui.Text(isDot ? "Chấm" : $"{l.Length / 2} điểm · dài {len:0.#} ô", true);
            }
            if (!isDot)
            {
                Gui.Separator("Độ dày (% viền, 0 = mặc định)");
                var rowH = Gui.GetRowHeight();
                var cur = editing ? _picture.SelectedLineThickness : _picture.NewLineThickness;
                var v = cur;
                var baseV = cur == 0 ? 100 : cur;
                Gui.BeginHorizontal();
                if (Gui.Button("-", new ImSize(44f, rowH))) v = Mathf.Max(10, baseV - 10);
                Gui.NumericEdit(ref v, new ImSize(80f, rowH), default, 10, 0, 800);
                if (Gui.Button("+", new ImSize(44f, rowH))) v = Mathf.Min(800, baseV + 10);
                if (Gui.Button("Mặc định")) v = 0;
                Gui.EndHorizontal();
                if (v != cur)
                {
                    if (editing) _picture.SetSelectedLineThickness(v);
                    else _picture.NewLineThickness = v;
                }
            }
            if (editing && _picture.SelectedLine >= 0 && Gui.Button("Xoá nét (Delete)")) _picture.DeleteSelectedLine();
        }

        private void DrawPictureInspector(PictureProperty p)
        {
            DrawInspectorHeader(new Color32(120, 130, 150, 255), "Tranh", "Click một mảnh để xem và sửa thuộc tính.");
            Gui.Separator("Thông tin chung");
            Gui.Text($"Khung: {p.width}x{p.height}", true);
            Gui.Text($"{p.regions.Count} mảnh · {p.regions.Select(r => r.colorId).Distinct().Count()} màu", true);
            Gui.Text($"Tổng cát: {p.regions.Sum(r => r.value)}", true);
            Gui.Text($"Đặt tay: {p.regions.Count(r => r.valueManual)} mảnh", true);

            Gui.Separator("Số cát theo màu (bấm màu để chọn các mảnh)");
            DrawPalette(_picture.SelectByColor, true);
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
            Gui.Separator("Thao tác");
            if (_picture.SelectedCount == 1) DrawCutSection(_picture.Picture);
            if (_picture.SelectedCount >= 2 && Gui.Button("Gộp (M)")) _picture.MergeSelected();
            DrawSubdivide();
            if (Gui.Button(_picture.SelectedCount >= 2 ? "Xoá các mảnh (Del)" : "Xoá mảnh (Del)")) _picture.DeleteSelectedPieces();
        }

        // Lỗ đang chọn (click biên lỗ): thông tin như một entity, xoá lỗ; sửa biên bằng cách kéo điểm trên tranh
        private void DrawHoleInspector(HoleData hole)
        {
            DrawInspectorHeader(new Color32(15, 23, 42, 255), $"Lỗ #{hole.hole + 1}", $"Thuộc mảnh #{hole.region + 1}");
            var contains = _picture.HoleContains(hole.hole);
            if (contains.Length > 0) Gui.Text($"Bên trong lỗ có mảnh {contains}", true);
            Gui.Text("Kéo điểm hoặc đầu thanh cong trên tranh để sửa · Shift+click biên lỗ thêm điểm · Enter chốt · Esc huỷ", true);
            Gui.Separator("Thao tác");
            if (Gui.Button("Xoá lỗ")) _picture.DeleteHole(hole.hole);
        }

        // Chia mảnh đang chọn: số mảnh sau khi chia, mặc định gợi ý theo diện tích mảnh
        private void DrawSubdivide()
        {
            var n = _picture.SplitCount;
            Gui.Text($"Số mảnh: {n}", true);
            if (Gui.Slider(ref n, Subdivide.MinCount, Subdivide.MaxCount)) _picture.SplitCount = n;
            if (Gui.Button("Chia mảnh (K)")) _picture.SubdivideSelected();
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
        private void DrawPalette(Action<int> pick, bool showCounts)
        {
            var counts = showCounts ? _picture.ColorSand() : null;
            var colors = ColorPalette.Colors;
            var swatch = Mathf.Min(MaxSwatch, (Gui.Layout.GetAvailableWidth() - Gui.Style.Layout.Spacing * (PerRow - 1)) / PerRow);
            for (var k = 0; k < colors.Length; k++)
            {
                var i = k < PaletteOrder.Length ? PaletteOrder[k] : k;
                if (k % PerRow == 0) Gui.BeginHorizontal();
                var r = Gui.AddLayoutRectWithSpacing(swatch, swatch);
                Color c = colors[i];
                if (pick != null)
                {
                    if (Gui.ColorButton(0xC0100000u + (uint)i, c, r)) pick(i);
                    if (!showCounts && i == _picture.CurrentColorId) Gui.Canvas.RectOutline(r, new Color32(255, 255, 255, 255), 3f);
                }
                else
                {
                    Gui.Canvas.Rect(r, (Color32)c); // chỉ xem: không bấm được
                }
                if (counts != null && counts.TryGetValue(i, out var n)) FitNumber(n.ToString(), TextColorOn(c), r);
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
