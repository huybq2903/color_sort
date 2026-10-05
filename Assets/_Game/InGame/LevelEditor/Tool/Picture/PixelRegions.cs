using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.InGame.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Id vùng mỗi pixel (0 = ngoài tranh) + palette id mỗi vùng (colors[0] bỏ trống).</summary>
    public class RegionMap
    {
        public int w, h;
        public int[] reg;
        public bool inked; // tách theo ô giữa các nét: đường cắt đã ở tâm nét
        public List<int> colors = new() { -1 };
        public int Count => colors.Count - 1;

        public RectInt[] Bounds()
        {
            var b = new RectInt[colors.Count];
            var min = new Vector2Int[colors.Count];
            var max = new Vector2Int[colors.Count];
            for (var i = 0; i < b.Length; i++) { min[i] = new Vector2Int(int.MaxValue, int.MaxValue); max[i] = new Vector2Int(-1, -1); }
            for (var p = 0; p < reg.Length; p++)
            {
                var id = reg[p];
                if (id <= 0) continue;
                int x = p % w, y = p / w;
                min[id] = Vector2Int.Min(min[id], new Vector2Int(x, y));
                max[id] = Vector2Int.Max(max[id], new Vector2Int(x, y));
            }
            for (var i = 1; i < b.Length; i++)
                if (max[i].x >= 0) b[i] = new RectInt(min[i].x, min[i].y, max[i].x - min[i].x + 1, max[i].y - min[i].y + 1);
            return b;
        }
    }

    /// <summary>Tiện ích trên RegionMap cho công cụ sửa tay: phát hiện lỗ trong vùng.</summary>
    public static class PixelRegions
    {
        internal static bool HasHole(RegionMap m, List<int> pixels) => TryFindHole(m, pixels, out _, out _, out _);

        // Tách mảnh rời thành vùng riêng và dồn id liên tục 1..Count
        internal static void Relabel(RegionMap m)
        {
            var old = m.reg;
            var reg = new int[old.Length];
            var colors = new List<int> { -1 };
            for (var i = 0; i < old.Length; i++)
            {
                if (old[i] == 0 || reg[i] != 0) continue;
                var oid = old[i];
                var id = colors.Count;
                colors.Add(m.colors[oid]);
                Flood(i, m.w, m.h, reg, id, q => old[q] == oid);
            }
            m.reg = reg;
            m.colors = colors;
        }

        private static bool TryFindHole(RegionMap m, List<int> pixels, out int cx, out int cy, out bool horizontal)
        {
            cx = cy = 0;
            horizontal = false;
            if (pixels.Count == 0) return false;

            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            foreach (var p in pixels)
            {
                int x = p % m.w, y = p / m.w;
                x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y);
            }

            // lưới bbox + viền 1 ô; flood từ góc qua ô không thuộc vùng = "ra được ngoài"
            int gw = x1 - x0 + 3, gh = y1 - y0 + 3;
            var inRegion = new bool[gw * gh];
            foreach (var p in pixels) inRegion[(p / m.w - y0 + 1) * gw + p % m.w - x0 + 1] = true;
            var outside = new int[gw * gh];
            Flood(0, gw, gh, outside, 1, q => !inRegion[q]);

            var holes = new int[gw * gh];
            int holeId = 0, bestId = 0, bestSize = 0;
            for (var q = 0; q < holes.Length; q++)
            {
                if (inRegion[q] || outside[q] != 0 || holes[q] != 0) continue;
                holeId++;
                var size = Flood(q, gw, gh, holes, holeId, t => !inRegion[t] && outside[t] == 0);
                if (size > bestSize) { bestSize = size; bestId = holeId; }
            }
            if (bestId == 0) return false;

            long sx = 0, sy = 0;
            for (var q = 0; q < holes.Length; q++)
                if (holes[q] == bestId) { sx += q % gw; sy += q / gw; }
            cx = (int)Math.Round((double)sx / bestSize) + x0 - 1;
            cy = (int)Math.Round((double)sy / bestSize) + y0 - 1;
            horizontal = y1 - y0 >= x1 - x0;
            return true;
        }

        // Flood 4-lân cận từ seed qua ô same(q) && ids[q] == 0, ghi ids = id; trả số ô
        private static int Flood(int seed, int w, int h, int[] ids, int id, Func<int, bool> same)
        {
            var stack = new Stack<int>();
            stack.Push(seed);
            ids[seed] = id;
            var count = 1;
            while (stack.Count > 0)
            {
                var p = stack.Pop();
                foreach (var q in Neighbors(p, w, h))
                {
                    if (ids[q] != 0 || !same(q)) continue;
                    ids[q] = id;
                    count++;
                    stack.Push(q);
                }
            }
            return count;
        }

        private static IEnumerable<int> Neighbors(int p, int w, int h)
        {
            int x = p % w, y = p / w;
            if (x > 0) yield return p - 1;
            if (x < w - 1) yield return p + 1;
            if (y > 0) yield return p - w;
            if (y < h - 1) yield return p + w;
        }
    }
}
