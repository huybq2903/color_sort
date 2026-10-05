using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.Core
{
    /// <summary>Điểm trong đa giác (trừ lỗ) xa biên nhất — tâm đường tròn nội tiếp lớn nhất (polylabel).</summary>
    public static class PolyLabel
    {
        private struct Cell
        {
            public float x, y, h, d, max;
        }

        public static Vector2 Find(IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes, out float radius)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in outer)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            float w = maxX - minX, h = maxY - minY, size = Mathf.Min(w, h), half = size * 0.5f;
            radius = 0f;
            if (size <= 0f) return new Vector2(minX, minY);
            var precision = size * 0.01f;
            var queue = new List<Cell>();
            for (var x = minX; x < maxX; x += size)
            for (var y = minY; y < maxY; y += size) queue.Add(Make(x + half, y + half, half, outer, holes));
            var best = Make(minX + w * 0.5f, minY + h * 0.5f, 0f, outer, holes);
            var guard = 0;
            while (queue.Count > 0 && guard++ < 20000)
            {
                var bi = 0;
                for (var i = 1; i < queue.Count; i++) if (queue[i].max > queue[bi].max) bi = i;
                var c = queue[bi];
                queue[bi] = queue[^1];
                queue.RemoveAt(queue.Count - 1);
                if (c.d > best.d) best = c;
                if (c.max - best.d <= precision) continue;
                var hh = c.h * 0.5f;
                queue.Add(Make(c.x - hh, c.y - hh, hh, outer, holes));
                queue.Add(Make(c.x + hh, c.y - hh, hh, outer, holes));
                queue.Add(Make(c.x - hh, c.y + hh, hh, outer, holes));
                queue.Add(Make(c.x + hh, c.y + hh, hh, outer, holes));
            }
            radius = Mathf.Max(0f, best.d);
            return new Vector2(best.x, best.y);
        }

        private static Cell Make(float x, float y, float h, IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes)
        {
            var d = SignedDist(x, y, outer, holes);
            return new Cell { x = x, y = y, h = h, d = d, max = d + h * 1.4142135f };
        }

        // Khoảng cách tới biên gần nhất: dương trong đa giác, âm ngoài
        private static float SignedDist(float x, float y, IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes)
        {
            var inside = false;
            var min = float.MaxValue;
            void Ring(IReadOnlyList<Vector2> r)
            {
                for (int i = 0, j = r.Count - 1; i < r.Count; j = i++)
                {
                    Vector2 a = r[i], b = r[j];
                    if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                    min = Mathf.Min(min, SegSq(x, y, a, b));
                }
            }
            Ring(outer);
            if (holes != null) foreach (var hole in holes) Ring(hole);
            var dist = Mathf.Sqrt(min);
            return inside ? dist : -dist;
        }

        private static float SegSq(float px, float py, Vector2 a, Vector2 b)
        {
            float dx = b.x - a.x, dy = b.y - a.y, x = a.x, y = a.y;
            if (dx != 0f || dy != 0f)
            {
                var t = ((px - x) * dx + (py - y) * dy) / (dx * dx + dy * dy);
                if (t > 1f) { x = b.x; y = b.y; }
                else if (t > 0f) { x += dx * t; y += dy * t; }
            }
            dx = px - x; dy = py - y;
            return dx * dx + dy * dy;
        }
    }
}
