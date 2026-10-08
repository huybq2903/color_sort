using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Khớp chuỗi điểm bằng ít đường Bézier bậc ba nhất trong dung sai (Schneider, "An Algorithm for Automatically Fitting Digitized Curves").</summary>
    public static class CurveFitter
    {
        public struct Cubic
        {
            public Vector2 p0, p1, p2, p3;

            public Vector2 At(float t)
            {
                var u = 1f - t;
                return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
            }
        }

        private const int TangentArm = 4;
        private const int MaxReparam = 6;

        // t1: tiếp tuyến ra khỏi điểm đầu; t2: tiếp tuyến ra khỏi điểm cuối (hướng ngược vào trong). Mặc định ước lượng từ dữ liệu
        public static List<Cubic> Fit(Vector2[] d, float tolerance, Vector2? t1 = null, Vector2? t2 = null)
        {
            var result = new List<Cubic>();
            if (d.Length < 2) return result;
            var arm = Mathf.Min(TangentArm, d.Length - 1);
            var a = t1 ?? (d[arm] - d[0]).normalized;
            var b = t2 ?? (d[d.Length - 1 - arm] - d[d.Length - 1]).normalized;
            Fit(d, 0, d.Length - 1, a, b, tolerance * tolerance, result);
            return result;
        }

        private static void Fit(Vector2[] d, int first, int last, Vector2 tHat1, Vector2 tHat2, float error, List<Cubic> output)
        {
            if (last - first == 1)
            {
                var dist = Vector2.Distance(d[first], d[last]) / 3f;
                output.Add(new Cubic { p0 = d[first], p1 = d[first] + tHat1 * dist, p2 = d[last] + tHat2 * dist, p3 = d[last] });
                return;
            }

            var u = ChordLength(d, first, last);
            var bez = Generate(d, first, last, u, tHat1, tHat2);
            var maxErr = MaxError(d, first, last, bez, u, out var split);
            if (maxErr < error)
            {
                output.Add(bez);
                return;
            }

            if (maxErr < error * 16f)
            {
                for (var i = 0; i < MaxReparam; i++)
                {
                    var up = Reparameterize(d, first, last, u, bez);
                    bez = Generate(d, first, last, up, tHat1, tHat2);
                    maxErr = MaxError(d, first, last, bez, up, out split);
                    if (maxErr < error)
                    {
                        output.Add(bez);
                        return;
                    }
                    u = up;
                }
            }

            var center = (d[split - 1] - d[split]) + (d[split] - d[split + 1]);
            var tCenter = center.sqrMagnitude < 1e-12f ? (d[split - 1] - d[split + 1]).normalized : center.normalized;
            Fit(d, first, split, tHat1, tCenter, error, output);
            Fit(d, split, last, -tCenter, tHat2, error, output);
        }

        private static float[] ChordLength(Vector2[] d, int first, int last)
        {
            var u = new float[last - first + 1];
            for (var i = first + 1; i <= last; i++) u[i - first] = u[i - first - 1] + Vector2.Distance(d[i], d[i - 1]);
            var total = u[last - first];
            for (var i = 1; i < u.Length; i++) u[i] = total > 1e-9f ? u[i] / total : i / (float)(u.Length - 1);
            return u;
        }

        private static Cubic Generate(Vector2[] d, int first, int last, float[] u, Vector2 t1, Vector2 t2)
        {
            var p0 = d[first];
            var p3 = d[last];
            float c00 = 0, c01 = 0, c11 = 0, x0 = 0, x1 = 0;
            for (var i = 0; i <= last - first; i++)
            {
                var t = u[i];
                float b0 = B0(t), b1 = B1(t), b2 = B2(t), b3 = B3(t);
                var a1 = t1 * b1;
                var a2 = t2 * b2;
                c00 += Vector2.Dot(a1, a1);
                c01 += Vector2.Dot(a1, a2);
                c11 += Vector2.Dot(a2, a2);
                var tmp = d[first + i] - (p0 * (b0 + b1) + p3 * (b2 + b3));
                x0 += Vector2.Dot(a1, tmp);
                x1 += Vector2.Dot(a2, tmp);
            }

            var det = c00 * c11 - c01 * c01;
            var alphaL = Mathf.Abs(det) > 1e-12f ? (x0 * c11 - x1 * c01) / det : 0f;
            var alphaR = Mathf.Abs(det) > 1e-12f ? (c00 * x1 - c01 * x0) / det : 0f;
            var seg = Vector2.Distance(p0, p3);
            var eps = 1e-6f * seg;
            if (alphaL < eps || alphaR < eps || alphaL > seg * 2f || alphaR > seg * 2f) alphaL = alphaR = seg / 3f; // thanh cong quá dài thì vọt ra thành gai
            return new Cubic { p0 = p0, p1 = p0 + t1 * alphaL, p2 = p3 + t2 * alphaR, p3 = p3 };
        }

        private static float[] Reparameterize(Vector2[] d, int first, int last, float[] u, Cubic bez)
        {
            var o = new float[u.Length];
            for (var i = 0; i < u.Length; i++) o[i] = NewtonRaphson(bez, d[first + i], u[i]);
            return o;
        }

        private static float NewtonRaphson(Cubic q, Vector2 p, float u)
        {
            var q0 = q.At(u);
            var d1 = 3f * ((1 - u) * (1 - u) * (q.p1 - q.p0) + 2f * (1 - u) * u * (q.p2 - q.p1) + u * u * (q.p3 - q.p2));
            var d2 = 6f * ((1 - u) * (q.p2 - 2f * q.p1 + q.p0) + u * (q.p3 - 2f * q.p2 + q.p1));
            var num = Vector2.Dot(q0 - p, d1);
            var den = Vector2.Dot(d1, d1) + Vector2.Dot(q0 - p, d2);
            if (Mathf.Abs(den) < 1e-12f) return u;
            return Mathf.Clamp01(u - num / den);
        }

        private static float MaxError(Vector2[] d, int first, int last, Cubic bez, float[] u, out int split)
        {
            split = (last - first + 1) / 2 + first;
            split = Mathf.Clamp(split, first + 1, last - 1);
            var max = 0f;
            for (var i = first + 1; i < last; i++)
            {
                var dist = (bez.At(u[i - first]) - d[i]).sqrMagnitude;
                if (dist >= max) { max = dist; split = i; }
            }
            return max;
        }

        private static float B0(float u) => (1 - u) * (1 - u) * (1 - u);
        private static float B1(float u) => 3f * u * (1 - u) * (1 - u);
        private static float B2(float u) => 3f * u * u * (1 - u);
        private static float B3(float u) => u * u * u;
    }
}
