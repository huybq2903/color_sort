using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.Core
{
    /// <summary>Tam giác hoá polygon có lỗ bằng earcut; tam giác luôn ngược chiều kim đồng hồ.</summary>
    public static class PolygonTriangulator
    {
        // Trả về tam giác theo chỉ số của danh sách điểm gộp (outer rồi từng lỗ, nối tiếp nhau); verts = danh sách điểm đó
        public static int[] Triangulate(IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes, out List<Vector2> verts)
        {
            verts = new List<Vector2>(outer);
            var starts = new List<int>();
            if (holes != null)
                foreach (var h in holes)
                {
                    if (h.Count < 3) continue;
                    starts.Add(verts.Count);
                    verts.AddRange(h);
                }
            return CounterClockwise(Earcut.Triangulate(verts, starts), verts);
        }

        public static int[] Triangulate(IReadOnlyList<Vector2> original) => CounterClockwise(Earcut.Triangulate(original, null), original);

        public static float SignedArea(IReadOnlyList<Vector2> p)
        {
            var s = 0f;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) s += p[j].x * p[i].y - p[i].x * p[j].y;
            return s * 0.5f;
        }

        private static int[] CounterClockwise(int[] tris, IReadOnlyList<Vector2> v)
        {
            for (var i = 0; i + 2 < tris.Length; i += 3)
            {
                Vector2 a = v[tris[i]], b = v[tris[i + 1]], c = v[tris[i + 2]];
                if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) < 0f) (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
            }
            return tris;
        }
    }
}
