using System.Collections.Generic;
using UnityEngine;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Bảng màu kề nhau học từ 100 level game (23 màu game, id 0..22): chọn màu cho ô mới theo xác suất kề thực tế.</summary>
    public static class ColorAdjacency
    {
        private static readonly int[][] Counts =
        {
            new[] { 134, 69, 168, 104, 156, 17, 120, 139, 95, 137, 55, 31, 141, 16, 56, 36, 7, 13, 6, 193, 13, 20, 19 },
            new[] { 69, 46, 163, 87, 79, 25, 90, 108, 62, 60, 28, 28, 106, 18, 28, 4, 5, 3, 2, 155, 13, 13, 20 },
            new[] { 168, 163, 165, 93, 143, 59, 245, 257, 97, 147, 63, 63, 185, 27, 82, 13, 15, 25, 7, 357, 20, 26, 32 },
            new[] { 104, 87, 93, 64, 299, 19, 103, 118, 78, 72, 43, 61, 45, 5, 42, 7, 67, 3, 14, 101, 7, 18, 13 },
            new[] { 156, 79, 143, 299, 117, 42, 159, 202, 94, 111, 66, 73, 103, 2, 37, 3, 44, 16, 11, 128, 38, 32, 28 },
            new[] { 17, 25, 59, 19, 42, 23, 78, 43, 13, 51, 48, 26, 58, 0, 16, 2, 7, 10, 4, 35, 2, 18, 46 },
            new[] { 120, 90, 245, 103, 159, 78, 87, 509, 107, 155, 68, 62, 176, 17, 33, 7, 9, 17, 5, 125, 26, 14, 68 },
            new[] { 139, 108, 257, 118, 202, 43, 509, 156, 125, 195, 77, 56, 216, 7, 30, 3, 20, 21, 13, 178, 14, 14, 132 },
            new[] { 95, 62, 97, 78, 94, 13, 107, 125, 31, 102, 33, 24, 72, 4, 30, 3, 2, 2, 1, 78, 21, 23, 16 },
            new[] { 137, 60, 147, 72, 111, 51, 155, 195, 102, 77, 122, 31, 112, 3, 42, 1, 14, 13, 4, 180, 11, 23, 33 },
            new[] { 55, 28, 63, 43, 66, 48, 68, 77, 33, 122, 40, 50, 87, 9, 27, 0, 17, 12, 1, 100, 3, 22, 14 },
            new[] { 31, 28, 63, 61, 73, 26, 62, 56, 24, 31, 50, 77, 64, 7, 60, 3, 9, 6, 0, 92, 9, 17, 28 },
            new[] { 141, 106, 185, 45, 103, 58, 176, 216, 72, 112, 87, 64, 127, 23, 76, 17, 6, 13, 9, 147, 11, 16, 70 },
            new[] { 16, 18, 27, 5, 2, 0, 17, 7, 4, 3, 9, 7, 23, 9, 9, 3, 0, 0, 0, 20, 3, 2, 1 },
            new[] { 56, 28, 82, 42, 37, 16, 33, 30, 30, 42, 27, 60, 76, 9, 45, 4, 15, 8, 2, 63, 6, 18, 26 },
            new[] { 36, 4, 13, 7, 3, 2, 7, 3, 3, 1, 0, 3, 17, 3, 4, 4, 0, 2, 0, 7, 0, 6, 5 },
            new[] { 7, 5, 15, 67, 44, 7, 9, 20, 2, 14, 17, 9, 6, 0, 15, 0, 29, 0, 0, 14, 0, 3, 12 },
            new[] { 13, 3, 25, 3, 16, 10, 17, 21, 2, 13, 12, 6, 13, 0, 8, 2, 0, 7, 3, 16, 6, 4, 0 },
            new[] { 6, 2, 7, 14, 11, 4, 5, 13, 1, 4, 1, 0, 9, 0, 2, 0, 0, 3, 11, 3, 0, 0, 4 },
            new[] { 193, 155, 357, 101, 128, 35, 125, 178, 78, 180, 100, 92, 147, 20, 63, 7, 14, 16, 3, 162, 18, 31, 21 },
            new[] { 13, 13, 20, 7, 38, 2, 26, 14, 21, 11, 3, 9, 11, 3, 6, 0, 0, 6, 0, 18, 14, 12, 4 },
            new[] { 20, 13, 26, 18, 32, 18, 14, 14, 23, 23, 22, 17, 16, 2, 18, 6, 3, 4, 0, 31, 12, 11, 12 },
            new[] { 19, 20, 32, 13, 28, 46, 68, 132, 16, 33, 14, 28, 70, 1, 26, 5, 12, 0, 4, 21, 4, 12, 30 },
        };

        private const int Known = 23;

        // Màu ≥ 23 (bổ sung) tra bảng bằng màu game gần nhất
        private static int Anchor(int colorId)
        {
            if (colorId < Known) return colorId;
            return Falcon.InGame.Core.ColorPalette.NearestLab(Falcon.InGame.Core.ColorPalette.ToLab(Falcon.InGame.Core.ColorPalette.Get(colorId)), new List<int>(System.Linq.Enumerable.Range(Known, 17)));
        }

        // Chọn ngẫu nhiên có trọng số một màu kề với baseColor; tránh màu avoid (-1 = không tránh), 10% vẫn cho trùng như game
        public static int Pick(int baseColor, int avoid, System.Random rng)
        {
            var row = Counts[Anchor(baseColor)];
            var total = 0f;
            var w = new float[Known];
            for (var c = 0; c < Known; c++)
            {
                w[c] = row[c];
                if (c == avoid && rng.NextDouble() > 0.1) w[c] = 0f;
                total += w[c];
            }
            if (total <= 0f) return baseColor;
            var r = (float)rng.NextDouble() * total;
            for (var c = 0; c < Known; c++)
            {
                r -= w[c];
                if (r <= 0f && w[c] > 0f) return c;
            }
            return baseColor;
        }
    }
}
