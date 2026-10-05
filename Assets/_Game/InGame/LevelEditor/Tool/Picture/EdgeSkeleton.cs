using System.Collections.Generic;
using Falcon.InGame.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Đoạn biên liên tục của mảnh a: b = mảnh kề, -1 = biên ngoài tranh; pts theo vòng của a, đầu và cuối là điểm nối.</summary>
    internal sealed class EdgeRun
    {
        public int a, b;
        public int lo, hi; // chỉ số đỉnh đầu và cuối của đoạn trong vòng của mảnh a
        public List<Vector2Int> pts;
    }

    /// <summary>Tách biên mảnh thành các đoạn dùng chung, khớp Bézier để sửa, lấy mẫu ngược về polyline.</summary>
    internal static class EdgeSkeleton
    {
        // Cạnh của vòng thuộc mảnh kề có chỉ số nhỏ nhất mà 2 đầu cạnh là hai đỉnh liền nhau trong vòng của nó; đổi mảnh kề thì cắt đoạn
        public static List<EdgeRun> RunsOf(PictureProperty p, int region)
        {
            var ring = ToPoints(p.regions[region].points);
            var n = ring.Count;
            var rings = new List<Vector2Int>[p.regions.Count];
            var owners = new Dictionary<Vector2Int, List<(int r, int i)>>();
            for (var r = 0; r < p.regions.Count; r++)
            {
                if (r == region) continue;
                rings[r] = ToPoints(p.regions[r].points);
                for (var i = 0; i < rings[r].Count; i++)
                {
                    if (!owners.TryGetValue(rings[r][i], out var l)) owners[rings[r][i]] = l = new List<(int r, int i)>();
                    l.Add((r, i));
                }
            }

            var keys = p.gen.inkGaps ? GapKeys(p, region, ring) : new int[n];
            for (var i = 0; i < n && !p.gen.inkGaps; i++)
            {
                keys[i] = -1;
                if (!owners.TryGetValue(ring[i], out var x) || !owners.TryGetValue(ring[(i + 1) % n], out var y)) continue;
                foreach (var (r, j) in x)
                {
                    if (keys[i] >= 0 && r >= keys[i]) continue;
                    var m = rings[r].Count;
                    foreach (var (r2, k) in y)
                        if (r2 == r && (k == (j + 1) % m || k == (j + m - 1) % m)) { keys[i] = r; break; }
                }
            }

            var start = -1;
            for (var i = 0; i < n; i++)
                if (keys[i] != keys[(i + n - 1) % n]) { start = i; break; }

            var runs = new List<EdgeRun>();
            if (start < 0)
            {
                var closed = new List<Vector2Int>(ring) { ring[0] };
                runs.Add(new EdgeRun { a = region, b = keys[0], pts = closed });
                return runs;
            }
            for (var s = 0; s < n;)
            {
                var k = keys[(start + s) % n];
                var pts = new List<Vector2Int> { ring[(start + s) % n] };
                var len = 0;
                while (s + len < n && keys[(start + s + len) % n] == k)
                {
                    len++;
                    pts.Add(ring[(start + s + len) % n]);
                }
                runs.Add(new EdgeRun { a = region, b = k, lo = (start + s) % n, hi = (start + s + len) % n, pts = pts });
                s += len;
            }
            return runs;
        }

        // Tranh có khe: các mảnh không chung đỉnh, mỗi cạnh của vòng thuộc về mảnh gần nhất bên kia khe (-1 = không có mảnh nào trong tầm khe)
        private static int[] GapKeys(PictureProperty p, int region, List<Vector2Int> ring)
        {
            var limit = Mathf.Max(1, p.width / 48) * Mathf.Max(1, p.unit) + 0.01f; // khớp PieceSize.MaxGap
            var rings = new List<Vector2Int>[p.regions.Count];
            for (var r = 0; r < rings.Length; r++) if (r != region) rings[r] = ToPoints(p.regions[r].points);
            var n = ring.Count;
            var keys = new int[n];
            for (var i = 0; i < n; i++)
            {
                var mid = ((Vector2)ring[i] + ring[(i + 1) % n]) * 0.5f;
                var bestD = limit;
                keys[i] = -1;
                for (var r = 0; r < rings.Length; r++)
                {
                    if (rings[r] == null) continue;
                    var m = rings[r].Count;
                    for (var k = 0; k < m; k++)
                    {
                        var d = PictureModel.DistToSegment(mid, rings[r][k], rings[r][(k + 1) % m]);
                        if (d <= bestD) { bestD = d; keys[i] = r; }
                    }
                }
            }
            return keys;
        }

        // Đoạn biên chung gần điểm pic (ô lưới) nhất trong bán kính; null nếu không có. includeOuter: tranh có khe, cho chọn cả đoạn không giáp mảnh nào
        public static EdgeRun Nearest(IEnumerable<EdgeRun> runs, Vector2 pic, int unit, float radius, bool includeOuter = false)
        {
            EdgeRun best = null;
            var bestD = radius;
            foreach (var r in runs)
            {
                if (r.b < 0 && !includeOuter) continue;
                for (var i = 0; i + 1 < r.pts.Count; i++)
                {
                    var d = PictureModel.DistToSegment(pic, (Vector2)r.pts[i] / unit, (Vector2)r.pts[i + 1] / unit);
                    if (d < bestD) { bestD = d; best = r; }
                }
            }
            return best;
        }

        public const float FitTolerance = 1f; // dung sai khớp Bézier (ô lưới): nhỏ để neo hiện ra vẫn ôm sát đường gốc

        // Khớp đoạn biên bằng ít Bézier nhất trong dung sai; toạ độ ra theo ô lưới
        public static List<CurveFitter.Cubic> Fit(EdgeRun run, int unit)
        {
            var pts = new List<Vector2>();
            foreach (var v in run.pts)
            {
                var q = (Vector2)v / unit;
                if (pts.Count == 0 || (pts[pts.Count - 1] - q).sqrMagnitude > 1e-6f) pts.Add(q);
            }
            return pts.Count < 2 ? new List<CurveFitter.Cubic>() : CurveFitter.Fit(pts.ToArray(), FitTolerance);
        }

        // Lấy mẫu chuỗi Bézier thành polyline int (×unit); hai đầu đúng bằng đầu và cuối đường cong
        public static List<Vector2Int> Sample(IReadOnlyList<CurveFitter.Cubic> curve, int unit, float step)
        {
            var o = new List<Vector2Int>();
            void Add(Vector2 p)
            {
                var q = Vector2Int.RoundToInt(p * unit);
                if (o.Count == 0 || o[o.Count - 1] != q) o.Add(q);
            }
            foreach (var c in curve)
            {
                var len = Vector2.Distance(c.p0, c.p3) + Vector2.Distance(c.p0, c.p1) + Vector2.Distance(c.p2, c.p3);
                var n = Mathf.Clamp(Mathf.CeilToInt(len / step), 1, 80);
                for (var k = 0; k < n; k++) Add(c.At(k / (float)n));
            }
            if (curve.Count > 0) Add(curve[curve.Count - 1].p3);
            return o;
        }

        private static List<Vector2Int> ToPoints(int[] a)
        {
            var o = new List<Vector2Int>(a.Length / 2);
            for (var i = 0; i + 1 < a.Length; i += 2) o.Add(new Vector2Int(a[i], a[i + 1]));
            return o;
        }
    }
}
