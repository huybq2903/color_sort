using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Đồ thị biên chung: mỗi đường ranh làm mượt đúng 1 lần, hai mảnh kề dùng chung điểm y hệt.</summary>
    public static class BoundaryGraph
    {
        private const byte R = 1, U = 2, L = 4, D = 8;
        private static readonly int[] Dx = { 1, 0, -1, 0 };
        private static readonly int[] Dy = { 0, 1, 0, -1 };

        private class Arc
        {
            public Vector2Int[] grid;   // đỉnh lưới gốc, từ nút đầu đến nút cuối (vòng kín: bỏ điểm lặp)
            public int left, right;     // nhãn bên trái / phải khi đi theo chiều grid
            public int startNode, endNode;
            public bool closed;
            public Vector2Int[] smooth; // điểm đã làm mượt, đã nhân unit và làm tròn
        }

        // Trả về polygon (int[] x0,y0,x1,y1,...) theo id vùng, nhân unit; null nếu vùng suy biến
        public static int[][] Build(RegionMap map, float sigma, int unit, float resampleStep = 2.5f, float fitTolerance = 0f) =>
            Build(map, sigma, unit, resampleStep, fitTolerance, out _);

        // holes[id] = các lỗ của vùng id (null nếu không có); polygon ngoài CCW, lỗ CW
        public static int[][] Build(RegionMap map, float sigma, int unit, float resampleStep, float fitTolerance, out List<int[]>[] holes)
        {
            int w = map.w, h = map.h, vw = w + 1;
            var mask = new byte[vw * (h + 1)];
            int Lab(int x, int y) => x < 0 || y < 0 || x >= w || y >= h ? 0 : map.reg[y * w + x];

            for (var y = 0; y <= h; y++)
            for (var x = 0; x < w; x++)
                if (Lab(x, y) != Lab(x, y - 1)) { mask[y * vw + x] |= R; mask[y * vw + x + 1] |= L; }
            for (var y = 0; y < h; y++)
            for (var x = 0; x <= w; x++)
                if (Lab(x, y) != Lab(x - 1, y)) { mask[y * vw + x] |= U; mask[(y + 1) * vw + x] |= D; }

            var arcs = new List<Arc>();
            var visited = new byte[mask.Length];
            bool IsNode(int v) => Bits(mask[v]) >= 3;

            for (var v = 0; v < mask.Length; v++)
            {
                if (!IsNode(v)) continue;
                for (var d = 0; d < 4; d++)
                    if ((mask[v] & (1 << d)) != 0 && (visited[v] & (1 << d)) == 0)
                        arcs.Add(Walk(v, d, false));
            }
            for (var v = 0; v < mask.Length; v++)
            {
                if (Bits(mask[v]) != 2) continue;
                for (var d = 0; d < 4; d++)
                    if ((mask[v] & (1 << d)) != 0 && (visited[v] & (1 << d)) == 0)
                    {
                        arcs.Add(Walk(v, d, true));
                        break;
                    }
            }

            Arc Walk(int start, int d0, bool loop)
            {
                var pts = new List<Vector2Int> { new(start % vw, start / vw) };
                int cur = start, d = d0;
                var a = new Arc { startNode = start, closed = loop };
                var (l0, r0) = Sides(start % vw, start / vw, d0);
                a.left = l0; a.right = r0;
                while (true)
                {
                    visited[cur] |= (byte)(1 << d);
                    var nx = cur % vw + Dx[d];
                    var ny = cur / vw + Dy[d];
                    var next = ny * vw + nx;
                    visited[next] |= (byte)(1 << ((d + 2) & 3));
                    cur = next;
                    if (loop && cur == start) break;
                    pts.Add(new Vector2Int(nx, ny));
                    if (!loop && IsNode(cur)) break;
                    var rest = mask[cur] & ~(1 << ((d + 2) & 3));
                    d = rest == R ? 0 : rest == U ? 1 : rest == L ? 2 : 3;
                }
                a.grid = pts.ToArray();
                a.endNode = cur;
                return a;
            }

            (int left, int right) Sides(int x, int y, int d) => d switch
            {
                0 => (Lab(x, y), Lab(x, y - 1)),
                1 => (Lab(x - 1, y), Lab(x, y)),
                2 => (Lab(x - 1, y - 1), Lab(x - 1, y)),
                _ => (Lab(x, y - 1), Lab(x - 1, y - 1)),
            };


            // làm mượt: các đường nối tiếp gần thẳng hàng qua ngã ba gộp thành 1 chuỗi, mỗi đường/chuỗi làm mượt đúng 1 lần
            var nodePos = new Dictionary<int, Vector2>();
            var smoothed = new Dictionary<Arc, List<Vector2>>();
            foreach (var chain in Chains(arcs))
                if (chain.Count > 1) SmoothChain(chain, sigma, resampleStep, fitTolerance, smoothed, nodePos);
            foreach (var a in arcs)
            {
                if (smoothed.ContainsKey(a)) continue;
                smoothed[a] = a.closed
                    ? ContourSmoother.Smooth(a.grid, sigma, resampleStep: resampleStep, fitTolerance: fitTolerance)
                    : ContourSmoother.SmoothArc(a.grid, sigma, resampleStep: resampleStep, fitTolerance: fitTolerance);
            }
            foreach (var a in arcs)
            {
                var sm = smoothed[a];
                if (!a.closed)
                {
                    // đầu đường nằm ở ngã ba đã dời theo chuỗi khác: kéo về đúng điểm đó, êm dần dọc đường
                    if (nodePos.TryGetValue(a.startNode, out var ps) && (ps - sm[0]).magnitude < MaxNodeShift) Blend(sm, ps - sm[0], true);
                    if (nodePos.TryGetValue(a.endNode, out var pe) && (pe - sm[sm.Count - 1]).magnitude < MaxNodeShift) Blend(sm, pe - sm[sm.Count - 1], false);
                }
                if (!NearRaw(sm, a.grid)) sm = a.grid.Select(v => new Vector2(v.x, v.y)).ToList(); // làm mượt văng khỏi biên thô (chuỗi cắt sai): dùng biên thô
                a.smooth = Scale(sm, unit, a.closed);
            }

            // ghép polygon: mỗi vùng đi ngược chiều kim đồng hồ (vùng nằm bên trái)
            var count = map.Count;
            var outgoing = new List<(Arc arc, bool rev)>[count + 1];
            for (var i = 0; i <= count; i++) outgoing[i] = new List<(Arc, bool)>();
            foreach (var a in arcs)
            {
                if (a.left > 0 && a.left <= count) outgoing[a.left].Add((a, false));
                if (a.right > 0 && a.right <= count) outgoing[a.right].Add((a, true)); // vòng kín: phía phải là vùng bao quanh, vòng thành lỗ
            }

            (int[][] polys, List<int[]>[] holes) Assemble()
            {
                var polys = new int[count + 1][];
                var holeSets = new List<int[]>[count + 1];
                for (var id = 1; id <= count; id++)
                {
                    var list = outgoing[id];
                    var used = new HashSet<(Arc, bool)>();
                    var loops = new List<(Vector2Int[] pts, double area)>();
                    foreach (var start in list)
                    {
                        if (used.Contains(start)) continue;
                        var pts = new List<Vector2Int>();
                        var cur = start;
                        var guard = list.Count + 2;
                        while (guard-- > 0)
                        {
                            used.Add(cur);
                            Append(pts, cur.arc, cur.rev);
                            if (cur.arc.closed) break;
                            var endNode = cur.rev ? cur.arc.startNode : cur.arc.endNode;
                            var next = Next(list, used, endNode, cur, start);
                            if (next.arc == null || next.Equals(start)) break;
                            cur = next;
                        }
                        loops.Add((pts.ToArray(), SignedArea(pts)));
                    }
                    if (loops.Count == 0) continue;
                    // vòng ngoài = vòng có |diện tích| lớn nhất (nếu bị lật chiều thì đảo lại); vòng cùng chiều nối vào (mảnh tự chạm tại 1 điểm), vòng ngược chiều là lỗ
                    var big = loops.OrderByDescending(l => Math.Abs(l.area)).First();
                    var sign = big.area >= 0 ? 1 : -1;
                    var poly = Dedup(sign > 0 ? big.pts : big.pts.Reverse().ToArray());
                    foreach (var (pts, area) in loops.Where(l => !ReferenceEquals(l.pts, big.pts) && l.area * sign > 0 && Math.Abs(l.area) >= MinHoleArea * unit * unit).OrderByDescending(l => Math.Abs(l.area))) // cụm cùng chiều quá nhỏ (vụn tam giác nối vào mảnh ở góc): bỏ, không nối vào polygon
                        MergeLoop(poly, Dedup(sign > 0 ? pts : pts.Reverse().ToArray()));
                    var holeLoops = loops.Where(l => l.area * sign < 0).Select(l => sign > 0 ? l.pts : l.pts.Reverse().ToArray()).ToList();
                    if (poly.Count < 3 || Math.Abs(SignedArea(poly)) < 2f) poly = RawLoop(list, unit) ?? poly; // suy biến: dùng vòng lưới thô, không bỏ mảnh
                    if (poly.Count < 3 || Math.Abs(SignedArea(poly)) < 2f) continue;
                    TrimLoops(poly); // vòng tự cắt (hình nơ): bỏ thùy nhỏ
                    polys[id] = Flatten(poly);

                    foreach (var pts in holeLoops)
                    {
                        var hp = Dedup(pts);
                        TrimLoops(hp);
                        if (hp.Count < 3 || -SignedArea(hp) < MinHoleArea * unit * unit || !MostlyInside(poly, hp)) continue; // lỗ li ti (vụn khe nằm trong mảnh): bỏ, mảnh phủ kín chỗ đó
                        (holeSets[id] ??= new List<int[]>()).Add(Flatten(hp));
                    }
                }

                return (polys, holeSets);
            }

            var res = Assemble();
            // mảnh mỏng bị làm mượt đè lên nhau (đa giác lật / lệch diện tích): các đường của nó dùng biên thô chưa làm mượt
            var pixels = new int[count + 1];
            foreach (var r in map.reg) if (r > 0) pixels[r]++;
            var bad = new HashSet<int>();
            for (var id = 1; id <= count; id++)
            {
                if (pixels[id] == 0) continue;
                var expected = pixels[id] * (double)unit * unit;
                var got = res.polys[id] == null ? 0.0 : Math.Abs(RingArea(res.polys[id]));
                if (res.holes[id] != null) foreach (var hl in res.holes[id]) got -= Math.Abs(RingArea(hl));
                // mảnh nhỏ co lại nhiều khi làm mượt là bình thường (hạt, chấm): ngưỡng rộng hơn, đừng chuyển sang biên thô bậc thang
                var small = pixels[id] < SmallFraction * map.w * map.h;
                if (got < (small ? RatioLowSmall : RatioLow) * expected || got > (small ? RatioHighSmall : RatioHigh) * expected) bad.Add(id);
            }
            if (bad.Count > 0)
            {
                foreach (var a in arcs)
                    if (bad.Contains(a.left) || bad.Contains(a.right)) a.smooth = Scale(a.grid.Select(v => new Vector2(v.x, v.y)).ToList(), unit, a.closed, false);
                res = Assemble();
            }
            holes = res.holes;
            return res.polys;
        }

        // Lỗ thuộc polygon nếu quá nửa số đỉnh của nó nằm trong (1 đỉnh có thể trùng biên khi lỗ chạm vòng ngoài)
        private static bool MostlyInside(List<Vector2Int> poly, List<Vector2Int> hole)
        {
            var step = Math.Max(1, hole.Count / 24);
            int inside = 0, total = 0;
            for (var i = 0; i < hole.Count; i += step) { total++; if (Inside(poly, hole[i])) inside++; }
            return inside * 2 > total;
        }

        // Chèn vòng extra vào poly: cầu nối cùng điểm giữa 2 đỉnh gần nhau nhất (độ rộng 0), poly thành 1 vòng chạm chính nó tại điểm đó
        private static void MergeLoop(List<Vector2Int> poly, List<Vector2Int> extra)
        {
            if (extra.Count < 3) return;
            int bi = 0, bj = 0;
            var bd = long.MaxValue;
            for (var i = 0; i < poly.Count; i++)
            for (var j = 0; j < extra.Count; j++)
            {
                long dx = poly[i].x - extra[j].x, dy = poly[i].y - extra[j].y, d = dx * dx + dy * dy;
                if (d < bd) { bd = d; bi = i; bj = j; }
            }
            var merged = new List<Vector2Int>(poly.Count + extra.Count + 2);
            for (var i = 0; i <= bi; i++) merged.Add(poly[i]);
            for (var k = 0; k <= extra.Count; k++) merged.Add(extra[(bj + k) % extra.Count]);
            for (var i = bi; i < poly.Count; i++) merged.Add(poly[i]);
            poly.Clear();
            poly.AddRange(merged);
        }

        // Cắt vòng tự cắt chỉ khi phần bỏ đi nhỏ (< 10% diện tích): bỏ cả thùy lớn sẽ để lại khe hở lớn giữa các mảnh
        private static void TrimLoops(List<Vector2Int> ring)
        {
            var before = Math.Abs(SignedArea(ring));
            var copy = new List<Vector2Int>(ring);
            RemoveLoops(copy, true);
            if (copy.Count < 3 || Math.Abs(SignedArea(copy)) < 0.9 * before) return;
            ring.Clear();
            ring.AddRange(copy);
        }

        private const float MinHoleArea = 100f; // diện tích lỗ nhỏ nhất được giữ (ô lưới 1920, ~11px² ở lưới 640)
        private const float MaxArcDeviation = 15f; // hộp bao của đường đã làm mượt không được lệch biên thô quá ngần này (ô lưới)

        private static bool NearRaw(List<Vector2> sm, Vector2Int[] raw)
        {
            float sx0 = float.MaxValue, sy0 = float.MaxValue, sx1 = float.MinValue, sy1 = float.MinValue;
            foreach (var p in sm) { sx0 = Mathf.Min(sx0, p.x); sx1 = Mathf.Max(sx1, p.x); sy0 = Mathf.Min(sy0, p.y); sy1 = Mathf.Max(sy1, p.y); }
            int rx0 = int.MaxValue, ry0 = int.MaxValue, rx1 = int.MinValue, ry1 = int.MinValue;
            foreach (var p in raw) { rx0 = Math.Min(rx0, p.x); rx1 = Math.Max(rx1, p.x); ry0 = Math.Min(ry0, p.y); ry1 = Math.Max(ry1, p.y); }
            return Mathf.Abs(sx0 - rx0) <= MaxArcDeviation && Mathf.Abs(sx1 - rx1) <= MaxArcDeviation && Mathf.Abs(sy0 - ry0) <= MaxArcDeviation && Mathf.Abs(sy1 - ry1) <= MaxArcDeviation;
        }

        private const float MaxNodeShift = 20f; // ngã ba dời tối đa chừng này (ô lưới): xa hơn là cắt chuỗi sai chỗ, bỏ qua kẻo sinh gai
        private const double RatioLow = 0.8, RatioHigh = 1.25, RatioLowSmall = 0.35, RatioHighSmall = 2.0, SmallFraction = 0.004;

        private static double RingArea(int[] p)
        {
            double a = 0;
            var n = p.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++) a += (double)p[j * 2] * p[i * 2 + 1] - (double)p[i * 2] * p[j * 2 + 1];
            return a * 0.5;
        }

        private const float ContinueDeg = 30f, BlendSpan = 24f;

        // Ngã ba đúng 3 đường: nối 2 đường gần thẳng hàng nhất (lệch ≤ ContinueDeg); trả các chuỗi (đường, đi ngược?)
        private static List<List<(Arc arc, bool rev)>> Chains(List<Arc> arcs)
        {
            var atNode = new Dictionary<int, List<(Arc arc, bool start)>>();
            foreach (var a in arcs)
            {
                if (a.closed || a.left == 0 || a.right == 0) continue; // đường sát mép tranh vốn thẳng: không gộp
                if (!atNode.TryGetValue(a.startNode, out var l1)) atNode[a.startNode] = l1 = new();
                l1.Add((a, true));
                if (!atNode.TryGetValue(a.endNode, out var l2)) atNode[a.endNode] = l2 = new();
                l2.Add((a, false));
            }

            var join = new Dictionary<(Arc, bool), (Arc arc, bool start)>();
            foreach (var list in atNode.Values)
            {
                if (list.Count != 3 || list[0].arc == list[1].arc || list[0].arc == list[2].arc || list[1].arc == list[2].arc) continue;
                var bestAngle = 180f - ContinueDeg;
                int bi = -1, bj = -1;
                for (var i = 0; i < 3; i++)
                for (var j = i + 1; j < 3; j++)
                {
                    var ang = Vector2.Angle(EndDir(list[i].arc, list[i].start), EndDir(list[j].arc, list[j].start));
                    if (ang < bestAngle) continue;
                    bestAngle = ang;
                    bi = i;
                    bj = j;
                }
                if (bi < 0) continue;
                join[(list[bi].arc, list[bi].start)] = list[bj];
                join[(list[bj].arc, list[bj].start)] = list[bi];
            }

            var chains = new List<List<(Arc arc, bool rev)>>();
            var visited = new HashSet<Arc>();
            void Follow(Arc first, bool rev)
            {
                var chain = new List<(Arc arc, bool rev)> { (first, rev) };
                visited.Add(first);
                var cur = (arc: first, rev);
                while (join.TryGetValue((cur.arc, cur.rev), out var nxt) && !visited.Contains(nxt.arc))
                {
                    cur = (nxt.arc, !nxt.start);
                    visited.Add(cur.arc);
                    chain.Add(cur);
                }
                chains.Add(chain);
            }
            foreach (var a in arcs)
            {
                if (visited.Contains(a)) continue;
                if (!join.ContainsKey((a, true))) Follow(a, false);
                else if (!join.ContainsKey((a, false))) Follow(a, true);
            }
            foreach (var a in arcs)
                if (!visited.Contains(a)) Follow(a, false); // vòng khép kín các đường: cắt tại 1 điểm
            return chains;
        }

        // Hướng đường rời khỏi nút (đo qua vài đỉnh để bớt nhiễu bậc thang)
        private static Vector2 EndDir(Arc a, bool atStart)
        {
            var g = a.grid;
            var k = Mathf.Min(8, g.Length - 1);
            return atStart ? (Vector2)(g[k] - g[0]) : (Vector2)(g[g.Length - 1 - k] - g[g.Length - 1]);
        }

        // Làm mượt cả chuỗi 1 lần rồi cắt lại tại các ngã ba; điểm cắt là vị trí mới của ngã ba
        private static void SmoothChain(List<(Arc arc, bool rev)> chain, float sigma, float step, float fit, Dictionary<Arc, List<Vector2>> smoothed, Dictionary<int, Vector2> nodePos)
        {
            var pts = new List<Vector2Int>();
            var nodeIdx = new List<int>();
            for (var i = 0; i < chain.Count; i++)
            {
                var g = chain[i].arc.grid;
                for (var k = 0; k < g.Length; k++)
                {
                    if (i > 0 && k == 0) continue;
                    pts.Add(g[chain[i].rev ? g.Length - 1 - k : k]);
                }
                if (i + 1 < chain.Count) nodeIdx.Add(pts.Count - 1);
            }
            var sm = ContourSmoother.SmoothArc(pts, sigma, resampleStep: step, fitTolerance: fit);

            var cuts = new List<int> { 0 };
            foreach (var ni in nodeIdx)
            {
                var target = (Vector2)pts[ni];
                var best = -1;
                var bestD = float.MaxValue;
                for (var j = cuts[cuts.Count - 1] + 1; j < sm.Count - 1; j++)
                {
                    var d = (sm[j] - target).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = j; }
                }
                if (best < 0) return; // quá ít điểm để cắt: để từng đường tự làm mượt
                cuts.Add(best);
            }
            cuts.Add(sm.Count - 1);

            for (var i = 0; i < chain.Count; i++)
            {
                var piece = sm.GetRange(cuts[i], cuts[i + 1] - cuts[i] + 1);
                if (chain[i].rev) piece.Reverse();
                smoothed[chain[i].arc] = piece;
                if (i + 1 < chain.Count) nodePos[chain[i].rev ? chain[i].arc.startNode : chain[i].arc.endNode] = sm[cuts[i + 1]];
            }
        }

        // Dời một đầu đường theo delta, giảm dần về 0 dọc theo BlendSpan đầu tiên của đường
        private static void Blend(List<Vector2> sm, Vector2 delta, bool atStart)
        {
            if (delta.sqrMagnitude < 1e-8f || sm.Count < 2) return;
            var total = 0f;
            for (var i = 1; i < sm.Count; i++) total += Vector2.Distance(sm[i], sm[i - 1]);
            var span = Mathf.Min(BlendSpan, total * 0.5f);
            var s = 0f;
            for (var k = 0; k < sm.Count; k++)
            {
                var i = atStart ? k : sm.Count - 1 - k;
                if (k > 0) s += Vector2.Distance(sm[i], sm[atStart ? i - 1 : i + 1]);
                if (s >= span) break;
                var t = 1f - s / span;
                sm[i] += delta * (t * t * (3f - 2f * t));
            }
        }

        // Vòng lớn nhất ghép từ đỉnh lưới gốc (không làm mượt), nhân unit
        private static List<Vector2Int> RawLoop(List<(Arc arc, bool rev)> list, int unit)
        {
            var pts = new List<Vector2Int>();
            foreach (var (arc, rev) in list)
            {
                var g = arc.grid;
                if (rev) for (var i = g.Length - 1; i >= (arc.closed ? 0 : 1); i--) pts.Add(g[i] * unit);
                else for (var i = 0; i < (arc.closed ? g.Length : g.Length - 1); i++) pts.Add(g[i] * unit);
            }
            var d = Dedup(pts.ToArray());
            return d.Count >= 3 ? d : null;
        }

        private static int[] Flatten(List<Vector2Int> poly)
        {
            var flat = new int[poly.Count * 2];
            for (var i = 0; i < poly.Count; i++) { flat[i * 2] = poly[i].x; flat[i * 2 + 1] = poly[i].y; }
            return flat;
        }

        private static bool Inside(List<Vector2Int> poly, Vector2Int p)
        {
            var inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if (poly[i].y > p.y == poly[j].y > p.y) continue;
                var x = poly[i].x + (float)(p.y - poly[i].y) * (poly[j].x - poly[i].x) / (poly[j].y - poly[i].y);
                if (p.x < x) inside = !inside;
            }
            return inside;
        }

        private static int Bits(byte m) => (m & 1) + ((m >> 1) & 1) + ((m >> 2) & 1) + ((m >> 3) & 1);

        private static Vector2Int[] Scale(List<Vector2> pts, int unit, bool closed, bool removeLoops = true)
        {
            var list = new List<Vector2Int>(pts.Count);
            foreach (var p in pts)
            {
                var q = new Vector2Int(Mathf.RoundToInt(p.x * unit), Mathf.RoundToInt(p.y * unit));
                if (list.Count == 0 || list[list.Count - 1] != q) list.Add(q);
            }
            if (closed && list.Count > 1 && list[0] == list[list.Count - 1]) list.RemoveAt(list.Count - 1);
            if (removeLoops) RemoveLoops(list, closed);
            return list.ToArray();
        }

        // Cắt các vòng nhỏ (đường tự cắt, mảnh thành hình nơ): 2 đoạn không kề nhau cắt nhau thì bỏ các đỉnh ở giữa, nối tại giao điểm
        private static void RemoveLoops(List<Vector2Int> pts, bool closed)
        {
            for (var pass = 0; pass < 200; pass++)
            {
                var n = pts.Count;
                var found = false;
                var segs = closed ? n : n - 1;
                for (var i = 0; i < segs && !found; i++)
                for (var j = i + 2; j < segs; j++)
                {
                    if (closed && i == 0 && j == n - 1) continue;
                    var a = pts[i]; var b = pts[(i + 1) % n]; var c = pts[j]; var d = pts[(j + 1) % n];
                    if (!ProperCross(a, b, c, d)) continue;
                    var hit = Intersect(a, b, c, d);
                    // bỏ đoạn vòng ngắn hơn (i+1..j hoặc phần còn lại của vòng kín)
                    if (closed && j - i > n / 2)
                    {
                        var keep = new List<Vector2Int>();
                        for (var k = i + 1; k <= j; k++) keep.Add(pts[k]);
                        keep.Add(hit);
                        pts.Clear(); pts.AddRange(keep);
                    }
                    else
                    {
                        pts.RemoveRange(i + 1, j - i);
                        pts.Insert(i + 1, hit);
                    }
                    found = true;
                    break;
                }
                if (!found) return;
            }
        }

        private static bool ProperCross(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
        {
            long O(Vector2Int p, Vector2Int q, Vector2Int r) => (long)(q.x - p.x) * (r.y - p.y) - (long)(q.y - p.y) * (r.x - p.x);
            long d1 = O(a, b, c), d2 = O(a, b, d), d3 = O(c, d, a), d4 = O(c, d, b);
            return d1 != 0 && d2 != 0 && d3 != 0 && d4 != 0 && (d1 > 0) != (d2 > 0) && (d3 > 0) != (d4 > 0);
        }

        private static Vector2Int Intersect(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
        {
            double x1 = a.x, y1 = a.y, x2 = b.x, y2 = b.y, x3 = c.x, y3 = c.y, x4 = d.x, y4 = d.y;
            var den = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (System.Math.Abs(den) < 1e-9) return b;
            var t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / den;
            return new Vector2Int((int)System.Math.Round(x1 + t * (x2 - x1)), (int)System.Math.Round(y1 + t * (y2 - y1)));
        }

        // Thêm điểm của đường (theo chiều thuận/ngược), bỏ điểm cuối vì đó là điểm đầu của đường kế tiếp
        private static void Append(List<Vector2Int> dst, Arc a, bool rev)
        {
            var s = a.smooth;
            if (a.closed)
            {
                if (rev) for (var i = s.Length - 1; i >= 0; i--) dst.Add(s[i]);
                else dst.AddRange(s);
                return;
            }
            if (rev) for (var i = s.Length - 1; i > 0; i--) dst.Add(s[i]);
            else for (var i = 0; i < s.Length - 1; i++) dst.Add(s[i]);
        }

        // Tại nút, chọn đường ra rẽ trái nhất trong các đường chưa dùng (hoặc quay về đường xuất phát)
        private static (Arc arc, bool rev) Next(List<(Arc arc, bool rev)> list, HashSet<(Arc, bool)> used, int node, (Arc arc, bool rev) cur, (Arc arc, bool rev) start)
        {
            var inDir = LastDir(cur.arc, cur.rev);
            (Arc, bool) best = default;
            var bestAngle = float.MinValue;
            foreach (var c in list)
            {
                var from = c.rev ? c.arc.endNode : c.arc.startNode;
                if (c.arc.closed || from != node) continue;
                if (used.Contains(c) && !c.Equals(start)) continue;
                var ang = Vector2.SignedAngle(inDir, FirstDir(c.arc, c.rev));
                if (ang > bestAngle) { bestAngle = ang; best = c; }
            }
            return best;
        }

        private static Vector2 FirstDir(Arc a, bool rev)
        {
            var g = a.grid;
            return rev ? (Vector2)(g[g.Length - 2] - g[g.Length - 1]) : (Vector2)(g[1] - g[0]);
        }

        private static Vector2 LastDir(Arc a, bool rev)
        {
            var g = a.grid;
            return rev ? (Vector2)(g[0] - g[1]) : (Vector2)(g[g.Length - 1] - g[g.Length - 2]);
        }

        private static List<Vector2Int> Dedup(Vector2Int[] pts)
        {
            var list = new List<Vector2Int>(pts.Length);
            foreach (var p in pts)
                if (list.Count == 0 || list[list.Count - 1] != p) list.Add(p);
            while (list.Count > 1 && list[0] == list[list.Count - 1]) list.RemoveAt(list.Count - 1);
            return list;
        }

        private static double SignedArea(IReadOnlyList<Vector2Int> p)
        {
            double s = 0;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) s += (double)p[j].x * p[i].y - (double)p[i].x * p[j].y;
            return s * 0.5;
        }
    }
}
