using System.Collections.Generic;
using System.Linq;
using Falcon.InGame.Core;
using Falcon.Shared.BaseLevelEditor;
using Imui.Controls;
using Imui.Core;
using Imui.IO.Events;
using Imui.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Tab Hàng chờ trong panel Quy trình: Generate, màu còn lệch, cột hộp kéo thả, chọn nhiều bằng Ctrl+click hoặc kéo khung, chuột phải sửa màu và cát.</summary>
    internal sealed class BoxQueuePanel
    {
        private const float MaxColW = 112f, MinColW = 58f, CompactW = 80f, Gap = 8f, ChipH = 42f, ChipGap = 5f, DragPixels = 6f;
        private const float RowStep = ChipH + ChipGap, BalanceW = 64f, BarW = 4f, TopGap = 12f; // BalanceW đủ chứa "−1250" hoặc "✔"
        private const float SwSize = 32f, SwGap = 4f, PopPad = 10f, PopRowH = 28f;
        private static readonly Color32 White = new(255, 255, 255, 255), Red = new(248, 113, 113, 255), Dim = new(148, 163, 184, 255);

        private enum Popup { None, Box, New }

        private readonly LevelEditorBoxQueue _boxes;
        private readonly LevelEditorPicture _picture;
        private readonly Dictionary<int, int> _addValue = new(); // cát cho hộp mới của từng cột (không lưu)
        private readonly List<(string id, ImRect rect)> _chips = new();
        private readonly List<(int col, ImRect rect)> _pluses = new();
        private readonly List<ImRect> _controls = new(); // nút và ô số: không bắt đầu kéo khung từ đây
        private ImRect _area, _view;
        private float _firstChipTop, _colLeft, _colW = MaxColW, _colsH;
        private Dictionary<int, int> _need = new(), _have = new();
        private List<int> _colors = new(); // chỉ các màu còn lệch
        private int _perRow = 1, _colCount, _slotCount, _scrollRows, _maxScroll; // _slotCount = số cột hiện có cộng cột Mới (nếu chưa đủ tối đa)
        private bool _compact; // cột hẹp: bỏ ô cát từng cột, dùng một ô cát chung cạnh Generate

        private string _pressId;
        private Vector2 _pressPos;
        private bool _additive, _dragging, _bandPending, _banding;

        private Popup _popup;
        private int _popupKey; // cột của nút + đang mở popup (khi Popup.New)
        private Vector2 _popupAt;
        private ImRect _pRect, _pMinus, _pVal, _pPlus;
        private ImRect[] _pSw = new ImRect[0];

        public BoxQueuePanel(LevelEditorBoxQueue boxes, LevelEditorPicture picture)
        {
            _boxes = boxes;
            _picture = picture;
        }

        // Vẽ vào phần còn trống của window đang mở
        public void Draw(ImGui gui, float leftW, float rightW)
        {
            var q = _boxes.Queue;
            if (q == null) return;
            HandleInput(gui, leftW, rightW);

            var rowH = gui.GetRowHeight();
            _need = _picture.ColorSand();
            _have = _boxes.ColorSand();
            _colors = _need.Keys.Union(_have.Keys).Where(c => _need.GetValueOrDefault(c) != _have.GetValueOrDefault(c)).OrderBy(c => c).ToList();
            _perRow = Mathf.Max(1, Mathf.FloorToInt((gui.Layout.GetAvailableWidth() + 4f) / (BalanceW + 4f)));
            var genH = TopGap + rowH + 6f; // chừa khoảng trống phía trên để Generate không dính hàng nút tab
            var headH = genH + (_colors.Count == 0 ? 0f : Mathf.CeilToInt(_colors.Count / (float)_perRow) * (rowH + 4f) + 2f); // ô màu tự xuống hàng
            _area = gui.AddLayoutRect(gui.Layout.GetAvailableWidth(), Mathf.Max(headH + RowStep, gui.Layout.GetAvailableHeight() - 2f));
            _view = new ImRect(_area.X, _area.Y, _area.W, _area.H - headH);
            _colCount = q.ColumnCount;
            _slotCount = Mathf.Min(_colCount + 1, BoxQueueProperty.MaxColumns);
            var bodyW = _area.W - BarW - 4f;
            _colW = Mathf.Clamp((bodyW - Gap * (_slotCount - 1)) / _slotCount, MinColW, MaxColW);
            _compact = _colW < CompactW;
            var rows = Mathf.Max(1, Enumerable.Range(0, _colCount).Max(c => q.Column(c).Count));
            _colsH = 18f + rows * RowStep + rowH + 6f; // tiêu đề cột, các hộp, hàng thêm hộp
            _maxScroll = Mathf.Max(0, Mathf.CeilToInt((_colsH - _view.H) / RowStep));
            _scrollRows = Mathf.Clamp(_scrollRows, 0, _maxScroll);
            _chips.Clear();
            _pluses.Clear();
            _controls.Clear();
            DrawColumns(gui, q, rowH, headH, bodyW);
            DrawScrollBar(gui);
            DrawTop(gui, rowH);
            DrawBalance(gui, rowH, genH);
            DrawOverlay(gui);
        }

        // y tính từ cạnh trên vùng nội dung xuống
        private ImRect AtTop(float x, float topOffset, float w, float h) => new(x, _area.Y + _area.H - topOffset - h, w, h);

        // Hàng đầu: ô cát chung (khi cột hẹp) bên trái, Generate ở giữa
        private void DrawTop(ImGui gui, float rowH)
        {
            var gen = AtTop(_area.X + (_area.W - 100f) / 2f, TopGap, 100f, rowH);
            _controls.Add(gen);
            if (gui.Button(gui.GetControlId("boxgen"), "Generate", gen)) GenerateBoxes(_boxes.Queue);
            if (!_compact) return;
            var num = AtTop(_area.X, TopGap, 56f, rowH);
            _controls.Add(num);
            var v = _addValue.GetValueOrDefault(0, 100);
            if (ImNumericEdit.NumericEdit(gui, ref v, num, default, PieceValue.Step, PieceValue.Min, PieceValue.Max)) _addValue[0] = v;
        }

        // Ô màu còn lệch: đủ rộng cho chữ, quá dài thì xuống hàng; màu đã đủ thì không hiện
        private void DrawBalance(ImGui gui, float rowH, float genH)
        {
            var ts = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
            for (var i = 0; i < _colors.Count; i++)
            {
                var c = _colors[i];
                var d = _have.GetValueOrDefault(c) - _need.GetValueOrDefault(c);
                var r = AtTop(_area.X + i % _perRow * (BalanceW + 4f), genH + i / _perRow * (rowH + 4f), BalanceW, rowH);
                _controls.Add(r);
                var col = (Color)ColorPalette.Get(c);
                if (gui.ColorButton(gui.GetControlId("boxcol" + c), col, r)) _boxes.NewColorId = c;
                gui.Canvas.Text($"{(d > 0 ? "+" : "−")}{Mathf.Abs(d)}", ColorSortEditorUI.TextColorOn(col), r, in ts);
                gui.Canvas.RectOutline(r, Red, 2f);
                if (c == _boxes.NewColorId) gui.Canvas.RectOutline(r, White, 3f);
            }
        }

        private void DrawColumns(ImGui gui, BoxQueueProperty q, float rowH, float headH, float bodyW)
        {
            var total = _slotCount * _colW + (_slotCount - 1) * Gap;
            _colLeft = _area.X + Mathf.Max(0f, (bodyW - total) / 2f);
            var top = headH - _scrollRows * RowStep; // cuộn theo cả hàng: phần nằm trên vùng nhìn thì bỏ qua
            _firstChipTop = top + 18f;
            var viewTop = _view.Y + _view.H;
            var viewBottom = _view.Y; // phần dưới vùng nhìn thì không vẽ
            var ts = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
            var small = new ImTextSettings(gui.Style.Layout.TextSize * 0.85f, 0.5f, 0.5f);
            for (var c = 0; c < _slotCount; c++) // c == _colCount là cột mới (khi chưa đủ số cột tối đa)
            {
                var x = _colLeft + c * (_colW + Gap);
                var spare = c == _colCount;
                if (_scrollRows == 0) gui.Canvas.Text(spare ? "Mới" : $"Cột {c}", Dim, AtTop(x, top, _colW, 18f), in small);
                var list = spare ? new List<BoxData>() : q.Column(c);
                for (var r = 0; r < list.Count; r++)
                {
                    var b = list[r];
                    var rc = AtTop(x, _firstChipTop + r * RowStep, _colW, ChipH);
                    if (rc.Y + rc.H > viewTop + 0.5f || rc.Y < viewBottom - 0.5f) continue; // ngoài vùng nhìn
                    _chips.Add((b.id, rc));
                    var col = (Color)ColorPalette.Get(b.colorId);
                    var ghost = _dragging && b.id == _pressId;
                    gui.Canvas.Rect(rc, ghost ? new Color32(255, 255, 255, 25) : (Color32)col);
                    gui.Canvas.RectOutline(rc, new Color32(11, 11, 11, 255), 1f);
                    if (!ghost) gui.Canvas.Text(b.value.ToString(), ColorSortEditorUI.TextColorOn(col), rc, in ts);
                    if (_boxes.IsSelected(b.id)) gui.Canvas.RectOutline(rc, White, 3f);
                }
                var slots = Mathf.Max(1, list.Count); // cột trống vẫn có một ô nét đứt
                if (list.Count == 0)
                {
                    var rc = AtTop(x, _firstChipTop, _colW, ChipH);
                    if (rc.Y + rc.H <= viewTop + 0.5f && rc.Y >= viewBottom - 0.5f) gui.Canvas.RectOutline(rc, new Color32(100, 116, 139, 255), 1f);
                }
                var addTop = _firstChipTop + slots * RowStep;
                var num = AtTop(x, addTop, _colW - 34f, rowH);
                var plus = _compact ? AtTop(x, addTop, _colW, rowH) : AtTop(x + _colW - 30f, addTop, 30f, rowH);
                if (plus.Y + plus.H > viewTop + 0.5f || plus.Y < viewBottom - 0.5f) continue;
                _controls.Add(plus);
                _pluses.Add((c, plus));
                var key = _compact ? 0 : c;
                var v = _addValue.GetValueOrDefault(key, 100);
                if (!_compact)
                {
                    _controls.Add(num);
                    if (ImNumericEdit.NumericEdit(gui, ref v, num, default, PieceValue.Step, PieceValue.Min, PieceValue.Max)) _addValue[key] = v;
                }
                var pc = (Color)ColorPalette.Get(_boxes.NewColorId);
                if (gui.ColorButton(gui.GetControlId("boxadd" + c), pc, plus)) _boxes.Add(c, _boxes.NewColorId, v);
                gui.Canvas.Text("+", ColorSortEditorUI.TextColorOn(pc), plus, in ts);
            }
        }

        // Thanh cuộn chỉ vị trí: chỉ hiện khi còn hàng bị ẩn, lăn chuột để cuộn
        private void DrawScrollBar(ImGui gui)
        {
            if (_maxScroll == 0) return;
            var track = new ImRect(_view.X + _view.W - BarW, _view.Y, BarW, _view.H);
            gui.Canvas.Rect(track, new Color32(255, 255, 255, 30));
            var thumbH = Mathf.Max(18f, track.H * track.H / _colsH);
            var t = _scrollRows / (float)_maxScroll;
            gui.Canvas.Rect(new ImRect(track.X, track.Y + track.H - thumbH - t * (track.H - thumbH), BarW, thumbH), new Color32(255, 255, 255, 140));
        }

        // Khung chọn đang kéo, hộp đang kéo theo chuột, chỗ sẽ thả và popup chuột phải
        private void DrawOverlay(ImGui gui)
        {
            var mp = gui.Input.MousePosition;
            gui.Canvas.PushOrder(ImWindow.WINDOW_ORDER_OFFSET * 8);
            if (_banding)
            {
                var band = Band(mp);
                gui.Canvas.Rect(band, new Color32(255, 255, 255, 30));
                gui.Canvas.RectOutline(band, new Color32(255, 255, 255, 160), 1f);
            }
            if (_dragging)
            {
                var b = _boxes.Queue.list.Find(x => x.id == _pressId);
                if (b != null)
                {
                    var col = (Color)ColorPalette.Get(b.colorId);
                    var rc = new ImRect(mp.x - _colW / 2f, mp.y - ChipH / 2f, _colW, ChipH);
                    gui.Canvas.Rect(rc, new Color32((byte)(col.r * 255), (byte)(col.g * 255), (byte)(col.b * 255), 215));
                    gui.Canvas.RectOutline(rc, White, 2f);
                    var ts = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
                    gui.Canvas.Text(b.value.ToString(), ColorSortEditorUI.TextColorOn(col), rc, in ts);
                }
                if (DropTarget(mp, out var c, out var r))
                {
                    var slot = AtTop(_colLeft + c * (_colW + Gap), _firstChipTop + r * RowStep - ChipGap / 2f, _colW, 3f);
                    gui.Canvas.Rect(slot, new Color32(250, 204, 21, 255));
                }
            }
            if (_popup != Popup.None) DrawPopup(gui);
            gui.Canvas.PopOrder();
        }

        // ---- popup chuột phải: bảng màu và số cát ----

        private int PopupColor()
        {
            if (_popup == Popup.New) return _boxes.NewColorId;
            var colors = _boxes.SelectedColorIds();
            return colors.Count == 1 ? colors[0] : -1;
        }

        private bool PopupValue(out int value)
        {
            if (_popup == Popup.Box) return _boxes.TryGetSelectedValue(out value);
            value = _addValue.GetValueOrDefault(_popupKey, 100);
            return true;
        }

        private void LayoutPopup()
        {
            var count = ColorPalette.Count;
            const int perRow = 5;
            var rows = Mathf.CeilToInt(count / (float)perRow);
            var w = PopPad * 2f + perRow * SwSize + (perRow - 1) * SwGap;
            var h = PopPad * 2f + rows * SwSize + (rows - 1) * SwGap + 10f + PopRowH;
            // Window cắt mọi thứ vẽ ngoài nó, nên popup phải nằm gọn trong vùng nội dung
            var x = Mathf.Clamp(_popupAt.x, _area.X, Mathf.Max(_area.X, _area.X + _area.W - w));
            var y = Mathf.Clamp(_popupAt.y - h, _area.Y, Mathf.Max(_area.Y, _area.Y + _area.H - h)); // cạnh trên popup đặt tại con trỏ
            _pRect = new ImRect(x, y, w, h);
            if (_pSw.Length != count) _pSw = new ImRect[count];
            var top = y + h - PopPad;
            for (var k = 0; k < count; k++)
                _pSw[k] = new ImRect(x + PopPad + k % perRow * (SwSize + SwGap), top - SwSize - k / perRow * (SwSize + SwGap), SwSize, SwSize);
            var rowY = y + PopPad;
            _pMinus = new ImRect(x + PopPad, rowY, 44f, PopRowH);
            _pPlus = new ImRect(x + w - PopPad - 44f, rowY, 44f, PopRowH);
            _pVal = new ImRect(_pMinus.X + 48f, rowY, _pPlus.X - _pMinus.X - 52f, PopRowH);
        }

        private void DrawPopup(ImGui gui)
        {
            LayoutPopup();
            var ts = new ImTextSettings(gui.Style.Layout.TextSize, 0.5f, 0.5f);
            gui.Canvas.Rect(_pRect, new Color32(38, 38, 38, 250));
            gui.Canvas.RectOutline(_pRect, new Color32(255, 255, 255, 70), 1f);
            var cur = PopupColor();
            for (var k = 0; k < _pSw.Length; k++)
            {
                var id = k < ColorSortEditorUI.PaletteOrder.Length ? ColorSortEditorUI.PaletteOrder[k] : k;
                var sw = ColorPalette.Get(id);
                if (!_need.ContainsKey(id)) sw.a = 13; // màu không có trong tranh: mờ còn 5%, không bấm được
                gui.Canvas.Rect(_pSw[k], sw);
                if (id == cur) gui.Canvas.RectOutline(_pSw[k], White, 3f);
            }
            var btn = new Color32(59, 59, 59, 255);
            gui.Canvas.Rect(_pMinus, btn);
            gui.Canvas.Rect(_pPlus, btn);
            gui.Canvas.Text("−", White, _pMinus, in ts);
            gui.Canvas.Text("+", White, _pPlus, in ts);
            gui.Canvas.Text(PopupValue(out var v) ? v.ToString() : "—", White, _pVal, in ts);
        }

        // Bấm trái trong popup: chọn màu hoặc đổi cát (màu không có trong tranh thì bỏ qua)
        private void PopupClick(Vector2 mp)
        {
            for (var k = 0; k < _pSw.Length; k++)
                if (_pSw[k].Contains(mp))
                {
                    var id = k < ColorSortEditorUI.PaletteOrder.Length ? ColorSortEditorUI.PaletteOrder[k] : k;
                    if (!_need.ContainsKey(id)) return;
                    if (_popup == Popup.Box) _boxes.SetSelectedColor(id);
                    else _boxes.NewColorId = id;
                    return;
                }
            if (_pMinus.Contains(mp)) StepPopup(-1);
            else if (_pPlus.Contains(mp)) StepPopup(1);
        }

        private void StepPopup(int dir)
        {
            if (_popup == Popup.Box) _boxes.StepSelectedValue(dir);
            else _addValue[_popupKey] = Mathf.Clamp(_addValue.GetValueOrDefault(_popupKey, 100) + dir * PieceValue.Step, PieceValue.Min, PieceValue.Max);
        }

        private void OpenPopup(Vector2 mp)
        {
            var chip = _chips.FindLast(c => c.rect.Contains(mp));
            if (chip.id != null)
            {
                if (!_boxes.IsSelected(chip.id)) _boxes.SelectMany(new[] { chip.id }, false); // hộp chưa chọn thì chọn riêng hộp đó
                _popup = Popup.Box;
            }
            else
            {
                var plus = _pluses.FindLast(p => p.rect.Contains(mp));
                if (plus.rect.W <= 0f) return;
                _popup = Popup.New;
                _popupKey = _compact ? 0 : plus.col;
            }
            _popupAt = mp;
        }

        // ---- kéo thả và chọn ----

        private ImRect Band(Vector2 mp) => new(Mathf.Min(_pressPos.x, mp.x), Mathf.Min(_pressPos.y, mp.y), Mathf.Abs(mp.x - _pressPos.x), Mathf.Abs(mp.y - _pressPos.y));

        // Cột và hàng chèn tại con trỏ; hàng = số hộp (không tính hộp đang kéo) nằm phía trên con trỏ, cộng số hàng đã cuộn
        private bool DropTarget(Vector2 mp, out int col, out int row)
        {
            row = 0;
            col = Mathf.FloorToInt((mp.x - _colLeft + Gap / 2f) / (_colW + Gap));
            if (col < 0 || col >= _slotCount || !_view.Contains(mp)) return false;
            var x = _colLeft + col * (_colW + Gap);
            foreach (var (id, rc) in _chips)
                if (id != _pressId && rc.X >= x - 1f && rc.X <= x + 1f && rc.Y + rc.H / 2f > mp.y) row++;
            row += _scrollRows;
            return true;
        }

        // Chuột đọc thẳng từ Input System: Imui không báo chuột bấm ngoài các window của nó (vd lên tranh)
        private void HandleInput(ImGui gui, float leftW, float rightW)
        {
            var m = Mouse.current;
            if (m == null) return;
            var screen = gui.Canvas.ScreenSize;
            var mp = m.position.ReadValue() * (screen.x / Mathf.Max(1f, Screen.width));
            var scroll = m.scroll.ReadValue().y;
            if (scroll != 0f && _area.Contains(mp)) _scrollRows = Mathf.Max(0, _scrollRows - (scroll > 0f ? 1 : -1)); // Draw kẹp phía trên
            var kb = Keyboard.current;
            var additive = kb != null && (kb.ctrlKey.isPressed || kb.leftCommandKey.isPressed);

            var left = m.leftButton.wasPressedThisFrame;
            if (left || m.rightButton.wasPressedThisFrame)
            {
                _pressId = null;
                _dragging = _banding = _bandPending = false;
                if (_popup != Popup.None)
                {
                    if (_pRect.Contains(mp))
                    {
                        if (left) PopupClick(mp);
                        return;
                    }
                    _popup = Popup.None; // bấm ra ngoài: đóng
                    if (left) return;
                }
                if (!left)
                {
                    if (_view.Contains(mp)) OpenPopup(mp);
                    return;
                }
                if (_view.Contains(mp))
                {
                    var hit = _chips.FindLast(c => c.rect.Contains(mp));
                    _pressPos = mp;
                    _additive = additive;
                    if (hit.id != null) _pressId = hit.id;
                    else if (!_controls.Any(c => c.Contains(mp))) _bandPending = true;
                }
                else if (OnCanvas(gui, mp, screen, leftW, rightW)) _boxes.ClearSelection(); // bấm lên tranh: bỏ chọn hộp
                if (!m.leftButton.wasReleasedThisFrame) return;
            }
            if (_popup != Popup.None) return;
            if (m.leftButton.isPressed)
            {
                if (_pressId != null && !_dragging && Vector2.Distance(mp, _pressPos) > DragPixels)
                {
                    _dragging = true;
                    _boxes.SelectMany(new[] { _pressId }, false); // kéo hộp nào thì chọn đúng hộp đó
                }
                if (_bandPending && Vector2.Distance(mp, _pressPos) > DragPixels) _banding = true;
            }
            else if (m.leftButton.wasReleasedThisFrame)
            {
                if (_dragging)
                {
                    if (DropTarget(mp, out var col, out var row)) _boxes.Move(_pressId, col, row);
                }
                else if (_pressId != null) _boxes.Select(_pressId, _additive);
                else if (_banding)
                {
                    var band = Band(mp);
                    _boxes.SelectMany(_chips.Where(c => Overlaps(band, c.rect)).Select(c => c.id), _additive);
                }
                else if (_bandPending && !_additive) _boxes.ClearSelection();
                _pressId = null;
                _dragging = _banding = _bandPending = false;
            }
            else if (_pressId != null || _dragging || _banding || _bandPending)
            {
                _pressId = null; // nút đã nhả ngoài khung hình theo dõi: bỏ trạng thái treo
                _dragging = _banding = _bandPending = false;
            }
        }

        // Vùng giữa hai panel, dưới menu và ô tên file: nơi tranh nằm
        private static bool OnCanvas(ImGui gui, Vector2 mp, Vector2 screen, float leftW, float rightW)
        {
            var rowH = gui.GetRowHeight();
            var menuH = gui.Style.Layout.InnerSpacing * 2f + rowH;
            return mp.x > leftW + 10f && mp.x < screen.x - rightW - 10f && mp.y > rowH && mp.y < screen.y - menuH - rowH;
        }

        private static bool Overlaps(ImRect a, ImRect b) => a.X < b.X + b.W && a.X + a.W > b.X && a.Y < b.Y + b.H && a.Y + a.H > b.Y;

        private void GenerateBoxes(BoxQueueProperty q)
        {
            if (_need.Count == 0)
            {
                LevelEditorMainUI.Warn("Chưa có tranh");
                return;
            }
            _boxes.Generate(_need, Mathf.Max(BoxQueueProperty.DefaultColumns, q.columns));
        }
    }
}
