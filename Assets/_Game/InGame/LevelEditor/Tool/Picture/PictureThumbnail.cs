using Falcon.InGame.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Vẽ ảnh xem trước nhỏ của tranh (màu từng mảnh, khe đen) để hiện ở danh sách Recent.</summary>
    public static class PictureThumbnail
    {
        private static readonly Color32 Gap = new(20, 20, 28, 255);

        public static Texture2D Render(PictureProperty p, int maxSide = 256)
        {
            if (p == null || p.regions.Count == 0 || p.width <= 0 || p.height <= 0) return null;

            var map = RegionRaster.Rasterize(p);
            var scale = Mathf.Min(1f, maxSide / (float)Mathf.Max(map.w, map.h));
            var w = Mathf.Max(1, Mathf.RoundToInt(map.w * scale));
            var h = Mathf.Max(1, Mathf.RoundToInt(map.h * scale));
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                var sy = Mathf.Min(map.h - 1, (int)((y + 0.5f) / scale));
                for (var x = 0; x < w; x++)
                {
                    var sx = Mathf.Min(map.w - 1, (int)((x + 0.5f) / scale));
                    var r = map.reg[sy * map.w + sx];
                    px[y * w + x] = r > 0 ? ColorPalette.Get(map.colors[r]) : Gap;
                }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
