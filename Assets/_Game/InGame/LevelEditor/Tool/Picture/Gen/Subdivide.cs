using System;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Kiểu chia mảnh; Auto chọn theo hình dạng mảnh.</summary>
    public enum SubdividePattern { Auto, Brick, Bands, Rays, Rings, Scales, Hex, Mondrian, Diamond }

    /// <summary>Chia 1 vùng thành các ô theo một kiểu mosaic: mỗi kiểu là một hàm gán ô theo toạ độ trong trục chính của vùng; ô nhỏ gộp vào ô kề, ô bị vùng lõm cắt rời tách riêng.</summary>
    internal static class Subdivide
    {
        public static readonly string[] PatternLabels = { "Tự động", "Gạch", "Dải cong", "Tia", "Vòng", "Vảy", "Tổ ong", "Mondrian", "Kim cương" };

        private const float TileAspect = 1.35f; // gạch dài hơn cao
        private const float StaggerJitter = 0.12f, RowWobble = 0.08f; // lệch tay của gạch (theo cỡ viên)
        private const float MinTileFraction = 0.4f; // ô nhỏ hơn 40% cỡ trung bình thì gộp vào ô kề
        private const int Offset = 4096, Stride = 8192; // khoá ô = (a + Offset) * Stride + b + Offset

        public const int MinCount = 2, MaxCount = 20;
        private const float TypicalFraction = 0.0157f; // mảnh game có diện tích trung vị 1,57% tranh

        // Số mảnh gợi ý cho một mảnh có diện tích (0..1 so với tranh): chia sao cho mỗi mảnh con cỡ mảnh game
        public static int Recommend(float areaFraction) => Mathf.Clamp(Mathf.RoundToInt(areaFraction / TypicalFraction), MinCount, MaxCount);

        // Toạ độ các pixel trong hệ trục chính (gốc ở trọng tâm): u dọc trục dài, v ngang
        private sealed class Frame
        {
            public float[] u, v;
            public float umin, umax, vmin, vmax, su, sv; // su, sv = độ lệch lớn nhất so với trọng tâm theo từng trục
            public float aspect; // độ dài / độ rộng theo độ lệch chuẩn
            public int n;
        }

        // Chỉ số ô (0..cells-1) của từng pixel trong pix; null nếu vùng quá nhỏ/hẹp để chia
        public static int[] FlowAssign(RegionMap map, List<int> pix, int target, SubdividePattern pattern, out int cells)
        {
            cells = 0;
            if (pix.Count == 0) return null;
            target = Mathf.Min(target, pix.Count / Mathf.Max(8, map.w * map.h / 5000)); // ô nhỏ nhất vẫn đủ lớn để giữ làm mảnh
            if (target < 2) return null;

            int w = map.w;
            int bx0 = int.MaxValue, by0 = int.MaxValue, bx1 = -1, by1 = -1;
            double mx = 0, my = 0;
            foreach (var p in pix)
            {
                int x = p % w, y = p / w;
                bx0 = Mathf.Min(bx0, x); bx1 = Mathf.Max(bx1, x); by0 = Mathf.Min(by0, y); by1 = Mathf.Max(by1, y);
                mx += x; my += y;
            }
            mx /= pix.Count; my /= pix.Count;
            int lw = bx1 - bx0 + 1, lh = by1 - by0 + 1;
            var f = MakeFrame(pix, w, mx, my);

            if (pattern == SubdividePattern.Auto)
            {
                var margin = Mathf.Max(2, Mathf.RoundToInt(0.04f * Mathf.Max(map.w, map.h)));
                var background = bx0 <= margin || by0 <= margin || bx1 >= map.w - 1 - margin || by1 >= map.h - 1 - margin; // mảnh chạm mép tranh: nền
                pattern = Choose(f, target, background, pix.Count * 31 + pix[0]);
            }
            var keys = Keys(pattern, f, target, pix.Count);
            if (keys == null) return null;

            var key = new int[lw * lh];
            for (var i = 0; i < key.Length; i++) key[i] = -1;
            var local = new int[pix.Count];
            for (var i = 0; i < pix.Count; i++)
            {
                local[i] = (pix[i] / w - by0) * lw + pix[i] % w - bx0;
                key[local[i]] = keys[i];
            }

            // cụm liền theo khoá (ô bị vùng lõm cắt đôi thành hai cụm), rồi gộp cụm nhỏ vào láng giềng chung biên dài nhất
            var comp = ImageOps.ComponentsByKey(key, lw, lh, out var count);
            if (count > 40 * target) return null; // chia quá vụn (vùng rất khúc khuỷu): bỏ, tránh gộp từng cụm lâu
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
                    border[k] = border.TryGetValue(k, out var c) ? c + 1 : 1;
                }
                if (x + 1 < lw) Pair(y * lw + x + 1);
                if (y + 1 < lh) Pair((y + 1) * lw + x);
            }
            var minArea = MinTileFraction * pix.Count / target;
            while (true)
            {
                var small = 0;
                for (var i = 1; i <= count; i++) if (Find(i) == i && (small == 0 || area[i] < area[small])) small = i;
                if (small == 0 || area[small] >= minArea) break;
                var bestN = 0; var bestL = 0;
                foreach (var kv in border)
                {
                    int p = Find((int)(kv.Key / (count + 1))), q = Find((int)(kv.Key % (count + 1)));
                    if (p == q || (p != small && q != small)) continue;
                    var other = p == small ? q : p;
                    if (kv.Value > bestL) { bestL = kv.Value; bestN = other; }
                }
                if (bestN == 0) break;
                parent[small] = bestN; area[bestN] += area[small];
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

        private static Frame MakeFrame(List<int> pix, int w, double mx, double my)
        {
            double sxx = 0, syy = 0, sxy = 0;
            foreach (var p in pix)
            {
                var dx = p % w - mx; var dy = p / w - my;
                sxx += dx * dx; syy += dy * dy; sxy += dx * dy;
            }
            var ang = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
            float ux = (float)Math.Cos(ang), uy = (float)Math.Sin(ang);
            var f = new Frame { n = pix.Count, u = new float[pix.Count], v = new float[pix.Count], umin = float.MaxValue, vmin = float.MaxValue, umax = float.MinValue, vmax = float.MinValue };
            double su2 = 0, sv2 = 0;
            for (var i = 0; i < pix.Count; i++)
            {
                float dx = (float)(pix[i] % w - mx), dy = (float)(pix[i] / w - my);
                var u = dx * ux + dy * uy;
                var v = -dx * uy + dy * ux;
                f.u[i] = u; f.v[i] = v;
                f.umin = Mathf.Min(f.umin, u); f.umax = Mathf.Max(f.umax, u);
                f.vmin = Mathf.Min(f.vmin, v); f.vmax = Mathf.Max(f.vmax, v);
                su2 += u * u; sv2 += v * v;
            }
            f.su = Mathf.Max(Mathf.Abs(f.umin), Mathf.Abs(f.umax));
            f.sv = Mathf.Max(Mathf.Abs(f.vmin), Mathf.Abs(f.vmax));
            f.aspect = (float)Math.Sqrt(su2 / Math.Max(1e-6, sv2));
            return f;
        }

        // Tự chọn kiểu: mảnh nền (chạm mép tranh) lấy một trong các kiểu hoạ tiết (cố định theo mảnh, mảnh khác nhau ra kiểu khác nhau); mảnh chủ thể theo hình: dài thì dải cong, tròn và nhiều ô thì vòng, còn lại gạch
        private static SubdividePattern Choose(Frame f, int target, bool background, int seed)
        {
            var pick = (seed & 0x7FFFFFFF) % 1000;
            SubdividePattern Of(params SubdividePattern[] options) => options[pick % options.Length];
            if (background)
            {
                if (f.aspect >= 2.5f) return Of(SubdividePattern.Brick, SubdividePattern.Mondrian, SubdividePattern.Bands);
                if (target >= 10) return Of(SubdividePattern.Rings, SubdividePattern.Scales, SubdividePattern.Hex, SubdividePattern.Rays, SubdividePattern.Mondrian);
                if (target >= 6) return Of(SubdividePattern.Rays, SubdividePattern.Hex, SubdividePattern.Mondrian, SubdividePattern.Scales);
                return Of(SubdividePattern.Mondrian, SubdividePattern.Brick, SubdividePattern.Diamond);
            }
            if (f.aspect >= 2f) return SubdividePattern.Bands;
            if (f.aspect < 1.4f && target >= 12) return SubdividePattern.Rings;
            return SubdividePattern.Brick;
        }

        private static int[] Keys(SubdividePattern pattern, Frame f, int target, int area)
        {
            switch (pattern)
            {
                case SubdividePattern.Bands: return BandKeys(f, target);
                case SubdividePattern.Rays: return RayKeys(f, target);
                case SubdividePattern.Rings: return RingKeys(f, target);
                case SubdividePattern.Mondrian: return MondrianKeys(f, target);
                case SubdividePattern.Scales: return Searched(f, target, area, r => ScaleKeys(f, r), r => 1.5f * r * r);
                case SubdividePattern.Hex: return Searched(f, target, area, a => HexKeys(f, a), a => 2.598f * a * a);
                case SubdividePattern.Diamond: return Searched(f, target, area, s => DiamondKeys(f, s), s => 0.5f * s * s);
                default: return Searched(f, target, area, r => BrickKeys(f, r), r => TileAspect * r * r);
            }
        }

        // Kiểu có một cỡ liên tục: tìm cỡ cho số ô đủ lớn xấp xỉ target (đếm theo diện tích ô nguyên vẹn của chính cỡ đó nên số ô giảm đều khi cỡ tăng)
        private static int[] Searched(Frame f, int target, int area, Func<float, int[]> keysAt, Func<float, float> cellArea)
        {
            var nominal = Mathf.Sqrt(area / (float)target / cellArea(1f));
            float lo = nominal * 0.4f, hi = nominal * 2.5f;
            for (var it = 0; it < 14; it++)
            {
                var mid = (lo + hi) * 0.5f;
                if (CountCells(keysAt(mid), cellArea(mid)) > target) lo = mid; else hi = mid;
            }
            return keysAt(hi);
        }

        private static int CountCells(int[] keys, float full)
        {
            var counts = new Dictionary<int, int>();
            foreach (var k in keys) counts[k] = counts.TryGetValue(k, out var n) ? n + 1 : 1;
            var big = 0;
            foreach (var n in counts.Values) if (n >= MinTileFraction * full) big++;
            return big;
        }

        private static int Key(int a, int b) => (a + Offset) * Stride + b + Offset;

        // ---- gạch so le: hàng song song trục dài, viên chữ nhật so le, lệch tay nhẹ ----
        private static int[] BrickKeys(Frame f, float r)
        {
            var c = r * TileAspect;
            var keys = new int[f.n];
            for (var i = 0; i < f.n; i++)
            {
                var wobble = RowWobble * r * Mathf.Sin((f.u[i] - f.umin) / (5f * c) * 2f * Mathf.PI);
                var row = Mathf.FloorToInt((f.v[i] - f.vmin + wobble) / r);
                var shift = (row & 1) * 0.5f * c + StaggerJitter * c * Hash(row);
                keys[i] = Key(row, Mathf.FloorToInt((f.u[i] - f.umin + shift) / c));
            }
            return keys;
        }

        // ---- tổ ong: lưới lục giác, hàng theo trục dài ----
        private static int[] HexKeys(Frame f, float a)
        {
            var keys = new int[f.n];
            for (var i = 0; i < f.n; i++)
            {
                float q = (0.57735f * f.u[i] - 0.33333f * f.v[i]) / a, rr = 0.66667f * f.v[i] / a;
                float x = q, z = rr, y = -x - z;
                float rx = Mathf.Round(x), ry = Mathf.Round(y), rz = Mathf.Round(z);
                float dx = Mathf.Abs(rx - x), dy = Mathf.Abs(ry - y), dz = Mathf.Abs(rz - z);
                if (dx > dy && dx > dz) rx = -ry - rz; else if (dy > dz) ry = -rx - rz; else rz = -rx - ry;
                keys[i] = Key((int)rz, (int)rx);
            }
            return keys;
        }

        // ---- kim cương: lưới nghiêng 45° so với trục dài ----
        private static int[] DiamondKeys(Frame f, float s)
        {
            var keys = new int[f.n];
            for (var i = 0; i < f.n; i++)
                keys[i] = Key(Mathf.FloorToInt((f.u[i] + f.v[i]) / s), Mathf.FloorToInt((f.u[i] - f.v[i]) / s));
            return keys;
        }

        // ---- vảy: các cung tròn so le, hàng dưới đè lên hàng trên như mái ngói ----
        private static int[] ScaleKeys(Frame f, float rad)
        {
            var rowH = 0.75f * rad;
            var keys = new int[f.n];
            for (var i = 0; i < f.n; i++)
            {
                float u = f.u[i] - f.umin, v = f.v[i] - f.vmin;
                var top = Mathf.FloorToInt((v + rad) / rowH);
                var bottom = Mathf.CeilToInt((v - rad) / rowH);
                var key = int.MinValue;
                for (var row = top; row >= bottom; row--)
                {
                    var shift = (row & 1) * rad;
                    var j = Mathf.RoundToInt((u - shift) / (2f * rad));
                    float dx = u - (shift + j * 2f * rad), dy = v - row * rowH;
                    if (dx * dx + dy * dy <= rad * rad) { key = Key(row, j); break; }
                }
                if (key == int.MinValue) key = Key(Mathf.RoundToInt(v / rowH), Mathf.RoundToInt(u / (2f * rad)));
                keys[i] = key;
            }
            return keys;
        }

        // ---- dải cong: dải chạy dọc xương sống của vùng (đường giữa uốn theo hình) và thu hẹp theo bề rộng tại chỗ, cắt ngang so le ----
        private static int[] BandKeys(Frame f, int target)
        {
            const int Bins = 48;
            var lo = new float[Bins]; var hi = new float[Bins];
            for (var b = 0; b < Bins; b++) { lo[b] = float.MaxValue; hi[b] = float.MinValue; }
            var span = Mathf.Max(1e-3f, f.umax - f.umin);
            for (var i = 0; i < f.n; i++)
            {
                var b = Mathf.Clamp((int)((f.u[i] - f.umin) / span * Bins), 0, Bins - 1);
                lo[b] = Mathf.Min(lo[b], f.v[i]); hi[b] = Mathf.Max(hi[b], f.v[i]);
            }
            var mid = new float[Bins]; var half = new float[Bins]; var have = new bool[Bins];
            var meanHalf = 0f; var hn = 0;
            for (var b = 0; b < Bins; b++)
            {
                have[b] = hi[b] >= lo[b];
                if (!have[b]) continue;
                mid[b] = (lo[b] + hi[b]) * 0.5f; half[b] = (hi[b] - lo[b]) * 0.5f;
                meanHalf += half[b]; hn++;
            }
            if (hn < 2) return null;
            meanHalf /= hn;
            for (var b = 0; b < Bins; b++) // ô trống: nội suy từ ô có dữ liệu gần nhất
            {
                if (have[b]) continue;
                int l = b, r = b;
                while (l >= 0 && !have[l]) l--;
                while (r < Bins && !have[r]) r++;
                if (l < 0) l = r; if (r >= Bins) r = l;
                mid[b] = (mid[l] + mid[r]) * 0.5f; half[b] = (half[l] + half[r]) * 0.5f;
            }
            Smooth(mid, 3); Smooth(half, 3);
            var width = f.vmax - f.vmin;
            var nbands = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(target * width / span)), 2, 8);
            var nseg = Mathf.Max(1, Mathf.CeilToInt(target / (float)nbands));
            var segLen = span / nseg;
            var keys = new int[f.n];
            for (var i = 0; i < f.n; i++)
            {
                var t = Mathf.Clamp((f.u[i] - f.umin) / span * Bins - 0.5f, 0f, Bins - 1.001f);
                int b0 = (int)t; var fr = t - b0;
                var m = Mathf.Lerp(mid[b0], mid[b0 + 1], fr);
                var h = Mathf.Max(0.35f * meanHalf, Mathf.Lerp(half[b0], half[b0 + 1], fr));
                var vv = Mathf.Clamp((f.v[i] - m) / h, -1f, 1f);
                var band = Mathf.Min(nbands - 1, Mathf.FloorToInt((vv + 1f) * 0.5f * nbands));
                var seg = Mathf.FloorToInt((f.u[i] - f.umin + (band & 1) * 0.5f * segLen) / segLen);
                keys[i] = Key(band, seg);
            }
            return keys;
        }

        private static void Smooth(float[] a, int passes)
        {
            var t = new float[a.Length];
            for (var p = 0; p < passes; p++)
            {
                for (var i = 0; i < a.Length; i++)
                    t[i] = (a[Mathf.Max(0, i - 1)] + 2f * a[i] + a[Mathf.Min(a.Length - 1, i + 1)]) * 0.25f;
                Array.Copy(t, a, a.Length);
            }
        }

        // ---- tia: các nhẻ toả ra từ trọng tâm, có một ô giữa khi nhiều nhẻ ----
        private static int[] RayKeys(Frame f, int target)
        {
            var n = Mathf.Clamp(target, 3, 28);
            var hub = n >= 9 ? 0.14f : 0f;
            var rmax = Mathf.Sqrt(f.su * f.su + f.sv * f.sv);
            var keys = new int[f.n];
            for (var i = 0; i < f.n; i++)
            {
                if (hub > 0f && f.u[i] * f.u[i] + f.v[i] * f.v[i] < hub * hub * rmax * rmax) { keys[i] = Key(0, 0); continue; }
                var th = Mathf.Atan2(f.v[i], f.u[i]) + Mathf.PI;
                keys[i] = Key(1, Mathf.Min(n - 1, Mathf.FloorToInt(th / (2f * Mathf.PI) * n)) + 1);
            }
            return keys;
        }

        // ---- vòng đồng tâm: vành elip theo hình vùng, mỗi vành chia nhẻ (số nhẻ tăng theo vành, so le), giữa là một ô ----
        private static int[] RingKeys(Frame f, int target)
        {
            int Cells(int nr) { var t = 1; for (var k = 1; k < nr; k++) t += RingCount(k); return t; }
            var best = 2; var bestD = int.MaxValue;
            for (var nr = 2; nr <= 8; nr++)
            {
                var d = Mathf.Abs(Cells(nr) - target);
                if (d < bestD) { bestD = d; best = nr; }
            }
            var nrings = best;
            var rmax = 1e-3f;
            var rad = new float[f.n]; var ang = new float[f.n];
            for (var i = 0; i < f.n; i++)
            {
                float x = f.u[i] / Mathf.Max(1e-3f, f.su), y = f.v[i] / Mathf.Max(1e-3f, f.sv);
                rad[i] = Mathf.Sqrt(x * x + y * y); ang[i] = Mathf.Atan2(y, x) + Mathf.PI;
                rmax = Mathf.Max(rmax, rad[i]);
            }
            var keys = new int[f.n];
            var hub = 0.55f * rmax / nrings; // ô giữa nhỏ hơn một vành để các vành ngoài không bị lép
            var ringW = (rmax - hub) / (nrings - 1);
            for (var i = 0; i < f.n; i++)
            {
                if (rad[i] < hub) { keys[i] = Key(0, 0); continue; }
                var ring = Mathf.Min(nrings - 1, 1 + Mathf.FloorToInt((rad[i] - hub) / ringW));
                var cnt = RingCount(ring);
                var sec = Mathf.FloorToInt(ang[i] / (2f * Mathf.PI) * cnt + 0.5f * (ring & 1)) % cnt;
                keys[i] = Key(ring, sec);
            }
            return keys;
        }

        // Số ô của vành k (k ≥ 1): chu vi / bề rộng vành ~ ô gần vuông
        private static int RingCount(int k) => Mathf.Max(4, Mathf.RoundToInt(2f * Mathf.PI * (k + 0.5f) / 1.25f));

        // ---- Mondrian: chia đôi liên tục ô lớn nhất theo trục dài của nó tại điểm ngẫu nhiên (35-65% số pixel), được các khối chữ nhật to nhỏ khác nhau ----
        private static int[] MondrianKeys(Frame f, int target)
        {
            var rng = new System.Random(f.n * 31 + target);
            var groups = new List<List<int>>();
            var all = new List<int>(f.n);
            for (var i = 0; i < f.n; i++) all.Add(i);
            groups.Add(all);
            var sample = new float[512];
            while (groups.Count < target)
            {
                var gi = 0;
                for (var k = 1; k < groups.Count; k++) if (groups[k].Count > groups[gi].Count) gi = k;
                var g = groups[gi];
                if (g.Count < 16) break;
                float u0 = float.MaxValue, u1 = float.MinValue, v0 = float.MaxValue, v1 = float.MinValue;
                foreach (var i in g) { u0 = Mathf.Min(u0, f.u[i]); u1 = Mathf.Max(u1, f.u[i]); v0 = Mathf.Min(v0, f.v[i]); v1 = Mathf.Max(v1, f.v[i]); }
                var alongU = (u1 - u0) >= (v1 - v0);
                var vals = alongU ? f.u : f.v;
                for (var s = 0; s < sample.Length; s++) sample[s] = vals[g[rng.Next(g.Count)]];
                Array.Sort(sample);
                var q = 0.35f + 0.3f * (float)rng.NextDouble();
                var cut = sample[Mathf.Clamp((int)(q * sample.Length), 0, sample.Length - 1)];
                var a = new List<int>(); var b = new List<int>();
                foreach (var i in g) (vals[i] < cut ? a : b).Add(i);
                if (a.Count == 0 || b.Count == 0) break;
                groups[gi] = a; groups.Add(b);
            }
            var keys = new int[f.n];
            for (var k = 0; k < groups.Count; k++) foreach (var i in groups[k]) keys[i] = Key(0, k);
            return keys;
        }

        // Số giả ngẫu nhiên cố định theo hàng, trong -1..1
        private static float Hash(int row)
        {
            unchecked
            {
                var x = (uint)(row * 73856093) ^ 0x9E3779B9u;
                x ^= x >> 15; x *= 0x2C1B3C6Du; x ^= x >> 12;
                return (x & 0xFFFF) / 32767.5f - 1f;
            }
        }
    }
}
