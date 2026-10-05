using System;
using System.Collections.Generic;
using Falcon.InGame.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Polygon → bản đồ pixel (scanline even-odd theo tâm pixel), lấp khe mảnh do giảm đỉnh.</summary>
    public static class RegionRaster
    {
        public static RegionMap Rasterize(PictureProperty p)
        {
            int w = p.width, h = p.height;
            var m = new RegionMap { w = w, h = h, reg = new int[w * h] };
            var xs = new List<float>();
            for (var i = 0; i < p.regions.Count; i++)
            {
                var unit = Mathf.Max(1, p.unit);
                var rings = new List<float[]> { Scaled(p.regions[i].points, unit) };
                if (p.regions[i].holes != null)
                    foreach (var hole in p.regions[i].holes) rings.Add(Scaled(hole, unit));
                m.colors.Add(p.regions[i].colorId);
                var y0 = h;
                var y1 = 0;
                foreach (var pts in rings)
                    for (var k = 1; k < pts.Length; k += 2) { y0 = Math.Min(y0, Mathf.FloorToInt(pts[k])); y1 = Math.Max(y1, Mathf.CeilToInt(pts[k])); }
                y0 = Math.Max(0, y0);
                y1 = Math.Min(h, y1);
                for (var y = y0; y < y1; y++)
                {
                    var yc = y + 0.5f;
                    xs.Clear();
                    foreach (var pts in rings)
                    {
                        var n = pts.Length / 2;
                        for (int a = 0, b = n - 1; a < n; b = a++)
                        {
                            float ya = pts[a * 2 + 1], yb = pts[b * 2 + 1];
                            if (ya > yc == yb > yc) continue;
                            float xa = pts[a * 2], xb = pts[b * 2];
                            xs.Add(xa + (yc - ya) * (xb - xa) / (yb - ya));
                        }
                    }
                    xs.Sort();
                    for (var k = 0; k + 1 < xs.Count; k += 2)
                    {
                        var from = Math.Max(0, Mathf.CeilToInt(xs[k] - 0.5f));
                        var to = Math.Min(w, Mathf.CeilToInt(xs[k + 1] - 0.5f));
                        for (var x = from; x < to; x++)
                            if (m.reg[y * w + x] == 0) m.reg[y * w + x] = i + 1;
                    }
                }
            }
            FillGaps(m);
            return m;
        }

        private static float[] Scaled(int[] raw, int unit)
        {
            var pts = new float[raw.Length];
            for (var k = 0; k < raw.Length; k++) pts[k] = raw[k] / (float)unit;
            return pts;
        }

        // Pixel trống có ≥ 2 lân cận thuộc mảnh → nhận mảnh đầu tiên; mép vùng trong suốt chỉ có 1 lân cận nên giữ trống
        private static void FillGaps(RegionMap m)
        {
            var fill = new List<(int idx, int id)>();
            for (var y = 0; y < m.h; y++)
            for (var x = 0; x < m.w; x++)
            {
                var i = y * m.w + x;
                if (m.reg[i] != 0) continue;
                int cnt = 0, id = 0;
                void Check(int q)
                {
                    if (m.reg[q] <= 0) return;
                    cnt++;
                    if (id == 0) id = m.reg[q];
                }
                if (x > 0) Check(i - 1);
                if (x < m.w - 1) Check(i + 1);
                if (y > 0) Check(i - m.w);
                if (y < m.h - 1) Check(i + m.w);
                if (cnt >= 2) fill.Add((i, id));
            }
            foreach (var (idx, id) in fill) m.reg[idx] = id;
        }
    }
}
