using Falcon.InGame.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Mỗi mảnh phải chứa được chữ số (giá trị của mảnh): mảnh có hình tròn nội tiếp nhỏ hơn bán kính tối thiểu thì gộp vào mảnh kề chung biên dài nhất.</summary>
    public static class PieceSize
    {
        public const float TextRadiusFraction = 0.025f; // bán kính hình tròn nội tiếp tối thiểu theo cạnh ngắn của tranh
        private const int MaxPasses = 12;

        public static float MinRadius(RegionMap m) => TextRadiusFraction * Mathf.Min(m.w, m.h);

        public static void EnsureTextFits(RegionMap m) => EnsureTextFits(m, MinRadius(m));

        public static void EnsureTextFits(RegionMap m, float minRadius)
        {
            for (var pass = 0; pass < MaxPasses; pass++)
            {
                var radius = InscribedRadii(m);
                var share = SharedBoundary(m);
                var area = new int[m.Count + 1];
                var edge = new bool[m.Count + 1]; // bbox chạm vùng sát mép ảnh
                int mx = (int)(EdgeBand * m.w), my = (int)(EdgeBand * m.h);
                for (var i = 0; i < m.reg.Length; i++)
                {
                    var r = m.reg[i];
                    if (r <= 0) continue;
                    area[r]++;
                    int x = i % m.w, y = i / m.w;
                    if (x < mx || y < my || x >= m.w - mx || y >= m.h - my) edge[r] = true;
                }
                var small = new List<int>();
                for (var id = 1; id < radius.Length; id++)
                    if (radius[id] > 0f && radius[id] < minRadius) small.Add(id);
                if (small.Count == 0) return;

                small.Sort((a, b) => area[a].CompareTo(area[b]));
                var parent = new int[m.Count + 1];
                for (var i = 0; i < parent.Length; i++) parent[i] = i;
                int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }

                var changed = false;
                foreach (var id in small)
                {
                    if (Find(id) != id) continue;
                    var best = 0;
                    var bl = 0;
                    foreach (var kv in share[id])
                    {
                        var o = Find(kv.Key);
                        if (o == id) continue;
                        if (kv.Value > bl) { bl = kv.Value; best = o; }
                    }
                    if (best == 0) continue;
                    parent[id] = best;
                    area[best] += area[id];
                    changed = true;
                }
                if (!changed) return;
                for (var i = 0; i < m.reg.Length; i++)
                    if (m.reg[i] > 0) m.reg[i] = Find(m.reg[i]);
                PixelRegions.Relabel(m);
            }
        }

        // Hậu kỳ: mảnh nhỏ hơn fraction diện tích tranh (chấm li ti) gộp vào mảnh kề chung biên dài nhất, kể cả khi nổi bật
        public static void RemoveSpecks(RegionMap m, float fraction)
        {
            for (var pass = 0; pass < 3; pass++)
            {
                var area = new int[m.Count + 1];
                foreach (var r in m.reg) if (r > 0) area[r]++;
                var limit = fraction * m.w * m.h;
                var share = SharedBoundary(m);
                var parent = new int[m.Count + 1];
                for (var i = 0; i < parent.Length; i++) parent[i] = i;
                int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
                var changed = false;
                for (var id = 1; id <= m.Count; id++)
                {
                    if (area[id] == 0 || area[id] >= limit) continue;
                    var best = 0;
                    var bl = 0;
                    foreach (var kv in share[id])
                        if (kv.Value > bl && area[kv.Key] >= limit) { bl = kv.Value; best = kv.Key; }
                    if (best == 0) continue;
                    parent[id] = best;
                    changed = true;
                }
                if (!changed) return;
                for (var i = 0; i < m.reg.Length; i++) if (m.reg[i] > 0) m.reg[i] = Find(m.reg[i]);
                PixelRegions.Relabel(m);
            }
        }

        // Quá nhiều mảnh: gộp dần mảnh nhỏ nhất vào láng giềng gần màu (biên chung dài, màu bảng gần) tới khi còn max mảnh
        public static void LimitCount(RegionMap m, int max)
        {
            for (var guard = 0; guard < 400 && m.Count > max; guard++)
            {
                var area = new int[m.Count + 1];
                foreach (var r in m.reg) if (r > 0) area[r]++;
                var share = SharedBoundary(m);
                var order = new List<int>();
                for (var id = 1; id <= m.Count; id++) order.Add(id);
                order.Sort((a, b) => area[a].CompareTo(area[b]));
                var parent = new int[m.Count + 1];
                for (var i = 0; i < parent.Length; i++) parent[i] = i;
                int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
                var excess = m.Count - max;
                var merged = 0;
                foreach (var id in order)
                {
                    if (merged >= excess) break;
                    if (Find(id) != id || share[id].Count == 0) continue;
                    var mine = ColorPalette.ToLab(ColorPalette.Get(m.colors[id]));
                    var best = 0;
                    var bs = -1f;
                    foreach (var kv in share[id])
                    {
                        var o = Find(kv.Key);
                        if (o == id) continue;
                        var de = (ColorPalette.ToLab(ColorPalette.Get(m.colors[o])) - mine).magnitude / 20f;
                        var score = kv.Value / (1f + de * de);
                        if (score > bs) { bs = score; best = o; }
                    }
                    if (best == 0) continue;
                    parent[id] = best;
                    merged++;
                }
                if (merged == 0) return;
                for (var i = 0; i < m.reg.Length; i++) if (m.reg[i] > 0) m.reg[i] = Find(m.reg[i]);
                PixelRegions.Relabel(m);
            }
        }

        private const float EdgeBand = 0.04f, EdgeNoiseArea = 0.0025f;
        private const float StandOutDeltaE = 14f, StandOutMinArea = 45;

        // Mảnh mảnh nhưng khác hẳn mọi mảnh kề về màu bảng và không phải màu pha giữa 2 mảnh kề (viền khử răng cưa) thì giữ, không gộp
        private static bool Stands(RegionMap m, int id, Dictionary<int, int>[] share, int[] area, bool nearEdge)
        {
            if (nearEdge && area[id] < EdgeNoiseArea * m.w * m.h) return false; // vụn sát mép ảnh (chữ mờ, vết quét): nhiễu
            if (share[id].Count == 0) return false;
            if (area[id] < StandOutMinArea) return false;
            var mine = ColorPalette.ToLab(ColorPalette.Get(m.colors[id]));
            var others = new List<Vector3>();
            foreach (var o in share[id].Keys)
            {
                var c = ColorPalette.ToLab(ColorPalette.Get(m.colors[o]));
                if ((c - mine).magnitude < StandOutDeltaE) return false;
                others.Add(c);
            }
            for (var i = 0; i < others.Count; i++)
            for (var j = i + 1; j < others.Count; j++)
                if (RegionOps.DistToSegment(mine, others[i], others[j]) < 9f) return false;
            return true;
        }

        // Bán kính hình tròn nội tiếp lớn nhất của từng mảnh (pixel); rìa ảnh tính là biên
        public static float[] InscribedRadii(RegionMap m)
        {
            int w = m.w, h = m.h, n = w * h;
            const int inf = int.MaxValue / 2;
            var d = new int[n];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                var l = m.reg[i];
                if (l <= 0) continue;
                var edge = x == 0 || y == 0 || x == w - 1 || y == h - 1 || m.reg[i - 1] != l || m.reg[i + 1] != l || m.reg[i - w] != l || m.reg[i + w] != l;
                d[i] = edge ? 3 : inf;
            }
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (d[i] == 0 || d[i] == 3) continue;
                var v = d[i];
                if (x > 0) v = Mathf.Min(v, d[i - 1] + 3);
                if (y > 0) v = Mathf.Min(v, d[i - w] + 3);
                if (x > 0 && y > 0) v = Mathf.Min(v, d[i - w - 1] + 4);
                if (x < w - 1 && y > 0) v = Mathf.Min(v, d[i - w + 1] + 4);
                d[i] = v;
            }
            var best = new int[m.Count + 1];
            for (var y = h - 1; y >= 0; y--)
            for (var x = w - 1; x >= 0; x--)
            {
                var i = y * w + x;
                if (d[i] == 0) continue;
                var v = d[i];
                if (v != 3)
                {
                    if (x < w - 1) v = Mathf.Min(v, d[i + 1] + 3);
                    if (y < h - 1) v = Mathf.Min(v, d[i + w] + 3);
                    if (x < w - 1 && y < h - 1) v = Mathf.Min(v, d[i + w + 1] + 4);
                    if (x > 0 && y < h - 1) v = Mathf.Min(v, d[i + w - 1] + 4);
                }
                if (v < inf && v > best[m.reg[i]]) best[m.reg[i]] = v;
            }
            var res = new float[m.Count + 1];
            for (var id = 1; id < res.Length; id++) res[id] = best[id] / 3f;
            return res;
        }

        // Với mỗi mảnh: độ dài biên chung (số cặp pixel) với từng mảnh kề
        private static Dictionary<int, int>[] SharedBoundary(RegionMap m)
        {
            var res = new Dictionary<int, int>[m.Count + 1];
            for (var i = 0; i < res.Length; i++) res[i] = new Dictionary<int, int>();
            int w = m.w, h = m.h;
            void Pair(int a, int b)
            {
                if (a <= 0 || b <= 0 || a == b) return;
                res[a].TryGetValue(b, out var v);
                res[a][b] = v + 1;
                res[b].TryGetValue(a, out v);
                res[b][a] = v + 1;
            }
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (x + 1 < w) Pair(m.reg[i], m.reg[i + 1]);
                if (y + 1 < h) Pair(m.reg[i], m.reg[i + w]);
            }
            return res;
        }
    }
}
