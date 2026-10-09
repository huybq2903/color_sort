using System.Collections.Generic;
using System.Linq;
using System.Text;
using Falcon.InGame.Core;
using Falcon.Shared.BaseLevelEditor;
using Newtonsoft.Json.Linq;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Phần riêng của Colot Sort cho trợ lý: lời dặn, bản tóm tắt tranh và hàng chờ đính kèm mỗi tin nhắn, chỉ chỗ và áp dụng đề xuất.</summary>
    internal sealed class ColorSortAgentHost : IAgentHost
    {
        private const float SmallPiece = 0.004f; // mảnh nhỏ hơn 0,4% diện tích khung thì báo là nhỏ
        private const int MaxPiecesListed = 120;

        private readonly LevelEditorPicture _picture;
        private readonly LevelEditorBoxQueue _boxes;

        public ColorSortAgentHost(LevelEditorPicture picture, LevelEditorBoxQueue boxes)
        {
            _picture = picture;
            _boxes = boxes;
        }

        public string SystemPrompt =>
            "Bạn là trợ lý trong tool Level Editor của game xếp màu (tranh mosaic chia thành mảnh có số cát, hộp chờ chứa cát theo màu). " +
            "Trả lời bằng tiếng Việt, ngắn gọn, đi thẳng vào việc. Gọi người dùng là \"bạn\", tự xưng \"mình\". " +
            "Đầu mỗi tin nhắn có bản tóm tắt level đang mở; bạn không có công cụ nào để đọc hay sửa gì khác. " +
            "Gộp hay đổi màu mảnh làm cát từng màu thay đổi (cát mảnh gộp tính lại theo diện tích): đừng khẳng định tổng cát không đổi khi chưa chắc, tool sẽ tự báo hệ quả. " +
            "Chỉ đề xuất gộp các mảnh có ghi \"kề\" nhau trong bản tóm tắt, và đề xuất nào cũng phải kiểm tra lại với số liệu trước khi nêu; không chắc thì đừng đề xuất. " +
            "Quy tắc: tổng cát các hộp cùng màu phải bằng tổng cát các mảnh màu đó; mỗi hộp 50..300 cát, bước 50; chữ số phải vừa trong mảnh nên mảnh quá nhỏ nên gộp. " +
            "Không bịa số liệu ngoài bản tóm tắt.";

        public string ProposalGuide =>
            "Các kind đề xuất và trường kèm theo:\n" +
            "- add_box: column (số cột, bắt đầu từ 0, tối đa 4), color (id màu), value (cát 50..300, bước 50)\n" +
            "- set_box: box (id hộp như \"b3\"), color và/hoặc value\n" +
            "- delete_box: box\n" +
            "- merge: pieces (mảng id mảnh như [\"r4\",\"r9\"], các mảnh phải kề nhau)\n" +
            "- recolor: piece (id mảnh), color\n" +
            "Trong trường kèm theo dùng id; trong detail nhắc mảnh theo số thứ tự #n.";

        public string[] QuickPrompts => new[] { "Kiểm tra level", "Cân bằng hộp theo màu" };

        public string BuildContext()
        {
            var p = _picture.Picture;
            if (p == null) return "[Level đang mở] Chưa có tranh.";
            var sb = new StringBuilder("[Level đang mở]\n");
            var frame = (double)p.width * p.unit * p.height * p.unit;
            sb.Append($"Khung {p.width}x{p.height}, {p.regions.Count} mảnh, {p.regions.Select(r => r.colorId).Distinct().Count()} màu, tổng cát {p.regions.Sum(r => r.value)}.\n");

            var adj = _picture.AdjacentPairs();
            var need = _picture.ColorSand();
            var have = _boxes.Queue != null ? _boxes.ColorSand() : new Dictionary<int, int>();
            sb.Append("Cát theo màu (id màu: cần trong tranh / có trong hộp):\n");
            foreach (var c in need.Keys.Union(have.Keys).OrderBy(x => x))
                sb.Append($"  màu {c}: {need.GetValueOrDefault(c)} / {have.GetValueOrDefault(c)}\n");

            var q = _boxes.Queue;
            if (q != null && q.list.Count > 0)
            {
                sb.Append($"Hàng chờ: {q.list.Count} hộp, {q.ColumnCount} cột (hộp đầu mỗi cột ra trước):\n");
                for (var c = 0; c < q.ColumnCount; c++)
                    sb.Append($"  cột {c}: {string.Join(", ", q.Column(c).Select(b => $"{b.id}(màu {b.colorId}={b.value})"))}\n");
            }
            else sb.Append("Hàng chờ: chưa có hộp.\n");

            sb.Append("Mảnh (#số thứ tự, id): màu, cát, diện tích so với khung, tâm theo % khung, các mảnh kề:\n");
            for (var i = 0; i < p.regions.Count && i < MaxPiecesListed; i++)
            {
                var r = p.regions[i];
                var area = Area(r.points) / frame;
                Centroid(r.points, p.width * p.unit, p.height * p.unit, out var cx, out var cy);
                sb.Append($"  #{i + 1} ({r.id}): màu {r.colorId}, cát {r.value}, {area * 100:0.##}%{(area < SmallPiece ? " NHỎ" : "")}, tâm ({cx:0}%,{cy:0}%), kề: {Neighbors(adj, i)}\n");
            }
            if (p.regions.Count > MaxPiecesListed) sb.Append($"  … và {p.regions.Count - MaxPiecesListed} mảnh nữa\n");
            return sb.ToString();
        }

        private static string Neighbors(HashSet<(int, int)> adj, int i)
        {
            var list = adj.Where(p => p.Item1 == i || p.Item2 == i).Select(p => "#" + ((p.Item1 == i ? p.Item2 : p.Item1) + 1)).OrderBy(x => x.Length).ThenBy(x => x).ToList();
            return list.Count == 0 ? "không" : string.Join(",", list);
        }

        public string Validate(AgentProposal proposal, out string note)
        {
            note = null;
            var a = Args(proposal);
            if (a == null) return "Đề xuất không đọc được";
            var pic = _picture.ColorSand();
            var box = _boxes.Queue != null ? _boxes.ColorSand() : new Dictionary<int, int>();
            Dictionary<int, int> picAfter = pic, boxAfter = box;
            switch (proposal.kind)
            {
                case "add_box":
                {
                    var column = a.Value<int?>("column") ?? -1;
                    var color = a.Value<int?>("color") ?? -1;
                    var value = a.Value<int?>("value") ?? 0;
                    if (!_boxes.CanAdd(column)) return $"Không thêm được hộp vào cột {column}";
                    if (!ValidColor(color)) return $"Màu {color} không hợp lệ";
                    var e = ValidValue(value);
                    if (e != null) return e;
                    boxAfter = Shift(box, color, value);
                    break;
                }
                case "set_box":
                {
                    var id = a.Value<string>("box");
                    if (string.IsNullOrEmpty(id) || !_boxes.Has(id)) return "Không thấy hộp " + id;
                    var color = a.Value<int?>("color");
                    var value = a.Value<int?>("value");
                    if (!color.HasValue && !value.HasValue) return "Không nói đổi màu hay cát";
                    if (color.HasValue && !ValidColor(color.Value)) return $"Màu {color} không hợp lệ";
                    if (value.HasValue) { var e = ValidValue(value.Value); if (e != null) return e; }
                    var b = _boxes.Queue.list.Find(x => x.id == id);
                    var newColor = color ?? b.colorId;
                    var newValue = value ?? b.value;
                    if (newColor == b.colorId && newValue == b.value) return "Hộp đã đúng như đề xuất";
                    boxAfter = Shift(Shift(box, b.colorId, -b.value), newColor, newValue);
                    break;
                }
                case "delete_box":
                {
                    var b = _boxes.Queue?.list.Find(x => x.id == a.Value<string>("box"));
                    if (b == null) return "Không thấy hộp " + a.Value<string>("box");
                    boxAfter = Shift(box, b.colorId, -b.value);
                    break;
                }
                case "merge":
                {
                    var ids = PieceIds(a).Distinct().ToList();
                    if (ids.Count < 2) return "Cần ít nhất 2 mảnh khác nhau";
                    var idx = ids.Select(PieceIndex).ToList();
                    if (idx.Any(i => i < 0)) return "Có mảnh không còn tồn tại";
                    var merged = _picture.TryMerge(idx, out var err);
                    if (merged == null) return err;
                    picAfter = merged.regions.GroupBy(r => r.colorId).ToDictionary(g => g.Key, g => g.Sum(r => r.value));
                    break;
                }
                case "recolor":
                {
                    var i = PieceIndex(a.Value<string>("piece") ?? "");
                    var color = a.Value<int?>("color") ?? -1;
                    if (i < 0) return "Không thấy mảnh " + a.Value<string>("piece");
                    if (!ValidColor(color)) return $"Màu {color} không hợp lệ";
                    var r = _picture.Picture.regions[i];
                    if (r.colorId == color) return "Mảnh đã có màu này";
                    picAfter = Shift(Shift(pic, r.colorId, -r.value), color, r.value);
                    break;
                }
                default:
                    return "Kiểu đề xuất chưa hỗ trợ: " + proposal.kind;
            }
            note = BalanceNote(pic, box, picAfter, boxAfter);
            return null;
        }

        private static Dictionary<int, int> Shift(Dictionary<int, int> d, int color, int delta)
        {
            var r = new Dictionary<int, int>(d);
            r[color] = r.GetValueOrDefault(color) + delta;
            return r;
        }

        // Báo các màu bị lệch cát (tranh khác hộp) sau khi áp dụng mà trước đó chưa lệch
        private static string BalanceNote(Dictionary<int, int> pic, Dictionary<int, int> box, Dictionary<int, int> picAfter, Dictionary<int, int> boxAfter)
        {
            var parts = new List<string>();
            foreach (var c in picAfter.Keys.Union(boxAfter.Keys).OrderBy(x => x))
            {
                var before = pic.GetValueOrDefault(c) == box.GetValueOrDefault(c);
                var pa = picAfter.GetValueOrDefault(c);
                var ba = boxAfter.GetValueOrDefault(c);
                if (before && pa != ba) parts.Add($"màu {c} lệch (tranh {pa}, hộp {ba})");
            }
            return parts.Count == 0 ? null : "Sau khi áp dụng: " + string.Join("; ", parts);
        }

        private static string ValidValue(int v) => v < PieceValue.Min || v > PieceValue.Max || v % PieceValue.Step != 0 ? $"Cát {v} phải từ {PieceValue.Min} đến {PieceValue.Max}, bước {PieceValue.Step}" : null;

        public void Preview(AgentProposal proposal)
        {
            var a = Args(proposal);
            if (a == null) return;
            var box = a.Value<string>("box");
            if (!string.IsNullOrEmpty(box) && _boxes.Has(box)) _boxes.SelectMany(new[] { box }, false);
            var ids = PieceIds(a);
            if (ids.Count > 0)
            {
                var idx = ids.Select(PieceIndex).Where(i => i >= 0).ToList();
                if (idx.Count > 0) _picture.SelectPieces(idx);
            }
        }

        public bool Apply(AgentProposal proposal, out string error)
        {
            error = Validate(proposal, out _); // level có thể đã đổi từ lúc trợ lý đề xuất
            if (error != null) return false;
            var a = Args(proposal);
            switch (proposal.kind)
            {
                case "add_box":
                {
                    var column = a.Value<int?>("column") ?? -1;
                    var color = a.Value<int?>("color") ?? -1;
                    var value = a.Value<int?>("value") ?? 0;
                    if (!_boxes.CanAdd(column)) { error = $"Không thêm được hộp vào cột {column}"; return false; }
                    if (!ValidColor(color)) { error = $"Màu {color} không hợp lệ"; return false; }
                    _boxes.Add(column, color, value);
                    return true;
                }
                case "set_box":
                {
                    var color = a.Value<int?>("color");
                    if (color.HasValue && !ValidColor(color.Value)) { error = $"Màu {color} không hợp lệ"; return false; }
                    if (!_boxes.SetBox(a.Value<string>("box"), color, a.Value<int?>("value"))) { error = "Không thấy hộp " + a.Value<string>("box"); return false; }
                    return true;
                }
                case "delete_box":
                    if (!_boxes.DeleteBox(a.Value<string>("box"))) { error = "Không thấy hộp " + a.Value<string>("box"); return false; }
                    return true;
                case "merge":
                {
                    var idx = PieceIds(a).Select(PieceIndex).ToList();
                    if (idx.Count < 2 || idx.Any(i => i < 0)) { error = "Không thấy đủ các mảnh cần gộp (có thể đã đổi sau lần chia hoặc gộp)"; return false; }
                    return _picture.MergePiecesNow(idx, out error);
                }
                case "recolor":
                {
                    var i = PieceIndex(a.Value<string>("piece"));
                    var color = a.Value<int?>("color") ?? -1;
                    if (i < 0) { error = "Không thấy mảnh " + a.Value<string>("piece"); return false; }
                    if (!ValidColor(color)) { error = $"Màu {color} không hợp lệ"; return false; }
                    _picture.SelectPieces(new[] { i });
                    _picture.PickPaletteColor(color);
                    return true;
                }
                default:
                    error = "Kiểu đề xuất chưa hỗ trợ: " + proposal.kind;
                    return false;
            }
        }

        private static JObject Args(AgentProposal p)
        {
            try { return JObject.Parse(p.args); }
            catch (Newtonsoft.Json.JsonException) { return null; }
        }

        private static bool ValidColor(int c) => c >= 0 && c < ColorPalette.Count;

        private static List<string> PieceIds(JObject a)
        {
            var ids = new List<string>();
            if (a["pieces"] is JArray arr) ids.AddRange(arr.Select(t => t.ToString()));
            var single = a.Value<string>("piece");
            if (!string.IsNullOrEmpty(single)) ids.Add(single);
            return ids;
        }

        private int PieceIndex(string id) => _picture.Picture?.regions.FindIndex(r => r.id == id) ?? -1;

        private static double Area(int[] pts)
        {
            double s = 0;
            var n = pts.Length / 2;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                s += (double)pts[i * 2] * pts[j * 2 + 1] - (double)pts[j * 2] * pts[i * 2 + 1];
            }
            return System.Math.Abs(s) / 2.0;
        }

        private static void Centroid(int[] pts, int w, int h, out double cx, out double cy)
        {
            double x = 0, y = 0;
            var n = pts.Length / 2;
            for (var i = 0; i < n; i++) { x += pts[i * 2]; y += pts[i * 2 + 1]; }
            cx = n == 0 ? 0 : 100.0 * x / n / System.Math.Max(1, w);
            cy = n == 0 ? 0 : 100.0 * y / n / System.Math.Max(1, h);
        }
    }
}
