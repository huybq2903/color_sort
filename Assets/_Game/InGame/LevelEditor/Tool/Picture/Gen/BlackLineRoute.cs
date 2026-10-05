using System.Linq;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Ảnh viền đen đặc (kính màu): pixel đen là nét, mỗi ô kín giữa các nét là 1 mảnh; null nếu không phải kiểu này.</summary>
    internal static class BlackLineRoute
    {
        private const int DarkMax = 35, MinArea = 185, MinPieces = 15, EdgeInk = 6;
        private const float MinCoverage = 0.55f;

        public static RegionMap Build(Color32[] px, Vector3[] lab, int w, int h)
        {
            var n = w * h;
            var free = new bool[n];
            for (var i = 0; i < n; i++) free[i] = Mathf.Max(px[i].r, Mathf.Max(px[i].g, px[i].b)) >= DarkMax && i % w >= EdgeInk && i % w < w - EdgeInk && i / w >= EdgeInk && i / w < h - EdgeInk; // dải sát mép là nét: không có khung đen kín thì ô nền không nối nhau qua mép
            var faces = ImageOps.Components(free, w, h, false, out var count);
            var area = new int[count + 1];
            foreach (var l in faces) if (l > 0) area[l]++;
            int kept = 0, covered = 0;
            for (var i = 1; i <= count; i++)
                if (area[i] >= MinArea) { kept++; covered += area[i]; }
            if (kept < MinPieces || covered < MinCoverage * n) return null;
            for (var i = 0; i < n; i++) if (faces[i] > 0 && area[faces[i]] < MinArea) faces[i] = 0;
            ImageOps.FillNearest(faces, w, h); // nét và chấm vụn về mảnh gần nhất để mảnh phủ kín khung
            var ids = RegionOps.Compact(faces);
            var cols = RegionOps.FaceColors(faces, ids, lab, free);
            var map = new RegionMap { w = w, h = h, reg = faces, inked = true };
            for (var id = 1; id <= ids; id++) map.colors.Add(cols[id]);
            return map;
        }
    }
}
