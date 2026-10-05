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
            for (var Y = 0; Y < H; Y++)
            for (var X = 0; X < W; X++)
                outside[Y * W + X] = m.reg[Y / scale * m.w + X / scale] == 0 ? 1f : 0f;
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

            return new RegionMap { w = W, h = H, reg = outReg, colors = new List<int>(m.colors), inked = m.inked, gaps = m.gaps };
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
