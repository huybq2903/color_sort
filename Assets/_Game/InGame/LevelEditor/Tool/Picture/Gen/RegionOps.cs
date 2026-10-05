using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.InGame.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Thao tác trên bản đồ nhãn vùng: gộp vùng nhỏ, gộp chuyển sắc, chọn màu bảng cho từng vùng.</summary>
    internal static class RegionOps
    {
        private const int MaxPasses = 40;

        // Vùng nhỏ hơn minArea gộp vào vùng kề có biên chung dài nhất
        private const float ColourBias = 8f;

        public static void MergeSmall(int[] lab, int w, int h, int minArea, Vector3[] img = null, float keepDeltaE = 12f, int keepMinArea = 40, int midArea = 0, float midDeltaE = 20f)
        {
            for (var pass = 0; pass < MaxPasses; pass++)
            {
                var area = Areas(lab);
                var keep = img != null ? StandOut(lab, img, w, h, area, minArea, keepDeltaE, keepMinArea) : null;
                // tầng giữa: mảnh vừa (minArea..midArea) cùng tông với láng giềng là vân/bóng, gộp; chỉ giữ khi tương phản mạnh
                var keepMid = img != null && midArea > minArea ? StandOut(lab, img, w, h, area, midArea, midDeltaE, minArea) : null;
                var share = new Dictionary<long, int>();
                bool Small(int id) => area[id] < minArea ? keep == null || !keep[id] : keepMid != null && area[id] < midArea && !keepMid[id];
                void Add(int a, int b)
                {
                    if (a <= 0 || b <= 0 || a == b) return;
                    if (Small(a)) { var k = (long)a * area.Length + b; share[k] = share.TryGetValue(k, out var v) ? v + 1 : 1; }
                    if (Small(b)) { var k = (long)b * area.Length + a; share[k] = share.TryGetValue(k, out var v) ? v + 1 : 1; }
                }
                for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var i = y * w + x;
                    if (x + 1 < w) Add(lab[i], lab[i + 1]);
                    if (y + 1 < h) Add(lab[i], lab[i + w]);
                }
                var meanOf = new Vector3[area.Length];
                if (img != null)
                {
                    for (var i = 0; i < lab.Length; i++) if (lab[i] > 0) meanOf[lab[i]] += img[i];
                    for (var id = 1; id < area.Length; id++) if (area[id] > 0) meanOf[id] /= area[id];
                }
                var best = new Dictionary<int, (int to, float len)>();
                foreach (var kv in share)
                {
                    int a = (int)(kv.Key / area.Length), b = (int)(kv.Key % area.Length);
                    float score = kv.Value;
                    if (img != null) { var de = (meanOf[a] - meanOf[b]).magnitude / ColourBias; score /= 1f + de * de; } // gộp vào láng giềng gần màu, không chỉ biên dài (đen không bị nuốt vào trắng)
                    if (!best.TryGetValue(a, out var cur) || score > cur.len) best[a] = (b, score);
                }
                if (best.Count == 0) return;
                var parent = new int[area.Length];
                for (var i = 0; i < parent.Length; i++) parent[i] = i;
                int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
                foreach (var a in best.Keys.OrderBy(k => area[k]))
                {
                    var to = Find(best[a].to);
                    if (Find(a) != to) parent[Find(a)] = to;
                }
                for (var i = 0; i < lab.Length; i++) if (lab[i] > 0) lab[i] = Find(lab[i]);
            }
        }

        // Vùng nhỏ nhưng màu trung bình khác hẳn mọi vùng kề (ΔE ≥ keepDeltaE) và không phải màu pha trộn nằm giữa 2 vùng kề (viền khử răng cưa): chi tiết nổi bật, không gộp
        private static bool[] StandOut(int[] lab, Vector3[] img, int w, int h, int[] area, int minArea, float keepDeltaE, int keepMinArea)
        {
            var sum = new Vector3[area.Length];
            for (var i = 0; i < lab.Length; i++) if (lab[i] > 0) sum[lab[i]] += img[i];
            var nb = new HashSet<int>[area.Length];
            void Pair(int a, int b)
            {
                if (a <= 0 || b <= 0 || a == b) return;
                (nb[a] ??= new HashSet<int>()).Add(b);
                (nb[b] ??= new HashSet<int>()).Add(a);
            }
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (x + 1 < w) Pair(lab[i], lab[i + 1]);
                if (y + 1 < h) Pair(lab[i], lab[i + w]);
            }
            var bx0 = new int[area.Length]; var by0 = new int[area.Length]; var bx1 = new int[area.Length]; var by1 = new int[area.Length];
            for (var i = 0; i < area.Length; i++) { bx0[i] = by0[i] = int.MaxValue; bx1[i] = by1[i] = -1; }
            for (var i = 0; i < lab.Length; i++)
            {
                var l = lab[i];
                if (l <= 0) continue;
                int x = i % w, y = i / w;
                if (x < bx0[l]) bx0[l] = x; if (x > bx1[l]) bx1[l] = x; if (y < by0[l]) by0[l] = y; if (y > by1[l]) by1[l] = y;
            }
            var keep = new bool[area.Length];
            for (var id = 1; id < area.Length; id++)
            {
                var longSide = Mathf.Max(bx1[id] - bx0[id], by1[id] - by0[id]) + 1;
                var strokeLike = longSide >= StrokeMinLength && area[id] / (float)longSide <= StrokeMaxThickness; // dài và mỏng: viền, gân, dây
                var floor = strokeLike ? StrokeMinArea : keepMinArea;
                if (area[id] < floor || area[id] >= minArea || nb[id] == null) continue;
                var m = sum[id] / area[id];
                var ok = true;
                var list = nb[id].Where(o => area[o] >= minArea).Select(o => sum[o] / area[o]).ToList(); // chỉ so với láng giềng đủ lớn: mảnh vụn khử răng cưa không làm mất nét
                if (list.Count == 0) continue;
                foreach (var c in list) if ((c - m).magnitude < keepDeltaE) { ok = false; break; }
                for (var i = 0; ok && i < list.Count; i++)
                for (var j = i + 1; j < list.Count; j++)
                    if (DistToSegment(m, list[i], list[j]) < BlendTolerance) { ok = false; break; }
                keep[id] = ok;
            }
            return keep;
        }

        private const float BlendTolerance = 7f, StrokeMaxThickness = 5f;
        private const int StrokeMinLength = 14, StrokeMinArea = 14;

        public static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var l2 = ab.sqrMagnitude;
            var t = l2 < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
            return (p - (a + ab * t)).magnitude;
        }

        private static int[] Areas(int[] lab)
        {
            var area = new int[(lab.Length == 0 ? 0 : lab.Max()) + 1];
            foreach (var l in lab) if (l > 0) area[l]++;
            return area;
        }

        // Dồn nhãn liên tục 1..n
        public static int Compact(int[] lab)
        {
            var map = new Dictionary<int, int>();
            for (var i = 0; i < lab.Length; i++)
            {
                if (lab[i] <= 0) continue;
                if (!map.TryGetValue(lab[i], out var id)) map[lab[i]] = id = map.Count + 1;
                lab[i] = id;
            }
            return map.Count;
        }

        // Màu bảng của mỗi vùng = màu bảng gần màu trung vị (L, a, b) của pixel lõi; không có lõi thì dùng cả vùng
        public static int[] FaceColors(int[] lab, int count, Vector3[] img, bool[] core)
        {
            var cols = new int[count + 1];
            var core3 = new List<Vector3>[count + 1];
            var all3 = new List<Vector3>[count + 1];
            for (var i = 0; i <= count; i++) { core3[i] = new List<Vector3>(); all3[i] = new List<Vector3>(); }
            for (var i = 0; i < lab.Length; i++)
            {
                if (lab[i] <= 0) continue;
                all3[lab[i]].Add(img[i]);
                if (core == null || core[i]) core3[lab[i]].Add(img[i]);
            }
            for (var id = 1; id <= count; id++)
            {
                var src = core3[id].Count >= 5 ? core3[id] : all3[id];
                cols[id] = src.Count == 0 ? 0 : ColorPalette.NearestLab(new Vector3(Median(src, 0), Median(src, 1), Median(src, 2)));
            }
            return cols;
        }

        // Màu bảng cho từng vùng có xét lân cận: vùng kề nhau mà tông gốc khác thật (ΔE > 5) thì không dùng chung màu bảng, để các lớp tông không bị nhoà vào nhau
        public static int[] AssignColors(int[] lab, int count, Vector3[] img, int w, int h, float separate = 5f)
        {
            var med = new Vector3[count + 1];
            var all3 = new List<Vector3>[count + 1];
            for (var i = 0; i <= count; i++) all3[i] = new List<Vector3>();
            var area = new int[count + 1];
            for (var i = 0; i < lab.Length; i++) if (lab[i] > 0) { all3[lab[i]].Add(img[i]); area[lab[i]]++; }
            for (var id = 1; id <= count; id++) if (all3[id].Count > 0) med[id] = new Vector3(Median(all3[id], 0), Median(all3[id], 1), Median(all3[id], 2));
            var nb = new HashSet<int>[count + 1];
            for (var i = 0; i <= count; i++) nb[i] = new HashSet<int>();
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                void Link(int j) { if (lab[i] > 0 && lab[j] > 0 && lab[i] != lab[j]) { nb[lab[i]].Add(lab[j]); nb[lab[j]].Add(lab[i]); } }
                if (x + 1 < w) Link(i + 1);
                if (y + 1 < h) Link(i + w);
            }
            var cols = new int[count + 1];
            var done = new bool[count + 1];
            var avoid = new List<int>();
            foreach (var id in Enumerable.Range(1, count).OrderByDescending(i => area[i]))
            {
                avoid.Clear();
                foreach (var o in nb[id]) if (done[o] && (med[o] - med[id]).magnitude > separate) avoid.Add(cols[o]);
                cols[id] = ColorPalette.NearestLab(med[id], avoid);
                done[id] = true;
            }
            return cols;
        }

        private static float Median(List<Vector3> v, int c)
        {
            var a = new float[v.Count];
            for (var i = 0; i < a.Length; i++) a[i] = v[i][c];
            Array.Sort(a);
            return a[a.Length / 2];
        }

        // Cặp vùng kề: tổng ΔLab dọc biên và số cặp pixel
        private static Dictionary<long, (double sum, int n)> PairStats(int[] lab, Vector3[] img, int w, int h, int stride, bool[] protect = null, Dictionary<long, int> hits = null)
        {
            var d = new Dictionary<long, (double, int)>();
            void Add(int i, int j)
            {
                int a = lab[i], b = lab[j];
                if (a <= 0 || b <= 0 || a == b) return;
                long k = (long)Math.Min(a, b) * stride + Math.Max(a, b);
                var v = (img[i] - img[j]).magnitude;
                d[k] = d.TryGetValue(k, out var c) ? (c.Item1 + v, c.Item2 + 1) : (v, 1);
                if (hits != null && (protect[i] || protect[j])) hits[k] = hits.TryGetValue(k, out var hv) ? hv + 1 : 1;
            }
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (x + 1 < w) Add(i, i + 1);
                if (y + 1 < h) Add(i, i + w);
            }
            return d.ToDictionary(kv => kv.Key, kv => (kv.Value.Item1, kv.Value.Item2));
        }

        // Ngưỡng Otsu 1 chiều có trọng số
        private static float Otsu(List<(float v, int w)> items)
        {
            items.Sort((a, b) => a.v.CompareTo(b.v));
            double tot = 0, tm = 0;
            foreach (var (v, w) in items) { tot += w; tm += (double)w * v; }
            double cw = 0, cm = 0, best = -1;
            var bt = items[0].v;
            for (var i = 0; i < items.Count - 1; i++)
            {
                cw += items[i].w; cm += (double)items[i].w * items[i].v;
                var w0 = cw; var w1 = tot - cw;
                if (w0 <= 0 || w1 <= 0) continue;
                var m0 = cm / w0; var m1 = (tm - cm) / w1;
                var var_ = w0 * w1 * (m0 - m1) * (m0 - m1);
                if (var_ > best) { best = var_; bt = (items[i].v + items[i + 1].v) / 2f; }
            }
            return bt;
        }

        // Gộp 2 vùng kề khi biên giữa chúng yếu (chuyển sắc, bóng mềm); ngưỡng yếu/mạnh tự chọn theo ảnh bằng Otsu
        public static void MergeGradients(int[] lab, Vector3[] smooth, int w, int h, float maxDeltaE = 30f, float fixedThreshold = -1f, bool[] protect = null)
        {
            var thr = fixedThreshold;
            for (var it = 0; it < MaxPasses; it++)
            {
                var count = lab.Max();
                var stride = count + 1;
                var hits = protect != null ? new Dictionary<long, int>() : null;
                var stats = PairStats(lab, smooth, w, h, stride, protect, hits).Where(kv => kv.Value.n >= 6 && (hits == null || !hits.TryGetValue(kv.Key, out var hh) || hh < 0.4f * kv.Value.n)).ToList(); // biên chủ yếu nằm trên gân/nét mảnh: ranh giới thật, không gộp
                if (stats.Count == 0) return;
                if (thr < 0f) thr = Mathf.Clamp(Otsu(stats.Select(kv => ((float)(kv.Value.sum / kv.Value.n), kv.Value.n)).ToList()), 2.5f, 8f);
                var sum = new Vector3[stride];
                var n = new int[stride];
                for (var i = 0; i < lab.Length; i++) if (lab[i] > 0) { sum[lab[i]] += smooth[i]; n[lab[i]]++; }
                var bestE = float.MaxValue;
                int ba = 0, bb = 0;
                foreach (var kv in stats)
                {
                    int a = (int)(kv.Key / stride), b = (int)(kv.Key % stride);
                    var e = (float)(kv.Value.sum / kv.Value.n);
                    if (e >= thr || e >= bestE) continue;
                    if ((sum[a] / n[a] - sum[b] / n[b]).magnitude >= maxDeltaE) continue;
                    bestE = e; ba = a; bb = b;
                }
                if (ba == 0) return;
                for (var i = 0; i < lab.Length; i++) if (lab[i] == bb) lab[i] = ba;
            }
        }

        // Vùng kề cùng màu bảng thì gộp (không có nét ngăn cách)
        public static void MergeSamePalette(int[] lab, int w, int h, int[] colorOf)
        {
            var parent = new Dictionary<int, int>();
            int Find(int x) { while (parent.TryGetValue(x, out var p) && p != x) x = p; return x; }
            void Link(int a, int b)
            {
                if (a <= 0 || b <= 0 || a == b || colorOf[a] != colorOf[b]) return;
                int ra = Find(a), rb = Find(b);
                if (ra != rb) parent[rb] = ra;
            }
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (x + 1 < w) Link(lab[i], lab[i + 1]);
                if (y + 1 < h) Link(lab[i], lab[i + w]);
            }
            for (var i = 0; i < lab.Length; i++) if (lab[i] > 0) lab[i] = Find(lab[i]);
        }
    }
}
