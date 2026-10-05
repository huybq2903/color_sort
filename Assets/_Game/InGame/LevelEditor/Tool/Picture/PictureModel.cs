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

        private const float PickRadius = 6f, RedrawSmooth = 0.5f; // bán kính chọn (ô lưới); vuốt mạnh hơn mặc định

        public PictureProperty Picture { get; }
        public RegionMap Map { get; }

        public PictureModel(PictureProperty p)
        {
            Picture = p;
            Map = RegionRaster.Rasterize(p);
        }

        private Vector2 Clamp(Vector2 p) => new(Mathf.Clamp(p.x, 0f, Picture.width), Mathf.Clamp(p.y, 0f, Picture.height));

        private const float DotFraction = 0.016f; // đường kính chấm mặc định theo chiều cao tranh

        // Thêm (hoặc thay nét số replace) nét trang trí từ đường vẽ (toạ độ lưới tranh); chỉ 1 điểm thì thành chấm
        public bool AddLine(IReadOnlyList<Vector2> pic, int replace = -1)
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
            if (replace >= 0 && replace < Picture.lines.Count) { Picture.lines[replace] = o; Picture.lineWidths[replace] = width; }
            else { Picture.lines.Add(o); Picture.lineWidths.Add(width); }
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
            return true;
        }

        // Điểm gần nhất trên đường biên mọi mảnh trong bán kính (toạ độ lưới); false nếu không có
        public bool NearestBoundaryPoint(Vector2 pic, float radius, out Vector2 point)
        {
            point = pic;
            var unit = Mathf.Max(1, Picture.unit);
            var bestD = radius;
            var found = false;
            foreach (var r in Picture.regions)
            {
                var pts = r.points;
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
            return true;
        }

        private void SyncLineWidths()
        {
            Picture.lines ??= new List<int[]>();
            Picture.lineWidths ??= new List<int>();
            while (Picture.lineWidths.Count < Picture.lines.Count) Picture.lineWidths.Add(0);
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
            if (!Touches(otherPx, tid)) { error = "Hai mảnh không kề nhau"; return -1; }

            foreach (var p in otherPx) Map.reg[p] = tid;
            if (!SupportsHoles && PixelRegions.HasHole(Map, Pixels(tid)))
            {
                foreach (var p in otherPx) Map.reg[p] = oid;
                error = "Gộp sẽ tạo mảnh có lỗ (vây kín mảnh khác)";
                return -1;
            }

            Picture.regions[target].points = Trace(tid, Map.Bounds());
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

        // Cắt theo đường cong tự do: đường đi (toạ độ lưới) là rào, mỗi cụm liền còn lại là 1 mảnh; hai đầu được kéo dài để chắc chắn ra tới biên mảnh
        public int SplitPath(int index, IReadOnlyList<Vector2> path, out string error)
        {
            error = null;
            var id = index + 1;
            if (path == null || path.Count < 2) { error = "Đường cắt quá ngắn"; return 0; }
            var pts = new List<Vector2>(path);
            var far = Mathf.Max(Map.w, Map.h);
            var d0 = (pts[0] - pts[Mathf.Min(3, pts.Count - 1)]).normalized;
            var d1 = (pts[pts.Count - 1] - pts[Mathf.Max(0, pts.Count - 4)]).normalized;
            if (d0 != Vector2.zero) pts.Insert(0, pts[0] + d0 * far);
            if (d1 != Vector2.zero) pts.Add(pts[pts.Count - 1] + d1 * far);

            var inRegion = new HashSet<int>(Pixels(id));
            var barrier = new HashSet<int>();
            for (var k = 0; k + 1 < pts.Count; k++)
            {
                var a = pts[k];
                var b = pts[k + 1];
                var steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude * 2f));
                for (var t = 0; t <= steps; t++)
                {
                    var q = Vector2.Lerp(a, b, t / (float)steps);
                    int x = Mathf.FloorToInt(q.x), y = Mathf.FloorToInt(q.y);
                    if (x < 0 || y < 0 || x >= Map.w || y >= Map.h) continue;
                    var i = y * Map.w + x;
                    if (inRegion.Contains(i)) barrier.Add(i);
                }
            }
            if (barrier.Count == 0) { error = "Đường cắt không đi qua mảnh"; return 0; }

            var comp = new Dictionary<int, int>();
            var comps = new List<List<int>>();
            foreach (var seed in inRegion)
            {
                if (barrier.Contains(seed) || comp.ContainsKey(seed)) continue;
                var list = new List<int>();
                var stack = new Stack<int>();
                comp[seed] = comps.Count;
                stack.Push(seed);
                while (stack.Count > 0)
                {
                    var p = stack.Pop();
                    list.Add(p);
                    int x = p % Map.w, y = p / Map.w;
                    void Visit(int q)
                    {
                        if (!inRegion.Contains(q) || barrier.Contains(q) || comp.ContainsKey(q)) return;
                        comp[q] = comps.Count;
                        stack.Push(q);
                    }
                    if (x > 0) Visit(p - 1);
                    if (x < Map.w - 1) Visit(p + 1);
                    if (y > 0) Visit(p - Map.w);
                    if (y < Map.h - 1) Visit(p + Map.w);
                }
                comps.Add(list);
            }
            if (comps.Count < 2) { error = "Đường cắt chưa chia mảnh làm đôi (kéo xuyên qua mảnh)"; return 0; }

            // pixel trên đường cắt thuộc cụm kề nó
            var queue = new Queue<int>(comp.Keys);
            var pending = new HashSet<int>(barrier);
            while (queue.Count > 0 && pending.Count > 0)
            {
                var p = queue.Dequeue();
                int x = p % Map.w, y = p / Map.w;
                void Spread(int q)
                {
                    if (!pending.Remove(q)) return;
                    comp[q] = comp[p];
                    comps[comp[p]].Add(q);
                    queue.Enqueue(q);
                }
                if (x > 0) Spread(p - 1);
                if (x < Map.w - 1) Spread(p + 1);
                if (y > 0) Spread(p - Map.w);
                if (y < Map.h - 1) Spread(p + Map.w);
            }
            if (comps.Any(c => c.Count < MinPiecePixels)) { error = "Mảnh cắt ra quá nhỏ"; return 0; }
            return Commit(index, id, comps);
        }

        // Vẽ lại đường biên chung: nét vẽ (toạ độ lưới) bắt đầu gần một đường biên; đoạn biên giữa đỉnh gần đầu nét và đỉnh gần cuối nét được thay bằng nét vẽ, cả 2 mảnh kề cùng đổi nên vẫn khít nhau
        public bool RedrawBoundary(IReadOnlyList<Vector2> path, out string error)
        {
            error = null;
            if (path == null || path.Count < 2) { error = "Nét vẽ quá ngắn"; return false; }
            path = path.Select(Clamp).ToList(); // nét vẽ ngoài khung bị kẹp lại, mảnh không tràn ra ngoài
            var unit = Mathf.Max(1, Picture.unit);
            var pic = path[0];
            var cands = new List<(float d, int r, int e)>();
            for (var r = 0; r < Picture.regions.Count; r++)
            {
                var pts = Picture.regions[r].points;
                var n = pts.Length / 2;
                for (var i = 0; i < n; i++)
                {
                    var a = new Vector2(pts[i * 2], pts[i * 2 + 1]) / unit;
                    var j = (i + 1) % n;
                    var b = new Vector2(pts[j * 2], pts[j * 2 + 1]) / unit;
                    var d = DistToSegment(pic, a, b);
                    if (d <= PickRadius) cands.Add((d, r, i));
                }
            }
            if (cands.Count == 0) { error = "Bắt đầu nét vẽ gần một đường biên"; return false; }
            cands.Sort((x, y) => x.d.CompareTo(y.d));
            foreach (var (_, ra, ea) in cands)
            {
            var A = Picture.regions[ra].points;
            var na = A.Length / 2;
            var e0 = (A[ea * 2], A[ea * 2 + 1]);
            var e1 = (A[(ea + 1) % na * 2], A[(ea + 1) % na * 2 + 1]);
            for (var rb = 0; rb < Picture.regions.Count; rb++)
            {
                if (rb == ra) continue;
                var B = Picture.regions[rb].points;
                var set = new HashSet<(int, int)>();
                for (var k = 0; k < B.Length; k += 2) set.Add((B[k], B[k + 1]));
                if (!set.Contains(e0) || !set.Contains(e1)) continue;

                var inB = new bool[na];
                for (var i = 0; i < na; i++) inB[i] = set.Contains((A[i * 2], A[i * 2 + 1]));
                int lo = ea, hi = (ea + 1) % na, steps = 0;
                while (inB[(lo - 1 + na) % na] && steps++ < na) lo = (lo - 1 + na) % na;
                steps = 0;
                while (inB[(hi + 1) % na] && steps++ < na) hi = (hi + 1) % na;
                var run = new List<Vector2Int>();
                for (var i = lo; ; i = (i + 1) % na)
                {
                    run.Add(new Vector2Int(A[i * 2], A[i * 2 + 1]));
                    if (i == hi) break;
                    if (run.Count > na) { error = "Đường biên khép kín: dùng Gộp/Tách"; return false; }
                }
                if (run.Count < 2) { error = "Đường quá ngắn"; return false; }

                // đầu và cuối nét vẽ bám vào đỉnh gần nhất của đường biên; nét vẽ ngược chiều đường thì đảo lại
                var from = NearestVertex(run, path[0] * unit);
                var to = NearestVertex(run, path[path.Count - 1] * unit);
                var reversed = from > to;
                if (reversed) (from, to) = (to, from);
                if (to == from) { error = "Vẽ dọc theo đường biên từ điểm này tới điểm kia"; return false; }

                var drawn = new List<Vector2Int>();
                foreach (var v in path)
                {
                    var q = Vector2Int.RoundToInt(v * unit);
                    if (drawn.Count == 0 || drawn[drawn.Count - 1] != q) drawn.Add(q);
                }
                if (reversed) drawn.Reverse();
                if (drawn.Count < 2) drawn.Add(drawn[0]);
                drawn[0] = run[from];
                drawn[drawn.Count - 1] = run[to];
                var d = new GenSettings();
                var sm = drawn.Count >= 8
                    ? ContourSmoother.SmoothArc(drawn, PictureGenerator.Sigma(d) * unit * RedrawSmooth, resampleStep: PictureGenerator.TidyStep * unit, fitTolerance: d.fitTolerance * unit * RedrawSmooth)
                    : drawn.ConvertAll(v => (Vector2)v);
                var chain = new List<Vector2Int>();
                foreach (var v in sm)
                {
                    var q = Vector2Int.RoundToInt(v);
                    if (chain.Count == 0 || chain[chain.Count - 1] != q) chain.Add(q);
                }
                chain[0] = run[from];
                chain[chain.Count - 1] = run[to];

                if (CrossesOthers(chain, run, ra, rb)) { error = "Nét vẽ cắt qua đường biên khác hoặc tự cắt chính nó, vẽ lại"; return false; }

                var whole = new List<Vector2Int>(run.GetRange(0, from));
                whole.AddRange(chain);
                whole.AddRange(run.GetRange(to + 1, run.Count - to - 1));
                if (!SwapRun(ra, rb, run, whole, lo, hi)) { error = "Biên không khớp mảnh kề"; return false; }
                return true;
            }
            }
            error = "Đây là biên ngoài tranh, không có mảnh kề";
            return false;
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
            if (run.b < 0) { error = "Biên ngoài tranh không sửa được"; return false; }
            if (chain.Count < 2 || chain[0] != run.pts[0] || chain[chain.Count - 1] != run.pts[run.pts.Count - 1])
            {
                error = "Hai đầu đoạn biên phải giữ nguyên";
                return false;
            }
            if (run.pts[0] == run.pts[run.pts.Count - 1]) { error = "Đường biên khép kín: dùng Gộp/Tách"; return false; }
            var whole = new List<Vector2Int>(chain);
            if (CrossesOthers(whole, run.pts, run.a, run.b)) { error = "Biên mới cắt qua đường biên khác hoặc tự cắt, kéo lại"; return false; }
            if (!SwapRun(run.a, run.b, run.pts, whole, run.lo, run.hi)) { error = "Biên không khớp mảnh kề"; return false; }
            Picture.regions[run.a].widths = null;
            Picture.regions[run.b].widths = null;
            var map = RegionRaster.Rasterize(Picture);
            int pa = 0, pb = 0;
            foreach (var id in map.reg)
            {
                if (id == run.a + 1) pa++;
                else if (id == run.b + 1) pb++;
            }
            if (pa < MinPiecePixels || pb < MinPiecePixels) { error = "Mảnh bị ép quá nhỏ"; return false; }
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
            for (var k = 0; k + 1 < chain.Count; k++)
            {
                for (var m = k + 2; m + 1 < chain.Count; m++)
                    if (SegCross(chain[k], chain[k + 1], chain[m], chain[m + 1])) return true;
                for (var r = 0; r < Picture.regions.Count; r++)
                {
                    var pts = Picture.regions[r].points;
                    var n = pts.Length / 2;
                    for (var i = 0; i < n; i++)
                    {
                        var c = new Vector2Int(pts[i * 2], pts[i * 2 + 1]);
                        var j = (i + 1) % n;
                        var d = new Vector2Int(pts[j * 2], pts[j * 2 + 1]);
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

        private static int NearestVertex(List<Vector2Int> run, Vector2 p)
        {
            var best = 0;
            var bd = float.MaxValue;
            for (var i = 0; i < run.Count; i++)
            {
                var d = ((Vector2)run[i] - p).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
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
            var polys = BoundaryGraph.Build(Map, PictureGenerator.Sigma(g), Picture.unit, PictureGenerator.TidyStep, g.fitTolerance, out var holes);
            foreach (var id in affected)
            {
                if (id < 1 || id >= polys.Length || id > Picture.regions.Count || polys[id] == null) continue;
                Picture.regions[id - 1].points = polys[id];
                Picture.regions[id - 1].holes = holes[id];
            }
        }

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
