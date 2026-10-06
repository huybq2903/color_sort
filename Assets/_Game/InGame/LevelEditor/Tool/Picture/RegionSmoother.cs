using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Phóng bản đồ vùng ×scale và làm mượt biên: blur từng mask, pixel lấy vùng mạnh nhất.</summary>
    public static class RegionSmoother
    {
        private const int Margin = 3, ParallelPixels = 40000;

        public static RegionMap Upscale(RegionMap m, int scale)
        {
            if (scale <= 1) return m;
            int W = m.w * scale, H = m.h * scale;
            var best = new float[W * H];
            var outReg = new int[W * H];
            var kernel = Gaussian(scale);
            var bounds = m.Bounds();

            // ngoài tranh (id 0) cũng tranh chấp như 1 vùng → viền giáp nền trong suốt cũng mượt
            var outside = new float[W * H];
            var weight = ThinGapWeights(m); // tranh có khe: nét mảnh (1-2 pixel) có trọng số cao hơn để không bị các mảnh lấn mất khi làm mượt
            for (var Y = 0; Y < H; Y++)
            for (var X = 0; X < W; X++)
            {
                var src = Y / scale * m.w + X / scale;
                outside[Y * W + X] = m.reg[src] == 0 ? weight?[src] ?? 1f : 0f;
            }
            best = Blur(outside, W, H, kernel);

            for (var id = 1; id <= m.Count; id++)
            {
                var bb = bounds[id];
                if (bb.width == 0) continue;
                int x0 = Math.Max(0, bb.xMin - Margin), y0 = Math.Max(0, bb.yMin - Margin);
                int x1 = Math.Min(m.w, bb.xMax + Margin), y1 = Math.Min(m.h, bb.yMax + Margin);
                int cw = (x1 - x0) * scale, ch = (y1 - y0) * scale;
                var buf = new float[cw * ch];
                for (var y = 0; y < ch; y++)
                for (var x = 0; x < cw; x++)
                    buf[y * cw + x] = m.reg[(y0 + y / scale) * m.w + x0 + x / scale] == id ? 1f : 0f;
                buf = Blur(buf, cw, ch, kernel);
                for (var y = 0; y < ch; y++)
                for (var x = 0; x < cw; x++)
                {
                    var v = buf[y * cw + x];
                    var gi = (y0 * scale + y) * W + x0 * scale + x;
                    if (v > best[gi]) { best[gi] = v; outReg[gi] = id; }
                }
            }

            return new RegionMap { w = W, h = H, reg = outReg, colors = new List<int>(m.colors), inked = m.inked, gaps = m.gaps, synthetic = m.synthetic };
        }

        private const float ThinInk = 1.5f, ThinWeight = 1.8f;

        private static float[] ThinGapWeights(RegionMap m)
        {
            if (!m.gaps) return null;
            var d = ImageOps.Edt(System.Array.ConvertAll(m.reg, v => v == 0), m.w, m.h);
            var w = new float[d.Length];
            for (var y = 0; y < m.h; y++)
            for (var x = 0; x < m.w; x++)
            {
                if (d[y * m.w + x] <= 0f) continue;
                var peak = 0f; // bề rộng nét = khoảng cách lớn nhất tới mép trong vùng lân cận (mép của nét dày vẫn thấy tâm nét)
                for (var dy = -2; dy <= 2; dy++)
                for (var dx = -2; dx <= 2; dx++)
                {
                    int xx = x + dx, yy = y + dy;
                    if (xx >= 0 && yy >= 0 && xx < m.w && yy < m.h) peak = Mathf.Max(peak, d[yy * m.w + xx]);
                }
                w[y * m.w + x] = peak <= ThinInk ? ThinWeight : 1f;
            }
            for (var i = 0; i < w.Length; i++) if (w[i] == 0f) w[i] = 1f;
            return w;
        }

        private static float[] Gaussian(float sigma)
        {
            var r = Mathf.CeilToInt(sigma * 3f);
            var k = new float[r * 2 + 1];
            var sum = 0f;
            for (var i = -r; i <= r; i++) sum += k[i + r] = Mathf.Exp(-i * i / (2f * sigma * sigma));
            for (var i = 0; i < k.Length; i++) k[i] /= sum;
            return k;
        }

        // Blur tách 2 chiều, ngoài biên coi như 0
        private static float[] Blur(float[] src, int w, int h, float[] k)
        {
            var r = k.Length / 2;
            var tmp = new float[src.Length];
            var dst = new float[src.Length];
            void Horizontal(int y)
            {
                for (var x = 0; x < w; x++)
                {
                    var s = 0f;
                    for (var i = -r; i <= r; i++)
                    {
                        var xx = x + i;
                        if (xx >= 0 && xx < w) s += src[y * w + xx] * k[i + r];
                    }
                    tmp[y * w + x] = s;
                }
            }
            void Vertical(int y)
            {
                for (var x = 0; x < w; x++)
                {
                    var s = 0f;
                    for (var i = -r; i <= r; i++)
                    {
                        var yy = y + i;
                        if (yy >= 0 && yy < h) s += tmp[yy * w + x] * k[i + r];
                    }
                    dst[y * w + x] = s;
                }
            }
            if ((long)w * h >= ParallelPixels)
            {
                Parallel.For(0, h, Horizontal);
                Parallel.For(0, h, Vertical);
            }
            else
            {
                for (var y = 0; y < h; y++) Horizontal(y);
                for (var y = 0; y < h; y++) Vertical(y);
            }
            return dst;
        }
    }
}
