using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Falcon.InGame.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Ảnh có nét chì / nét đen: dò nét, làm mảnh về tâm nét, mỗi ô kín giữa các nét là 1 mảnh; null nếu ảnh không thuộc kiểu này.</summary>
    internal static class InkRoute
    {
        private static readonly int[] Kernels = { 7, 11, 17, 25 };
        private static readonly float[] Thresholds = { 14f, 24f };
        private const int FarGap = 24, KnotReach = 3;
        private const float KnotGray = 55f;
        private const int FixJ = 8; // bán kính (px) vùng chỉnh ngã ba chữ T
        private const float MaskSmooth = 1.3f;
        private const float InkSatFree = 40f, InkSatSpan = 50f;
        private const float PreferSmall = 0.97f;
        private const int GoodMinArea = 150, MinPiecesGood = 8, MergeMinArea = 40;
        private const float MaxFaceFraction = 0.35f, MinCoverage = 0.55f, MinInk = 0.01f, MinInkVeins = 0.002f, MaxInk = 0.45f;

        private sealed class Candidate
        {
            public float score, coverage;
            public bool[] ink, skel;
            public int count, kernel;
        }

        public static RegionMap Build(Color32[] px, Vector3[] lab, int w, int h)
        {
            var n = w * h;
            var g = Prepare(px, w, h);
            var outline = SubjectOutline(px, lab, w, h);
            var veins = FlatRoute.ThinBright(px, w, h); // gân sáng mảnh cũng là đường cắt (cánh bướm chia ô bởi gân)
            if (veins != null) g = ImageOps.Open(g, w, h, FlatRoute.VeinWidth); // bỏ gân sáng khỏi ảnh dò nét tối: gân làm dải màu sát nó trông như nét
            Candidate best = null;
            var jobs = new List<(int k, float thr)>();
            foreach (var k in Kernels) foreach (var thr in Thresholds) jobs.Add((k, thr));
            var gate = new object();
            var notInk = NotInk(px, w, h); // màu bão hoà (kính xanh, tím...) không phải nét đen
            var dark = DarkPixels(px, notInk); // đen đặc: chỗ nét dày giao nhau, black-hat không thấy vì rộng hơn cửa sổ
            var cands = new List<Candidate>();
            // 8 tổ hợp (cỡ cửa sổ × ngưỡng) độc lập nhau: chạy song song
            Parallel.ForEach(jobs, job =>
            {
                var (k, thr) = job;
                var clo = ImageOps.Close(g, w, h, k);
                var bh = new float[n];
                for (var i = 0; i < n; i++) bh[i] = (clo[i] - g[i]) * (1f - notInk[i]);
                var ink = Hysteresis(bh, w, h, thr);
                if (veins != null) for (var i = 0; i < n; i++) ink[i] |= veins[i];
                var near = ink;
                for (var d = 0; d < KnotReach; d++) near = ImageOps.Dilate3(near, w, h);
                for (var i = 0; i < n; i++) if (dark[i] && near[i]) ink[i] = true;
                ink = ImageOps.Dilate3(ink, w, h);
                ink = ImageOps.SmoothMask(ink, w, h, MaskSmooth); // đường nét mịn trước khi làm mảnh: xương không còn răng cưa
                var frac = ink.Count(b => b) / (float)n;
                if (frac <= (veins != null ? MinInkVeins : MinInk) || frac >= MaxInk) return;
                var sk = Skeleton.Thin(ink, w, h);
                var len = Mathf.RoundToInt(k * 0.9f);
                Skeleton.PruneSpurs(sk, w, h, len);
                Skeleton.CloseGaps(sk, w, h, len);
                Skeleton.CloseGaps(sk, w, h, FarGap, ink); // chỗ nét dày giao nhau làm xương đứt xa hơn
                if (FixJ > 0) Skeleton.FixJunctions(sk, w, h, FixJ);
                var c = Score(sk, w, h);
                if (c.count < MinPiecesGood) return;
                c.ink = ink; c.skel = sk;
                lock (gate) { c.kernel = k; cands.Add(c); }
            });
            // có gân sáng: ưu tiên cửa sổ nhỏ nhất có điểm ≥ 85% điểm tốt nhất (cửa sổ lớn làm nét phình thành dải rộng, đường xương lệch khỏi gân mảnh); không có gân: điểm cao nhất (giữ cả nét rộng như viền mắt)
            if (cands.Count > 0)
            {
                var top = cands.Max(c => c.score);
                best = veins != null
                    ? cands.Where(c => c.score >= PreferSmall * top).OrderBy(c => c.kernel).ThenByDescending(c => c.score).First()
                    : cands.OrderByDescending(c => c.score).ThenBy(c => c.kernel).First();
            }
            if (best == null || best.coverage < MinCoverage) return null;
            if (outline != null)
            {
                // đường bao chủ thể hợp vào mặt nạ nét rồi làm mảnh lại: đường bao sát nét có sẵn nhập thành 1 đường (không đường kép), chỗ chưa có nét thì được lấp
                var union = ImageOps.Dilate3(outline, w, h);
                for (var i = 0; i < n; i++) union[i] |= best.ink[i];
                union = ImageOps.SmoothMask(union, w, h, MaskSmooth);
                var sk = Skeleton.Thin(union, w, h);
                var len = Mathf.RoundToInt(best.kernel * 0.9f);
                Skeleton.PruneSpurs(sk, w, h, len);
                Skeleton.CloseGaps(sk, w, h, len);
                Skeleton.CloseGaps(sk, w, h, FarGap, union);
                if (FixJ > 0) Skeleton.FixJunctions(sk, w, h, FixJ);
                best.skel = sk;
                best.ink = union;
            }
            return Faces(best.ink, best.skel, lab, w, h);
        }

        private const float BorderHomogeneous = 0.9f, BorderTolerance = 6f, SubjectDelta = 14f, MaxSubject = 0.8f;

        // Ảnh có nền đồng màu (viền ảnh giống nhau): đường bao 1 pixel của vùng khác màu nền; null nếu không có nền đồng màu
        private static bool[] SubjectOutline(Color32[] px, Vector3[] lab, int w, int h)
        {
            var border = new List<Vector3>();
            for (var x = 0; x < w; x++) { border.Add(lab[x]); border.Add(lab[(h - 1) * w + x]); }
            for (var y = 1; y < h - 1; y++) { border.Add(lab[y * w]); border.Add(lab[y * w + w - 1]); }
            float Med(int c) { var a = border.Select(v => v[c]).OrderBy(v => v).ToArray(); return a[a.Length / 2]; }
            var bg = new Vector3(Med(0), Med(1), Med(2));
            if (border.Count(v => (v - bg).magnitude < BorderTolerance) < BorderHomogeneous * border.Count) return null;
            var n = w * h;
            var subject = new bool[n];
            var cnt = 0;
            for (var i = 0; i < n; i++) if (subject[i] = (lab[i] - bg).magnitude > SubjectDelta) cnt++;
            if (cnt < 0.01f * n || cnt > MaxSubject * n) return null;
            var res = new bool[n];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (!subject[i]) continue;
                if (x == 0 || y == 0 || x == w - 1 || y == h - 1 || !subject[i - 1] || !subject[i + 1] || !subject[i - w] || !subject[i + w]) res[i] = true;
            }
            return res;
        }

        private static bool[] DarkPixels(Color32[] px, float[] notInk)
        {
            var gray = ImageOps.Gray(px);
            var d = new bool[px.Length];
            for (var i = 0; i < d.Length; i++) d[i] = gray[i] < KnotGray && notInk[i] < 0.3f;
            return d;
        }

        // 0..1: mức pixel là màu bão hoà (nét đen thật gần như không có sắc); làm mờ nhẹ cho khỏi lỗ chỗ
        private static float[] NotInk(Color32[] px, int w, int h)
        {
            var f = new float[px.Length];
            for (var i = 0; i < f.Length; i++)
            {
                int r = px[i].r, g = px[i].g, b = px[i].b;
                var sat = Mathf.Max(r, Mathf.Max(g, b)) - Mathf.Min(r, Mathf.Min(g, b));
                f[i] = Mathf.Clamp01((sat - InkSatFree) / InkSatSpan);
            }
            return ImageOps.Gauss(f, w, h, 1.2f);
        }

        // Xám làm mờ; pixel lạnh (kính xanh lục tối) được nâng sáng để chì nâu ấm mới nổi lên khi dò nét
        private static float[] Prepare(Color32[] px, int w, int h)
        {
            var g = ImageOps.Gauss(ImageOps.Gray(px), w, h, 0.9f);
            var lift = new float[px.Length];
            for (var i = 0; i < lift.Length; i++)
            {
                var warm = px[i].r - 0.5f * (px[i].g + px[i].b);
                lift[i] = Mathf.Clamp(-warm, 0f, 40f) * 0.6f;
            }
            lift = ImageOps.Gauss(lift, w, h, 0.9f);
            for (var i = 0; i < g.Length; i++) g[i] = Mathf.Clamp(g[i] + lift[i], 0f, 255f);
            return g;
        }

        // Nét mờ (> 0.45 thr) chỉ giữ nếu thành phần của nó có đoạn rõ (> thr)
        private static bool[] Hysteresis(float[] bh, int w, int h, float thr)
        {
            var low = new bool[bh.Length];
            for (var i = 0; i < low.Length; i++) low[i] = bh[i] > thr * 0.45f;
            var cc = ImageOps.Components(low, w, h, true, out var count);
            var mx = new float[count + 1];
            for (var i = 0; i < cc.Length; i++) if (cc[i] > 0 && bh[i] > mx[cc[i]]) mx[cc[i]] = bh[i];
            var ink = new bool[bh.Length];
            for (var i = 0; i < ink.Length; i++) ink[i] = cc[i] > 0 && mx[cc[i]] > thr;
            return ink;
        }

        private static Candidate Score(bool[] sk, int w, int h)
        {
            var free = sk.Select(b => !b).ToArray();
            var cc = ImageOps.Components(free, w, h, false, out var count);
            var area = new int[count + 1];
            foreach (var l in cc) if (l > 0) area[l]++;
            var total = (float)(w * h);
            var good = 0;
            long covered = 0;
            for (var i = 1; i <= count; i++)
                if (area[i] >= GoodMinArea && area[i] <= MaxFaceFraction * total) { good++; covered += area[i]; }
            var cov = covered / total;
            return new Candidate { count = good, coverage = cov, score = cov * Mathf.Min(good, 40) };
        }

        private const int ThickIterations = 3;

        // Nét dày (viền đen quanh chủ thể): đường cắt nằm giữa nét làm chủ thể mất nửa viền. Nửa nét phía nền trả về mảnh chủ thể kề, giữ nguyên hình dáng
        private static void ReturnThickInk(int[] faces, bool[] ink, int w, int h)
        {
            var n = w * h;
            var open = ink;
            for (var i = 0; i < ThickIterations; i++) open = ImageOps.Erode3(open, w, h);
            for (var i = 0; i < ThickIterations; i++) open = ImageOps.Dilate3(open, w, h);
            var any = false;
            for (var i = 0; i < n; i++) if (open[i]) { any = true; break; }
            if (!any) return;
            // nền = mặt có nhiều pixel chạm mép ảnh nhất
            var border = new Dictionary<int, int>();
            void Edge(int i) { var f = faces[i]; if (f > 0) border[f] = border.TryGetValue(f, out var c) ? c + 1 : 1; }
            for (var x = 0; x < w; x++) { Edge(x); Edge((h - 1) * w + x); }
            for (var y = 0; y < h; y++) { Edge(y * w); Edge(y * w + w - 1); }
            if (border.Count == 0) return;
            var bg = border.OrderByDescending(kv => kv.Value).First().Key;
            // BFS từ pixel không phải nền, chỉ đi qua pixel nét dày đang thuộc nền
            var q = new Queue<int>();
            for (var i = 0; i < n; i++)
                if (faces[i] != bg && faces[i] > 0) q.Enqueue(i);
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int x = p % w, y = p / w;
                for (var d = 0; d < 4; d++)
                {
                    int nx = x + (d == 0 ? -1 : d == 1 ? 1 : 0), ny = y + (d == 2 ? -1 : d == 3 ? 1 : 0);
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    var j = ny * w + nx;
                    if (faces[j] == bg && open[j]) { faces[j] = faces[p]; q.Enqueue(j); }
                }
            }
        }

        private const int ThreadRadius = 4;
        private const float ThreadKeep = 0.3f;

        // Mảnh rò qua khe hở nét thành sợi mảnh (rộng dưới 2×ThreadRadius+1 pixel): cắt sợi, pixel đó về mảnh kề; mảnh mỏng nguyên bản (còn dưới 30% sau khi mở) giữ nguyên
        private static void TrimThreads(int[] faces, int w, int h)
        {
            var n = w * h;
            var bx0 = new Dictionary<int, int>(); var by0 = new Dictionary<int, int>(); var bx1 = new Dictionary<int, int>(); var by1 = new Dictionary<int, int>();
            var area = new Dictionary<int, int>();
            for (var i = 0; i < n; i++)
            {
                var f = faces[i];
                if (f <= 0) continue;
                int x = i % w, y = i / w;
                if (!area.ContainsKey(f)) { area[f] = 0; bx0[f] = x; bx1[f] = x; by0[f] = y; by1[f] = y; }
                area[f]++;
                if (x < bx0[f]) bx0[f] = x; if (x > bx1[f]) bx1[f] = x; if (y < by0[f]) by0[f] = y; if (y > by1[f]) by1[f] = y;
            }
            var removed = new List<int>();
            foreach (var kv in area)
            {
                var f = kv.Key;
                if (kv.Value < 150) continue;
                int pad = ThreadRadius + 1, x0 = Mathf.Max(0, bx0[f] - pad), y0 = Mathf.Max(0, by0[f] - pad);
                int lw = Mathf.Min(w, bx1[f] + pad + 1) - x0, lh = Mathf.Min(h, by1[f] + pad + 1) - y0;
                var m = new bool[lw * lh];
                for (var y = 0; y < lh; y++) for (var x = 0; x < lw; x++) m[y * lw + x] = faces[(y0 + y) * w + x0 + x] == f;
                var open = m;
                for (var i = 0; i < ThreadRadius; i++) open = ImageOps.Erode3(open, lw, lh);
                for (var i = 0; i < ThreadRadius; i++) open = ImageOps.Dilate3(open, lw, lh);
                var keep = 0;
                for (var i = 0; i < m.Length; i++) if (open[i]) keep++;
                if (keep < ThreadKeep * kv.Value) continue;
                for (var y = 0; y < lh; y++)
                for (var x = 0; x < lw; x++)
                {
                    var j = y * lw + x;
                    if (m[j] && !open[j]) removed.Add((y0 + y) * w + x0 + x);
                }
            }
            if (removed.Count == 0) return;
            foreach (var i in removed) faces[i] = 0;
            ImageOps.FillNearest(faces, w, h);
        }

        private static RegionMap Faces(bool[] ink, bool[] sk, Vector3[] lab, int w, int h)
        {
            var free = sk.Select(b => !b).ToArray();
            var faces = ImageOps.Components(free, w, h, false, out _);
            RegionOps.MergeSmall(faces, w, h, MergeMinArea);
            ImageOps.FillNearest(faces, w, h); // pixel xương và vùng vụn bị bỏ về mặt gần nhất
            ReturnThickInk(faces, ink, w, h);
            TrimThreads(faces, w, h);
            var core = ImageOps.Erode3(ink.Select(b => !b).ToArray(), w, h);
            var count = RegionOps.Compact(faces);
            var cols = RegionOps.FaceColors(faces, count, lab, core);
            var map = new RegionMap { w = w, h = h, reg = faces, inked = true };
            for (var id = 1; id <= count; id++) map.colors.Add(cols[id]);
            return map;
        }
    }
}
