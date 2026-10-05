using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.Core
{
    /// <summary>Ear clipping O(n²); polygon có lỗ được nối lỗ vào viền ngoài bằng cầu rồi cắt tai.</summary>
    public static class PolygonTriangulator
    {
        // Trả về tam giác theo chỉ số của danh sách điểm gộp (outer rồi từng lỗ, nối tiếp nhau); verts = danh sách điểm đó
        public static int[] Triangulate(IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes, out List<Vector2> verts)
        {
            verts = new List<Vector2>(outer);
            if (holes == null || holes.Count == 0) return Triangulate(verts);

            // vòng ngoài CCW, lỗ CW; xử lý lỗ có x lớn nhất trước, mỗi lỗ nối từ đỉnh phải nhất tới đỉnh ngoài nhìn thấy được
            var ring = new List<Vector2>(outer);
            if (SignedArea(ring) < 0f) ring.Reverse();
            var hs = new List<List<Vector2>>();
            foreach (var h in holes)
            {
                var l = new List<Vector2>(h);
                if (l.Count < 3) continue;
                if (SignedArea(l) > 0f) l.Reverse();
                hs.Add(l);
            }
            hs.Sort((a, b) => MaxX(b).CompareTo(MaxX(a)));
            foreach (var h in hs)
            {
                var hi = 0;
                for (var i = 1; i < h.Count; i++) if (h[i].x > h[hi].x) hi = i;
                var bridge = FindBridge(ring, h[hi], hs, h);
                var merged = new List<Vector2>(ring.Count + h.Count + 2);
                for (var i = 0; i <= bridge; i++) merged.Add(ring[i]);
                for (var k = 0; k <= h.Count; k++) merged.Add(h[(hi + k) % h.Count]);
                merged.Add(ring[bridge]);
                for (var i = bridge + 1; i < ring.Count; i++) merged.Add(ring[i]);
                ring = merged;
            }
            verts = ring;
            var tris = Triangulate(ring);
            return tris;
        }

        private static float MaxX(List<Vector2> l)
        {
            var m = float.MinValue;
            foreach (var v in l) m = Mathf.Max(m, v.x);
            return m;
        }

        // Đỉnh vòng gần nhất mà đoạn nối không cắt cạnh nào của vòng hay của các lỗ khác
        private static int FindBridge(List<Vector2> ring, Vector2 from, List<List<Vector2>> holes, List<Vector2> self)
        {
            var order = new List<int>();
            for (var i = 0; i < ring.Count; i++) order.Add(i);
            order.Sort((a, b) => (ring[a] - from).sqrMagnitude.CompareTo((ring[b] - from).sqrMagnitude));
            foreach (var i in order)
                if (SegmentClear(ring, from, ring[i], i) && HolesClear(holes, self, from, ring[i]) && !CrossesOwn(self, from, ring[i])) return i;
            return order[0];
        }

        // Cầu nối không được xuyên qua bên trong chính lỗ đang nối (cắt cạnh của lỗ đó)
        private static bool CrossesOwn(List<Vector2> hole, Vector2 a, Vector2 b)
        {
            for (var i = 0; i < hole.Count; i++)
                if (Cross2(a, b, hole[i], hole[(i + 1) % hole.Count])) return true;
            return false;
        }

        private static bool SegmentClear(List<Vector2> ring, Vector2 a, Vector2 b, int skip)
        {
            for (var i = 0; i < ring.Count; i++)
            {
                var j = (i + 1) % ring.Count;
                if (i == skip || j == skip) continue;
                if (Cross2(a, b, ring[i], ring[j])) return false;
            }
            return true;
        }

        private static bool HolesClear(List<List<Vector2>> holes, List<Vector2> self, Vector2 a, Vector2 b)
        {
            foreach (var h in holes)
            {
                if (ReferenceEquals(h, self)) continue;
                for (var i = 0; i < h.Count; i++)
                    if (Cross2(a, b, h[i], h[(i + 1) % h.Count])) return false;
            }
            return true;
        }

        // Hai đoạn ab và cd cắt nhau thật sự (không chỉ chạm đầu mút)
        private static bool Cross2(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            if (a == c || a == d || b == c || b == d) return false;
            var d1 = Cross(a, b, c);
            var d2 = Cross(a, b, d);
            var d3 = Cross(c, d, a);
            var d4 = Cross(c, d, b);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        public static int[] Triangulate(IReadOnlyList<Vector2> original)
        {
            // chuẩn hoá về cỡ ~1000 đơn vị: ngưỡng thẳng hàng cố định không còn phụ thuộc cỡ toạ độ (toạ độ thế giới rất nhỏ)
            var n = original.Count;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var v in original) { minX = Mathf.Min(minX, v.x); minY = Mathf.Min(minY, v.y); maxX = Mathf.Max(maxX, v.x); maxY = Mathf.Max(maxY, v.y); }
            var ext = Mathf.Max(maxX - minX, maxY - minY);
            var k = ext > 1e-9f ? 1000f / ext : 1f;
            var pts = new Vector2[n];
            for (var i = 0; i < n; i++) pts[i] = (original[i] - new Vector2(minX, minY)) * k;
            var tris = new List<int>();
            if (n < 3) return tris.ToArray();

            var ccw = SignedArea(pts) >= 0f;
            var idx = new List<int>(n);
            for (var i = 0; i < n; i++) idx.Add(ccw ? i : n - 1 - i);

            var guard = n * n;
            while (idx.Count > 3 && guard-- > 0)
            {
                var clipped = false;
                for (var i = 0; i < idx.Count; i++)
                {
                    int a = idx[(i + idx.Count - 1) % idx.Count], b = idx[i], c = idx[(i + 1) % idx.Count];
                    var cross = Cross(pts[a], pts[b], pts[c]);
                    if (Mathf.Abs(cross) < 1e-6f)
                    {
                        idx.RemoveAt(i); // đỉnh thẳng hàng: bỏ, không sinh tam giác
                        clipped = true;
                        break;
                    }
                    if (cross < 0f || AnyInside(pts, idx, a, b, c)) continue;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped)
                {
                    // kẹt (điểm trùng/gần trùng ở cầu nối lỗ): cắt đỉnh lồi vi phạm ít nhất để đi tiếp, không nối quạt cả phần còn lại
                    var best = -1;
                    var bestScore = int.MaxValue;
                    for (var i = 0; i < idx.Count; i++)
                    {
                        int a = idx[(i + idx.Count - 1) % idx.Count], b = idx[i], c = idx[(i + 1) % idx.Count];
                        if (Cross(pts[a], pts[b], pts[c]) < 0f) continue;
                        var score = CountInside(pts, idx, a, b, c);
                        if (score < bestScore) { bestScore = score; best = i; }
                    }
                    if (best < 0) break;
                    int ea = idx[(best + idx.Count - 1) % idx.Count], eb = idx[best], ec = idx[(best + 1) % idx.Count];
                    tris.Add(ea); tris.Add(eb); tris.Add(ec);
                    idx.RemoveAt(best);
                }
            }

            if (idx.Count > 3) Debug.LogWarning($"PolygonTriangulator: kẹt ở {idx.Count} đỉnh, fallback fan");
            for (var i = 1; i + 1 < idx.Count; i++)
            {
                tris.Add(idx[0]); tris.Add(idx[i]); tris.Add(idx[i + 1]);
            }
            return tris.ToArray();
        }

        public static float SignedArea(IReadOnlyList<Vector2> p)
        {
            var s = 0f;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) s += p[j].x * p[i].y - p[i].x * p[j].y;
            return s * 0.5f;
        }

        private static int CountInside(IReadOnlyList<Vector2> pts, List<int> idx, int a, int b, int c)
        {
            var n = 0;
            foreach (var p in idx)
            {
                if (p == a || p == b || p == c) continue;
                var v = pts[p];
                if (v == pts[a] || v == pts[b] || v == pts[c]) continue;
                if (Cross(pts[a], pts[b], v) >= 0f && Cross(pts[b], pts[c], v) >= 0f && Cross(pts[c], pts[a], v) >= 0f) n++;
            }
            return n;
        }

        private static bool AnyInside(IReadOnlyList<Vector2> pts, List<int> idx, int a, int b, int c)
        {
            foreach (var p in idx)
            {
                if (p == a || p == b || p == c) continue;
                var v = pts[p];
                if (v == pts[a] || v == pts[b] || v == pts[c]) continue;
                if (Cross(pts[a], pts[b], v) >= 0f && Cross(pts[b], pts[c], v) >= 0f && Cross(pts[c], pts[a], v) >= 0f)
                    return true;
            }
            return false;
        }

        private static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }
}
