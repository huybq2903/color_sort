using System;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Làm mượt biên kiểu vector hoá: giữ góc nhọn, đoạn giữa hai góc làm mượt, giảm về điểm chính rồi nối bằng spline.</summary>
    public static class ContourSmoother
    {
        private const int MaxCornerArm = 10; // tay đo góc tối đa (mẫu, 1 mẫu ~ 1 pixel lưới): đủ dài để nhận góc đã bị bo nhẹ

        // Tay đo co theo độ dài đường để hình nhỏ vẫn nhận ra góc
        private const float StraightTolFactor = 2f, MinStraightLength = 24f, StraightTolPerLength = 0.02f, StraightTolBase = 1.5f;

        // Hình/đoạn ngắn không được làm mượt quá tay: sigma tối đa 1/SigmaFraction độ dài (mẫu), nên mỏ, mắt nhỏ không bị bo tròn mất góc
        private const int SigmaFraction = 8;

        // flow (chỉ khi chia mảnh): đoạn dài giữ độ cong chính, bỏ gợn nhỏ như nét vẽ tay: sigma tới 1/9 độ dài, nhưng không lệch quá 1,2% độ dài so với đường làm mượt nhẹ (lệch nhiều thì khe đen hai bên không còn đều)
        private static readonly float[] FlowDivisors = { 9f, 14f, 24f };
        private const float FlowDrift = 0.012f;
        private const int FlowMinLength = 24;

        private static Vector2[] Flow(Vector2[] seg, float sigma)
        {
            var basic = GaussOpen(seg, Fit(sigma, seg.Length));
            if (seg.Length < FlowMinLength) return basic;
            var limit = FlowDrift * seg.Length;
            foreach (var div in FlowDivisors)
            {
                var s = seg.Length / div;
                if (s <= sigma) break;
                var o = GaussOpen(seg, s);
                var dev = 0f;
                for (var i = 0; i < o.Length && dev <= limit; i++) dev = Mathf.Max(dev, Vector2.Distance(o[i], basic[i]));
                if (dev <= limit) return o;
            }
            return basic;
        }

        private static float Fit(float sigma, int length) => Mathf.Min(sigma, Mathf.Max(1.5f, length / (float)SigmaFraction));

        private static int Arm(int n, int divisor) => Mathf.Clamp(n / divisor, 4, MaxCornerArm);

        // Vòng kín. resampleStep > 0: sau khi giảm điểm, nối bằng spline Catmull-Rom và lấy mẫu cách nhau ≤ step
        public static List<Vector2> Smooth(IReadOnlyList<Vector2Int> loop, float sigma, float cornerDeg = 55f, float epsilon = 0.4f, float resampleStep = 0f, float fitTolerance = 0f, bool flow = false)
        {
            if (sigma <= 0f || loop.Count < 4) return ToFloat(loop);
            var q = Densify(loop);
            if (q.Length < 8) return ToFloat(loop);

            var corners = FindCorners(q, Mathf.Min(2f, sigma), cornerDeg);
            var result = new List<Vector2>();
            if (corners.Count == 0)
            {
                var s = GaussCircular(q, Fit(sigma, q.Length));
                var closed = new Vector2[s.Length + 1];
                Array.Copy(s, closed, s.Length);
                closed[s.Length] = s[0];
                AddPiece(result, closed, epsilon, resampleStep, fitTolerance, true);
            }
            else
            {
                for (var i = 0; i < corners.Count; i++)
                {
                    var seg = Segment(q, corners[i], corners[(i + 1) % corners.Count], corners.Count == 1);
                    AddPiece(result, flow ? Flow(seg, sigma) : GaussOpen(seg, Fit(sigma, seg.Length)), epsilon, resampleStep, fitTolerance, false);
                }
            }
            return result.Count >= 3 ? result : ToFloat(loop);
        }

        // Đường hở từ nút đến nút: hai đầu cố định. Trả về từ điểm đầu đến điểm cuối (bao gồm cả hai)
        public static List<Vector2> SmoothArc(IReadOnlyList<Vector2Int> arc, float sigma, float cornerDeg = 55f, float epsilon = 0.4f, float resampleStep = 0f, float fitTolerance = 0f, bool flow = false)
        {
            var q = DensifyOpen(arc);
            if (sigma <= 0f || q.Length < 8) return ToFloat(arc);

            var q0 = GaussOpen(q, Mathf.Min(2f, sigma));
            var n = q.Length;
            var arm = Arm(n, 4);
            var ang = new float[n];
            for (var i = arm; i < n - arm; i++)
                ang[i] = Mathf.Abs(Vector2.SignedAngle(q0[i] - q0[i - arm], q0[i + arm] - q0[i]));

            var cuts = new List<int> { 0 };
            for (var i = arm; i < n - arm; i++)
            {
                if (ang[i] <= cornerDeg) continue;
                var isMax = true;
                for (var j = 1; j <= arm && isMax; j++)
                    isMax = ang[i] > ang[i - j] && ang[i] >= ang[i + j];
                if (isMax) cuts.Add(i);
            }
            cuts.Add(n - 1);

            var result = new List<Vector2>();
            for (var c = 0; c + 1 < cuts.Count; c++)
            {
                var len = cuts[c + 1] - cuts[c] + 1;
                var seg = new Vector2[len];
                Array.Copy(q, cuts[c], seg, 0, len);
                AddPiece(result, flow ? Flow(seg, sigma) : GaussOpen(seg, Fit(sigma, seg.Length)), epsilon, resampleStep, fitTolerance, false);
            }
            result.Add(q[n - 1]);
            return result;
        }

        private static List<Vector2> ToFloat(IReadOnlyList<Vector2Int> loop)
        {
            var r = new List<Vector2>(loop.Count);
            foreach (var v in loop) r.Add(v);
            return r;
        }

        // Cạnh của biên luôn song song trục nên chia đều theo từng bước 1 đơn vị
        private static Vector2[] Densify(IReadOnlyList<Vector2Int> loop)
        {
            var list = new List<Vector2>(loop.Count * 2);
            for (var i = 0; i < loop.Count; i++)
            {
                Vector2 a = loop[i], b = loop[(i + 1) % loop.Count];
                var steps = Mathf.Max(1, Mathf.RoundToInt(Vector2.Distance(a, b)));
                for (var k = 0; k < steps; k++) list.Add(Vector2.Lerp(a, b, k / (float)steps));
            }
            return list.ToArray();
        }

        private static Vector2[] DensifyOpen(IReadOnlyList<Vector2Int> arc)
        {
            var list = new List<Vector2>(arc.Count * 2);
            for (var i = 0; i + 1 < arc.Count; i++)
            {
                Vector2 a = arc[i], b = arc[i + 1];
                var steps = Mathf.Max(1, Mathf.RoundToInt(Vector2.Distance(a, b)));
                for (var k = 0; k < steps; k++) list.Add(Vector2.Lerp(a, b, k / (float)steps));
            }
            if (arc.Count > 0) list.Add(arc[arc.Count - 1]);
            return list.ToArray();
        }

        // Góc = chỗ đổi hướng gắt và là cực đại cục bộ trên đường đã làm mượt nhẹ
        private static List<int> FindCorners(Vector2[] q, float sigma0, float thetaDeg)
        {
            var n = q.Length;
            var q0 = GaussCircular(q, sigma0);
            var arm = Arm(n, 8);
            var ang = new float[n];
            for (var i = 0; i < n; i++)
            {
                var a = q0[i] - q0[(i - arm + n * 2) % n];
                var b = q0[(i + arm) % n] - q0[i];
                ang[i] = Mathf.Abs(Vector2.SignedAngle(a, b));
            }

            var corners = new List<int>();
            for (var i = 0; i < n; i++)
            {
                if (ang[i] <= thetaDeg) continue;
                var isMax = true;
                for (var j = 1; j <= arm && isMax; j++)
                    isMax = ang[i] > ang[(i - j + n) % n] && ang[i] >= ang[(i + j) % n];
                if (isMax) corners.Add(i);
            }
            return corners;
        }

        private static Vector2[] Segment(Vector2[] q, int from, int to, bool wholeLoop)
        {
            var n = q.Length;
            var len = wholeLoop ? n + 1 : (to - from + n) % n + 1;
            var seg = new Vector2[len];
            for (var k = 0; k < len; k++) seg[k] = q[(from + k) % n];
            return seg;
        }

        private static float[] Kernel(float sigma)
        {
            var r = Mathf.CeilToInt(sigma * 3f);
            var k = new float[r * 2 + 1];
            var sum = 0f;
            for (var i = -r; i <= r; i++) sum += k[i + r] = Mathf.Exp(-i * i / (2f * sigma * sigma));
            for (var i = 0; i < k.Length; i++) k[i] /= sum;
            return k;
        }

        private static Vector2[] GaussCircular(Vector2[] q, float sigma)
        {
            if (sigma <= 0f) return q;
            var k = Kernel(sigma);
            var r = k.Length / 2;
            var n = q.Length;
            var o = new Vector2[n];
            for (var i = 0; i < n; i++)
            {
                var s = Vector2.zero;
                for (var j = -r; j <= r; j++) s += q[((i + j) % n + n) % n] * k[j + r];
                o[i] = s;
            }
            return o;
        }

        // Phản xạ lẻ qua 2 đầu để giữ nguyên điểm đầu/cuối và hướng tiếp tuyến
        private static Vector2[] GaussOpen(Vector2[] s, float sigma)
        {
            var n = s.Length;
            if (n < 3) return s;
            var k = Kernel(sigma);
            var r = k.Length / 2;
            var o = new Vector2[n];
            for (var i = 0; i < n; i++)
            {
                var acc = Vector2.zero;
                for (var j = -r; j <= r; j++)
                {
                    var idx = i + j;
                    Vector2 p;
                    if (idx < 0) p = 2f * s[0] - s[Mathf.Min(-idx, n - 1)];
                    else if (idx >= n) p = 2f * s[n - 1] - s[Mathf.Max(2 * (n - 1) - idx, 0)];
                    else p = s[idx];
                    acc += p * k[j + r];
                }
                o[i] = acc;
            }
            o[0] = s[0];
            o[n - 1] = s[n - 1];
            return o;
        }

        // Giảm điểm bằng Douglas–Peucker, rồi (nếu step > 0) nối lại bằng spline; thêm mọi điểm trừ điểm cuối
        private static void AddPiece(List<Vector2> dst, Vector2[] pts, float eps, float step, float fitTol, bool closedPiece)
        {
            if (fitTol > 0f && step > 0f && pts.Length >= 3)
            {
                FitPiece(dst, pts, fitTol, step, closedPiece);
                return;
            }
            var key = Simplify(pts, eps);
            if (step <= 0f) { for (var i = 0; i < key.Count - 1; i++) dst.Add(key[i]); return; }
            Spline(dst, key, step);
        }

        private const float FitMinTolerance = 1.6f, FitSampleStep = 36f, FitSagitta = 0.5f; // đơn vị lưới: càng lớn càng ít điểm, đường vẫn cong mượt (độ võng 0,5 ô ≈ 0,03% khung)

        // Khớp ít Bézier bậc ba nhất trong dung sai, rồi lấy mẫu cách nhau ≤ step; vòng kín liền tiếp tuyến tại điểm nối
        private static void FitPiece(List<Vector2> dst, Vector2[] pts, float tol, float step, bool closedPiece)
        {
            tol = Mathf.Max(tol, FitMinTolerance); // level cũ lưu dung sai nhỏ hơn: vẫn dùng mức mới để ít điểm
            step = Mathf.Max(step, FitSampleStep);
            tol = Mathf.Min(tol, Mathf.Max(0.8f, pts.Length / 50f)); // đoạn ngắn: dung sai nhỏ để giữ hình dạng (ngà, mỏ)
            if (!closedPiece && IsStraight(pts, StraightTolFactor * tol))
            {
                dst.Add(pts[0]);
                return;
            }
            Vector2? t1 = null, t2 = null;
            if (closedPiece)
            {
                var arm = Mathf.Min(4, (pts.Length - 1) / 2);
                var t = (pts[arm] - pts[pts.Length - 1 - arm]).normalized;
                t1 = t;
                t2 = -t;
            }
            foreach (var c in CurveFitter.Fit(pts, tol, t1, t2))
            {
                var len = Vector2.Distance(c.p0, c.p1) + Vector2.Distance(c.p1, c.p2) + Vector2.Distance(c.p2, c.p3);
                // đoạn thẳng/cong nhẹ lấy thưa (tới step), chỗ cong gắt lấy dày vừa đủ để độ võng mỗi đoạn ≤ FitSagitta
                var chord = c.p3 - c.p0;
                var dev = Mathf.Max(DistToLine(c.p1, c.p0, chord), DistToLine(c.p2, c.p0, chord));
                var k = Mathf.Max(1, Mathf.Max(Mathf.CeilToInt(len / step), Mathf.CeilToInt(Mathf.Sqrt(0.75f * dev / FitSagitta))));
                for (var j = 0; j < k; j++) dst.Add(j == 0 ? c.p0 : c.At(j / (float)k));
            }
        }

        // Đoạn dài lệch khỏi dây cung ≤ tol thì coi là thẳng
        private static bool IsStraight(Vector2[] pts, float tol)
        {
            var chord = pts[pts.Length - 1] - pts[0];
            if (chord.magnitude < MinStraightLength) return false;
            tol = Mathf.Min(tol, StraightTolPerLength * chord.magnitude + StraightTolBase); // đường ngắn không được duỗi thẳng quá tay
            foreach (var p in pts)
                if (DistToLine(p, pts[0], chord) > tol) return false;
            return true;
        }

        private static float DistToLine(Vector2 p, Vector2 a, Vector2 dir)
        {
            var l = dir.magnitude;
            return l < 1e-6f ? Vector2.Distance(p, a) : Mathf.Abs(dir.x * (p.y - a.y) - dir.y * (p.x - a.x)) / l;
        }

        private static List<Vector2> Simplify(Vector2[] pts, float eps)
        {
            var keep = new bool[pts.Length];
            keep[0] = keep[pts.Length - 1] = true;
            var stack = new Stack<(int, int)>();
            stack.Push((0, pts.Length - 1));
            while (stack.Count > 0)
            {
                var (lo, hi) = stack.Pop();
                if (hi - lo < 2) continue;
                var a = pts[lo];
                var ab = pts[hi] - a;
                var l2 = ab.sqrMagnitude;
                var best = -1;
                var dmax = 0f;
                for (var i = lo + 1; i < hi; i++)
                {
                    var t = l2 < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(pts[i] - a, ab) / l2);
                    var d = Vector2.Distance(pts[i], a + t * ab);
                    if (d > dmax) { dmax = d; best = i; }
                }
                if (dmax <= eps) continue;
                keep[best] = true;
                stack.Push((lo, best));
                stack.Push((best, hi));
            }
            var r = new List<Vector2>();
            for (var i = 0; i < pts.Length; i++)
                if (keep[i]) r.Add(pts[i]);
            return r;
        }

        // Catmull-Rom hướng tâm (alpha 0.5) qua các điểm chính; hai đầu phản xạ nên đầu mút không bị lệch
        private static void Spline(List<Vector2> dst, List<Vector2> p, float step)
        {
            var m = p.Count - 1;
            for (var i = 0; i < m; i++)
            {
                var p1 = p[i];
                var p2 = p[i + 1];
                var p0 = i > 0 ? p[i - 1] : 2f * p1 - p2;
                var p3 = i + 2 <= m ? p[i + 2] : 2f * p2 - p1;
                var k = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p1, p2) / step));
                for (var j = 0; j < k; j++) dst.Add(k == 1 ? p1 : CatmullRom(p0, p1, p2, p3, j / (float)k));
            }
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u)
        {
            const float alpha = 0.5f;
            float t0 = 0f, t1 = t0 + Knot(p0, p1, alpha), t2 = t1 + Knot(p1, p2, alpha), t3 = t2 + Knot(p2, p3, alpha);
            var t = Mathf.Lerp(t1, t2, u);
            var a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
            var a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
            var a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;
            var b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2;
            var b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3;
            return (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;
        }

        private static float Knot(Vector2 a, Vector2 b, float alpha) => Mathf.Max(1e-4f, Mathf.Pow(Vector2.Distance(a, b), alpha));
    }
}
