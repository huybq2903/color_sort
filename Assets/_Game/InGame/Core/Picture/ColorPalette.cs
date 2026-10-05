using System.Linq;
using UnityEngine;

namespace Falcon.InGame.Core
{
    /// <summary>Bảng 40 màu cố định, id = index, chỉ được thêm cuối: 23 màu đầu là bảng màu game (csm_levels), 17 màu sau bổ sung.</summary>
    public static class ColorPalette
    {
        private static readonly uint[] Hex =
        {
            0xF9241A, 0xF1621A, 0xFFE306, 0x029D5D, 0xB7D91D, 0x1CD8F2, 0x1837E5, 0x2FA6FF, 0x8A1FBB, 0xFD55C2,
            0xFFBBBB, 0xAB441A, 0xF2F4F4, 0x4C4C4C, 0xFFDFBE, 0xAF0A6F, 0x6AE0A7, 0xC148F7, 0x3AE96D, 0xFDAA13,
            0xAB8EFF, 0xD48CFA, 0xB7E0FC, 0x1A1A1A, 0x0D1B6B, 0x0B5A34, 0x5C2A12, 0x8A0F1C, 0x9A9A9A, 0xCFD3D6,
            0xE0A46C, 0xF6B48A, 0xD4871F, 0x0AA5A0, 0x7A8C1A, 0xFF8A7A, 0xFFF3A0, 0x4A1470, 0x3A6FB0, 0xC9F2C7,
        };

        public static readonly Color32[] Colors = Build();

        public static int Count => Colors.Length;

        private static readonly Vector3[] Lab = Colors.Select(ToLab).ToArray();

        // Màu bảng gần nhất theo CIE Lab (đen ra xám đậm chứ không ra xanh lá)
        public static int Nearest(Color32 c) => NearestLab(ToLab(c));

        // Như trên nhưng cho phép loại trừ các id đã dùng
        public static int NearestLab(Vector3 lab, System.Collections.Generic.ICollection<int> exclude = null)
        {
            var best = -1;
            var bd = float.MaxValue;
            for (var i = 0; i < Lab.Length; i++)
            {
                if (exclude != null && exclude.Contains(i)) continue;
                var d = (Lab[i] - lab).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            return best >= 0 ? best : NearestLab(lab);
        }

        public static Vector3 ToLab(Color32 c)
        {
            static float Lin(byte v)
            {
                var s = v / 255f;
                return s <= 0.04045f ? s / 12.92f : Mathf.Pow((s + 0.055f) / 1.055f, 2.4f);
            }
            static float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
            float r = Lin(c.r), g = Lin(c.g), b = Lin(c.b);
            var x = (0.4124f * r + 0.3576f * g + 0.1805f * b) / 0.95047f;
            var y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            var z = (0.0193f * r + 0.1192f * g + 0.9505f * b) / 1.08883f;
            return new Vector3(116f * F(y) - 16f, 500f * (F(x) - F(y)), 200f * (F(y) - F(z)));
        }

        public static Color32 Get(int id) => id >= 0 && id < Colors.Length ? Colors[id] : new Color32(255, 0, 255, 255);

        private static Color32[] Build()
        {
            var c = new Color32[Hex.Length];
            for (var i = 0; i < Hex.Length; i++)
                c[i] = new Color32((byte)(Hex[i] >> 16), (byte)(Hex[i] >> 8), (byte)Hex[i], 255);
            return c;
        }
    }
}
