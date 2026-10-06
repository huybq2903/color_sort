using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.InGame.Core;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Tạo tranh khởi đầu (1 mảnh phủ khung) để dựng tiếp bằng công cụ tay; thuật toán tách ảnh sẽ viết lại.</summary>
    public static class PictureGenerator
    {
        public const float Epsilon = 0.75f;
        private const int FlatGapRadius = 1; // khe giữa các mảnh ảnh không nét: 2 + 2 × radius pixel ở lưới workSize
        private const int IslandMaxArea = 150, IslandReach = 7; // cụm tách rời nhỏ hơn 150px và có mảnh khác trong 7px (lưới ×3)
        private const int GapTrim = 2; // pixel bỏ mỗi bên nét dày ở lưới ×3 (≈0,7px ở lưới 640); 0 = tắt
        private const float SigmaPerLevel = 0.57f; // mức 3 ở lưới x3 ~ sigma 5 mẫu
        internal const int TidyUnit = 4;
        private const int MaxPieces = 100; // game có tối đa 100 mảnh mỗi tranh
        private const float SpeckFraction = 0.0004f; // mảnh nhỏ hơn 0,04% diện tích tranh là chấm li ti, bị gộp
        internal const float TidyStep = 6f; // khoảng cách lấy mẫu spline (theo lưới)

        internal static float Sigma(GenSettings s) => s.curveSmooth * SigmaPerLevel * Math.Max(1, s.smoothScale);

        public static PictureProperty Generate(Texture2D src, GenSettings s)
        {
            var px = Compose(src, s, out var w, out var h, out var bgId);
            return Generate(px, w, h, s, bgId);
        }

        // Ảnh → vùng theo kiểu ảnh (nét chì/đen hoặc phẳng) → làm mượt → polygon dùng chung biên; chạy được ở luồng nền
        public static PictureProperty Generate(Color32[] px, int w, int h, GenSettings s, int bgHint = -1)
        {
            var lab = LabImage.From(px);
            var map = BlackLineRoute.Build(px, lab, w, h) ?? InkRoute.Build(px, lab, w, h);
            if (map == null)
            {
                map = FlatRoute.Build(px, lab, w, h);
                if (map != null)
                {
                    PieceSize.RemoveSpecks(map, SpeckFraction); // hạt nhỏ gộp trước khi khoét khe: để khe co chúng rồi mới gộp thì vòng khe của hạt còn lại và nhập vào đường khe gần đó thành chỗ nhô
                    RegionOps.KeepFrame(map, px); // khung màu đồng nhất quanh ảnh thành viền như ảnh có viền
                    RegionOps.CarveGaps(map, FlatGapRadius); // ảnh không nét: khe cố định giữa các mảnh cho giống tranh có viền
                }
            }
            if (map == null) return null;
            var scale = Math.Max(1, s.smoothScale);
            if (scale > 1) map = RegionSmoother.Upscale(map, scale);
            if (map.synthetic) RegionOps.FillEmptyBlobs(map);
            RegionOps.TrimGaps(map, GapTrim * Math.Max(1, scale) / 3); // nét giữ độ mảnh như ảnh gốc: vòng làm mượt và phóng lưới làm nét phình ra
            RegionOps.AbsorbIslands(map, IslandMaxArea, IslandReach); // vụn tách rời cùng nhãn (thường ở góc/mép): về mảnh gần nhất thay vì dính vào mảnh sai
            PieceSize.RemoveSpecks(map, SpeckFraction);
            PieceSize.LimitCount(map, MaxPieces);
            PieceSize.EnsureTextFits(map); // mảnh nhỏ nhất vẫn phải chứa được chữ số (làm sau khi phóng: làm trước khi phóng khiến dải mỏng bị tô nhầm màu nền)
            return BuildPicture(map, s); // viền chì cùng 1 độ dày mặc định, dù nét trong ảnh dày mỏng khác nhau
        }

        internal static PictureProperty BuildPicture(RegionMap map, GenSettings s)
        {
            var p = new PictureProperty { width = map.w, height = map.h, unit = TidyUnit, gen = s.Clone() };
            p.gen.inkGaps = map.gaps;
            var polys = BoundaryGraph.Build(map, Sigma(s), TidyUnit, TidyStep, s.fitTolerance, out var holes);
            for (var id = 1; id < polys.Length; id++)
                if (polys[id] != null) p.regions.Add(new RegionData { points = polys[id], colorId = map.colors[id], holes = holes[id] });
            p.ClampToFrame();
            PieceValue.Assign(p);
            return p.regions.Count > 0 ? p : null;
        }

        // Biên pixel → polygon lưu trong level: làm mượt giữ góc (curveSmooth > 0) hoặc chỉ giảm đỉnh; null nếu suy biến
        internal static int[] Polygon(List<Vector2Int> outer, GenSettings s, int unit)
        {
            if (outer.Count < 3) return null;
            var poly = new List<Vector2Int>();
            IEnumerable<Vector2> src = s.curveSmooth > 0f
                ? ContourSmoother.Smooth(outer, Sigma(s), resampleStep: unit > 1 ? TidyStep : 0f, fitTolerance: unit > 1 ? s.fitTolerance : 0f)
                : RegionTracer.Simplify(outer, Epsilon).Select(v => (Vector2)v);
            foreach (var v in src)
            {
                var r = Vector2Int.RoundToInt(v * unit);
                if (poly.Count == 0 || poly[^1] != r) poly.Add(r);
            }
            if (poly.Count > 1 && poly[0] == poly[^1]) poly.RemoveAt(poly.Count - 1);
            if (poly.Count < 3 || RegionTracer.Area2(poly) < 2L * unit * unit) return null;
            var pts = new int[poly.Count * 2];
            for (var i = 0; i < poly.Count; i++)
            {
                pts[i * 2] = poly[i].x;
                pts[i * 2 + 1] = poly[i].y;
            }
            return pts;
        }

        // Khung bao các pixel đặc (alpha > 128) cộng lề nhỏ; ảnh không có vùng trong suốt hoặc trống thì trả cả ảnh
        public static RectInt OpaqueBounds(Texture2D src)
        {
            int w = src.width, h = src.height;
            var px = src.GetPixels32();
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                if (px[y * w + x].a <= 128) continue;
                if (x < x0) x0 = x;
                if (x > x1) x1 = x;
                if (y < y0) y0 = y;
                if (y > y1) y1 = y;
            }
            if (x1 < 0) return new RectInt(0, 0, w, h);
            if (x0 == 0 && y0 == 0 && x1 == w - 1 && y1 == h - 1) // không có vùng trong suốt: chủ thể nhỏ trên nền đồng màu thì cắt theo chủ thể
                (x0, y0, x1, y1) = SubjectBounds(px, w, h);
            var pad = Mathf.Max(1, Mathf.RoundToInt(0.03f * Mathf.Max(x1 - x0 + 1, y1 - y0 + 1)));
            x0 = Mathf.Max(0, x0 - pad);
            y0 = Mathf.Max(0, y0 - pad);
            x1 = Mathf.Min(w - 1, x1 + pad);
            y1 = Mathf.Min(h - 1, y1 + pad);
            return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        // Đặt ảnh (đã cắt lề trong suốt) vào khung frameW:frameH, phần dư và phần trong suốt là màu nền; canvas luôn đặc
        public static Color32[] Compose(Texture2D src, GenSettings s, out int w, out int h) => Compose(src, s, out w, out h, out _);

        // bgId: id màu nền trong bảng nếu nền là màu bảng (để cắt tia đúng vùng nền), -1 nếu không
        public static Color32[] Compose(Texture2D src, GenSettings s, out int w, out int h, out int bgId)
        {
            var crop = OpaqueBounds(src);
            FrameLayout.CanvasSize(s.frameW, s.frameH, s.workSize, out w, out h);
            var place = FrameLayout.Place(crop.width, crop.height, s.frameW, s.frameH, s.fitCover, out var uv);
            var bg = Background(src, crop, s.bgColorId);
            var near = ColorPalette.Nearest(bg);
            bgId = ColorPalette.Get(near).Equals(bg) ? near : -1;
            src.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                if (u < place.xMin || u >= place.xMax || v < place.yMin || v >= place.yMax) { px[y * w + x] = bg; continue; }
                float su = uv.x + (u - place.x) / place.width * uv.width, sv = uv.y + (v - place.y) / place.height * uv.height;
                var t = src.GetPixelBilinear((crop.x + su * crop.width) / src.width, (crop.y + sv * crop.height) / src.height);
                px[y * w + x] = new Color32(
                    (byte)Mathf.RoundToInt(Mathf.Lerp(bg.r, t.r * 255f, t.a)),
                    (byte)Mathf.RoundToInt(Mathf.Lerp(bg.g, t.g * 255f, t.a)),
                    (byte)Mathf.RoundToInt(Mathf.Lerp(bg.b, t.b * 255f, t.a)), 255);
            }
            return px;
        }

        // Nền chọn tay theo bảng màu; tự động: ảnh có vùng trong suốt → màu sáng trong bảng xa màu chủ thể nhất, ảnh đặc → màu trung bình viền ảnh
        private static Color32 Background(Texture2D src, RectInt crop, int bgColorId)
        {
            if (bgColorId >= 0) return ColorPalette.Get(Mathf.Min(bgColorId, ColorPalette.Count - 1));
            var px = src.GetPixels32();
            long r = 0, g = 0, b = 0, n = 0;
            var used = new int[ColorPalette.Count];
            long sn = 0;
            var transparent = false;
            for (var y = crop.yMin; y < crop.yMax; y++)
            for (var x = crop.xMin; x < crop.xMax; x++)
            {
                var c = px[y * src.width + x];
                if (c.a <= 128) { transparent = true; continue; }
                if (((x + y) & 1) == 0) { used[ColorPalette.Nearest(c)]++; sn++; }
                if (x != crop.xMin && x != crop.xMax - 1 && y != crop.yMin && y != crop.yMax - 1) continue;
                r += c.r; g += c.g; b += c.b; n++;
            }
            if (transparent || n == 0) return DistinctBackground(used, sn);
            return new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }

        // Màu nền = màu sáng trong bảng xa nhất mọi màu chủ thể đang dùng (≥0.5% pixel), để chủ thể không bao giờ cùng màu với nền
        private static Color32 DistinctBackground(int[] used, long total)
        {
            var subject = new List<Vector3>();
            for (var i = 0; i < used.Length; i++)
                if (total > 0 && used[i] >= 0.005f * total) subject.Add(ColorPalette.ToLab(ColorPalette.Get(i)));
            var best = 0;
            var bestD = -1f;
            for (var i = 0; i < ColorPalette.Count; i++)
            {
                var l = ColorPalette.ToLab(ColorPalette.Get(i));
                if (l.x < 70f || new Vector2(l.y, l.z).magnitude > 30f) continue; // nền nhạt nhẹ, không rực
                var d = float.MaxValue;
                foreach (var v in subject) d = Mathf.Min(d, Vector3.Distance(l, v));
                if (d > bestD) { bestD = d; best = i; }
            }
            return ColorPalette.Get(best);
        }

        private const int BorderDiff = 70;
        private const float MinSubjectFraction = 0.6f; // bbox chủ thể phải nhỏ hơn 60% ảnh thì mới cắt

        // Bbox các pixel khác màu viền ảnh (nền đồng màu); không có nền đồng màu thì trả cả ảnh
        private static (int x0, int y0, int x1, int y1) SubjectBounds(Color32[] px, int w, int h)
        {
            long r = 0, g = 0, b = 0, n = 0;
            void Add(Color32 c) { r += c.r; g += c.g; b += c.b; n++; }
            for (var x = 0; x < w; x++) { Add(px[x]); Add(px[(h - 1) * w + x]); }
            for (var y = 1; y < h - 1; y++) { Add(px[y * w]); Add(px[y * w + w - 1]); }
            int mr = (int)(r / n), mg = (int)(g / n), mb = (int)(b / n);
            int dev = 0, total = 0;
            for (var x = 0; x < w; x++) { dev += Diff(px[x], mr, mg, mb) > BorderDiff ? 1 : 0; total++; }
            if (dev > 0.1f * total) return (0, 0, w - 1, h - 1); // viền không đồng màu: không có nền để tách
            // đếm pixel khác nền theo cột/hàng, bỏ rìa 1.5% (vết quét) và các chấm lẻ
            var cols = new int[w];
            var rows = new int[h];
            int mx = Mathf.Max(1, w * 3 / 200), my = Mathf.Max(1, h * 3 / 200);
            for (var y = my; y < h - my; y++)
            for (var x = mx; x < w - mx; x++)
            {
                if (Diff(px[y * w + x], mr, mg, mb) <= BorderDiff) continue;
                cols[x]++;
                rows[y]++;
            }
            int cmin = Mathf.Max(3, h / 250), rmin = Mathf.Max(3, w / 250);
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (var x = 0; x < w; x++) if (cols[x] >= cmin) { x0 = Mathf.Min(x0, x); x1 = x; }
            for (var y = 0; y < h; y++) if (rows[y] >= rmin) { y0 = Mathf.Min(y0, y); y1 = y; }
            if (x1 < 0 || (long)(x1 - x0 + 1) * (y1 - y0 + 1) > MinSubjectFraction * w * h) return (0, 0, w - 1, h - 1);
            return (x0, y0, x1, y1);
        }

        private static int Diff(Color32 c, int r, int g, int b) => Mathf.Abs(c.r - r) + Mathf.Abs(c.g - g) + Mathf.Abs(c.b - b);
    }
}
