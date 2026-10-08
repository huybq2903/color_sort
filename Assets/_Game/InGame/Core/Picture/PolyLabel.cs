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
            var precision = size * 0.02f; // vị trí số không cần chính xác hơn 2% cỡ mảnh
            var o = ToArray(outer);
            var hs = new List<Vector2[]>();
            if (holes != null) foreach (var hole in holes) hs.Add(ToArray(hole));
            var queue = new List<Cell>();
            for (var x = minX; x < maxX; x += size)
            for (var y = minY; y < maxY; y += size) queue.Add(Make(x + half, y + half, half, o, hs));
            var best = Make(minX + w * 0.5f, minY + h * 0.5f, 0f, o, hs);
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
                queue.Add(Make(c.x - hh, c.y - hh, hh, o, hs));
                queue.Add(Make(c.x + hh, c.y - hh, hh, o, hs));
                queue.Add(Make(c.x - hh, c.y + hh, hh, o, hs));
                queue.Add(Make(c.x + hh, c.y + hh, hh, o, hs));
            }
            radius = Mathf.Max(0f, best.d);
            return new Vector2(best.x, best.y);
        }

        private static Vector2[] ToArray(IReadOnlyList<Vector2> l)
        {
            var a = new Vector2[l.Count];
            for (var i = 0; i < a.Length; i++) a[i] = l[i];
            return a;
        }

        private static Cell Make(float x, float y, float h, Vector2[] outer, List<Vector2[]> holes)
        {
            var d = SignedDist(x, y, outer, holes);
            return new Cell { x = x, y = y, h = h, d = d, max = d + h * 1.4142135f };
        }

        // Khoảng cách tới biên gần nhất: dương trong đa giác, âm ngoài
        private static float SignedDist(float x, float y, Vector2[] outer, List<Vector2[]> holes)
        {
            var inside = false;
            var min = float.MaxValue;
            Ring(outer, x, y, ref inside, ref min);
            foreach (var hole in holes) Ring(hole, x, y, ref inside, ref min);
            var dist = Mathf.Sqrt(min);
            return inside ? dist : -dist;
        }

        private static void Ring(Vector2[] r, float x, float y, ref bool inside, ref float min)
        {
            for (int i = 0, j = r.Length - 1; i < r.Length; j = i++)
            {
                Vector2 a = r[i], b = r[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                var sq = SegSq(x, y, a, b);
                if (sq < min) min = sq;
            }
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
