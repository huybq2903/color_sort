using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Đặt ảnh vào khung chữ nhật: vừa khung (phần dư là nền) hoặc phủ kín (cắt phần thừa).</summary>
    public static class FrameLayout
    {
        // Trả vị trí ảnh trong khung (0..1, y = 0 ở dưới) và phần ảnh được dùng (uv 0..1)
        public static Rect Place(int srcW, int srcH, int frameW, int frameH, bool cover, out Rect uv)
        {
            var sa = srcW / (float)srcH;
            var fa = frameW / (float)frameH;
            uv = new Rect(0f, 0f, 1f, 1f);
            if (Mathf.Approximately(sa, fa)) return new Rect(0f, 0f, 1f, 1f);
            var srcWider = sa > fa;
            if (cover)
            {
                var keep = srcWider ? fa / sa : sa / fa;
                uv = srcWider ? new Rect((1f - keep) * 0.5f, 0f, keep, 1f) : new Rect(0f, (1f - keep) * 0.5f, 1f, keep);
                return new Rect(0f, 0f, 1f, 1f);
            }
            var fill = srcWider ? fa / sa : sa / fa;
            return srcWider ? new Rect(0f, (1f - fill) * 0.5f, 1f, fill) : new Rect((1f - fill) * 0.5f, 0f, fill, 1f);
        }

        // Cạnh dài của canvas = longSide, giữ tỉ lệ khung
        public static void CanvasSize(int frameW, int frameH, int longSide, out int w, out int h)
        {
            var k = longSide / (float)Mathf.Max(frameW, frameH);
            w = Mathf.Max(1, Mathf.RoundToInt(frameW * k));
            h = Mathf.Max(1, Mathf.RoundToInt(frameH * k));
        }

        // uv (0..1) trong vùng crop của ảnh → hình chữ nhật pixel
        public static RectInt SourceRect(RectInt crop, Rect uv) => new(
            crop.x + Mathf.RoundToInt(uv.x * crop.width),
            crop.y + Mathf.RoundToInt(uv.y * crop.height),
            Mathf.Max(1, Mathf.RoundToInt(uv.width * crop.width)),
            Mathf.Max(1, Mathf.RoundToInt(uv.height * crop.height)));
    }
}
