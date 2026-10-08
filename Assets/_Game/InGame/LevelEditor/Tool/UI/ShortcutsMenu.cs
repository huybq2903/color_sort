using Falcon.Shared.BaseLevelEditor;
using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Nút "Phím tắt" trên menu bar: mở bảng liệt kê phím tắt và thao tác chuột của tool.</summary>
    [MenuItem("Phím tắt", 100)]
    public class ShortcutsMenuItem : IMenuItem
    {
        private static PopupShortcuts _popup;

        public void OnClick()
        {
            _popup ??= new PopupShortcuts();
            LevelEditorManager.Get<LevelEditorPopup>().Open(_popup);
        }
    }

    public class PopupShortcuts : PopupBase
    {
        private const string Content =
            "CHỌN\n" +
            "Click  Chọn mảnh hoặc nét  ·  Ctrl+click  Chọn thêm / bớt mảnh\n" +
            "Kéo trên nền  Chọn khung  ·  Click biên của mảnh đã chọn  Sửa biên\n" +
            "M  Gộp các mảnh đã chọn  ·  K  Chia mảnh đã chọn\n" +
            "Bấm màu trong Thông tin tranh  Chọn mọi mảnh màu đó\n" +
            "Delete  Xoá nét hoặc mảnh đang chọn  ·  Esc  Bỏ chọn\n\n" +
            "CẮT MẢNH (đang chọn đúng 1 mảnh)\n" +
            "Shift+click  Đặt điểm cắt (điểm đầu trên biên hoặc trong mảnh)\n" +
            "Từ điểm thứ 3, bấm gần điểm đầu  Khép kín đường cắt\n" +
            "Enter  Cắt  ·  Bỏ chọn mảnh hoặc Esc  Huỷ\n" +
            "Đường khép kín + \"Khoét lỗ trống\"  Tạo lỗ trong mảnh (lỗ không chạm biên, không có cát)\n" +
            "Click biên lỗ  Hiện thông tin lỗ, kéo điểm trên tranh để sửa  ·  Nút Xoá lỗ  Lấp lỗ lại\n\n" +
            "BÚT (Sửa biên)\n" +
            "Shift+click  Đặt điểm, hoặc chèn điểm lên đường khi sửa biên\n" +
            "Kéo đầu thanh cong  Uốn đường  ·  Delete  Xoá điểm\n" +
            "Enter  Chốt  ·  Esc  Huỷ\n\n" +
            "CHUNG\n" +
            "Z  Hoàn tác  ·  Y  Làm lại\n" +
            "Giữ chuột giữa kéo  Dịch chuyển màn hình";

        private const float MinW = 360f, MaxW = 640f, HPad = 16f, VPad = 10f, WinPad = 24f; // WinPad: chừa cho lề trong của cửa sổ Imui

        public override void Draw(ImGui gui)
        {
            var rowH = gui.GetRowHeight();
            var titleBarH = rowH + gui.Style.Layout.InnerSpacing * 2f;
            var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0f, 0f, true);
            var text = gui.MeasureTextSize(Content, in settings, new Vector2(MaxW - HPad * 2f - WinPad, 0f));
            var w = Mathf.Clamp(text.x + HPad * 2f + WinPad, MinW, MaxW);
            var h = titleBarH + VPad + text.y + VPad;
            var screen = gui.Canvas.ScreenSize;
            var rect = new ImRect((screen.x - w) / 2f, (screen.y - h) / 2f, w, h);

            var open = true;
            gui.BeginWindow("Phím tắt", ref open, rect, ImWindowFlag.NoResizing | ImWindowFlag.NoMoving);
            var layoutW = gui.GetLayoutWidth();
            gui.AddSpacing(VPad);
            var contentRect = gui.AddLayoutRect(layoutW, text.y);
            var textRect = new ImRect(contentRect.X + HPad, contentRect.Y, Mathf.Max(1f, contentRect.W - HPad * 2f), contentRect.H); // thụt hai bên cho chữ không dính viền
            gui.Canvas.Text(Content, new Color32(203, 213, 225, 255), textRect, in settings);
            gui.EndWindow();

            if (!open) IsOpen = false;
        }
    }
}
