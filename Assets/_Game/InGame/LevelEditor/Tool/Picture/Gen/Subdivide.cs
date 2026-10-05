using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Chia 1 vùng thành các ô ôm theo hình thể: dải đẳng khoảng cách dọc vùng tính từ 1 đầu mút, vùng đủ rộng thì chia thêm theo trục ngắn, so le như gạch.</summary>
    internal static class Subdivide
    {
        private const int MaxCells = 24;
        private const float Bend = 1.1f;

        public const int MinCount = 2, MaxCount = 20;
        private const float TypicalFraction = 0.0157f; // mảnh game có diện tích trung vị 1,57% tranh

        // Số mảnh gợi ý cho một mảnh có diện tích (0..1 so với tranh): chia sao cho mỗi mảnh con cỡ mảnh game
        public static int Recommend(float areaFraction) => Mathf.Clamp(Mathf.RoundToInt(areaFraction / TypicalFraction), MinCount, MaxCount);

        // Chỉ số ô (0..cells-1) của từng pixel trong pix; null nếu vùng quá nhỏ/hẹp để chia
        public static int[] FlowAssign(RegionMap map, List<int> pix, int target, out int cells)
        {
            cells = 0;
            int w = map.w;
            int bx0 = int.MaxValue, by0 = int.MaxValue, bx1 = -1, by1 = -1;
            double mx = 0, my = 0;
            foreach (var p in pix)
            {
                int x = p % w, y = p / w;
                bx0 = Mathf.Min(bx0, x); bx1 = Mathf.Max(bx1, x); by0 = Mathf.Min(by0, y); by1 = Mathf.Max(by1, y);
                mx += x; my += y;
            }
            if (pix.Count == 0) return null;
            mx /= pix.Count; my /= pix.Count;
            int lw = bx1 - bx0 + 1, lh = by1 - by0 + 1;
            var local = new int[pix.Count];
            var inside = new bool[lw * lh];
            for (var i = 0; i < pix.Count; i++)
            {
                local[i] = (pix[i] / w - by0) * lw + pix[i] % w - bx0;
                inside[local[i]] = true;
            }
            // đầu mút: điểm xa trọng tâm nhất
            var seed = local[0];
            var far = -1f;
            for (var i = 0; i < pix.Count; i++)
            {
                float dx = pix[i] % w - (float)mx, dy = pix[i] / w - (float)my, d = dx * dx + dy * dy;
                if (d > far) { far = d; seed = local[i]; }
            }
            var g = Geodesic(inside, lw, lh, seed);
            var dt = ImageOps.Edt(inside, lw, lh);
            var dmax = 1f;
            for (var i = 0; i < pix.Count; i++) dmax = Mathf.Max(dmax, dt[local[i]]);
            var gmax = 0f;
            for (var i = 0; i < pix.Count; i++) gmax = Mathf.Max(gmax, g[local[i]]);

            double sxx = 0, syy = 0, sxy = 0;
            foreach (var p in pix)
            {
                var dx = p % w - mx; var dy = p / w - my;
                sxx += dx * dx; syy += dy * dy; sxy += dx * dy;
            }
            var ang = 0.5 * System.Math.Atan2(2 * sxy, sxx - syy);
            float ux = (float)System.Math.Cos(ang), uy = (float)System.Math.Sin(ang);
            float vmin = float.MaxValue, vmax = float.MinValue;
            var vv = new float[pix.Count];
            for (var i = 0; i < pix.Count; i++)
            {
                float dx = (float)(pix[i] % w - mx), dy = (float)(pix[i] / w - my);
                vv[i] = -dx * ux + dy * uy;
                vmin = Mathf.Min(vmin, vv[i]); vmax = Mathf.Max(vmax, vv[i]);
            }
            float length = gmax + 1f, width = vmax - vmin + 1f;
            var nc = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(target * width / length)), 1, 4);
            var nb = Mathf.Clamp(Mathf.CeilToInt(target / (float)nc), 1, MaxCells);
            if (nb * nc < 2) return null;
            float bw = gmax / nb + 1e-3f, cw = (vmax - vmin) / nc + 1e-3f;
            var key = new int[lw * lh];
            for (var i = 0; i < key.Length; i++) key[i] = -1;
            for (var i = 0; i < pix.Count; i++)
            {
                var warped = g[local[i]] - Bend * bw * (dt[local[i]] / dmax - 0.5f); // giữa thân chậm hơn mép: dải thành cung ôm theo hình thể
                var b = Mathf.Clamp(Mathf.FloorToInt(warped / bw), 0, nb - 1);
                var c = Mathf.Clamp(Mathf.FloorToInt((vv[i] - vmin + (b % 2) * cw * 0.5f) / cw), 0, nc);
                key[local[i]] = b * 10 + c;
            }
            // cụm liền theo khoá, rồi gộp ô nhỏ nhất vào láng giềng chung biên dài nhất cho tới đúng target ô
            var comp = ImageOps.ComponentsByKey(key, lw, lh, out var count);
            var area = new int[count + 1];
            foreach (var l in comp) if (l > 0) area[l]++;
            var parent = new int[count + 1];
            for (var i = 0; i < parent.Length; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
            var border = new Dictionary<long, int>();
            for (var y = 0; y < lh; y++)
            for (var x = 0; x < lw; x++)
            {
                var a1 = comp[y * lw + x];
                if (a1 == 0) continue;
                void Pair(int j)
                {
                    var b1 = comp[j];
                    if (b1 == 0 || b1 == a1) return;
                    long k = (long)Mathf.Min(a1, b1) * (count + 1) + Mathf.Max(a1, b1);
                    border[k] = border.TryGetValue(k, out var v) ? v + 1 : 1;
                }
                if (x + 1 < lw) Pair(y * lw + x + 1);
                if (y + 1 < lh) Pair((y + 1) * lw + x);
            }
            var alive = count;
            while (alive > target)
            {
                var small = 0;
                for (var i = 1; i <= count; i++) if (Find(i) == i && (small == 0 || area[i] < area[small])) small = i;
                var bestN = 0; var bestL = 0;
                foreach (var kv in border)
                {
                    int p = Find((int)(kv.Key / (count + 1))), q = Find((int)(kv.Key % (count + 1)));
                    if (p == q || (p != small && q != small)) continue;
                    var other = p == small ? q : p;
                    if (kv.Value > bestL) { bestL = kv.Value; bestN = other; }
                }
                if (bestN == 0) break;
                parent[small] = bestN; area[bestN] += area[small]; alive--;
            }
            var index = new Dictionary<int, int>();
            var result = new int[pix.Count];
            for (var i = 0; i < pix.Count; i++)
            {
                var root = Find(comp[local[i]]);
                if (!index.TryGetValue(root, out var cell)) index[root] = cell = index.Count;
                result[i] = cell;
            }
            cells = index.Count;
            return cells < 2 ? null : result;
        }

        // Khoảng cách đi trong vùng từ seed (chamfer 1 / 1.414, quét xuôi ngược tới khi hội tụ)
        private static float[] Geodesic(bool[] inside, int w, int h, int seed)
        {
            const float D = 1.4142f;
            var g = new float[inside.Length];
            for (var i = 0; i < g.Length; i++) g[i] = float.MaxValue;
            g[seed] = 0f;
            for (var sweep = 0; sweep < 60; sweep++)
            {
                var changed = false;
                for (var pass = 0; pass < 2; pass++)
                {
                    int y0 = pass == 0 ? 0 : h - 1, y1 = pass == 0 ? h : -1, dy = pass == 0 ? 1 : -1;
                    int x0 = pass == 0 ? 0 : w - 1, x1 = pass == 0 ? w : -1, dx = pass == 0 ? 1 : -1;
                    for (var y = y0; y != y1; y += dy)
                    for (var x = x0; x != x1; x += dx)
                    {
                        var i = y * w + x;
                        if (!inside[i]) continue;
                        var best = g[i];
                        for (var oy = -1; oy <= 1; oy++)
                        for (var ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            int nx = x + ox, ny = y + oy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            var q = ny * w + nx;
                            if (!inside[q] || g[q] == float.MaxValue) continue;
                            var v = g[q] + (ox != 0 && oy != 0 ? D : 1f);
                            if (v < best) best = v;
                        }
                        if (best < g[i]) { g[i] = best; changed = true; }
                    }
                }
                if (!changed) break;
            }
            for (var i = 0; i < g.Length; i++) if (g[i] == float.MaxValue) g[i] = 0f;
            return g;
        }
    }
}
