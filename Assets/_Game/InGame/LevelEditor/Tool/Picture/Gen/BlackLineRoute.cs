using System.Linq;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Ảnh viền đen đặc (kính màu): pixel đen là nét, mỗi ô kín giữa các nét là 1 mảnh; null nếu không phải kiểu này.</summary>
    internal static class BlackLineRoute
    {
        private const int DarkMax = 35, MinArea = 185, MinPieces = 15, EdgeInk = 6;
        private const int VeinWindow = 7, VeinContrast = 60, VeinMax = 120;
        private const int MinInkArea = 60;
        private const float MinCoverage = 0.55f;

        public static RegionMap Build(Color32[] px, Vector3[] lab, int w, int h)
        {
            var n = w * h;
            var free = new bool[n];
            var peak = new float[n];
            for (var i = 0; i < n; i++) peak[i] = Mathf.Max(px[i].r, Mathf.Max(px[i].g, px[i].b));
            var closed = ImageOps.Close(peak, w, h, VeinWindow); // gân mảnh bị pha màu kính nên không đủ đen: nét tối hơn hẳn vùng quanh cũng là nét
            for (var i = 0; i < n; i++) free[i] = peak[i] >= DarkMax && !(closed[i] - peak[i] > VeinContrast && peak[i] < VeinMax) && i % w >= EdgeInk && i % w < w - EdgeInk && i / w >= EdgeInk && i / w < h - EdgeInk; // dải sát mép là nét: không có khung đen kín thì ô nền không nối nhau qua mép
            var faces = ImageOps.Components(free, w, h, false, out var count);
            var area = new int[count + 1];
            foreach (var l in faces) if (l > 0) area[l]++;
            int kept = 0, covered = 0;
            for (var i = 1; i <= count; i++)
                if (area[i] >= MinArea) { kept++; covered += area[i]; }
            if (kept < MinPieces || covered < MinCoverage * n) return null;
            for (var i = 0; i < n; i++) if (faces[i] > 0 && area[faces[i]] < MinArea) faces[i] = 0;
            FillCells(faces, free, w, h); // nét giữ nhãn 0 (khe giữa các mảnh); chỉ ô vụn bị bỏ và dải mép được lấp về mảnh gần nhất
            var ids = RegionOps.Compact(faces);
            var cols = RegionOps.FaceColors(faces, ids, lab, free);
            var map = new RegionMap { w = w, h = h, reg = faces, inked = true, gaps = true };
            for (var id = 1; id <= ids; id++) map.colors.Add(cols[id]);
            return map;
        }

        // Lan nhãn mảnh sang pixel chưa có nhãn nhưng không phải nét thật (ô vụn bị bỏ, dải EdgeInk sát mép tranh)
        private static void FillCells(int[] faces, bool[] free, int w, int h)
        {
            var inkCells = ImageOps.Components(free.Select(b => !b).ToArray(), w, h, true, out var inkCount);
            var inkArea = new int[inkCount + 1];
            foreach (var l in inkCells) if (l > 0) inkArea[l]++;
            bool Fillable(int i) => free[i] || i % w < EdgeInk || i % w >= w - EdgeInk || i / w < EdgeInk || i / w >= h - EdgeInk || (inkCells[i] > 0 && inkArea[inkCells[i]] < MinInkArea); // đốm nét lẻ không phải gân
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
    }
}
