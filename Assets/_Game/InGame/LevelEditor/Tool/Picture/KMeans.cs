using System;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>k-means++ seed cố định + Lloyd; dùng cho cả màu (rgb) lẫn toạ độ (x,y,0).</summary>
    public static class KMeans
    {
        public static Vector3[] Run(Vector3[] pts, int k, int seed, int iterations, out int[] assign)
        {
            var rnd = new System.Random(seed);
            var c = new Vector3[k];
            c[0] = pts[rnd.Next(pts.Length)];

            var d2 = new double[pts.Length];
            for (var i = 0; i < pts.Length; i++) d2[i] = double.MaxValue;
            for (var j = 1; j < k; j++)
            {
                double sum = 0;
                for (var i = 0; i < pts.Length; i++)
                {
                    d2[i] = Math.Min(d2[i], (pts[i] - c[j - 1]).sqrMagnitude); // chỉ so với tâm mới nhất
                    sum += d2[i];
                }
                var r = rnd.NextDouble() * sum;
                var pick = pts.Length - 1;
                for (var i = 0; i < pts.Length; i++)
                {
                    r -= d2[i];
                    if (r <= 0) { pick = i; break; }
                }
                c[j] = pts[pick];
            }

            assign = new int[pts.Length];
            for (var it = 0; ; it++)
            {
                for (var i = 0; i < pts.Length; i++) assign[i] = Nearest(pts[i], c);
                if (it == iterations) break;

                var sums = new double[k * 3];
                var cnt = new int[k];
                for (var i = 0; i < pts.Length; i++)
                {
                    var a = assign[i];
                    sums[a * 3] += pts[i].x; sums[a * 3 + 1] += pts[i].y; sums[a * 3 + 2] += pts[i].z;
                    cnt[a]++;
                }
                for (var j = 0; j < k; j++)
                    if (cnt[j] > 0)
                        c[j] = new Vector3((float)(sums[j * 3] / cnt[j]), (float)(sums[j * 3 + 1] / cnt[j]), (float)(sums[j * 3 + 2] / cnt[j]));
            }
            return c;
        }

        public static int Nearest(Vector3 v, Vector3[] set)
        {
            var best = 0;
            var bd = float.MaxValue;
            for (var i = 0; i < set.Length; i++)
            {
                float dx = set[i].x - v.x, dy = set[i].y - v.y, dz = set[i].z - v.z; // tính tay: Mono không inline toán tử Vector3
                var d = dx * dx + dy * dy + dz * dz;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }
    }
}
