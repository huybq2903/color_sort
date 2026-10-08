using System.Collections.Generic;
using System.Linq;
using Falcon.InGame.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Ảnh không có nét: làm mịn, gom màu, tách vùng liên thông, gộp vùng nhỏ / chuyển sắc / cùng màu bảng.</summary>
    internal static class FlatRoute
    {
        private const int Clusters = 28, MergeMinArea = 80, KMeansIterations = 25;
        private const float BinSize = 4f, CentreSeparation = 7f, FlatCoverage = 0.65f, CoverageDelta = 5f, MinCentreFraction = 0.0008f;
        private const int MaxCentres = 64, ExtraCentres = 16;
        internal static float AreaScale = 1f, GradScale = 1f, MaxDeltaScale = 1f; // độ gộp vùng vụn / chuyển sắc
        internal static int KeepMinArea = 40; // vùng nổi bật nhỏ hơn cỡ này vẫn bị gộp
        private const float NoisyLevel = 1.2f;
        private const float ExtraDistance = 9f, ExtraFraction = 0.01f;

        public static RegionMap Build(Color32[] px, Vector3[] labPal, int w, int h)
        {
            var lab = LabImage.Oklab(px); // tách/gộp vùng trong Oklab; labPal (Lab) chỉ để chọn màu bảng
            var n = w * h;
            var valid = Enumerable.Repeat(true, n).ToArray();
            var sm = LabImage.Bilateral(lab, valid, w, h, 3, 14f); // 1 lượt: lượt thứ 2 làm nhoà chi tiết mảnh
            // ảnh có vân (canvas, giấy, nhiễu): khử nhiễu mạnh trước khi tìm màu và tách vùng
            var noise = 0.0;
            var noisy = false;
            for (var i = 0; i < n; i += 7) noise += (lab[i] - sm[i]).magnitude;
            noise /= (n + 6) / 7;
            noisy = noise > NoisyLevel;
            if (noisy)
            {
                lab = LabImage.Bilateral(LabImage.Bilateral(sm, valid, w, h, 4, 18f), valid, w, h, 4, 18f);
                sm = lab;
            }
            var flat = FlatColours(lab);
            var assign = flat ?? Cluster(sm);
            var veins = noisy ? null : ThinBright(px, w, h); // gân/nét sáng mảnh: ranh giới giữa các vùng (như nét chì), không thành vùng riêng
            if (veins != null) for (var i = 0; i < n; i++) if (veins[i]) assign[i] = -1;
            var lab4 = ImageOps.ComponentsByKey(assign, w, h, out _);
            RegionOps.MergeSmall(lab4, w, h, (int)(MergeMinArea * 4 * AreaScale), lab, keepMinArea: KeepMinArea, midArea: (int)(MergeMinArea * 12 * AreaScale)); // gộp mạnh vùng vụn, trừ nét mảnh và chi tiết nổi bật (xem StandOut)
            ImageOps.FillNearest(lab4, w, h);
            // tranh phẳng: chỉ gộp các dải có biên rất mượt (chuyển sắc), biên có bước đổi màu gắt là ranh giới thật; ảnh khác: ngưỡng tự chọn
            if (flat == null) RegionOps.MergeGradients(lab4, Blur(lab, w, h, 0.6f), w, h, 30f * MaxDeltaScale, protect: veins, thrScale: GradScale);
            else RegionOps.MergeGradients(lab4, lab, w, h, 11f, 1.8f, veins);
            var count = RegionOps.Compact(lab4);
            var cols = RegionOps.AssignColors(lab4, count, labPal, w, h, noisy ? 14f : 5f); // mảnh kề nhau khác tông thì khác màu bảng; không gộp theo màu bảng
            var map = new RegionMap { w = w, h = h, reg = lab4 };
            for (var id = 1; id <= count; id++) map.colors.Add(cols[id]);
            return map;
        }

        internal const int VeinWidth = 5; private const int VeinMinLength = 16;
        private const float VeinContrast = 22f, VeinMaxFraction = 0.06f;

        // Nét sáng mảnh (rộng dưới VeinWidth, dài từ VeinMinLength): top-hat theo độ sáng; null nếu ảnh quá nhiều nét (vân, hoạ tiết)
        internal static bool[] ThinBright(Color32[] px, int w, int h)
        {
            var n = w * h;
            var g = ImageOps.Gauss(ImageOps.Gray(px), w, h, 0.6f);
            var open = ImageOps.Open(g, w, h, VeinWidth);
            var th = new bool[n];
            for (var i = 0; i < n; i++) th[i] = g[i] - open[i] > VeinContrast;
            var cc = ImageOps.Components(th, w, h, true, out var count);
            var x0 = new int[count + 1]; var y0 = new int[count + 1]; var x1 = new int[count + 1]; var y1 = new int[count + 1];
            for (var i = 1; i <= count; i++) { x0[i] = y0[i] = int.MaxValue; x1[i] = y1[i] = -1; }
            for (var i = 0; i < n; i++)
            {
                var l = cc[i];
                if (l == 0) continue;
                int x = i % w, y = i / w;
                if (x < x0[l]) x0[l] = x; if (x > x1[l]) x1[l] = x; if (y < y0[l]) y0[l] = y; if (y > y1[l]) y1[l] = y;
            }
            var res = new bool[n];
            var any = 0;
            for (var i = 0; i < n; i++)
            {
                var l = cc[i];
                if (l == 0 || Mathf.Max(x1[l] - x0[l], y1[l] - y0[l]) + 1 < VeinMinLength) continue;
                res[i] = true; any++;
            }
            return any == 0 || any > VeinMaxFraction * n ? null : res;
        }

        private static int[] Cluster(Vector3[] sm)
        {
            KMeans.Run(sm, Clusters, 1, KMeansIterations, out var assign);
            return assign;
        }

        // Tranh phẳng: tìm các màu có thật bằng biểu đồ Lab (ô 4 đơn vị), mỗi pixel về màu gần nhất; null nếu ảnh không phải kiểu phẳng (chuyển sắc, ảnh chụp)
        private static int[] FlatColours(Vector3[] lab)
        {
            var bins = new Dictionary<(int, int, int), (int n, Vector3 sum)>();
            foreach (var c in lab)
            {
                var k = (Mathf.FloorToInt(c.x / BinSize), Mathf.FloorToInt(c.y / BinSize), Mathf.FloorToInt(c.z / BinSize));
                bins[k] = bins.TryGetValue(k, out var b) ? (b.n + 1, b.sum + c) : (1, c);
            }
            var minCount = Mathf.Max(30, Mathf.RoundToInt(MinCentreFraction * lab.Length));
            var centres = new List<Vector3>();
            foreach (var b in bins.Values.OrderByDescending(v => v.n))
            {
                if (b.n < minCount || centres.Count >= MaxCentres) break;
                var c = b.sum / b.n;
                if (centres.All(o => (o - c).magnitude > CentreSeparation)) centres.Add(c);
            }
            if (centres.Count < 2) return null;
            var assign = new int[lab.Length];
            var dist = new float[lab.Length];
            void Assign()
            {
                for (var i = 0; i < lab.Length; i++)
                {
                    var best = 0;
                    var bd = float.MaxValue;
                    for (var k = 0; k < centres.Count; k++)
                    {
                        var d = (lab[i] - centres[k]).sqrMagnitude;
                        if (d < bd) { bd = d; best = k; }
                    }
                    assign[i] = best;
                    dist[i] = Mathf.Sqrt(bd);
                }
            }
            Assign();
            // vùng nhỏ / chuyển sắc không đủ điểm để thành màu riêng: bổ sung màu bằng k-means trên các pixel còn xa mọi màu
            var far = new List<Vector3>();
            for (var i = 0; i < lab.Length; i++) if (dist[i] > ExtraDistance) far.Add(lab[i]);
            if (far.Count > ExtraFraction * lab.Length)
            {
                var step = Mathf.Max(1, far.Count / 20000);
                var sample = far.Where((_, i) => i % step == 0).ToArray();
                var extra = KMeans.Run(sample, Mathf.Min(ExtraCentres, sample.Length), 3, KMeansIterations, out _);
                foreach (var e in extra) if (centres.All(o => (o - e).magnitude > CentreSeparation * 0.7f)) centres.Add(e);
                Assign();
            }
            var covered = dist.Count(d => d < CoverageDelta);
            return covered >= FlatCoverage * lab.Length ? assign : null;
        }

        private static Vector3[] Blur(Vector3[] lab, int w, int h, float sigma)
        {
            var ch = new float[3][];
            for (var c = 0; c < 3; c++)
            {
                var a = new float[lab.Length];
                for (var i = 0; i < a.Length; i++) a[i] = lab[i][c];
                ch[c] = ImageOps.Gauss(a, w, h, sigma);
            }
            var res = new Vector3[lab.Length];
            for (var i = 0; i < res.Length; i++) res[i] = new Vector3(ch[0][i], ch[1][i], ch[2][i]);
            return res;
        }
    }
}
