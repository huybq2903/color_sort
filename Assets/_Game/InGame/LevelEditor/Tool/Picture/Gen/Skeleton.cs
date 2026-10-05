using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Làm mảnh mặt nạ nét về đường 1 pixel nằm ở tâm nét (Zhang-Suen) và cắt nhánh cụt ngắn.</summary>
    public static class Skeleton
    {
        public static bool[] Thin(bool[] src, int w, int h)
        {
            var m = (bool[])src.Clone();
            var del = new List<int>();
            bool changed;
            do
            {
                changed = false;
                for (var pass = 0; pass < 2; pass++)
                {
                    del.Clear();
                    for (var y = 1; y < h - 1; y++)
                    for (var x = 1; x < w - 1; x++)
                    {
                        var i = y * w + x;
                        if (!m[i]) continue;
                        bool p2 = m[i - w], p3 = m[i - w + 1], p4 = m[i + 1], p5 = m[i + w + 1], p6 = m[i + w], p7 = m[i + w - 1], p8 = m[i - 1], p9 = m[i - w - 1];
                        var b = (p2 ? 1 : 0) + (p3 ? 1 : 0) + (p4 ? 1 : 0) + (p5 ? 1 : 0) + (p6 ? 1 : 0) + (p7 ? 1 : 0) + (p8 ? 1 : 0) + (p9 ? 1 : 0);
                        if (b < 2 || b > 6) continue;
                        var a = (!p2 && p3 ? 1 : 0) + (!p3 && p4 ? 1 : 0) + (!p4 && p5 ? 1 : 0) + (!p5 && p6 ? 1 : 0) + (!p6 && p7 ? 1 : 0) + (!p7 && p8 ? 1 : 0) + (!p8 && p9 ? 1 : 0) + (!p9 && p2 ? 1 : 0);
                        if (a != 1) continue;
                        if (pass == 0 ? p2 && p4 && p6 || p4 && p6 && p8 : p2 && p4 && p8 || p2 && p6 && p8) continue;
                        del.Add(i);
                    }
                    foreach (var i in del) m[i] = false;
                    changed |= del.Count > 0;
                }
            } while (changed);
            return m;
        }

        private const int JunctionMode = 2;

        // Ngã ba kiểu chữ T: cặp nhánh gần thẳng hàng nhất (góc > 150°) thành 1 đường cong liền theo tiếp tuyến hai nhánh; nhánh còn lại đi theo hướng của nó tới đường đó (mode 2) hoặc nối vuông góc (mode 1)
        public static void FixJunctions(bool[] sk, int w, int h, int r)
        {
            var n = sk.Length;
            var deg = new bool[n];
            for (var i = 0; i < n; i++) deg[i] = sk[i] && Degree(sk, w, h, i) >= 3;
            var seen = new bool[n];
            for (var j0 = 0; j0 < n; j0++)
            {
                if (!deg[j0] || seen[j0] || !sk[j0]) continue;
                var cluster = new List<int>();
                var st = new Stack<int>(); st.Push(j0); seen[j0] = true;
                while (st.Count > 0)
                {
                    var p = st.Pop(); cluster.Add(p);
                    int x = p % w, y = p / w;
                    for (var dy = -2; dy <= 2; dy++)
                    for (var dx = -2; dx <= 2; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        var q = ny * w + nx;
                        if (deg[q] && !seen[q]) { seen[q] = true; st.Push(q); }
                    }
                }
                float cx = 0, cy = 0;
                foreach (var p in cluster) { cx += p % w; cy += p / w; }
                cx /= cluster.Count; cy /= cluster.Count;
                var clusterSet = new HashSet<int>(cluster);
                var inDisc = new HashSet<int>(cluster);
                var q2 = new Queue<int>(cluster);
                var other = false;
                var exits = new List<int>();
                while (q2.Count > 0)
                {
                    var p = q2.Dequeue();
                    int x = p % w, y = p / w;
                    var outside = false;
                    for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        var q = ny * w + nx;
                        if (!sk[q] || inDisc.Contains(q)) continue;
                        var d = Mathf.Sqrt((nx - cx) * (nx - cx) + (ny - cy) * (ny - cy));
                        if (d <= r) { inDisc.Add(q); q2.Enqueue(q); if (deg[q] && !clusterSet.Contains(q)) other = true; }
                        else outside = true;
                    }
                    if (outside) exits.Add(p);
                }
                if (other) continue;
                var branches = new List<(int px, Vector2 dir)>();
                var used = new HashSet<int>();
                foreach (var e in exits)
                {
                    if (used.Contains(e)) continue;
                    foreach (var f in exits) if (Mathf.Abs(e % w - f % w) <= 2 && Mathf.Abs(e / w - f / w) <= 2) used.Add(f);
                    var visited = new HashSet<int>(inDisc);
                    var cur = e; var far = e;
                    for (var step = 0; step < 8; step++)
                    {
                        int x = cur % w, y = cur / w, best = -1; float bd = -1;
                        for (var dy = -1; dy <= 1; dy++)
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            var q = ny * w + nx;
                            if (!sk[q] || visited.Contains(q)) continue;
                            var d = (nx - cx) * (nx - cx) + (ny - cy) * (ny - cy);
                            if (d > bd) { bd = d; best = q; }
                        }
                        if (best < 0) break;
                        visited.Add(best); cur = best; far = best;
                    }
                    var v = new Vector2(far % w - e % w, far / w - e / w);
                    if (v.sqrMagnitude < 4f) v = new Vector2(e % w - cx, e / w - cy);
                    branches.Add((e, v.normalized));
                }
                if (branches.Count < 3) continue;
                int bi = -1, bj = -1; var bc = 2f;
                for (var i = 0; i < branches.Count; i++)
                for (var j = i + 1; j < branches.Count; j++)
                {
                    var c = Vector2.Dot(branches[i].dir, branches[j].dir);
                    if (c < bc) { bc = c; bi = i; bj = j; }
                }
                var tee = bc <= -0.866f;
                Vector2 junction = default;
                if (!tee)
                {
                    // không có cặp thẳng hàng: kéo dài mọi nhánh theo hướng của nó, giao điểm = điểm gần cả các tia nhất (bình phương tối thiểu)
                    float m00 = 0, m01 = 0, m11 = 0, b0 = 0, b1 = 0;
                    foreach (var br in branches)
                    {
                        Vector2 e = new Vector2(br.px % w, br.px / w), d = br.dir;
                        float a00 = 1f - d.x * d.x, a01 = -d.x * d.y, a11 = 1f - d.y * d.y;
                        m00 += a00; m01 += a01; m11 += a11;
                        b0 += a00 * e.x + a01 * e.y; b1 += a01 * e.x + a11 * e.y;
                    }
                    var det = m00 * m11 - m01 * m01;
                    if (Mathf.Abs(det) < 1e-3f) continue;
                    junction = new Vector2((m11 * b0 - m01 * b1) / det, (m00 * b1 - m01 * b0) / det);
                    if ((junction - new Vector2(cx, cy)).magnitude > r) continue;
                }
                int wx0 = int.MaxValue, wy0 = int.MaxValue, wx1 = -1, wy1 = -1;
                foreach (var p in inDisc) { wx0 = Mathf.Min(wx0, p % w); wx1 = Mathf.Max(wx1, p % w); wy0 = Mathf.Min(wy0, p / w); wy1 = Mathf.Max(wy1, p / w); }
                wx0 = Mathf.Max(0, wx0 - 4); wy0 = Mathf.Max(0, wy0 - 4); wx1 = Mathf.Min(w - 1, wx1 + 4); wy1 = Mathf.Min(h - 1, wy1 + 4);
                var saved = new bool[(wx1 - wx0 + 1) * (wy1 - wy0 + 1)];
                for (var yy = wy0; yy <= wy1; yy++) for (var xx = wx0; xx <= wx1; xx++) saved[(yy - wy0) * (wx1 - wx0 + 1) + xx - wx0] = sk[yy * w + xx];
                var cellsBefore = CountCells(sk, w, wx0, wy0, wx1, wy1);
                foreach (var p in inDisc) sk[p] = false;
                if (!tee)
                {
                    foreach (var br in branches) Line(sk, w, new Vector2(br.px % w, br.px / w), junction);
                    if (CountCells(sk, w, wx0, wy0, wx1, wy1) != cellsBefore)
                        for (var yy = wy0; yy <= wy1; yy++) for (var xx = wx0; xx <= wx1; xx++) sk[yy * w + xx] = saved[(yy - wy0) * (wx1 - wx0 + 1) + xx - wx0];
                    continue;
                }
                Vector2 A = new Vector2(branches[bi].px % w, branches[bi].px / w), B = new Vector2(branches[bj].px % w, branches[bj].px / w);
                var L = (B - A).magnitude;
                Vector2 P1 = A - branches[bi].dir * (L / 3f), P2 = B - branches[bj].dir * (L / 3f);
                var curve = new List<Vector2>();
                var cs = Mathf.CeilToInt(L * 2f) + 2;
                for (var t = 0; t <= cs; t++)
                {
                    var u = t / (float)cs; var v1 = 1f - u;
                    curve.Add(v1 * v1 * v1 * A + 3f * v1 * v1 * u * P1 + 3f * v1 * u * u * P2 + u * u * u * B);
                }
                for (var t = 0; t + 1 < curve.Count; t++) Line(sk, w, curve[t], curve[t + 1]);
                for (var k = 0; k < branches.Count; k++)
                {
                    if (k == bi || k == bj) continue;
                    var T = new Vector2(branches[k].px % w, branches[k].px / w);
                    var target = curve[0]; var bd = float.MaxValue;
                    foreach (var c in curve)
                    {
                        float dist;
                        if (JunctionMode == 2)
                        {
                            var rel = c - T; var along = Vector2.Dot(rel, -branches[k].dir);
                            var perp = (rel + branches[k].dir * along).magnitude;
                            dist = along > 0 ? perp : perp + 1000f;
                        }
                        else dist = (c - T).magnitude;
                        if (dist < bd) { bd = dist; target = c; }
                    }
                    Line(sk, w, T, target);
                }
                if (CountCells(sk, w, wx0, wy0, wx1, wy1) != cellsBefore)
                {
                    for (var yy = wy0; yy <= wy1; yy++) for (var xx = wx0; xx <= wx1; xx++) sk[yy * w + xx] = saved[(yy - wy0) * (wx1 - wx0 + 1) + xx - wx0];
                    continue;
                }
            }
        }

        private static int CountCells(bool[] sk, int w, int x0, int y0, int x1, int y1)
        {
            int ww = x1 - x0 + 1, hh = y1 - y0 + 1, count = 0;
            var seen = new bool[ww * hh];
            var st = new Stack<int>();
            for (var i = 0; i < seen.Length; i++)
            {
                if (seen[i] || sk[(y0 + i / ww) * w + x0 + i % ww]) continue;
                count++; seen[i] = true; st.Push(i);
                while (st.Count > 0)
                {
                    var p = st.Pop(); int x = p % ww, y = p / ww;
                    for (var d = 0; d < 4; d++)
                    {
                        int nx = x + (d == 0 ? -1 : d == 1 ? 1 : 0), ny = y + (d == 2 ? -1 : d == 3 ? 1 : 0);
                        if (nx < 0 || ny < 0 || nx >= ww || ny >= hh) continue;
                        var q = ny * ww + nx;
                        if (!seen[q] && !sk[(y0 + ny) * w + x0 + nx]) { seen[q] = true; st.Push(q); }
                    }
                }
            }
            return count;
        }

        private static void Line(bool[] sk, int w, Vector2 a, Vector2 b)
        {
            var steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)) * 2f) + 1;
            for (var t = 0; t <= steps; t++)
            {
                var p = Vector2.Lerp(a, b, t / (float)steps);
                int px = Mathf.RoundToInt(p.x), py = Mathf.RoundToInt(p.y);
                if (px < 0 || py < 0 || px >= w || py * w + px >= sk.Length) continue;
                sk[py * w + px] = true;
            }
        }

        private static int Degree(bool[] sk, int w, int h, int i)
        {
            int x = i % w, y = i / w, n = 0;
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < w && ny < h && sk[ny * w + nx]) n++;
            }
            return n;
        }

        // Từ đầu mút đi theo nhánh tới ngã ba; nhánh ngắn hơn len thì xoá (không xoá ngã ba), nhánh dài giữ nguyên chiều dài
        public static void PruneSpurs(bool[] sk, int w, int h, int len)
        {
            for (var round = 0; round < 2; round++)
            {
                var any = false;
                for (var i = 0; i < sk.Length; i++)
                {
                    if (!sk[i] || Degree(sk, w, h, i) > 1) continue;
                    var path = new List<int> { i };
                    var prev = -1;
                    var cur = i;
                    var junction = false;
                    while (path.Count <= len)
                    {
                        var next = -1;
                        int x = cur % w, y = cur / w, cnt = 0;
                        for (var dy = -1; dy <= 1; dy++)
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            var q = ny * w + nx;
                            if (!sk[q] || q == prev || path.Contains(q)) continue;
                            cnt++; next = q;
                        }
                        if (cnt == 0) break;
                        if (cnt > 1 || Degree(sk, w, h, next) > 2) { junction = true; break; }
                        prev = cur; cur = next; path.Add(cur);
                    }
                    if (path.Count > len) continue;
                    if (!junction && Degree(sk, w, h, cur) > 1) continue;
                    foreach (var q in path) sk[q] = false;
                    any = true;
                }
                if (!any) return;
            }
        }

        // Đoạn từ (ax, ay) tới (bx, by) có ít nhất 85% điểm nằm trên mặt nạ nét
        private static bool OnInk(bool[] ink, int w, float ax, float ay, int bx, int by)
        {
            var steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(bx - ax), Mathf.Abs(by - ay)));
            if (steps == 0) return true;
            var hit = 0;
            for (var t = 0; t <= steps; t++)
            {
                int px = Mathf.RoundToInt(Mathf.Lerp(ax, bx, t / (float)steps)), py = Mathf.RoundToInt(Mathf.Lerp(ay, by, t / (float)steps));
                if (ink[py * w + px]) hit++;
            }
            return hit >= 0.85f * (steps + 1);
        }

        // Nối đầu mút với đường xương khác nằm phía trước trong tầm maxDist (bịt khe hở nét chì mờ)
        // ink != null: chỉ nối khi đoạn nối nằm trên mặt nạ nét (khe hở xa hơn nhưng vẫn là chỗ nét dày giao nhau)
        public static void CloseGaps(bool[] sk, int w, int h, int maxDist, bool[] ink = null)
        {
            var ends = new List<int>();
            for (var i = 0; i < sk.Length; i++) if (sk[i] && Degree(sk, w, h, i) <= 1) ends.Add(i);
            foreach (var e in ends)
            {
                var branch = new List<int> { e };
                var prev = -1;
                var cur = e;
                for (var k = 0; k < 8; k++)
                {
                    int x = cur % w, y = cur / w, next = -1;
                    for (var dy = -1; dy <= 1 && next < 0; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        var q = ny * w + nx;
                        if (sk[q] && q != prev && !branch.Contains(q)) { next = q; break; }
                    }
                    if (next < 0) break;
                    prev = cur; cur = next; branch.Add(cur);
                }
                float ex = e % w, ey = e / w;
                var dirX = ex - cur % w;
                var dirY = ey - cur / w;
                var dl = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (dl < 1e-3f) continue;
                dirX /= dl; dirY /= dl;
                var best = -1;
                var bd = float.MaxValue;
                for (var y = Mathf.Max(0, (int)ey - maxDist); y <= Mathf.Min(h - 1, (int)ey + maxDist); y++)
                for (var x = Mathf.Max(0, (int)ex - maxDist); x <= Mathf.Min(w - 1, (int)ex + maxDist); x++)
                {
                    var q = y * w + x;
                    if (!sk[q] || branch.Contains(q)) continue;
                    float vx = x - ex, vy = y - ey, d = Mathf.Sqrt(vx * vx + vy * vy);
                    if (d < 1.5f || d > maxDist || (vx * dirX + vy * dirY) / d < 0.7f) continue;
                    if (d >= bd) continue;
                    if (ink != null && !OnInk(ink, w, ex, ey, x, y)) continue;
                    bd = d; best = q;
                }
                if (best < 0) continue;
                var steps = Mathf.CeilToInt(bd * 2f);
                float tx = best % w, ty = best / w;
                for (var t = 0; t <= steps; t++)
                {
                    int px = Mathf.RoundToInt(Mathf.Lerp(ex, tx, t / (float)steps)), py = Mathf.RoundToInt(Mathf.Lerp(ey, ty, t / (float)steps));
                    sk[py * w + px] = true;
                }
            }
        }
    }
}
