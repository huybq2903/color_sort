using UnityEngine;

namespace Falcon.InGame.Core
{
    /// <summary>Giá trị cát của mảnh theo diện tích (quy luật đo từ 100 level game): ≈ 0,0107 mỗi pixel trên tranh 816², làm tròn bậc 50, trong 50..300.</summary>
    public static class PieceValue
    {
        public const int Step = 50, Min = 50, Max = 300;
        private const float PerFraction = 7125f; // 0,0107 × 816²: giá trị thô của mảnh chiếm toàn bộ tranh

        public static int FromAreaFraction(float fraction) =>
            Mathf.Clamp(Mathf.RoundToInt(fraction * PerFraction / Step) * Step, Min, Max);

        // Làm tròn về bậc 50 và kẹp trong 50..300
        public static int Clamp(int value) => Mathf.Clamp(Mathf.RoundToInt(value / (float)Step) * Step, Min, Max);

        // Giá trị gợi ý theo diện tích (đa giác trừ lỗ, theo toạ độ lưu)
        public static int Suggest(PictureProperty p, RegionData r)
        {
            var unit = Mathf.Max(1, p.unit);
            var total = (double)p.width * unit * p.height * unit;
            if (total <= 0) return r.value;
            var a = Area(r.points);
            if (r.holes != null) foreach (var h in r.holes) a -= Area(h);
            return FromAreaFraction((float)(a / total));
        }

        // Tính lại giá trị các mảnh chưa đặt tay
        public static void Assign(PictureProperty p)
        {
            if (p == null) return;
            foreach (var r in p.regions)
                if (!r.valueManual) r.value = Suggest(p, r);
        }

        private static double Area(int[] pts)
        {
            double s = 0;
            var n = pts.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++) s += (double)pts[j * 2] * pts[i * 2 + 1] - (double)pts[i * 2] * pts[j * 2 + 1];
            return System.Math.Abs(s) * 0.5;
        }
    }
}
