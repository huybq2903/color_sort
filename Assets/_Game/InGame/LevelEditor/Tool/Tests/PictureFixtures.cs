using Falcon.InGame.Core;

namespace Falcon.InGame.LevelEditor.Tests
{
    internal static class PictureFixtures
    {
        // Ba hình chữ nhật cao 100 nối tiếp: x 0-30, 30-60, 60-100
        public static PictureProperty ThreeRects()
        {
            var p = new PictureProperty { width = 100, height = 100, unit = 1 };
            p.regions.Add(new RegionData { points = new[] { 0, 0, 30, 0, 30, 100, 0, 100 }, colorId = 1 });
            p.regions.Add(new RegionData { points = new[] { 30, 0, 60, 0, 60, 100, 30, 100 }, colorId = 2 });
            p.regions.Add(new RegionData { points = new[] { 60, 0, 100, 0, 100, 100, 60, 100 }, colorId = 3 });
            return p;
        }

        // Hai hình chữ nhật 50x100 kề nhau, chung biên x=50; unit = 1
        public static PictureProperty TwoRects()
        {
            var p = new PictureProperty { width = 100, height = 100, unit = 1 };
            p.regions.Add(new RegionData { points = new[] { 0, 0, 50, 0, 50, 100, 0, 100 }, colorId = 1 });
            p.regions.Add(new RegionData { points = new[] { 50, 0, 100, 0, 100, 100, 50, 100 }, colorId = 2 });
            return p;
        }
    }
}
