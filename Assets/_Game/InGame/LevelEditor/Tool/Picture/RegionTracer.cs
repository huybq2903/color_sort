using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Dò biên ngoài vùng pixel thành polygon CCW trên lưới góc pixel, rồi giảm đỉnh.</summary>
    public static class RegionTracer
    {
        public static List<Vector2Int> TraceOuter(int[] reg, int w, int h, int id) =>
            TraceOuter(reg, w, h, id, new RectInt(0, 0, w, h));

        public static List<Vector2Int> TraceOuter(int[] reg, int w, int h, int id, RectInt bounds)
        {
            var result = new List<Vector2Int>();
            var start = -1;
            for (var y = bounds.yMin; y < bounds.yMax && start < 0; y++)
            for (var x = bounds.xMin; x < bounds.xMax; x++)
                if (reg[y * w + x] == id) { start = y * w + x; break; }
            if (start < 0) return result;

            bool In(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && reg[y * w + x] == id;

            // cạnh biên có hướng, vùng luôn nằm bên TRÁI
            var next = new Dictionary<Vector2Int, List<Vector2Int>>();
            void Add(int x0, int y0, int x1, int y1)
            {
                var k = new Vector2Int(x0, y0);
                if (!next.TryGetValue(k, out var l)) next[k] = l = new List<Vector2Int>(2);
                l.Add(new Vector2Int(x1, y1));
            }

            for (var y = bounds.yMin; y < bounds.yMax; y++)
            for (var x = bounds.xMin; x < bounds.xMax; x++)
            {
                if (reg[y * w + x] != id) continue;
                if (!In(x, y - 1)) Add(x, y, x + 1, y);
                if (!In(x + 1, y)) Add(x + 1, y, x + 1, y + 1);
                if (!In(x, y + 1)) Add(x + 1, y + 1, x, y + 1);
                if (!In(x - 1, y)) Add(x, y + 1, x, y);
            }

            // góc dưới-trái của pixel quét đầu tiên chỉ có 1 cạnh ra và thuộc biên ngoài
            var origin = new Vector2Int(start % w, start / w);
            var loop = new List<Vector2Int>();
            var cur = origin;
            var dir = Vector2Int.zero;
            do
            {
                var outs = next[cur];
                var pick = 0;
                if (outs.Count > 1)
                {
                    var left = new Vector2Int(-dir.y, dir.x); // chỗ chạm chéo: rẽ trái, tách 2 pixel chéo
                    for (var i = 0; i < outs.Count; i++)
                        if (outs[i] - cur == left) pick = i;
                }
                var nxt = outs[pick];
                outs.RemoveAt(pick);
                loop.Add(cur);
                dir = nxt - cur;
                cur = nxt;
            } while (cur != origin && loop.Count <= 4 * reg.Length);

            for (var i = 0; i < loop.Count; i++)
            {
                var prev = loop[(i + loop.Count - 1) % loop.Count];
                var c = loop[i];
                var nx = loop[(i + 1) % loop.Count];
                if (c - prev != nx - c) result.Add(c); // chỉ giữ đỉnh góc
            }
            return result;
        }

        public static List<Vector2Int> Simplify(List<Vector2Int> poly, float epsilon)
        {
            var n = poly.Count;
            if (n <= 4) return new List<Vector2Int>(poly);

            var far = 0;
            var best = -1f;
            for (var i = 1; i < n; i++)
            {
                var d = ((Vector2)(poly[i] - poly[0])).sqrMagnitude;
                if (d > best) { best = d; far = i; }
            }

            var keep = new bool[n];
            keep[0] = keep[far] = true;
            Dp(poly, 0, far, epsilon, keep);
            Dp(poly, far, n, epsilon, keep); // index n = đỉnh 0 (vòng kín)

            var res = new List<Vector2Int>();
            for (var i = 0; i < n; i++)
                if (keep[i]) res.Add(poly[i]);
            return res;
        }

        public static long Area2(IReadOnlyList<Vector2Int> p)
        {
            long s = 0;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) s += (long)p[j].x * p[i].y - (long)p[i].x * p[j].y;
            return s;
        }

        private static void Dp(List<Vector2Int> p, int i0, int i1, float eps, bool[] keep)
        {
            if (i1 - i0 < 2) return;
            Vector2 a = p[i0], b = p[i1 % p.Count];
            var idx = -1;
            var dmax = 0f;
            for (var k = i0 + 1; k < i1; k++)
            {
                var d = DistToSegment(p[k], a, b);
                if (d > dmax) { dmax = d; idx = k; }
            }
            if (dmax <= eps) return;
            keep[idx] = true;
            Dp(p, i0, idx, eps, keep);
            Dp(p, idx, i1, eps, keep);
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var len2 = ab.sqrMagnitude;
            if (len2 < 1e-9f) return (p - a).magnitude;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return (p - (a + t * ab)).magnitude;
        }
    }
}
