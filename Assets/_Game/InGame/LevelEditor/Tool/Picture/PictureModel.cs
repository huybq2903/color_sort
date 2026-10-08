using System.Collections.Generic;
using System.Linq;
using Falcon.InGame.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Tranh đang sửa + bản đồ pixel (id = index + 1) cho hit-test, gộp, tách.</summary>
    public class PictureModel
    {
        public int MinPiecePixels => System.Math.Max(4, Map.w * Map.h / 10000); // ~0.01% tranh: lưới x3 thì ~60px

        // Tranh vector hoá lưu được mảnh có lỗ; chế độ cũ thì không
        private bool SupportsHoles => Vectorised(Picture);

        // Biên chung của hai mảnh chỉ có cùng tập đỉnh khi tranh được vector hoá (BoundaryGraph)
        internal static bool Vectorised(PictureProperty p) => p.unit > 1 && p.gen.tidy && p.gen.curveSmooth > 0f;

        private const float PickRadius = 6f; // bán kính chọn (ô lưới)

        private Vector2 Clamp(Vector2 p) => new(Mathf.Clamp(p.x, 0f, Picture.width), Mathf.Clamp(p.y, 0f, Picture.height));

        public PictureProperty Picture { get; }
        public RegionMap Map { get; }

        public PictureModel(PictureProperty p)
        {
            Picture = p;
            Map = RegionRaster.Rasterize(p);
        }


        private const float DotFraction = 0.016f; // đường kính chấm mặc định theo chiều cao tranh

        // Thêm (hoặc thay nét số replace) nét trang trí từ đường vẽ (toạ độ lưới tranh); chỉ 1 điểm thì thành chấm
        public bool AddLine(IReadOnlyList<Vector2> pic, int replace = -1, int thickness = 0)
        {
            if (pic == null || pic.Count == 0) return false;
            var unit = Picture.unit;
            pic = pic.Select(Clamp).ToList();
            var pts = pic.Count == 1 ? new List<Vector2> { pic[0], pic[0] + Vector2.right * 0.01f } : Smooth(pic);
            var o = new int[pts.Count * 2];
            for (var i = 0; i < pts.Count; i++)
            {
                o[i * 2] = Mathf.RoundToInt(pts[i].x * unit);
                o[i * 2 + 1] = Mathf.RoundToInt(pts[i].y * unit);
            }
            SyncLineWidths();
            var width = pic.Count == 1 ? Mathf.RoundToInt(DotFraction * Picture.height * unit) : 0;
            if (replace >= 0 && replace < Picture.lines.Count) { Picture.lines[replace] = o; Picture.lineWidths[replace] = width; } // sửa nét: giữ độ dày cũ
            else { Picture.lines.Add(o); Picture.lineWidths.Add(width); Picture.lineThickness.Add(Mathf.Max(0, thickness)); Picture.lineIds.Add(null); }
            return true;
        }

        // Đặt độ dày nét theo % độ dày viền (0 = mặc định); false nếu chỉ số sai
        public bool SetLineThickness(int index, int percent)
        {
            SyncLineWidths();
            if (index < 0 || index >= Picture.lines.Count) return false;
            Picture.lineThickness[index] = Mathf.Max(0, percent);
            return true;
        }

        // Chỉ số nét/chấm gần điểm nhất (trong bán kính chọn); -1 nếu không có
        public int NearestLine(Vector2 pic)
        {
            if (Picture.lines == null) return -1;
            SyncLineWidths();
            var unit = Picture.unit;
            var best = -1;
            var bestD = Mathf.Max(PickRadius, 0.01f * Picture.height);
            for (var li = 0; li < Picture.lines.Count; li++)
            {
                var l = Picture.lines[li];
                var d = float.MaxValue;
                for (var k = 0; k + 3 < l.Length; k += 2)
                    d = Mathf.Min(d, SegDistance(pic, new Vector2(l[k], l[k + 1]) / unit, new Vector2(l[k + 2], l[k + 3]) / unit));
                if (Picture.lineWidths[li] > 0) d -= Picture.lineWidths[li] * 0.5f / unit; // chấm: tính tới mép
                if (d < bestD) { bestD = d; best = li; }
            }
            return best;
        }

        // Xoá nét/chấm gần điểm nhất; false nếu không có
        public bool RemoveLine(Vector2 pic)
        {
            var best = NearestLine(pic);
            if (best < 0) return false;
            Picture.lines.RemoveAt(best);
            Picture.lineWidths.RemoveAt(best);
            Picture.lineThickness.RemoveAt(best);
            Picture.lineIds.RemoveAt(best);
            return true;
        }

        // Điểm gần nhất trên đường biên mọi mảnh trong bán kính (toạ độ lưới); false nếu không có
        public bool NearestBoundaryPoint(Vector2 pic, float radius, out Vector2 point, int region = -1)
        {
            point = pic;
            var unit = Mathf.Max(1, Picture.unit);
            var bestD = radius;
            var found = false;
            for (var ri = 0; ri < Picture.regions.Count; ri++)
            {
                if (region >= 0 && ri != region) continue; // chỉ xét biên của mảnh này
                var pts = Picture.regions[ri].points;
                var n = pts.Length / 2;
                for (var i = 0; i < n; i++)
                {
                    var a = new Vector2(pts[i * 2], pts[i * 2 + 1]) / unit;
                    var j = (i + 1) % n;
                    var b = new Vector2(pts[j * 2], pts[j * 2 + 1]) / unit;
                    var ab = b - a;
                    var t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(pic - a, ab) / ab.sqrMagnitude);
                    var q = a + ab * t;
                    var d = Vector2.Distance(pic, q);
                    if (d < bestD) { bestD = d; point = q; found = true; }
                }
            }
            return found;
        }

        // Xoá nét/chấm theo chỉ số; false nếu chỉ số sai
        internal bool RemoveLineAt(int index)
        {
            SyncLineWidths();
            if (index < 0 || index >= Picture.lines.Count) return false;
            Picture.lines.RemoveAt(index);
            Picture.lineWidths.RemoveAt(index);
            Picture.lineThickness.RemoveAt(index);
            Picture.lineIds.RemoveAt(index);
            return true;
        }

        private void SyncLineWidths()
        {
            Picture.lines ??= new List<int[]>();
            Picture.lineWidths ??= new List<int>();
            while (Picture.lineWidths.Count < Picture.lines.Count) Picture.lineWidths.Add(0);
            Picture.lineThickness ??= new List<int>();
            while (Picture.lineThickness.Count < Picture.lines.Count) Picture.lineThickness.Add(0);
            Picture.lineIds ??= new List<string>();
            while (Picture.lineIds.Count < Picture.lines.Count) Picture.lineIds.Add(null);
        }

        private static float SegDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // Vuốt nhẹ nét vẽ tay rồi bỏ đỉnh thừa (Douglas-Peucker)
        private List<Vector2> Smooth(IReadOnlyList<Vector2> pic)
        {
            var s = new List<Vector2>(pic);
            for (var pass = 0; pass < 2; pass++)
            {
                var t = new List<Vector2>(s);
                for (var i = 1; i < s.Count - 1; i++) t[i] = (s[i - 1] + 2f * s[i] + s[i + 1]) * 0.25f;
                s = t;
            }
            var keep = new bool[s.Count];
            keep[0] = keep[^1] = true;
            Simplify(s, 0, s.Count - 1, 0.002f * Picture.height, keep);
            var o = new List<Vector2>();
            for (var i = 0; i < s.Count; i++) if (keep[i]) o.Add(s[i]);
            return o.Count >= 2 ? o : new List<Vector2> { s[0], s[^1] + Vector2.right * 0.01f };
        }

        private static void Simplify(List<Vector2> p, int a, int b, float eps, bool[] keep)
        {
            var far = -1f; var at = -1;
            for (var i = a + 1; i < b; i++)
            {
                var d = SegDistance(p[i], p[a], p[b]);
                if (d > far) { far = d; at = i; }
            }
            if (at < 0 || far <= eps) return;
            keep[at] = true;
            Simplify(p, a, at, eps, keep);
            Simplify(p, at, b, eps, keep);
        }

        public int HitTest(Vector2 pic)
        {
            int x = Mathf.FloorToInt(pic.x), y = Mathf.FloorToInt(pic.y);
            if (x < 0 || y < 0 || x >= Map.w || y >= Map.h) return -1;
            return Map.reg[y * Map.w + x] - 1;
        }

        public int Merge(int target, int other, out string error)
        {
            error = null;
            if (target < 0 || other < 0 || target == other) { error = "Chọn 2 mảnh khác nhau"; return -1; }
            int tid = target + 1, oid = other + 1;
            var otherPx = Pixels(oid);
            var gap = new List<int>(); // tranh có khe nét: 2 mảnh cách nhau bằng khe hẹp vẫn gộp được, khe được lấp
            if (!Touches(otherPx, tid))
            {
                if (Picture.gen.inkGaps) PieceSize.ForEachGap(Map, (a, b, s, len, stride) =>
                {
                    if ((a == tid && b == oid) || (a == oid && b == tid)) for (var t = 0; t < len; t++) gap.Add(s + t * stride);
                });
                if (gap.Count == 0) { error = "Hai mảnh không kề nhau"; return -1; }
            }

            foreach (var p in otherPx) Map.reg[p] = tid;
            foreach (var p in gap) Map.reg[p] = tid;
            if (!SupportsHoles && PixelRegions.HasHole(Map, Pixels(tid)))
            {
                foreach (var p in otherPx) Map.reg[p] = oid;
                foreach (var p in gap) Map.reg[p] = 0;
                error = "Gộp sẽ tạo mảnh có lỗ (vây kín mảnh khác)";
                return -1;
            }

            Picture.regions[target].points = Trace(tid, Map.Bounds());
            Picture.regions[target].valueManual = false; // mảnh gộp có diện tích khác hẳn: tính lại số cát
            RemoveRegion(other);
            var merged = other < target ? target - 1 : target;
            RebuildBoundaries(new[] { merged + 1 });
            return merged;
        }

        // Gộp các mảnh đã chọn vào mảnh đầu; mảnh chưa kề đợi tới khi kề; trả bản tranh mới, null (kèm lỗi) nếu không gộp hết được
        internal static PictureProperty MergeMany(PictureProperty p, IReadOnlyList<int> indices, out string error)
        {
            error = null;
            var cur = p;
            var target = indices[0];
            var rest = indices.Skip(1).Distinct().Where(i => i != target).ToList();
            while (rest.Count > 0)
            {
                var done = false;
                for (var k = 0; k < rest.Count && !done; k++)
                {
                    var model = new PictureModel(cur.Clone());
                    var other = rest[k];
                    var t = model.Merge(target, other, out var err);
                    if (t < 0)
                    {
                        error = err;
                        continue;
                    }
                    cur = model.Picture;
                    target = t;
                    rest.RemoveAt(k);
                    for (var j = 0; j < rest.Count; j++) if (rest[j] > other) rest[j]--;
                    done = true;
                }
                if (!done)
                {
                    error = "Các mảnh chọn không liền nhau, không gộp được hết" + (error != null ? $" ({error})" : "");
                    return null;
                }
            }
            error = null;
            return cur;
        }

        // Chỉ số mọi mảnh có pixel trong khung giữa hai điểm (ô lưới, thứ tự góc tuỳ ý)
        internal List<int> RegionsInRect(Vector2 a, Vector2 b)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x))), x1 = Mathf.Min(Map.w - 1, Mathf.FloorToInt(Mathf.Max(a.x, b.x)));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y))), y1 = Mathf.Min(Map.h - 1, Mathf.FloorToInt(Mathf.Max(a.y, b.y)));
            var set = new SortedSet<int>();
            for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    if (Map.reg[y * Map.w + x] > 0) set.Add(Map.reg[y * Map.w + x] - 1);
            return set.ToList();
        }

        public int Split(int index, Vector2 a, Vector2 b, out string error)
        {
            error = null;
            var id = index + 1;
            var dir = b - a;
            if (dir.sqrMagnitude < 1f) { error = "Đường cắt quá ngắn"; return 0; }

            var left = new List<int>();
            var right = new List<int>();
            foreach (var p in Pixels(id))
            {
                float cx = p % Map.w + 0.5f, cy = p / Map.w + 0.5f;
                (dir.x * (cy - a.y) - dir.y * (cx - a.x) > 0 ? left : right).Add(p);
            }
            if (left.Count == 0 || right.Count == 0) { error = "Đường cắt không đi qua mảnh"; return 0; }

            // mỗi cụm liền của từng nửa là 1 mảnh; nửa trái tạm mang id -1
            foreach (var p in left) Map.reg[p] = -1;
            var pieces = new List<List<int>>();
            pieces.AddRange(Components(right, id));
            pieces.AddRange(Components(left, -1));
            if (pieces.Any(pc => pc.Count < MinPiecePixels))
            {
                foreach (var p in left) Map.reg[p] = id;
                error = "Mảnh cắt ra quá nhỏ";
                return 0;
            }

            return Commit(index, id, pieces);
        }

        private const float CutOverreach = 3f; // đường cắt thò ra khỏi hai điểm chọn tối đa chừng này (ô lưới) để chắc chạm biên, không cắt tiếp phần khác của mảnh

        // Cắt theo đường cong tự do: đường đi (toạ độ lưới) là rào, mỗi cụm liền còn lại là 1 mảnh; hai đầu chỉ thò ra vài ô để chắc chạm biên mảnh
        // gap > 0 (tranh có khe nét): dải rộng gap ô lưới quanh đường cắt thành khe trống giữa hai mảnh
        public int SplitPath(int index, IReadOnlyList<Vector2> path, out string error, int gap = 0)
        {
            error = null;
            var id = index + 1;
            if (path == null || path.Count < 2) { error = "Đường cắt quá ngắn"; return 0; }
            var pts = new List<Vector2>(path);
            var closed = pts.Count >= 3 && (pts[0] - pts[pts.Count - 1]).magnitude < 1.5f; // đường cắt khép kín (cắt riêng một mảng trong lòng mảnh): không thò đầu mút
            if (closed)
            {
                pts.Add(pts[0]);
            }
            else
            {
                var d0 = (pts[0] - pts[Mathf.Min(3, pts.Count - 1)]).normalized;
                var d1 = (pts[pts.Count - 1] - pts[Mathf.Max(0, pts.Count - 4)]).normalized;
                if (d0 != Vector2.zero) pts.Insert(0, pts[0] + d0 * CutOverreach);
                if (d1 != Vector2.zero) pts.Add(pts[pts.Count - 1] + d1 * CutOverreach);
            }

            int w = Map.w, h = Map.h, total = w * h;
            var inRegion = new bool[total];
            for (var i = 0; i < total; i++) inRegion[i] = Map.reg[i] == id;
            var barrier = new bool[total];
            var barrierList = new List<int>();
            for (var k = 0; k + 1 < pts.Count; k++)
            {
                var a = pts[k];
                var b = pts[k + 1];
                var steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude * 2f));
                for (var t = 0; t <= steps; t++)
                {
                    var q = Vector2.Lerp(a, b, t / (float)steps);
                    int x = Mathf.FloorToInt(q.x), y = Mathf.FloorToInt(q.y);
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    var i = y * w + x;
                    if (inRegion[i] && !barrier[i]) { barrier[i] = true; barrierList.Add(i); }
                }
            }
            if (barrierList.Count == 0) { error = "Đường cắt không đi qua mảnh"; return 0; }
            List<int> gapList = null;
            if (gap > 0)
            {
                gapList = new List<int>();
                var inGap = new bool[total];
                var rad = gap * 0.5f;
                var ri = Mathf.CeilToInt(rad);
                foreach (var b in barrierList)
                    for (var dy = -ri; dy <= ri; dy++)
                    for (var dx = -ri; dx <= ri; dx++)
                    {
                        int x = b % w + dx, y = b / w + dy;
                        if (dx * dx + dy * dy > rad * rad || x < 0 || y < 0 || x >= w || y >= h) continue;
                        var q = y * w + x;
                        if (inRegion[q] && !inGap[q]) { inGap[q] = true; gapList.Add(q); }
                    }
                foreach (var q in gapList)
                    if (!barrier[q]) { barrier[q] = true; barrierList.Add(q); }
            }

            var comp = new int[total];
            System.Array.Fill(comp, -1);
            var comps = new List<List<int>>();
            var stack = new Stack<int>();
            for (var seed = 0; seed < total; seed++)
            {
                if (!inRegion[seed] || barrier[seed] || comp[seed] >= 0) continue;
                var list = new List<int>();
                comp[seed] = comps.Count;
                stack.Push(seed);
                while (stack.Count > 0)
                {
                    var p = stack.Pop();
                    list.Add(p);
                    int x = p % w, y = p / w;
                    void Visit(int q)
                    {
                        if (!inRegion[q] || barrier[q] || comp[q] >= 0) return;
                        comp[q] = comps.Count;
                        stack.Push(q);
                    }
                    if (x > 0) Visit(p - 1);
                    if (x < w - 1) Visit(p + 1);
                    if (y > 0) Visit(p - w);
                    if (y < h - 1) Visit(p + w);
                }
                comps.Add(list);
            }
            if (comps.Count < 2) { error = "Đường cắt chưa chia mảnh làm đôi (kéo xuyên qua mảnh)"; return 0; }

            // pixel trên đường cắt thuộc cụm kề nó (có khe: pixel dải cắt để trống, không chia cho mảnh nào)
            var queue = new Queue<int>();
            foreach (var list in comps) foreach (var p in list) queue.Enqueue(p);
            var pending = new bool[total];
            var pendingCount = 0;
            if (gapList == null) foreach (var q in barrierList) { pending[q] = true; pendingCount++; }
            while (queue.Count > 0 && pendingCount > 0)
            {
                var p = queue.Dequeue();
                int x = p % w, y = p / w;
                void Spread(int q)
                {
                    if (!pending[q]) return;
                    pending[q] = false;
                    pendingCount--;
                    comp[q] = comp[p];
                    comps[comp[p]].Add(q);
                    queue.Enqueue(q);
                }
                if (x > 0) Spread(p - 1);
                if (x < w - 1) Spread(p + 1);
                if (y > 0) Spread(p - w);
                if (y < h - 1) Spread(p + w);
            }
            if (comps.Any(c => c.Count < MinPiecePixels)) { error = "Mảnh cắt ra quá nhỏ"; return 0; }
            if (gapList != null) foreach (var p in gapList) Map.reg[p] = 0;
            return Commit(index, id, comps);
        }

        // ---- Lỗ trong mảnh: lỗ không có cát, không được chạm biên mảnh (chạm biên là một phần bị khoét: sửa biên) ----

        private const float HoleMinCells2 = 100f; // diện tích lỗ nhỏ nhất (ô lưới²), khớp BoundaryGraph.MinHoleArea

        private static List<Vector2> RingOf(int[] pts)
        {
            var o = new List<Vector2>(pts.Length / 2);
            for (var i = 0; i + 1 < pts.Length; i += 2) o.Add(new Vector2(pts[i], pts[i + 1]));
            return o;
        }

        private static float SignedArea(IReadOnlyList<Vector2> r)
        {
            double s = 0;
            for (int i = 0, j = r.Count - 1; i < r.Count; j = i++) s += (double)r[j].x * r[i].y - (double)r[i].x * r[j].y;
            return (float)(s * 0.5);
        }

        private static bool PointIn(IReadOnlyList<Vector2> poly, Vector2 p)
        {
            var inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if (poly[i].y > p.y == poly[j].y > p.y) continue;
                var x = poly[i].x + (p.y - poly[i].y) * (poly[j].x - poly[i].x) / (poly[j].y - poly[i].y);
                if (p.x < x) inside = !inside;
            }
            return inside;
        }

        private static float MinDistTo(Vector2 p, IReadOnlyList<Vector2> ring)
        {
            var best = float.MaxValue;
            for (var i = 0; i < ring.Count; i++) best = Mathf.Min(best, DistToSegment(p, ring[i], ring[(i + 1) % ring.Count]));
            return best;
        }

        private static bool RingsCross(List<Vector2Int> a, IReadOnlyList<Vector2> b)
        {
            for (var i = 0; i < a.Count; i++)
            {
                Vector2Int p = a[i], q = a[(i + 1) % a.Count];
                for (var j = 0; j < b.Count; j++)
                    if (SegCross(p, q, Vector2Int.RoundToInt(b[j]), Vector2Int.RoundToInt(b[(j + 1) % b.Count]))) return true;
            }
            return false;
        }

        // Lỗ hợp lệ: đủ lớn, không tự cắt, nằm gọn trong mảnh cách biên ≥ 1 ô, không chồng lỗ khác (bỏ qua lỗ skip)
        private bool CheckHole(int index, int skip, List<Vector2Int> ringInt, out string error)
        {
            error = null;
            var unit = Mathf.Max(1, Picture.unit);
            if (ringInt.Count < 3) { error = "Lỗ cần ít nhất 3 điểm"; return false; }
            var ring = ringInt.ConvertAll(v => (Vector2)v);
            if (Mathf.Abs(SignedArea(ring)) / (unit * unit) < HoleMinCells2) { error = "Lỗ quá nhỏ"; return false; }
            for (var i = 0; i < ringInt.Count; i++)
                for (var j = i + 2; j < ringInt.Count; j++)
                {
                    if (i == 0 && j == ringInt.Count - 1) continue;
                    if (SegCross(ringInt[i], ringInt[(i + 1) % ringInt.Count], ringInt[j], ringInt[(j + 1) % ringInt.Count])) { error = "Biên lỗ tự cắt nhau, kéo lại"; return false; }
                }
            var outer = RingOf(Picture.regions[index].points);
            const string touch = "Lỗ phải nằm gọn trong mảnh, không chạm biên (phần chạm biên là phần bị khoét: dùng sửa biên)";
            foreach (var v in ring)
                if (!PointIn(outer, v) || MinDistTo(v, outer) < unit) { error = touch; return false; }
            foreach (var v in outer)
                if (MinDistTo(v, ring) < unit) { error = touch; return false; }
            if (RingsCross(ringInt, outer)) { error = touch; return false; }
            var holes = Picture.regions[index].holes;
            if (holes != null)
                for (var h = 0; h < holes.Count; h++)
                {
                    if (h == skip) continue;
                    var other = RingOf(holes[h]);
                    var overlap = RingsCross(ringInt, other);
                    foreach (var v in ring) overlap |= PointIn(other, v);
                    foreach (var v in other) overlap |= PointIn(ring, v);
                    if (overlap) { error = "Lỗ chồng lên lỗ khác"; return false; }
                }
            return true;
        }

        // Các mảnh khác (ngoài host) nằm trọn trong vòng; crossing = mảnh đầu tiên bị vòng cắt ngang (một phần trong, một phần ngoài), -1 nếu không có
        private List<int> PiecesInRing(int host, List<Vector2> ring, out int crossing)
        {
            crossing = -1;
            var inside = new List<int>();
            var ringInt = ring.ConvertAll(Vector2Int.RoundToInt);
            Vector2 min = ring[0], max = ring[0];
            foreach (var v in ring) { min = Vector2.Min(min, v); max = Vector2.Max(max, v); }
            for (var r = 0; r < Picture.regions.Count; r++)
            {
                if (r == host) continue;
                var other = RingOf(Picture.regions[r].points);
                var count = 0;
                Vector2 omin = other[0], omax = other[0];
                foreach (var v in other)
                {
                    if (PointIn(ring, v)) count++;
                    omin = Vector2.Min(omin, v);
                    omax = Vector2.Max(omax, v);
                }
                if (count == other.Count) { inside.Add(r); continue; }
                var overlapBox = omax.x >= min.x && omin.x <= max.x && omax.y >= min.y && omin.y <= max.y;
                if (crossing < 0 && (count > 0 || overlapBox && RingsCross(ringInt, other))) crossing = r;
            }
            return inside;
        }

        // Các mảnh nằm trong lỗ h của mảnh index (số thứ tự mảnh, bắt đầu từ 0)
        internal List<int> PiecesInHole(int index, int h)
        {
            var holes = Picture.regions[index].holes;
            if (holes == null || h < 0 || h >= holes.Count) return new List<int>();
            return PiecesInRing(index, RingOf(holes[h]), out _);
        }

        private static string PieceNames(IEnumerable<int> pieces) => string.Join(", ", pieces.Select(r => "#" + (r + 1)));

        private int[] HoleRing(int index, List<Vector2Int> ringInt)
        {
            var ring = ringInt.ConvertAll(v => (Vector2)v);
            if (Mathf.Sign(SignedArea(ring)) == Mathf.Sign(SignedArea(RingOf(Picture.regions[index].points)))) ringInt = new List<Vector2Int>(ringInt.AsEnumerable().Reverse()); // lỗ ngược chiều vòng ngoài
            var o = new int[ringInt.Count * 2];
            for (var i = 0; i < ringInt.Count; i++) { o[i * 2] = ringInt[i].x; o[i * 2 + 1] = ringInt[i].y; }
            return o;
        }

        // Thêm lỗ cho mảnh index từ vòng điểm (toạ độ lưu, chưa khép lặp điểm đầu)
        internal bool AddHole(int index, List<Vector2Int> ringInt, out string error)
        {
            if (index < 0 || index >= Picture.regions.Count) { error = "Không có mảnh để khoét lỗ"; return false; }
            if (!CheckHole(index, -1, ringInt, out error)) return false;
            var inside = PiecesInRing(index, ringInt.ConvertAll(v => (Vector2)v), out var crossing);
            if (inside.Count > 0 || crossing >= 0) { error = $"Vòng khoét chạm mảnh khác ({PieceNames(inside.Concat(crossing >= 0 ? new[] { crossing } : new int[0]))})"; return false; }
            var r = Picture.regions[index];
            (r.holes ??= new List<int[]>()).Add(HoleRing(index, ringInt));
            r.holeWidths = null;
            return true;
        }

        // Xoá lỗ h của mảnh index: phần lỗ thuộc lại về mảnh
        internal bool RemoveHole(int index, int h, out string error)
        {
            error = null;
            if (index < 0 || index >= Picture.regions.Count) return false;
            var r = Picture.regions[index];
            if (r.holes == null || h < 0 || h >= r.holes.Count) return false;
            var inside = PiecesInHole(index, h);
            if (inside.Count > 0) { error = $"Lỗ đang chứa mảnh {PieceNames(inside)}: gộp hoặc xoá mảnh đó trước khi xoá lỗ"; return false; }
            r.holes.RemoveAt(h);
            if (r.holes.Count == 0) r.holes = null;
            r.holeWidths = null;
            return true;
        }

        // Thay vòng lỗ h của mảnh index
        internal bool ReplaceHole(int index, int h, List<Vector2Int> ringInt, out string error)
        {
            error = null;
            if (index < 0 || index >= Picture.regions.Count) { error = "Không có mảnh"; return false; }
            var r = Picture.regions[index];
            if (r.holes == null || h < 0 || h >= r.holes.Count) { error = "Không có lỗ này"; return false; }
            if (!CheckHole(index, h, ringInt, out error)) return false;
            var before = PiecesInHole(index, h);
            var after = PiecesInRing(index, ringInt.ConvertAll(v => (Vector2)v), out var crossing);
            if (crossing >= 0) { error = $"Lỗ mới cắt vào mảnh {PieceNames(new[] { crossing })} bên trong, kéo lại"; return false; }
            if (!before.OrderBy(x => x).SequenceEqual(after.OrderBy(x => x))) { error = "Lỗ mới phải bao đúng các mảnh đang nằm trong lỗ"; return false; }
            r.holes[h] = HoleRing(index, ringInt);
            r.holeWidths = null;
            return true;
        }

        // Thay đoạn run (theo chiều mảnh ra) bằng whole trong mảnh ra, và bản đảo chiều trong mảnh rb
        private bool SwapRun(int ra, int rb, List<Vector2Int> run, List<Vector2Int> whole, int lo, int hi)
        {
            var A = Picture.regions[ra].points;
            var B = Picture.regions[rb].points;
            var bl = IndexOf(B, run[run.Count - 1]);
            var bh = IndexOf(B, run[0]);
            if (lo < 0 || hi < 0 || lo >= A.Length / 2 || hi >= A.Length / 2 || bl < 0 || bh < 0) return false;
            Picture.regions[ra].points = Replace(A, lo, hi, whole);
            var rev = new List<Vector2Int>(whole);
            rev.Reverse();
            Picture.regions[rb].points = Replace(B, bl, bh, rev);
            return true;
        }

        // Thay đoạn biên chung bằng chuỗi mới (cùng 2 đầu); ghi vào Picture hiện tại nên gọi trên bản clone, lỗi thì bỏ bản clone
        internal bool ReplaceRun(EdgeRun run, IReadOnlyList<Vector2Int> chain, out string error)
        {
            error = null;
            var gapMode = Picture.gen.inkGaps; // mảnh cách nhau bằng khe: mỗi mảnh sửa biên của riêng nó
            if (run.b < 0 && !gapMode) { error = "Biên ngoài tranh không sửa được"; return false; }
            if (chain.Count < 2 || chain[0] != run.pts[0] || chain[chain.Count - 1] != run.pts[run.pts.Count - 1])
            {
                error = "Hai đầu đoạn biên phải giữ nguyên";
                return false;
            }
            if (run.pts[0] == run.pts[run.pts.Count - 1]) { error = "Đường biên khép kín: dùng Gộp/Tách"; return false; }
            var whole = new List<Vector2Int>(chain);
            if (CrossesOthers(whole, run.pts, run.a, gapMode ? -1 : run.b)) { error = "Biên mới cắt qua đường biên khác hoặc tự cắt, kéo lại"; return false; }
            if (gapMode)
            {
                var A = Picture.regions[run.a].points;
                if (run.lo < 0 || run.hi < 0 || run.lo >= A.Length / 2 || run.hi >= A.Length / 2) { error = "Biên không khớp mảnh"; return false; }
                Picture.regions[run.a].points = Replace(A, run.lo, run.hi, whole);
            }
            else if (!SwapRun(run.a, run.b, run.pts, whole, run.lo, run.hi)) { error = "Biên không khớp mảnh kề"; return false; }
            Picture.regions[run.a].widths = null;
            if (!gapMode) Picture.regions[run.b].widths = null;
            var map = RegionRaster.Rasterize(Picture);
            int pa = 0, pb = 0;
            foreach (var id in map.reg)
            {
                if (id == run.a + 1) pa++;
                else if (!gapMode && id == run.b + 1) pb++;
            }
            if (pa < MinPiecePixels || (!gapMode && pb < MinPiecePixels)) { error = "Mảnh bị ép quá nhỏ"; return false; }
            return true;
        }

        // Thay các đỉnh lo..hi (vòng, gồm cả hai đầu) bằng chuỗi mới (cùng 2 đầu)
        private static int[] Replace(int[] pts, int lo, int hi, List<Vector2Int> chain)
        {
            var n = pts.Length / 2;
            var res = new List<int>();
            foreach (var v in chain) { res.Add(v.x); res.Add(v.y); }
            for (var i = (hi + 1) % n; i != lo; i = (i + 1) % n)
            {
                res.Add(pts[i * 2]);
                res.Add(pts[i * 2 + 1]);
                if ((i + 1) % n == lo) break;
            }
            // chuỗi mới đã chứa đỉnh lo và hi; phần còn lại là các đỉnh ngoài đoạn, theo thứ tự vòng
            return res.ToArray();
        }

        // Nét mới có cắt thật sự (không tính chạm đầu mút) cạnh nào của đường biên khác, hay tự cắt mình không; cạnh của chính đoạn biên cũ bị bỏ qua
        private bool CrossesOthers(List<Vector2Int> chain, List<Vector2Int> oldRun, int ra, int rb)
        {
            var old = new HashSet<(Vector2Int, Vector2Int)>();
            for (var i = 0; i + 1 < oldRun.Count; i++)
            {
                old.Add((oldRun[i], oldRun[i + 1]));
                old.Add((oldRun[i + 1], oldRun[i]));
            }
            var bounds = new RectInt[Picture.regions.Count];
            for (var r = 0; r < bounds.Length; r++)
            {
                var pts = Picture.regions[r].points;
                int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
                for (var i = 0; i + 1 < pts.Length; i += 2)
                {
                    x0 = Mathf.Min(x0, pts[i]); x1 = Mathf.Max(x1, pts[i]);
                    y0 = Mathf.Min(y0, pts[i + 1]); y1 = Mathf.Max(y1, pts[i + 1]);
                }
                bounds[r] = new RectInt(x0, y0, x1 - x0, y1 - y0);
            }
            var segs = chain.Count - 1;
            var minX = new int[segs]; var maxX = new int[segs]; var minY = new int[segs]; var maxY = new int[segs];
            for (var k = 0; k < segs; k++)
            {
                Vector2Int p = chain[k], q = chain[k + 1];
                minX[k] = p.x < q.x ? p.x : q.x; maxX[k] = p.x < q.x ? q.x : p.x;
                minY[k] = p.y < q.y ? p.y : q.y; maxY[k] = p.y < q.y ? q.y : p.y;
            }
            for (var k = 0; k < segs; k++)
            {
                int sx0 = minX[k], sx1 = maxX[k], sy0 = minY[k], sy1 = maxY[k];
                for (var m = k + 2; m < segs; m++)
                {
                    if (sx1 < minX[m] || sx0 > maxX[m] || sy1 < minY[m] || sy0 > maxY[m]) continue; // hộp bao hai đoạn tách nhau
                    if (SegCross(chain[k], chain[k + 1], chain[m], chain[m + 1])) return true;
                }
                for (var r = 0; r < Picture.regions.Count; r++)
                {
                    var bb = bounds[r];
                    if (sx1 < bb.xMin || sx0 > bb.xMax || sy1 < bb.yMin || sy0 > bb.yMax) continue; // đoạn nằm ngoài hộp bao của mảnh: không thể cắt
                    var pts = Picture.regions[r].points;
                    var n = pts.Length / 2;
                    for (var i = 0; i < n; i++)
                    {
                        var j = (i + 1) % n;
                        int cx = pts[i * 2], cy = pts[i * 2 + 1], dx = pts[j * 2], dy = pts[j * 2 + 1];
                        if (sx1 < (cx < dx ? cx : dx) || sx0 > (cx < dx ? dx : cx) || sy1 < (cy < dy ? cy : dy) || sy0 > (cy < dy ? dy : cy)) continue; // hộp bao hai đoạn tách nhau
                        var c = new Vector2Int(cx, cy);
                        var d = new Vector2Int(dx, dy);
                        if ((r == ra || r == rb) && old.Contains((c, d))) continue;
                        if (SegCross(chain[k], chain[k + 1], c, d)) return true;
                    }
                }
            }
            return false;
        }

        // Hai đoạn cắt nhau thật sự (dấu chéo trái ngược, không chung đầu mút)
        private static bool SegCross(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
        {
            if (a == c || a == d || b == c || b == d) return false;
            long Cr(Vector2Int p, Vector2Int q, Vector2Int r) => (long)(q.x - p.x) * (r.y - p.y) - (long)(q.y - p.y) * (r.x - p.x);
            var d1 = Cr(a, b, c);
            var d2 = Cr(a, b, d);
            var d3 = Cr(c, d, a);
            var d4 = Cr(c, d, b);
            return (d1 > 0 && d2 < 0 || d1 < 0 && d2 > 0) && (d3 > 0 && d4 < 0 || d3 < 0 && d4 > 0);
        }

        private static int IndexOf(int[] pts, Vector2Int v)
        {
            for (var i = 0; i < pts.Length / 2; i++)
                if (pts[i * 2] == v.x && pts[i * 2 + 1] == v.y) return i;
            return -1;
        }

        internal static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var l2 = ab.sqrMagnitude;
            var t = l2 < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            return Vector2.Distance(p, a + ab * t);
        }

        // Số mảnh gợi ý khi chia mảnh index (theo diện tích mảnh so với cả tranh)
        public int RecommendSplit(int index)
        {
            if (index < 0 || index >= Picture.regions.Count) return Subdivide.MinCount;
            var px = 0;
            var id = index + 1;
            foreach (var r in Map.reg) if (r == id) px++;
            return Subdivide.Recommend(px / (float)(Map.w * Map.h));
        }

        // Chia từng mảnh được chọn thành các ô ôm theo hình thể (xem Subdivide); ô quá nhỏ gộp vào ô kề; trả về số mảnh mới thêm
        public int SubdivideRegions(IReadOnlyList<int> indices, int count, out string error)
        {
            error = null;
            var touched = new List<int>();
            var added = 0;
            foreach (var index in indices.Distinct().OrderBy(i => i))
            {
                if (index < 0 || index >= Picture.regions.Count) continue;
                var id = index + 1;
                var pix = Pixels(id);
                var cell = Subdivide.FlowAssign(Map, pix, count, out var n);
                if (cell == null) continue;

                var groups = new List<int>[n];
                for (var k = 0; k < n; k++) groups[k] = new List<int>();
                for (var i = 0; i < pix.Count; i++) groups[cell[i]].Add(pix[i]);
                for (var k = 0; k < n; k++) foreach (var p in groups[k]) Map.reg[p] = -(k + 2);
                var pieces = new List<List<int>>();
                for (var k = 0; k < n; k++) pieces.AddRange(Components(groups[k], -(k + 2)));

                for (var j = 0; j < pieces.Count; j++) foreach (var p in pieces[j]) Map.reg[p] = -(j + 2); // nhãn tạm riêng từng cụm
                foreach (var j in Enumerable.Range(0, pieces.Count).OrderBy(j => pieces[j].Count))
                {
                    if (pieces[j].Count == 0 || pieces[j].Count >= MinPiecePixels) continue;
                    var near = new Dictionary<int, int>();
                    foreach (var p in pieces[j])
                    {
                        int x = p % Map.w, y = p / Map.w;
                        void Look(int q) { if (Map.reg[q] < -1 && Map.reg[q] != -(j + 2)) near[Map.reg[q]] = near.TryGetValue(Map.reg[q], out var c) ? c + 1 : 1; }
                        if (x > 0) Look(p - 1);
                        if (x < Map.w - 1) Look(p + 1);
                        if (y > 0) Look(p - Map.w);
                        if (y < Map.h - 1) Look(p + Map.w);
                    }
                    if (near.Count == 0) continue;
                    var to = -near.OrderByDescending(kv => kv.Value).First().Key - 2;
                    foreach (var p in pieces[j]) Map.reg[p] = -(to + 2);
                    pieces[to].AddRange(pieces[j]);
                    pieces[j].Clear();
                }
                pieces.RemoveAll(pc => pc.Count == 0);
                if (pieces.Count < 2)
                {
                    foreach (var p in pix) Map.reg[p] = id;
                    continue;
                }
                var firstNew = Picture.regions.Count;
                added += Commit(index, id, pieces, touched);
                Recolor(index, firstNew);
            }
            if (touched.Count == 0)
            {
                error = "Không chia được: mảnh quá nhỏ hoặc quá hẹp";
                return 0;
            }
            RebuildBoundaries(touched);
            return added;
        }

        // Ô mới đổi sang màu họ hàng theo bảng kề nhau học từ game; ô đầu giữ màu gốc, ô sau tránh trùng ô liền trước
        private void Recolor(int index, int firstNew)
        {
            var rng = new System.Random(index * 7919 + 17);
            var baseColor = Picture.regions[index].colorId;
            var baseLab = ColorPalette.ToLab(ColorPalette.Get(baseColor));
            var edge = TouchesBorder(index + 1); // mảnh sát mép tranh là nền: tự do đổi màu; chủ thể giữ tông gần màu gốc
            var prev = baseColor;
            for (var r = firstNew; r < Picture.regions.Count; r++)
            {
                var c = baseColor;
                for (var t = 0; t < 12; t++)
                {
                    var cand = ColorAdjacency.Pick(baseColor, prev, rng);
                    if (edge || (ColorPalette.ToLab(ColorPalette.Get(cand)) - baseLab).magnitude <= SubjectTone) { c = cand; break; }
                }
                Picture.regions[r].colorId = c;
                Map.colors[r + 1] = c;
                prev = c;
            }
        }

        private const float SubjectTone = 38f; // chủ thể: màu ô mới cách màu gốc không quá ΔE này

        private bool TouchesBorder(int id)
        {
            int w = Map.w, h = Map.h;
            for (var x = 0; x < w; x++) if (Map.reg[x] == id || Map.reg[(h - 1) * w + x] == id) return true;
            for (var y = 0; y < h; y++) if (Map.reg[y * w] == id || Map.reg[y * w + w - 1] == id) return true;
            return false;
        }

        // Gán id cho các mảnh (mảnh đầu giữ id cũ), dựng lại biên; trả về số mảnh mới
        private int Commit(int index, int id, List<List<int>> pieces, List<int> touched = null)
        {
            var color = Picture.regions[index].colorId;
            Picture.regions[index].valueManual = false; // mảnh bị cắt hoặc chia: tính lại số cát
            var baseCount = Picture.regions.Count;
            var ids = new int[pieces.Count];
            for (var k = 0; k < pieces.Count; k++)
            {
                ids[k] = k == 0 ? id : baseCount + k;
                foreach (var p in pieces[k]) Map.reg[p] = ids[k];
            }
            for (var k = 1; k < pieces.Count; k++) Map.colors.Add(color);
            var bounds = Map.Bounds();
            Picture.regions[index].points = Trace(id, bounds);
            for (var k = 1; k < pieces.Count; k++)
                Picture.regions.Add(new RegionData { points = Trace(ids[k], bounds), colorId = color });
            if (touched != null) touched.AddRange(ids); // chia hàng loạt: dựng lại biên 1 lần ở cuối
            else RebuildBoundaries(ids);
            return pieces.Count - 1;
        }

        // Tranh vector hoá: dựng lại biên chung cho mảnh bị sửa và các mảnh kề để chúng khít nhau; mảnh khác giữ nguyên (không bị làm mượt lại)
        private void RebuildBoundaries(IEnumerable<int> editedIds)
        {
            var g = Picture.gen;
            if (Picture.unit <= 1 || !g.tidy || g.curveSmooth <= 0f) return;
            var affected = new HashSet<int>(editedIds);
            foreach (var id in editedIds) AddNeighbours(id, affected);

            // chỉ dựng đồ thị biên trong khung bao các mảnh bị ảnh hưởng (chừa lề), không dựng lại cả tranh
            var all = Map.Bounds();
            int x0 = Map.w, y0 = Map.h, x1 = 0, y1 = 0;
            foreach (var id in affected)
            {
                if (id < 1 || id >= all.Length) continue;
                var b = all[id];
                if (b.width == 0) continue; // mảnh không còn pixel
                x0 = Mathf.Min(x0, b.xMin); y0 = Mathf.Min(y0, b.yMin); x1 = Mathf.Max(x1, b.xMax); y1 = Mathf.Max(y1, b.yMax);
            }
            x0 = Mathf.Max(0, x0 - CropMargin); y0 = Mathf.Max(0, y0 - CropMargin);
            x1 = Mathf.Min(Map.w, x1 + CropMargin); y1 = Mathf.Min(Map.h, y1 + CropMargin);
            if (x1 <= x0 || y1 <= y0) return;
            int cw = x1 - x0, ch = y1 - y0;
            var crop = new RegionMap { w = cw, h = ch, reg = new int[cw * ch], colors = Map.colors, inked = Map.inked, gaps = Map.gaps, synthetic = Map.synthetic };
            for (var y = 0; y < ch; y++) System.Array.Copy(Map.reg, (y0 + y) * Map.w + x0, crop.reg, y * cw, cw);

            var polys = BoundaryGraph.Build(crop, PictureGenerator.Sigma(g), Picture.unit, PictureGenerator.TidyStep, g.fitTolerance, out var holes);
            int ox = x0 * Picture.unit, oy = y0 * Picture.unit;
            int[] Shift(int[] a)
            {
                var r = new int[a.Length];
                for (var k = 0; k + 1 < a.Length; k += 2) { r[k] = a[k] + ox; r[k + 1] = a[k + 1] + oy; }
                return r;
            }
            foreach (var id in affected)
            {
                if (id < 1 || id >= polys.Length || id > Picture.regions.Count || polys[id] == null) continue;
                Picture.regions[id - 1].points = Shift(polys[id]);
                Picture.regions[id - 1].holes = holes[id]?.ConvertAll(Shift);
            }
        }

        private const int CropMargin = 4; // lề (ô lưới) quanh khung dựng lại biên

        private void AddNeighbours(int id, HashSet<int> into)
        {
            int w = Map.w, h = Map.h;
            for (var p = 0; p < Map.reg.Length; p++)
            {
                if (Map.reg[p] != id) continue;
                int x = p % w, y = p / w;
                void Add(int q) { if (Map.reg[q] > 0) into.Add(Map.reg[q]); }
                if (x > 0) Add(p - 1);
                if (x < w - 1) Add(p + 1);
                if (y > 0) Add(p - w);
                if (y < h - 1) Add(p + w);
            }
        }

        private List<List<int>> Components(List<int> pixels, int label)
        {
            var result = new List<List<int>>();
            var seen = new HashSet<int>();
            foreach (var seed in pixels)
            {
                if (!seen.Add(seed)) continue;
                var piece = new List<int> { seed };
                var stack = new Stack<int>();
                stack.Push(seed);
                while (stack.Count > 0)
                {
                    var p = stack.Pop();
                    int x = p % Map.w, y = p / Map.w;
                    void Visit(int q)
                    {
                        if (Map.reg[q] != label || !seen.Add(q)) return;
                        piece.Add(q);
                        stack.Push(q);
                    }
                    if (x > 0) Visit(p - 1);
                    if (x < Map.w - 1) Visit(p + 1);
                    if (y > 0) Visit(p - Map.w);
                    if (y < Map.h - 1) Visit(p + Map.w);
                }
                result.Add(piece);
            }
            return result;
        }

        private List<int> Pixels(int id)
        {
            var l = new List<int>();
            for (var p = 0; p < Map.reg.Length; p++)
                if (Map.reg[p] == id) l.Add(p);
            return l;
        }

        private bool Touches(List<int> pixels, int id)
        {
            foreach (var p in pixels)
            {
                int x = p % Map.w, y = p / Map.w;
                if (x > 0 && Map.reg[p - 1] == id || x < Map.w - 1 && Map.reg[p + 1] == id ||
                    y > 0 && Map.reg[p - Map.w] == id || y < Map.h - 1 && Map.reg[p + Map.w] == id) return true;
            }
            return false;
        }

        private int[] Trace(int id, RectInt[] bounds)
        {
            var outer = RegionTracer.TraceOuter(Map.reg, Map.w, Map.h, id, bounds[id]);
            var unit = Mathf.Max(1, Picture.unit);
            var pts = PictureGenerator.Polygon(outer, Picture.gen, unit);
            if (pts != null) return pts;
            var poly = RegionTracer.Simplify(outer, PictureGenerator.Epsilon); // suy biến sau làm mượt: dùng bản thô
            pts = new int[poly.Count * 2];
            for (var i = 0; i < poly.Count; i++)
            {
                pts[i * 2] = poly[i].x * unit;
                pts[i * 2 + 1] = poly[i].y * unit;
            }
            return pts;
        }

        // Xoá các mảnh theo chỉ số (để lại khoảng trống); trả về số mảnh đã xoá
        internal int RemovePieces(IEnumerable<int> indices)
        {
            var n = 0;
            foreach (var i in indices.Distinct().OrderByDescending(x => x))
            {
                if (i < 0 || i >= Picture.regions.Count) continue;
                Picture.regions.RemoveAt(i);
                n++;
            }
            return n;
        }

        private void RemoveRegion(int index)
        {
            var id = index + 1;
            Picture.regions.RemoveAt(index);
            Map.colors.RemoveAt(id);
            for (var p = 0; p < Map.reg.Length; p++)
                if (Map.reg[p] > id) Map.reg[p]--;
        }
    }
}
