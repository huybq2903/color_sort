using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Hình xem trước từng kiểu chia mảnh: chạy chính thuật toán chia trên một hình mẫu nhỏ, nên hình luôn khớp kết quả thật.</summary>
    internal static class PatternThumbs
    {
        private const int Size = 96, Cells = 9;
        private static Texture2D[] _tex;

        // Hình của kiểu pattern (SubdividePattern); Auto không có hình, trả null
        public static Texture2D Get(int pattern)
        {
            _tex ??= Build();
            return pattern > 0 && pattern < _tex.Length ? _tex[pattern] : null;
        }

        private static Texture2D[] Build()
        {
            var tex = new Texture2D[Subdivide.PatternLabels.Length];
            var map = new RegionMap { w = Size, h = Size, reg = new int[Size * Size] };
            var pix = new List<int>();
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                float dx = x - Size / 2f, dy = y - Size / 2f;
                var th = Mathf.Atan2(dy, dx);
                var radius = 0.43f * Size * (1f + 0.12f * Mathf.Sin(3f * th + 0.5f) + 0.06f * Mathf.Sin(5f * th + 1.3f));
                if (Mathf.Sqrt(dx * dx + dy * dy) >= radius) continue;
                map.reg[y * Size + x] = 1;
                pix.Add(y * Size + x);
            }
            for (var p = 1; p < tex.Length; p++)
            {
                var cell = Subdivide.FlowAssign(map, pix, Cells, (SubdividePattern)p, out _);
                tex[p] = Render(map, pix, cell);
            }
            return tex;
        }

        private static Texture2D Render(RegionMap map, List<int> pix, int[] cell)
        {
            var owner = new int[Size * Size];
            for (var i = 0; i < owner.Length; i++) owner[i] = -1;
            for (var i = 0; i < pix.Count; i++) owner[pix[i]] = cell != null ? cell[i] : 0;
            var bg = new Color32(38, 38, 38, 255);
            var ink = new Color32(14, 14, 20, 255);
            var colors = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var i = y * Size + x;
                if (owner[i] < 0) { colors[i] = bg; continue; }
                var edge = false;
                for (var d = 0; d < 4 && !edge; d++)
                {
                    int nx = x + (d == 0 ? -1 : d == 1 ? 1 : 0), ny = y + (d == 2 ? -1 : d == 3 ? 1 : 0);
                    edge = nx < 0 || ny < 0 || nx >= Size || ny >= Size || owner[ny * Size + nx] != owner[i];
                }
                colors[i] = edge ? ink : Pastel(owner[i]);
            }
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            t.SetPixels32(colors);
            t.Apply();
            return t;
        }

        private static Color32 Pastel(int i) => Color.HSVToRGB(i * 0.381966f % 1f, 0.38f, 0.95f);
    }
}
