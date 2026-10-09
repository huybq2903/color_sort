using System;
using System.Linq;
using Imui.Controls;
using Imui.Core;
using Imui.IO.Touch;
using Imui.Rendering;
using Imui.Style;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falcon.Shared.BaseLevelEditor
{
    public partial class LevelEditorAgent
    {
        private const float HeaderH = 42f, InputH = 96f, ChipH = 30f, Space = 8f, SidePad = 16f, TopPad = 14f, TitleScale = 1.25f;
        private static readonly Color32 CEdge = new(88, 85, 78, 255), CBg = new(38, 38, 36, 255), CCard = new(45, 44, 41, 255), CLine = new(63, 61, 57, 255), CBubble = new(54, 53, 50, 255);
        private static readonly Color32 CText = new(236, 233, 226, 255), CMute = new(168, 164, 154, 255), CAccent = new(217, 119, 87, 255), CWarn = new(251, 191, 36, 255), CWhite = new(255, 255, 255, 255), CHover = new(58, 56, 52, 255);

        private static readonly (string id, string name, string desc)[] Models =
        {
            ("sonnet", "Sonnet", "Cân bằng, nhanh"),
            ("opus", "Opus", "Mạnh nhất, chậm hơn"),
            ("haiku", "Haiku", "Nhẹ, rất nhanh"),
        };

        private bool _list; // đang mở danh sách hội thoại
        private bool _renaming, _focusRename; // đang sửa tên hội thoại
        private string _renameText = "";
        private bool _modelMenu; // đang mở menu chọn model

        private static string ModelId => PlayerPrefs.GetString("Agent.Model", ClaudeCli.DefaultModel);

        private static string ModelName(string id) => Array.Find(Models, m => m.id == id).name ?? id;

        private static void Gap(ImGui gui, float h) => gui.AddLayoutRect(1f, h);

        // Màu nền ấm, bo góc; ô nhập trong suốt vì thẻ nhập tự vẽ nền
        private static void PushStyle(ImGui gui)
        {
            gui.Style.Window.Box.BackColor = CBg;
            gui.Style.Window.Box.BorderColor = CLine;
            gui.Style.Window.Box.BorderThickness = 1f;
            gui.Style.Window.Box.BorderRadius = new ImRectRadius(12f);
            gui.Style.Window.ContentPadding = 10f;
            gui.Style.TextEdit.Normal.Box.BackColor = default;
            gui.Style.TextEdit.Normal.Box.BorderThickness = 0f;
            gui.Style.TextEdit.Selected.Box.BackColor = default;
            gui.Style.TextEdit.Selected.Box.BorderThickness = 0f;
        }

        private static ImTextSettings TS(ImGui gui, bool wrap = false, float ax = 0f, float ay = 0f, float scale = 1f) =>
            new(gui.Style.Layout.TextSize * scale, ax, ay, wrap, wrap ? ImTextOverflow.Overflow : ImTextOverflow.Ellipsis);

        // ---- thanh đầu ----

        private void DrawHeader(ImGui gui, bool chat)
        {
            var head = gui.AddLayoutRect(gui.Layout.GetAvailableWidth(), HeaderH);
            var close = new ImRect(head.Right - 34f, head.Y + 5f, 32f, 32f);
            var add = new ImRect(close.X - 36f, close.Y, 32f, 32f);
            var edit = new ImRect(add.X - 36f, close.Y, 32f, 32f);
            if (chat && _renaming) DrawRename(gui, head, add, close);
            else
            {
                var title = chat ? Current.name : "Trợ lý";
                var titleR = new ImRect(head.X + 6f, head.Y, (chat ? edit.X : close.X) - head.X - 10f, head.H);
                var ts = TS(gui, false, 0f, 0.5f, TitleScale);
                if (chat && gui.InvisibleButton(titleR)) _list = !_list;
                gui.Text(title, ts, CText, titleR);
                if (chat)
                {
                    var tw = Mathf.Min(gui.MeasureTextSize(title, ts).x, titleR.W - 24f);
                    DrawChevron(gui, new Vector2(titleR.X + tw + 14f, head.Y + head.H / 2f), CMute);
                }
                if (IconButton(gui, close, (c, col) => DrawCross(gui, c, col))) _open = false;
                if (chat)
                {
                    if (IconButton(gui, add, (c, col) => DrawPlus(gui, c, col))) { NewChat(); _list = false; _stick = true; }
                    if (IconButton(gui, edit, (c, col) => DrawPencil(gui, c, col))) { _renaming = true; _focusRename = true; _renameText = Current.name; _list = false; }
                }
            }
            gui.Canvas.Rect(new ImRect(head.X, head.Y - 3f, head.W, 1f), CLine);
            Gap(gui, 4f);
        }

        // Ô sửa tên thay cho title: Enter hoặc dấu tích lưu, Esc hoặc dấu x huỷ
        private void DrawRename(ImGui gui, ImRect head, ImRect ok, ImRect cancel)
        {
            var box = new ImRect(head.X + 2f, head.Y + 5f, ok.X - head.X - 6f, 32f);
            gui.Canvas.RectWithOutline(box, CCard, CAccent, 1f, new ImRectRadius(8f));
            var id = gui.GetControlId("agent.rename");
            ref var state = ref gui.Storage.Get<ImTextEditState>(id);
            if (_focusRename)
            {
                gui.SetActiveControl(id);
                state.Caret = _renameText.Length; // chọn hết chữ để gõ là thay tên
                state.Selection = -_renameText.Length;
                _focusRename = false;
            }
            gui.TextEdit(id, ref _renameText, ref state, new ImRect(box.X + 6f, box.Y, box.W - 12f, box.H), false, 60, ImTouchKeyboardType.Default, "Tên hội thoại");
            var kb = Keyboard.current;
            var commit = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
            var abort = kb != null && kb.escapeKey.wasPressedThisFrame;
            if (IconButton(gui, ok, (c, col) => DrawCheck(gui, c, CAccent))) commit = true;
            if (IconButton(gui, cancel, (c, col) => DrawCross(gui, c, col))) abort = true;
            if (commit)
            {
                var name = _renameText.Trim();
                if (name.Length > 0) { Current.name = name; Current.renamed = true; AgentChats.Save(_chats); }
                _renaming = false;
            }
            else if (abort) _renaming = false;
        }

        private static void DrawPencil(ImGui gui, Vector2 c, Color32 col)
        {
            gui.Canvas.Line(c + new Vector2(-5f, -5f), c + new Vector2(4f, 4f), col, 2.4f);
            gui.Canvas.Line(c + new Vector2(-6f, -6f), c + new Vector2(-4f, -4f), col, 3.6f);
        }

        private bool IconButton(ImGui gui, ImRect r, Action<Vector2, Color32> draw)
        {
            var clicked = gui.InvisibleButton(r, out var st);
            if (st != ImButtonState.Normal) gui.Canvas.Rect(r, CHover, new ImRectRadius(6f));
            draw(r.Center, st == ImButtonState.Normal ? CMute : CText);
            return clicked;
        }

        private static void DrawPlus(ImGui gui, Vector2 c, Color32 col)
        {
            gui.Canvas.Line(c + new Vector2(-6f, 0f), c + new Vector2(6f, 0f), col, 2f);
            gui.Canvas.Line(c + new Vector2(0f, -6f), c + new Vector2(0f, 6f), col, 2f);
        }

        private static void DrawCross(ImGui gui, Vector2 c, Color32 col)
        {
            gui.Canvas.Line(c + new Vector2(-5f, -5f), c + new Vector2(5f, 5f), col, 2f);
            gui.Canvas.Line(c + new Vector2(-5f, 5f), c + new Vector2(5f, -5f), col, 2f);
        }

        private static void DrawChevron(ImGui gui, Vector2 c, Color32 col)
        {
            gui.Canvas.Line(c + new Vector2(-4f, 2f), c + new Vector2(0f, -2f), col, 1.6f);
            gui.Canvas.Line(c + new Vector2(0f, -2f), c + new Vector2(4f, 2f), col, 1.6f);
        }

        // Nút viền hoặc đặc, bo tròn, chữ giữa
        private static bool Pill(ImGui gui, ImRect r, string label, Color32 fill, Color32 textColor, bool outline)
        {
            var clicked = gui.InvisibleButton(r, out var st);
            var hot = st != ImButtonState.Normal;
            var radius = new ImRectRadius(Mathf.Min(r.H / 2f, 13f));
            if (outline) gui.Canvas.RectWithOutline(r, hot ? CHover : default, CEdge, 1f, radius);
            else gui.Canvas.Rect(r, hot ? Color32.Lerp(fill, CWhite, 0.15f) : fill, radius);
            var tw = TextW(gui, label);
            gui.Text(label, TS(gui, false, 0f, 0.5f), textColor, new ImRect(r.X + (r.W - tw) / 2f, r.Y, tw + 6f, r.H)); // tự canh giữa: align 0.5 của Imui làm mất chữ
            return clicked;
        }

        private static float TextW(ImGui gui, string s) => gui.MeasureTextSize(s).x;

        // ---- hội thoại ----

        private void DrawChat(ImGui gui)
        {
            var chat = Current;
            var width = gui.Layout.GetAvailableWidth();
            var quick = Host?.QuickPrompts ?? Array.Empty<string>();
            var showChips = !chat.running && quick.Length > 0 && !_list;
            var gap = gui.Style.Layout.InnerSpacing;
            var bottomH = (_list ? 0f : InputH + gap + Space) + (showChips ? ChipH + gap + Space : 0f);
            var areaH = Mathf.Max(90f, gui.Layout.GetAvailableHeight() - bottomH - gap);

            var view = gui.AddLayoutRect(width, areaH);
            view = new ImRect(view.X + SidePad, view.Y, view.W - SidePad, view.H); // chừa lề trái cho nội dung
            gui.Layout.Push(ImAxis.Vertical, view);
            gui.Canvas.PushClipRect(view); // Imui không tự cắt nội dung vùng cuộn
            gui.BeginScrollable();
            if (_list) DrawChatList(gui, view.W - 14f);
            else DrawMessages(gui, chat, view.W - 14f);
            gui.EndScrollable(ImScrollFlag.HideHorBar);
            gui.Canvas.PopClipRect();
            gui.Layout.Pop();
            if (_list) return;

            Gap(gui, Space);
            if (showChips) { DrawChips(gui, quick, width); Gap(gui, Space); }
            DrawInput(gui, chat, width);
        }

        private void DrawChatList(ImGui gui, float w)
        {
            var row = new ImRect();
            Gap(gui, TopPad);
            for (var i = 0; i < _chats.Count; i++)
            {
                var c = _chats[i];
                Gap(gui, 2f);
                row = gui.AddLayoutRect(w, 36f);
                var del = new ImRect(row.Right - 32f, row.Y + 2f, 32f, 32f);
                var hit = new ImRect(row.X, row.Y, row.W - 32f, row.H);
                var clicked = gui.InvisibleButton(hit, out var rs);
                if (i == _cur) gui.Canvas.Rect(row, CBubble, new ImRectRadius(6f));
                else if (rs != ImButtonState.Normal) gui.Canvas.Rect(row, CHover, new ImRectRadius(6f));
                var name = (c.running ? "● " : "") + c.name;
                var when = c.When();
                var ww = TextW(gui, when) + 8f;
                gui.Text(name, TS(gui, false, 0f, 0.5f), CText, new ImRect(row.X + 10f, row.Y, row.W - 46f - ww, row.H));
                gui.Text(when, TS(gui, false, 1f, 0.5f), CMute, new ImRect(row.Right - 38f - ww, row.Y, ww, row.H));
                if (IconButton(gui, del, (p, col) => DrawCross(gui, p, col))) { DeleteChat(c); return; }
                if (clicked) { _cur = i; _list = false; _stick = true; return; }
            }
        }

        private void DrawMessages(ImGui gui, AgentChat chat, float w)
        {
            Gap(gui, TopPad);
            var ts = TS(gui, true);
            foreach (var m in chat.messages)
            {
                if (m.role == "user")
                {
                    var maxW = w * 0.84f;
                    var size = gui.MeasureTextSize(m.text, ts, new Vector2(maxW - 24f, 10000f));
                    var bw = Mathf.Min(maxW, size.x + 24f);
                    var bh = size.y + 16f;
                    var r = gui.AddLayoutRect(w, bh);
                    var b = new ImRect(r.Right - bw, r.Y, bw, bh);
                    gui.Canvas.Rect(b, CBubble, new ImRectRadius(14f));
                    gui.Text(m.text, ts, CText, new ImRect(b.X + 12f, b.Y + 8f, bw - 24f, bh - 16f));
                    Gap(gui, 10f);
                    continue;
                }
                if (string.IsNullOrEmpty(m.text))
                {
                    if (chat.running) DrawThinking(gui, w);
                    continue;
                }
                DrawMarkdown(gui, Visible(m.text));
                DrawProposals(gui, m, w);
                Gap(gui, 12f);
            }
            if (_stick)
            {
                gui.SetScrollOffset(new Vector2(0f, 1e7f));
                _stick = false;
            }
        }

        private static void DrawThinking(ImGui gui, float w)
        {
            var r = gui.AddLayoutRect(w, gui.GetRowHeight());
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 5f);
            var c = CAccent;
            c.a = (byte)(110 + 145 * pulse);
            gui.Canvas.Circle(new Vector2(r.X + 6f, r.Y + r.H / 2f), 4f, c);
            gui.Text("Đang suy nghĩ" + new string('.', 1 + (int)(Time.realtimeSinceStartup * 2f) % 3), TS(gui, false, 0f, 0.5f), CMute, new ImRect(r.X + 18f, r.Y, r.W - 18f, r.H));
        }

        // Markdown rút gọn: tiêu đề và dòng in đậm thành chữ trắng, gạch đầu dòng thành chấm; bỏ ký tự **, `
        private static void DrawMarkdown(ImGui gui, string text)
        {
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) { gui.AddLayoutRect(1f, 8f); continue; }
                var bold = line.StartsWith("#") || (line.StartsWith("**") && line.EndsWith("**") && line.Length > 4);
                var bullet = line.StartsWith("- ") || line.StartsWith("* ");
                if (bullet) line = line.Substring(2);
                line = line.TrimStart('#', ' ').Replace("**", "").Replace("`", "");
                gui.Text(bullet ? "•  " + line : line, bold ? CWhite : CText, true);
                gui.AddLayoutRect(1f, 3f);
            }
        }

        // ---- đề xuất ----

        private void DrawProposals(ImGui gui, AgentMessage m, float w)
        {
            if (m.proposals == null || m.proposals.Count == 0) return;
            var ts = TS(gui, true, 0f, 0f, 0.92f);
            var rowH = gui.GetRowHeight();
            Gap(gui, 6f);
            var textW = w - 20f - 28f - 10f;
            foreach (var p in m.proposals)
            {
                var detailH = string.IsNullOrEmpty(p.detail) ? 0f : gui.MeasureTextSize(p.detail, ts, new Vector2(textW, 10000f)).y + 2f;
                var noteH = string.IsNullOrEmpty(p.note) ? 0f : gui.MeasureTextSize(p.note, ts, new Vector2(textW, 10000f)).y + 4f;
                var footH = p.state == 0 ? 30f + 6f : rowH * 0.8f;
                var h = 10f + rowH * 0.9f + detailH + noteH + footH + 10f;
                var r = gui.AddLayoutRect(w, h);
                Gap(gui, 6f);
                var dim = p.state != 0;
                gui.Canvas.RectWithOutline(r, dim ? CBg : CCard, CLine, 1f, new ImRectRadius(10f));

                var icon = new ImRect(r.X + 10f, r.Top - 10f - 28f, 28f, 28f);
                gui.Canvas.Rect(icon, CBubble, new ImRectRadius(7f));
                if (p.state == 1) DrawCheck(gui, icon.Center, CAccent);
                else gui.Canvas.CircleWithOutline(icon.Center, 5f, default, dim ? CMute : CAccent, 1.6f);

                var tx = r.X + 10f + 28f + 10f;
                var titleR = new ImRect(tx, r.Top - 10f - rowH * 0.9f, textW, rowH * 0.9f);
                if (gui.InvisibleButton(titleR)) Host?.Preview(p); // bấm tên để xem chỗ đó trên tranh
                gui.Text(p.title, TS(gui, false, 0f, 0f), dim ? CMute : CWhite, titleR);
                if (detailH > 0f) gui.Text(p.detail, ts, CMute, new ImRect(tx, titleR.Y - detailH + 2f, textW, detailH - 2f));
                if (noteH > 0f) gui.Text(p.note, ts, CWarn, new ImRect(tx, titleR.Y - detailH - noteH + 2f, textW, noteH - 2f));

                if (p.state == 0)
                {
                    var apply = new ImRect(tx, r.Y + 10f, TextW(gui, "Áp dụng") + 40f, 30f);
                    if (Pill(gui, apply, "Áp dụng", CAccent, CWhite, false)) ApplyOne(p);
                    if (Pill(gui, new ImRect(apply.Right + 10f, apply.Y, TextW(gui, "Bỏ qua") + 36f, 30f), "Bỏ qua", default, CMute, true)) { p.state = 2; AgentChats.Save(_chats); }
                }
                else gui.Text(p.state == 1 ? "Đã áp dụng · Ctrl+Z để hoàn tác" : "Đã bỏ qua", TS(gui, false, 0f, 0.5f, 0.9f), CMute, new ImRect(tx, r.Y + 8f, textW, rowH * 0.8f));
            }

            var pending = m.proposals.Count(x => x.state == 0);
            if (pending == 0) return;
            var bar = gui.AddLayoutRect(w, 32f);
            if (pending >= 2)
            {
                var label = $"Áp dụng tất cả ({pending})";
                var bw = TextW(gui, label) + 28f;
                if (Pill(gui, new ImRect(bar.Right - bw, bar.Y, bw, bar.H), label, CAccent, CWhite, false)) ApplyAll(m);
            }
            gui.Text("Bấm tên đề xuất để xem trước trên tranh", TS(gui, false, 0f, 0.5f, 0.85f), CMute, new ImRect(bar.X, bar.Y, bar.W - 150f, bar.H));
        }

        private static void DrawCheck(ImGui gui, Vector2 c, Color32 col)
        {
            gui.Canvas.Line(c + new Vector2(-5f, 0f), c + new Vector2(-1.5f, -4f), col, 2f);
            gui.Canvas.Line(c + new Vector2(-1.5f, -4f), c + new Vector2(5f, 4f), col, 2f);
        }

        // ---- ô nhập ----

        private void DrawChips(ImGui gui, string[] quick, float width)
        {
            var row = gui.AddLayoutRect(width, ChipH);
            var x = row.X;
            foreach (var q in quick)
            {
                var cw = TextW(gui, q) + 24f;
                if (x + cw > row.Right) break;
                if (Pill(gui, new ImRect(x, row.Y, cw, ChipH), q, default, CText, true)) Send(q);
                x += cw + 8f;
            }
        }

        private void DrawInput(ImGui gui, AgentChat chat, float width)
        {
            var card = gui.AddLayoutRect(width, InputH);
            gui.Canvas.RectWithOutline(card, new Color32(48, 48, 46, 255), CLine, 1f, new ImRectRadius(14f));

            var editR = new ImRect(card.X + 6f, card.Y + 38f, card.W - 12f, card.H - 44f);
            var changed = gui.TextEdit(ref _input, editR, true, 0, ImTouchKeyboardType.Default, "Nhờ trợ lý…");
            var shift = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            var enter = changed && _input.EndsWith("\n") && !shift; // Enter gửi, Shift+Enter xuống dòng

            var send = new ImRect(card.Right - 10f - 30f, card.Y + 8f, 30f, 30f);
            var empty = string.IsNullOrWhiteSpace(_input);
            var clicked = gui.InvisibleButton(send, out var st);
            var fill = chat.running || !empty ? CAccent : Color32.Lerp(CAccent, CBg, 0.6f);
            gui.Canvas.Rect(send, st != ImButtonState.Normal ? Color32.Lerp(fill, CWhite, 0.15f) : fill, new ImRectRadius(8f));
            var c = send.Center;
            if (chat.running) gui.Canvas.Rect(new ImRect(c.x - 5f, c.y - 5f, 10f, 10f), CWhite, new ImRectRadius(2f));
            else
            {
                gui.Canvas.Line(c + new Vector2(0f, -6f), c + new Vector2(0f, 6f), CWhite, 2.2f);
                gui.Canvas.Line(c + new Vector2(-5f, 1f), c + new Vector2(0f, 6f), CWhite, 2.2f);
                gui.Canvas.Line(c + new Vector2(5f, 1f), c + new Vector2(0f, 6f), CWhite, 2.2f);
            }

            var model = ModelName(ModelId);
            var mw = TextW(gui, model);
            var modelBtn = new ImRect(send.X - 10f - mw - 28f, send.Y, mw + 28f, send.H);
            if (gui.InvisibleButton(modelBtn, out var ms)) _modelMenu = !_modelMenu;
            if (ms != ImButtonState.Normal || _modelMenu) gui.Canvas.Rect(modelBtn, CHover, new ImRectRadius(8f));
            gui.Text(model, TS(gui, false, 0f, 0.5f), CText, new ImRect(modelBtn.X + 8f, modelBtn.Y, mw + 6f, modelBtn.H));
            DrawChevron(gui, new Vector2(modelBtn.Right - 10f, modelBtn.Y + modelBtn.H / 2f), CMute);
            gui.Text("Enter gửi · Shift+Enter xuống dòng", TS(gui, false, 0f, 0.5f, 0.85f), CMute, new ImRect(card.X + 16f, send.Y, modelBtn.X - card.X - 20f, send.H));
            if (_modelMenu) DrawModelMenu(gui, card, modelBtn);

            if (chat.running)
            {
                if (clicked) Stop(chat);
                if (enter) _input = _input.TrimEnd('\n', '\r');
            }
            else if ((clicked || enter) && !empty)
            {
                Send(_input);
                _input = "";
            }
            else if (enter) _input = "";
        }

        // Menu chọn model hiện phía trên nhãn model; bấm ra ngoài thì đóng
        private void DrawModelMenu(ImGui gui, ImRect card, ImRect btn)
        {
            const float w = 300f, rowH = 38f, pad = 6f;
            var menu = new ImRect(card.Right - w - 6f, btn.Top + 8f, w, Models.Length * rowH + pad * 2f);
            gui.Canvas.RectWithOutline(menu, CCard, CEdge, 1f, new ImRectRadius(10f));
            var cur = ModelId;
            for (var i = 0; i < Models.Length; i++)
            {
                var (id, name, desc) = Models[i];
                var row = new ImRect(menu.X + pad, menu.Top - pad - (i + 1) * rowH, menu.W - pad * 2f, rowH);
                var clicked = gui.InvisibleButton(row, out var st);
                if (st != ImButtonState.Normal) gui.Canvas.Rect(row, CHover, new ImRectRadius(6f));
                var on = id == cur;
                gui.Text(name, TS(gui, false, 0f, 0.5f), on ? CAccent : CText, new ImRect(row.X + 10f, row.Y, 70f, row.H));
                gui.Text(desc, TS(gui, false, 1f, 0.5f, 0.85f), CMute, new ImRect(row.X + 80f, row.Y, row.W - 90f, row.H));
                if (clicked)
                {
                    PlayerPrefs.SetString("Agent.Model", id);
                    _modelMenu = false;
                }
            }
            var m = Mouse.current;
            if (m == null || !m.leftButton.wasPressedThisFrame) return;
            var mp = m.position.ReadValue() * (gui.Canvas.ScreenSize.x / Mathf.Max(1f, Screen.width));
            if (!menu.Contains(mp) && !btn.Contains(mp)) _modelMenu = false;
        }
    }
}
