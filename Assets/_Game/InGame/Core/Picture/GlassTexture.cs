using UnityEngine;

namespace Falcon.InGame.Core
{
    /// <summary>Kính nghệ thuật dạng vệt (wispy): R = mảng mây nhăn như giấy bạc vò, G = loang tông rất rộng. Tileable; shader dùng để sáng/tối cùng một màu mảnh.</summary>
    public static class GlassTexture
    {
        public static Texture2D Create(int size = 512, int seed = 7)
        {
            var px = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                // mảng mây nhăn như giấy bạc vò: nhiễu uốn mạnh + nếp gấp mềm, đẳng hướng, mép loang mềm
                var wu = u + 0.13f * (Fbm(u, v, 3, 3, seed + 11, 3, 0.5f) - 0.5f);
                var wv = v + 0.13f * (Fbm(u, v, 3, 3, seed + 17, 3, 0.5f) - 0.5f);
                var cloud = Fbm(wu, wv, 2, 2, seed, 3, 0.45f);
                var fold = 1f - Mathf.Abs(2f * Fbm(wu, wv, 3, 3, seed + 21, 2, 0.5f) - 1f);
                fold = Mathf.SmoothStep(0.30f, 0.95f, fold);
                var crinkle = Mathf.Clamp01((0.62f * cloud + 0.38f * fold - 0.22f) / 0.56f);
                var tone = Fbm(u, v, 2, 2, seed + 5, 2, 0.5f);          // loang tông rất rộng
                px[y * size + x] = new Color32(Enc(crinkle), Enc(tone), 128, 0);
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "GlassCrinkle",
            };
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // Kính tinh thể vỡ như csm: nhiều lớp tam giác chồng nhau, cạnh sắc; R = độ sáng mặt, G = gờ (không dùng), B = sắc ánh. Tileable
        public static Texture2D CreateFacets(int size = 1024, int cells = 4, int seed = 19)
        {
            var rnd = new System.Random(seed);
            var br = new float[size * size];
            var hu = new float[size * size];
            for (var i = 0; i < br.Length; i++) { br[i] = 0.5f; hu[i] = 0.5f; }
            const int Layers = 3;
            var Amp = new[] { 1.1f, 0.7f, 0.45f };
            for (var layer = 0; layer < Layers; layer++)
            {
                var count = 12 + layer * 8; // ít mặt, mặt to: kính mềm, không vỡ vụn
                var radius = (0.46f - layer * 0.12f) * size;
                for (var k = 0; k < count; k++)
                {
                    float cx = (float)rnd.NextDouble() * size, cy = (float)rnd.NextDouble() * size;
                    var p = new Vector2[3];
                    var baseAng = (float)rnd.NextDouble() * 6.2832f;
                    for (var j = 0; j < 3; j++)
                    {
                        var ang = baseAng + j * 2.094f + ((float)rnd.NextDouble() - 0.5f) * 1.6f;
                        var r = radius * (0.4f + 0.6f * (float)rnd.NextDouble());
                        p[j] = new Vector2(cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r);
                    }
                    var v = Mathf.Clamp01(0.5f + ((float)rnd.NextDouble() - 0.5f) * Amp[layer]);
                    var h = (float)rnd.NextDouble();
                    for (var oy = -1; oy <= 1; oy++)
                    for (var ox = -1; ox <= 1; ox++)
                        Fill(br, hu, size, p, ox * size, oy * size, v, h);
                }
            }
            var sbr = Blur(br, size, size / 40);
            var shu = Blur(hu, size, size / 40);
            for (var i = 0; i < br.Length; i++) { br[i] = Mathf.Lerp(br[i], sbr[i], 0.8f); hu[i] = Mathf.Lerp(hu[i], shu[i], 0.8f); } // mép mặt loang mềm, vẫn còn nếp nhẹ
            var px = new Color32[size * size];
            for (var i = 0; i < px.Length; i++) px[i] = new Color32(Enc(br[i]), 0, Enc(hu[i]), 255);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "GlassFacets" };
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // Làm mờ hộp 2 lượt, cuộn vòng theo tile
        private static float[] Blur(float[] src, int size, int radius)
        {
            var cur = src;
            for (var pass = 0; pass < 2; pass++)
            {
                var tmp = new float[cur.Length];
                var dst = new float[cur.Length];
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    float s = 0;
                    for (var k = -radius; k <= radius; k++) s += cur[y * size + (x + k + size) % size];
                    tmp[y * size + x] = s / (2 * radius + 1);
                }
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    float s = 0;
                    for (var k = -radius; k <= radius; k++) s += tmp[((y + k + size) % size) * size + x];
                    dst[y * size + x] = s / (2 * radius + 1);
                }
                cur = dst;
            }
            return cur;
        }

        // Tô tam giác có viền chống răng cưa ~1px, cuộn vòng theo tile
        private static void Fill(float[] br, float[] hu, int size, Vector2[] p, int dx, int dy, float v, float h)
        {
            var a = p[0] + new Vector2(dx, dy); var b = p[1] + new Vector2(dx, dy); var c = p[2] + new Vector2(dx, dy);
            var x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))) - 1);
            var x1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))) + 1);
            var y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))) - 1);
            var y1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))) + 1);
            if (x0 > x1 || y0 > y1) return;
            var area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (Mathf.Abs(area) < 4f) return;
            var sgn = Mathf.Sign(area);
            for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                var d = Mathf.Min(Edge(a, b, fx, fy, sgn), Mathf.Min(Edge(b, c, fx, fy, sgn), Edge(c, a, fx, fy, sgn)));
                var cov = Mathf.Clamp01(d + 0.5f);
                if (cov <= 0f) continue;
                var i = y * size + x;
                br[i] = Mathf.Lerp(br[i], v, cov);
                hu[i] = Mathf.Lerp(hu[i], h, cov);
            }
        }

        // Khoảng cách có dấu (pixel) từ điểm tới cạnh p-q, dương ở phía trong
        private static float Edge(Vector2 p, Vector2 q, float x, float y, float sgn)
        {
            var ex = q.x - p.x; var ey = q.y - p.y;
            return sgn * (ex * (y - p.y) - ey * (x - p.x)) / Mathf.Sqrt(ex * ex + ey * ey + 1e-6f);
        }

        private static byte Enc(float v) => (byte)(Mathf.Clamp01(v) * 255f);

        // Nhiễu giá trị tuần hoàn theo từng trục (fx, fy ô cơ sở), 4 octave: không lộ đường nối khi ghép tile
        private static float Fbm(float u, float v, int fx, int fy, int seed, int octaves, float persistence)
        {
            float sum = 0, amp = 0.5f, norm = 0f;
            for (var o = 0; o < octaves; o++)
            {
                var px = fx << o;
                var py = fy << o;
                sum += amp * Noise(Mathf.Repeat(u, 1f) * px, Mathf.Repeat(v, 1f) * py, px, py, seed + o * 31);
                norm += amp;
                amp *= persistence;
            }
            return sum / norm;
        }

        private static float Noise(float x, float y, int periodX, int periodY, int seed)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float V(int cx, int cy) => Hash(((cx % periodX) + periodX) % periodX, ((cy % periodY) + periodY) % periodY, seed);
            var top = Mathf.Lerp(V(ix, iy), V(ix + 1, iy), fx);
            var bot = Mathf.Lerp(V(ix, iy + 1), V(ix + 1, iy + 1), fx);
            return Mathf.Lerp(top, bot, fy);
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
