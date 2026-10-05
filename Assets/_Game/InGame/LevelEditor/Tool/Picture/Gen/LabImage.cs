using System;
using Falcon.InGame.Core;
using System.Threading.Tasks;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Ảnh trong không gian Lab và bộ lọc song phương giữ biên.</summary>
    public static class LabImage
    {
        // Oklab (L, a, b) nhân OkScale: khoảng cách đều hơn Lab ở vùng xanh lam; dùng để tách/gộp vùng (màu bảng vẫn so bằng Lab)
        public const float OkScale = 140f;

        public static Vector3[] Oklab(Color32[] px)
        {
            static float Lin(byte v)
            {
                var s = v / 255f;
                return s <= 0.04045f ? s / 12.92f : Mathf.Pow((s + 0.055f) / 1.055f, 2.4f);
            }
            var res = new Vector3[px.Length];
            for (var i = 0; i < px.Length; i++)
            {
                float r = Lin(px[i].r), g = Lin(px[i].g), b = Lin(px[i].b);
                var l = Mathf.Pow(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b, 1f / 3f);
                var m = Mathf.Pow(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b, 1f / 3f);
                var s = Mathf.Pow(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b, 1f / 3f);
                res[i] = new Vector3(0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
                    1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
                    0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s) * OkScale;
            }
            return res;
        }

        public static Vector3[] From(Color32[] px)
        {
            var lab = new Vector3[px.Length];
            for (var i = 0; i < px.Length; i++) lab[i] = ColorPalette.ToLab(px[i]);
            return lab;
        }

        // Trung bình có trọng số theo khoảng cách không gian và độ giống màu; pixel ngoài valid không tham gia
        public static Vector3[] Bilateral(Vector3[] lab, bool[] valid, int w, int h, int radius = 2, float sigmaColor = 12f)
        {
            var spatial = new float[(2 * radius + 1) * (2 * radius + 1)];
            for (var dy = -radius; dy <= radius; dy++)
            for (var dx = -radius; dx <= radius; dx++)
                spatial[(dy + radius) * (2 * radius + 1) + dx + radius] = Mathf.Exp(-(dx * dx + dy * dy) / (2f * radius * radius));

            var inv2 = 1f / (2f * sigmaColor * sigmaColor);
            var o = (Vector3[])lab.Clone();
            Parallel.For(0, h, y =>
            {
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (!valid[i]) continue;
                var c = lab[i];
                var sum = Vector3.zero;
                var wsum = 0f;
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var yy = y + dy;
                    if (yy < 0 || yy >= h) continue;
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        var xx = x + dx;
                        if (xx < 0 || xx >= w) continue;
                        var j = yy * w + xx;
                        if (!valid[j]) continue;
                        var d = lab[j] - c;
                        var wt = spatial[(dy + radius) * (2 * radius + 1) + dx + radius] * Mathf.Exp(-(d.x * d.x + d.y * d.y + d.z * d.z) * inv2);
                        sum += lab[j] * wt;
                        wsum += wt;
                    }
                }
                o[i] = sum / wsum;
            }
            });
            return o;
        }
    }
}
