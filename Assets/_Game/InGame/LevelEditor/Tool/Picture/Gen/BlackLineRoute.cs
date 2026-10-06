using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Ảnh viền đen đặc (kính màu): pixel đen là nét, mỗi ô kín giữa các nét là 1 mảnh; null nếu không phải kiểu này.</summary>
    internal static class BlackLineRoute
    {
        private const int DarkMax = 35, MinArea = 185, MinPieces = 15, EdgeInk = 6;
        private const int VeinWindow = 7, VeinContrast = 60, VeinMax = 120;
        private const int MinInkArea = 60, MaxRing = 16, GapReach = 8;
        private const float MaxLineHalfWidth = 3.2f;
        private const float RingFraction = 0.6f, MaxRingDepth = 0.05f, LineProbe = 8f;
        private const float MinCoverage = 0.55f;

        public static RegionMap Build(Color32[] px, Vector3[] lab, int w, int h)
        {
            var n = w * h;
            var free = new bool[n];
            var peak = new float[n];
            for (var i = 0; i < n; i++) peak[i] = Mathf.Max(px[i].r, Mathf.Max(px[i].g, px[i].b));
            var closed = ImageOps.Close(peak, w, h, VeinWindow); // gân mảnh bị pha màu kính nên không đủ đen: nét tối hơn hẳn vùng quanh cũng là nét
            var band = Bands(peak, w, h);
            for (var i = 0; i < n; i++) free[i] = peak[i] >= DarkMax && !(closed[i] - peak[i] > VeinContrast && peak[i] < VeinMax) && !band.Contains(i % w, i / w); // dải sát mép là nét: không có khung đen kín thì ô nền không nối nhau qua mép
            CloseSmallGaps(free, w, h); // kẽ hở nhỏ giữa đầu một nét và nét khác làm hai mảnh dính nhau: nối lại
            var faces = ImageOps.Components(free, w, h, false, out var count);
            var area = new int[count + 1];
            foreach (var l in faces) if (l > 0) area[l]++;
            int kept = 0, covered = 0;
            for (var i = 1; i <= count; i++)
                if (area[i] >= MinArea) { kept++; covered += area[i]; }
            if (kept < MinPieces || covered < MinCoverage * n) return null;
            for (var i = 0; i < n; i++) if (faces[i] > 0 && area[faces[i]] < MinArea) faces[i] = 0;
            FillCells(faces, free, w, h, band); // nét giữ nhãn 0 (khe giữa các mảnh); chỉ ô vụn bị bỏ và dải khung sát mép tranh được lấp về mảnh gần nhất
            ExtendLines(faces, band, w, h); // nét chạm dải khung kéo dài theo hướng đoạn cuối, cùng độ dày, tới mép tranh
            var ids = RegionOps.Compact(faces);
            var cols = RegionOps.FaceColors(faces, ids, lab, free);
            var map = new RegionMap { w = w, h = h, reg = faces, inked = true, gaps = true };
            for (var id = 1; id <= ids; id++) map.colors.Add(cols[id]);
            return map;
        }

        // Đầu nét (xương nét) cách nét khác không quá GapReach pixel theo hướng nét thì nối lại bằng đoạn nét mỏng (2px), free = false trên đoạn nối
        private static void CloseSmallGaps(bool[] free, int w, int h)
        {
            var n = w * h;
            var ink = new bool[n];
            for (var i = 0; i < n; i++) ink[i] = !free[i];
            var sk = Skeleton.Thin(ink, w, h);
            var before = (bool[])sk.Clone();
            var width = ImageOps.Edt(ink, w, h);
            var thick = new bool[n]; // đầu xương nằm trong khối đặc (đồng tử, mũi): không phải đầu nét, không nối
            for (var i = 0; i < n; i++) thick[i] = width[i] > MaxLineHalfWidth;
            Skeleton.CloseGaps(sk, w, h, GapReach, null, thick);
            var bridge = new bool[n];
            for (var i = 0; i < n; i++) bridge[i] = sk[i] && !before[i];
            bridge = ImageOps.Dilate3(bridge, w, h);
            for (var i = 0; i < n; i++) if (bridge[i]) free[i] = false;
        }

        // Lan nhãn mảnh sang pixel chưa có nhãn nhưng không phải nét thật (ô vụn bị bỏ, dải EdgeInk sát mép tranh)
        private static void FillCells(int[] faces, bool[] free, int w, int h, Band band)
        {
            var inkCells = ImageOps.Components(free.Select(b => !b).ToArray(), w, h, true, out var inkCount);
            var inkArea = new int[inkCount + 1];
            foreach (var l in inkCells) if (l > 0) inkArea[l]++;
            bool Fillable(int i) => free[i] || band.Filled(i % w, i / w) || (inkCells[i] > 0 && inkArea[inkCells[i]] < MinInkArea); // đốm nét lẻ không phải gân
            var q = new System.Collections.Generic.Queue<int>();
            for (var i = 0; i < faces.Length; i++) if (faces[i] > 0) q.Enqueue(i);
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int x = p % w, y = p / w;
                void Try(int n)
                {
                    if (faces[n] != 0 || !Fillable(n)) return;
                    faces[n] = faces[p];
                    q.Enqueue(n);
                }
                if (x > 0) Try(p - 1);
                if (x < w - 1) Try(p + 1);
                if (y > 0) Try(p - w);
                if (y < h - 1) Try(p + w);
            }
        }

        // Dải khung ở 4 cạnh (số pixel tính từ mép) và cạnh nào có đường khung đen thật
        private readonly struct Band
        {
            private readonly int _l, _r, _t, _b, _w, _h;
            private readonly bool _rl, _rr, _rt, _rb; // cạnh có đường khung đen thật
            public Band(int l, int r, int t, int b, int w, int h, bool rl, bool rr, bool rt, bool rb)
            {
                _l = l; _r = r; _t = t; _b = b; _w = w; _h = h;
                _rl = rl; _rr = rr; _rt = rt; _rb = rb;
            }
            public bool Contains(int x, int y) => x < _l || x >= _w - _r || y < _t || y >= _h - _b;
            public bool Kept(int x, int y) => (_rl && x < _l) || (_rr && x >= _w - _r) || (_rt && y < _t) || (_rb && y >= _h - _b); // dải của cạnh có khung: giữ làm nét
            public bool Filled(int x, int y) => Contains(x, y) && !Kept(x, y); // dải của cạnh không khung: lấp về mảnh gần nhất
        }

        // Độ dày dải khung từng cạnh: có đường khung đen chạy dọc cạnh (>= RingFraction chiều dài) trong vùng sát mép thì dải phủ hết đường đó (kể cả dải ngoài khung) và được giữ làm nét như các nét trong; cạnh không có khung dùng EdgeInk và được lấp
        private static Band Bands(float[] peak, int w, int h)
        {
            var maxD = Mathf.Max(EdgeInk + 1, Mathf.RoundToInt(MaxRingDepth * Mathf.Min(w, h)));
            (int depth, bool ring) Side(System.Func<int, float> frac)
            {
                var d = 0;
                while (d < maxD && frac(d) < RingFraction) d++;
                if (d >= maxD) return (EdgeInk, false);
                var start = d;
                while (d < maxD + MaxRing && frac(d) >= RingFraction) d++;
                return d - start > MaxRing ? (EdgeInk, false) : (d + 1, true); // có khung: dải đúng bằng độ dày khung (không nới tới EdgeInk), khung mảnh thì giữ mảnh
            }
            float Col(int x) { int c = 0, t = 0; for (var y = h / 10; y < h - h / 10; y++) { t++; if (peak[y * w + x] < DarkMax) c++; } return c / (float)t; }
            float Row(int y) { int c = 0, t = 0; for (var x = w / 10; x < w - w / 10; x++) { t++; if (peak[y * w + x] < DarkMax) c++; } return c / (float)t; }
            var l = Side(d => Col(d));
            var r = Side(d => Col(w - 1 - d));
            var t = Side(d => Row(d));
            var b = Side(d => Row(h - 1 - d));
            return new Band(l.depth, r.depth, t.depth, b.depth, w, h, l.ring, r.ring, t.ring, b.ring);
        }

        // Nét (khe) chạm mép tranh ở cạnh không có khung: kéo dài thẳng theo hướng đoạn cuối với cùng độ dày tới mép tranh
        private static void ExtendLines(int[] faces, Band band, int w, int h)
        {
            var n = w * h;
            var ink = new bool[n];
            for (var i = 0; i < n; i++) ink[i] = faces[i] == 0 && !band.Filled(i % w, i / w);
            var dist = ImageOps.Edt(ink, w, h);
            var contact = new bool[n];
            var inward = new Vector2[n]; // hướng từ pixel nét sang dải khung
            for (var i = 0; i < n; i++)
            {
                if (!ink[i]) continue;
                int x = i % w, y = i / w;
                for (var d = 0; d < 4; d++)
                {
                    int dx = d == 0 ? -1 : d == 1 ? 1 : 0, dy = d == 2 ? -1 : d == 3 ? 1 : 0;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h || !band.Filled(nx, ny)) continue;
                    contact[i] = true;
                    inward[i] += new Vector2(dx, dy);
                }
            }
            var groups = ImageOps.Components(contact, w, h, true, out var count);
            var members = new List<int>[count + 1];
            for (var i = 0; i < n; i++)
            {
                if (groups[i] <= 0) continue;
                (members[groups[i]] ??= new List<int>()).Add(i);
            }
            for (var g = 1; g <= count; g++)
            {
                var m = members[g];
                Vector2 c = Vector2.zero, to = Vector2.zero;
                float thick = 0f;
                int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                foreach (var i in m)
                {
                    int x = i % w, y = i / w;
                    c += new Vector2(x, y);
                    to += inward[i];
                    thick = Mathf.Max(thick, 2f * dist[i]);
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
                c /= m.Count;
                if (thick < 1.5f || to.sqrMagnitude < 0.01f) continue;
                if (Mathf.Max(maxX - minX, maxY - minY) + 1 > 3f * thick + 6f) continue; // đoạn dài chạy dọc khung: không phải đầu nét
                to.Normalize();
                var dir = LineDirection(ink, w, h, c, to, LineProbe);
                if (dir == Vector2.zero) continue;
                if (Vector2.Dot(dir, to) < 0f) dir = -dir;
                if (Vector2.Dot(dir, to) < 0.3f) continue; // nét chạy dọc dải khung: không kéo
                var r = thick * 0.5f;
                var ri = Mathf.CeilToInt(r);
                for (var t = 0f; t < w + h; t += 0.5f)
                {
                    var p = c + dir * t;
                    int px = Mathf.RoundToInt(p.x), py = Mathf.RoundToInt(p.y);
                    if (px < -ri || py < -ri || px >= w + ri || py >= h + ri) break;
                    for (var dy = -ri; dy <= ri; dy++)
                    for (var dx = -ri; dx <= ri; dx++)
                    {
                        int x = px + dx, y = py + dy;
                        if (x < 0 || y < 0 || x >= w || y >= h || dx * dx + dy * dy > r * r + 0.25f) continue;
                        if (band.Filled(x, y)) faces[y * w + x] = 0;
                    }
                }
            }
        }

        private static bool Behind(bool[] ink, int w, int x, int y, Vector2 c, Vector2 to, float rad)
        {
            var d = new Vector2(x, y) - c;
            return ink[y * w + x] && d.sqrMagnitude <= rad * rad && Vector2.Dot(d, to) <= 0.5f;
        }

        // Hướng chính của nét quanh điểm c: trục chính (PCA) của các pixel nét nằm phía trong (xa dải khung) trong bán kính rad; bán kính nhỏ để không lẫn nét khác cắt ngang
        private static Vector2 LineDirection(bool[] ink, int w, int h, Vector2 c, Vector2 to, float rad)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(c.x - rad)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(c.x + rad));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(c.y - rad)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(c.y + rad));
            double sx = 0, sy = 0, k = 0;
            for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
                if (Behind(ink, w, x, y, c, to, rad)) { sx += x; sy += y; k++; }
            if (k < 3) return Vector2.zero;
            double mx = sx / k, my = sy / k, cxx = 0, cxy = 0, cyy = 0;
            for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
                if (Behind(ink, w, x, y, c, to, rad))
                {
                    double dx = x - mx, dy = y - my;
                    cxx += dx * dx; cxy += dx * dy; cyy += dy * dy;
                }
            var ang = 0.5 * System.Math.Atan2(2 * cxy, cxx - cyy);
            return new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang));
        }
    }
}
