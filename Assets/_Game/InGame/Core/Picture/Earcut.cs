using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.Core
{
    /// <summary>Earcut (Mapbox, ISC): tam giác hoá đa giác có lỗ, đỉnh trùng, cạnh thẳng hàng; không dùng đường cong z-order nên O(n²).</summary>
    internal static class Earcut
    {
        private sealed class Node
        {
            public readonly int i;
            public readonly double x, y;
            public Node prev, next;
            public bool steiner;

            public Node(int i, double x, double y) { this.i = i; this.x = x; this.y = y; }
        }

        // verts: vòng ngoài rồi các lỗ nối tiếp; holeStarts: chỉ số đỉnh bắt đầu của mỗi lỗ; trả về chỉ số đỉnh của từng tam giác
        public static int[] Triangulate(IReadOnlyList<Vector2> verts, IReadOnlyList<int> holeStarts)
        {
            var tris = new List<int>();
            var hasHoles = holeStarts != null && holeStarts.Count > 0;
            var outerLen = hasHoles ? holeStarts[0] : verts.Count;
            var outer = Link(verts, 0, outerLen, true);
            if (outer == null || outer.next == outer.prev) return tris.ToArray();
            if (hasHoles) outer = EliminateHoles(verts, holeStarts, outer);
            EarcutLinked(outer, tris, 0);
            return tris.ToArray();
        }

        private static void EarcutLinked(Node ear, List<int> tris, int pass)
        {
            if (ear == null) return;
            var stop = ear;
            while (ear.prev != ear.next)
            {
                var prev = ear.prev;
                var next = ear.next;
                if (IsEar(ear))
                {
                    tris.Add(prev.i); tris.Add(ear.i); tris.Add(next.i);
                    RemoveNode(ear);
                    ear = next.next;
                    stop = next.next;
                    continue;
                }
                ear = next;
                if (ear == stop)
                {
                    if (pass == 0) EarcutLinked(FilterPoints(ear, null), tris, 1);
                    else if (pass == 1)
                    {
                        ear = CureLocalIntersections(FilterPoints(ear, null), tris);
                        EarcutLinked(ear, tris, 2);
                    }
                    else if (pass == 2) SplitEarcut(ear, tris);
                    break;
                }
            }
        }

        private static bool IsEar(Node ear)
        {
            Node a = ear.prev, b = ear, c = ear.next;
            if (Area(a, b, c) >= 0) return false; // đỉnh lõm
            double ax = a.x, bx = b.x, cx = c.x, ay = a.y, by = b.y, cy = c.y;
            var x0 = System.Math.Min(ax, System.Math.Min(bx, cx));
            var y0 = System.Math.Min(ay, System.Math.Min(by, cy));
            var x1 = System.Math.Max(ax, System.Math.Max(bx, cx));
            var y1 = System.Math.Max(ay, System.Math.Max(by, cy));
            var p = c.next;
            while (p != a)
            {
                if (p.x >= x0 && p.x <= x1 && p.y >= y0 && p.y <= y1 && PointInTriangleExceptFirst(ax, ay, bx, by, cx, cy, p.x, p.y) && Area(p.prev, p, p.next) >= 0) return false;
                p = p.next;
            }
            return true;
        }

        // Cắt các giao cắt cục bộ nhỏ (hai cạnh gần nhau cắt nhau)
        private static Node CureLocalIntersections(Node start, List<int> tris)
        {
            var p = start;
            do
            {
                Node a = p.prev, b = p.next.next;
                if (!Equals(a, b) && Intersects(a, p, p.next, b) && LocallyInside(a, b) && LocallyInside(b, a))
                {
                    tris.Add(a.i); tris.Add(p.i); tris.Add(b.i);
                    RemoveNode(p);
                    RemoveNode(p.next);
                    p = start = b;
                }
                p = p.next;
            } while (p != start);
            return FilterPoints(p, null);
        }

        // Kẹt: tìm đường chéo hợp lệ, tách đa giác làm đôi rồi tam giác hoá từng nửa
        private static void SplitEarcut(Node start, List<int> tris)
        {
            var a = start;
            do
            {
                var b = a.next.next;
                while (b != a.prev)
                {
                    if (a.i != b.i && IsValidDiagonal(a, b))
                    {
                        var c = SplitPolygon(a, b);
                        a = FilterPoints(a, a.next);
                        c = FilterPoints(c, c.next);
                        EarcutLinked(a, tris, 0);
                        EarcutLinked(c, tris, 0);
                        return;
                    }
                    b = b.next;
                }
                a = a.next;
            } while (a != start);
        }

        private static Node EliminateHoles(IReadOnlyList<Vector2> verts, IReadOnlyList<int> holeStarts, Node outer)
        {
            var queue = new List<Node>();
            for (var i = 0; i < holeStarts.Count; i++)
            {
                var start = holeStarts[i];
                var end = i < holeStarts.Count - 1 ? holeStarts[i + 1] : verts.Count;
                var list = Link(verts, start, end, false);
                if (list == null) continue;
                if (list == list.next) list.steiner = true;
                queue.Add(Leftmost(list));
            }
            queue.Sort((a, b) => a.x.CompareTo(b.x));
            foreach (var hole in queue) outer = EliminateHole(hole, outer);
            return outer;
        }

        private static Node EliminateHole(Node hole, Node outer)
        {
            var bridge = FindHoleBridge(hole, outer);
            if (bridge == null) return outer;
            var bridgeReverse = SplitPolygon(bridge, hole);
            FilterPoints(bridgeReverse, bridgeReverse.next);
            return FilterPoints(bridge, bridge.next);
        }

        // Cầu nối từ điểm trái nhất của lỗ tới đỉnh nhìn thấy được của vòng ngoài
        private static Node FindHoleBridge(Node hole, Node outer)
        {
            var p = outer;
            var hx = hole.x;
            var hy = hole.y;
            var qx = double.NegativeInfinity;
            Node m = null;
            do
            {
                if (Equals(hole, p)) return p;
                if (hy <= p.y && hy >= p.next.y && p.next.y != p.y)
                {
                    var x = p.x + (hy - p.y) * (p.next.x - p.x) / (p.next.y - p.y);
                    if (x <= hx && x > qx)
                    {
                        qx = x;
                        m = p.x < p.next.x ? p : p.next;
                        if (x == hx) return m;
                    }
                }
                p = p.next;
            } while (p != outer);
            if (m == null) return null;

            var stop = m;
            double mx = m.x, my = m.y;
            var tanMin = double.PositiveInfinity;
            p = m;
            do
            {
                if (hx >= p.x && p.x >= mx && hx != p.x && PointInTriangle(hy < my ? hx : qx, hy, mx, my, hy < my ? qx : hx, hy, p.x, p.y))
                {
                    var tan = System.Math.Abs(hy - p.y) / (hx - p.x);
                    if (LocallyInside(p, hole) && (tan < tanMin || (tan == tanMin && (p.x > m.x || (p.x == m.x && SectorContainsSector(m, p))))))
                    {
                        m = p;
                        tanMin = tan;
                    }
                }
                p = p.next;
            } while (p != stop);
            return m;
        }

        private static bool SectorContainsSector(Node m, Node p) => Area(m.prev, m, p.prev) < 0 && Area(p.next, m, m.next) < 0;

        private static bool IsValidDiagonal(Node a, Node b) =>
            a.next.i != b.i && a.prev.i != b.i && !IntersectsPolygon(a, b) &&
            ((LocallyInside(a, b) && LocallyInside(b, a) && MiddleInside(a, b) && (Area(a.prev, a, b.prev) != 0 || Area(a, b.prev, b) != 0)) ||
             (Equals(a, b) && Area(a.prev, a, a.next) > 0 && Area(b.prev, b, b.next) > 0));

        private static double Area(Node p, Node q, Node r) => (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);

        private static bool Equals(Node p1, Node p2) => p1.x == p2.x && p1.y == p2.y;

        private static bool Intersects(Node p1, Node q1, Node p2, Node q2)
        {
            int o1 = Sign(Area(p1, q1, p2)), o2 = Sign(Area(p1, q1, q2)), o3 = Sign(Area(p2, q2, p1)), o4 = Sign(Area(p2, q2, q1));
            if (o1 != o2 && o3 != o4) return true;
            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
            if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
            if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
            if (o4 == 0 && OnSegment(p2, q1, q2)) return true;
            return false;
        }

        private static bool OnSegment(Node p, Node q, Node r) =>
            q.x <= System.Math.Max(p.x, r.x) && q.x >= System.Math.Min(p.x, r.x) && q.y <= System.Math.Max(p.y, r.y) && q.y >= System.Math.Min(p.y, r.y);

        private static int Sign(double v) => v > 0 ? 1 : v < 0 ? -1 : 0;

        private static bool IntersectsPolygon(Node a, Node b)
        {
            var p = a;
            do
            {
                if (p.i != a.i && p.next.i != a.i && p.i != b.i && p.next.i != b.i && Intersects(p, p.next, a, b)) return true;
                p = p.next;
            } while (p != a);
            return false;
        }

        private static bool LocallyInside(Node a, Node b) =>
            Area(a.prev, a, a.next) < 0 ? Area(a, b, a.next) >= 0 && Area(a, a.prev, b) >= 0 : Area(a, b, a.prev) < 0 || Area(a, a.next, b) < 0;

        private static bool MiddleInside(Node a, Node b)
        {
            var p = a;
            var inside = false;
            double px = (a.x + b.x) / 2, py = (a.y + b.y) / 2;
            do
            {
                if (p.y > py != p.next.y > py && p.next.y != p.y && px < (p.next.x - p.x) * (py - p.y) / (p.next.y - p.y) + p.x) inside = !inside;
                p = p.next;
            } while (p != a);
            return inside;
        }

        private static Node SplitPolygon(Node a, Node b)
        {
            var a2 = new Node(a.i, a.x, a.y);
            var b2 = new Node(b.i, b.x, b.y);
            Node an = a.next, bp = b.prev;
            a.next = b; b.prev = a;
            a2.next = an; an.prev = a2;
            b2.next = a2; a2.prev = b2;
            bp.next = b2; b2.prev = bp;
            return b2;
        }

        private static Node Leftmost(Node start)
        {
            var p = start;
            var leftmost = start;
            do
            {
                if (p.x < leftmost.x || (p.x == leftmost.x && p.y < leftmost.y)) leftmost = p;
                p = p.next;
            } while (p != start);
            return leftmost;
        }

        private static bool PointInTriangle(double ax, double ay, double bx, double by, double cx, double cy, double px, double py) =>
            (cx - px) * (ay - py) >= (ax - px) * (cy - py) && (ax - px) * (by - py) >= (bx - px) * (ay - py) && (bx - px) * (cy - py) >= (cx - px) * (by - py);

        private static bool PointInTriangleExceptFirst(double ax, double ay, double bx, double by, double cx, double cy, double px, double py) =>
            !(ax == px && ay == py) && PointInTriangle(ax, ay, bx, by, cx, cy, px, py);

        private static Node Link(IReadOnlyList<Vector2> v, int start, int end, bool clockwise)
        {
            Node last = null;
            if (clockwise == SignedArea(v, start, end) > 0)
                for (var i = start; i < end; i++) last = InsertNode(i, v[i].x, v[i].y, last);
            else
                for (var i = end - 1; i >= start; i--) last = InsertNode(i, v[i].x, v[i].y, last);
            if (last != null && Equals(last, last.next))
            {
                RemoveNode(last);
                last = last.next;
            }
            return last;
        }

        private static double SignedArea(IReadOnlyList<Vector2> v, int start, int end)
        {
            double sum = 0;
            for (int i = start, j = end - 1; i < end; j = i, i++) sum += ((double)v[j].x - v[i].x) * ((double)v[i].y + v[j].y);
            return sum;
        }

        // Bỏ đỉnh trùng và thẳng hàng
        private static Node FilterPoints(Node start, Node end)
        {
            if (start == null) return null;
            end ??= start;
            var p = start;
            bool again;
            do
            {
                again = false;
                if (!p.steiner && (Equals(p, p.next) || Area(p.prev, p, p.next) == 0))
                {
                    RemoveNode(p);
                    p = end = p.prev;
                    if (p == p.next) break;
                    again = true;
                }
                else p = p.next;
            } while (again || p != end);
            return end;
        }

        private static Node InsertNode(int i, double x, double y, Node last)
        {
            var p = new Node(i, x, y);
            if (last == null)
            {
                p.prev = p;
                p.next = p;
            }
            else
            {
                p.next = last.next;
                p.prev = last;
                last.next.prev = p;
                last.next = p;
            }
            return p;
        }

        private static void RemoveNode(Node p)
        {
            p.next.prev = p.prev;
            p.prev.next = p.next;
        }
    }
}
