using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Phép xử lý ảnh nền (C# thuần): làm mờ, morphology, thành phần liên thông, lấp theo láng giềng gần nhất.</summary>
    internal static class ImageOps
    {
        public static float[] Gray(Color32[] px)
        {
            var g = new float[px.Length];
            for (var i = 0; i < g.Length; i++) g[i] = 0.299f * px[i].r + 0.587f * px[i].g + 0.114f * px[i].b;
            return g;
        }

        // Gauss tách trục, biên lặp pixel
        public static float[] Gauss(float[] src, int w, int h, float sigma)
        {
            if (sigma <= 0f) return (float[])src.Clone();
            var r = Mathf.Max(1, Mathf.CeilToInt(3f * sigma));
            var k = new float[2 * r + 1];
            float sum = 0;
            for (var i = -r; i <= r; i++) sum += k[i + r] = Mathf.Exp(-i * i / (2f * sigma * sigma));
            for (var i = 0; i < k.Length; i++) k[i] /= sum;
            var tmp = new float[src.Length];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                float a = 0;
                for (var i = -r; i <= r; i++) a += k[i + r] * src[y * w + Mathf.Clamp(x + i, 0, w - 1)];
                tmp[y * w + x] = a;
            }
            var res = new float[src.Length];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                float a = 0;
                for (var i = -r; i <= r; i++) a += k[i + r] * tmp[Mathf.Clamp(y + i, 0, h - 1) * w + x];
                res[y * w + x] = a;
            }
            return res;
        }

        // Max (dilate = true) hoặc min trong cửa sổ vuông k×k, tách trục
        private static float[] Extreme(float[] src, int w, int h, int k, bool max)
        {
            var r = k / 2;
            var tmp = new float[src.Length];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var v = src[y * w + x];
                for (var i = -r; i <= r; i++)
                {
                    var q = src[y * w + Mathf.Clamp(x + i, 0, w - 1)];
                    if (max ? q > v : q < v) v = q;
                }
                tmp[y * w + x] = v;
            }
            var res = new float[src.Length];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var v = tmp[y * w + x];
                for (var i = -r; i <= r; i++)
                {
                    var q = tmp[Mathf.Clamp(y + i, 0, h - 1) * w + x];
                    if (max ? q > v : q < v) v = q;
                }
                res[y * w + x] = v;
            }
            return res;
        }

        // Đóng (dilate rồi erode): lấp các cấu trúc tối hẹp hơn k
        // Mở (erode rồi dilate): xoá cấu trúc sáng hẹp hơn k; g - Open(g) = nét sáng mảnh (top-hat)
        public static float[] Open(float[] g, int w, int h, int k) => Extreme(Extreme(g, w, h, k, false), w, h, k, true);

        // Làm mịn mặt nạ nhị phân: blur Gauss rồi lấy ngưỡng 0.5 (bỏ răng cưa, giữ nét dày ≥ vài pixel)
        public static bool[] SmoothMask(bool[] m, int w, int h, float sigma)
        {
            var f = new float[m.Length];
            for (var i = 0; i < f.Length; i++) f[i] = m[i] ? 1f : 0f;
            f = Gauss(f, w, h, sigma);
            var r = new bool[m.Length];
            for (var i = 0; i < r.Length; i++) r[i] = f[i] > 0.5f;
            return r;
        }

        public static float[] Close(float[] g, int w, int h, int k) => Extreme(Extreme(g, w, h, k, true), w, h, k, false);

        public static bool[] Dilate3(bool[] m, int w, int h) => Morph3(m, w, h, true);

        public static bool[] Erode3(bool[] m, int w, int h) => Morph3(m, w, h, false);

        private static bool[] Morph3(bool[] m, int w, int h, bool dilate)
        {
            var res = new bool[m.Length];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var v = !dilate;
                for (var dy = -1; dy <= 1 && v != dilate; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    var q = nx >= 0 && ny >= 0 && nx < w && ny < h && m[ny * w + nx];
                    if (dilate ? q : !q) { v = dilate; break; }
                }
                res[y * w + x] = v;
            }
            return res;
        }

        // Nhãn thành phần liên thông của pixel free (1..count); 0 = không thuộc
        public static int[] Components(bool[] free, int w, int h, bool eight, out int count)
        {
            var lab = new int[free.Length];
            count = 0;
            var st = new Stack<int>();
            for (var i = 0; i < free.Length; i++)
            {
                if (!free[i] || lab[i] != 0) continue;
                count++;
                lab[i] = count;
                st.Push(i);
                while (st.Count > 0)
                {
                    var p = st.Pop();
                    int x = p % w, y = p / w;
                    for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (!eight && dx != 0 && dy != 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        var q = ny * w + nx;
                        if (free[q] && lab[q] == 0) { lab[q] = count; st.Push(q); }
                    }
                }
            }
            return lab;
        }

        // Như trên nhưng 2 pixel liền kề cùng key thì cùng thành phần (key < 0 bị bỏ)
        public static int[] ComponentsByKey(int[] key, int w, int h, out int count)
        {
            var lab = new int[key.Length];
            var id = 0;
            var st = new Stack<int>();
            for (var i = 0; i < key.Length; i++)
            {
                if (key[i] < 0 || lab[i] != 0) continue;
                id++;
                lab[i] = id;
                st.Push(i);
                while (st.Count > 0)
                {
                    var p = st.Pop();
                    int x = p % w, y = p / w;
                    for (var d = 0; d < 4; d++)
                    {
                        int nx = x + (d == 0 ? -1 : d == 1 ? 1 : 0), ny = y + (d == 2 ? -1 : d == 3 ? 1 : 0);
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        var q = ny * w + nx;
                        if (lab[q] == 0 && key[q] == key[i]) { lab[q] = id; st.Push(q); }
                    }
                }
            }
            count = id;
            return lab;
        }

        // Pixel nhãn 0 nhận nhãn của pixel có nhãn gần nhất (BFS nhiều nguồn)
        public static void FillNearest(int[] lab, int w, int h)
        {
            var q = new Queue<int>();
            for (var i = 0; i < lab.Length; i++) if (lab[i] != 0) q.Enqueue(i);
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int x = p % w, y = p / w;
                void Try(int n)
                {
                    if (lab[n] != 0) return;
                    lab[n] = lab[p];
                    q.Enqueue(n);
                }
                if (x > 0) Try(p - 1);
                if (x < w - 1) Try(p + 1);
                if (y > 0) Try(p - w);
                if (y < h - 1) Try(p + w);
            }
        }

        // Khoảng cách Euclid tới pixel ngoài mặt nạ (Felzenszwalb, 2 lượt, số thực 64 bit)
        public static float[] Edt(bool[] m, int w, int h)
        {
            const double Inf = 1e9;
            var f = new double[w * h];
            for (var i = 0; i < f.Length; i++) f[i] = m[i] ? Inf : 0.0;
            var tmp = new double[w * h];
            Pass(f, tmp, w, h, true);
            Pass(tmp, f, w, h, false);
            var res = new float[w * h];
            for (var i = 0; i < res.Length; i++) res[i] = (float)System.Math.Sqrt(f[i]);
            return res;
        }

        private static void Pass(double[] src, double[] dst, int w, int h, bool columns)
        {
            var len = columns ? h : w;
            var cnt = columns ? w : h;
            var v = new int[len]; var z = new double[len + 1]; var d = new double[len];
            for (var c = 0; c < cnt; c++)
            {
                double At(int i) => columns ? src[i * w + c] : src[c * w + i];
                var k = 0; v[0] = 0; z[0] = double.NegativeInfinity; z[1] = double.PositiveInfinity;
                for (var q = 1; q < len; q++)
                {
                    double s;
                    while (true)
                    {
                        var p = v[k];
                        s = ((At(q) + (double)q * q) - (At(p) + (double)p * p)) / (2.0 * q - 2.0 * p);
                        if (s <= z[k]) k--; else break;
                    }
                    k++; v[k] = q; z[k] = s; z[k + 1] = double.PositiveInfinity;
                }
                k = 0;
                for (var q = 0; q < len; q++)
                {
                    while (z[k + 1] < q) k++;
                    var p = v[k];
                    d[q] = (double)(q - p) * (q - p) + At(p);
                }
                for (var q = 0; q < len; q++)
                {
                    if (columns) dst[q * w + c] = d[q]; else dst[c * w + q] = d[q];
                }
            }
        }
    }
}
