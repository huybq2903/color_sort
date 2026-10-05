using System.Collections.Generic;
using System.Linq;
using Falcon.InGame.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falcon.InGame.LevelEditor.Tests
{
    public class PictureModelGapTests
    {
        // Hai mảnh cách nhau khe `gap` pixel (lưới 100x100, unit 1)
        private static PictureProperty TwoRectsWithGap(int gap, bool inkGaps)
        {
            var p = new PictureProperty { width = 100, height = 100, unit = 1 };
            p.gen.inkGaps = inkGaps;
            var left = 50 - gap / 2;
            var right = left + gap;
            p.regions.Add(new RegionData { points = new[] { 0, 0, left, 0, left, 100, 0, 100 }, colorId = 1 });
            p.regions.Add(new RegionData { points = new[] { right, 0, 100, 0, 100, 100, right, 100 }, colorId = 2 });
            return p;
        }

        [Test]
        public void Merge_AcrossNarrowGap_FillsGapIntoOnePiece()
        {
            var model = new PictureModel(TwoRectsWithGap(2, true));
            Assert.AreEqual(0, model.Merge(0, 1, out var err), err);
            Assert.AreEqual(1, model.Picture.regions.Count);
            Assert.IsTrue(model.Map.reg.All(id => id == 1));
        }

        [Test]
        public void Merge_GapWiderThanLimit_Fails()
        {
            var model = new PictureModel(TwoRectsWithGap(10, true));
            Assert.AreEqual(-1, model.Merge(0, 1, out var err));
            Assert.IsNotNull(err);
            Assert.AreEqual(2, model.Picture.regions.Count);
        }

        [Test]
        public void Split_PieceInGapPicture_KeepsGapUntouched()
        {
            var model = new PictureModel(TwoRectsWithGap(2, true));
            Assert.AreEqual(1, model.Split(0, new UnityEngine.Vector2(20, 0), new UnityEngine.Vector2(20, 100), out var err), err);
            Assert.AreEqual(3, model.Picture.regions.Count);
            Assert.AreEqual(2, model.Map.reg.Count(id => id == 0) / 100); // khe 2px x 100 hàng vẫn trống
        }

        private static PictureProperty OneRect(bool inkGaps)
        {
            var p = new PictureProperty { width = 100, height = 100, unit = 1 };
            p.gen.inkGaps = inkGaps;
            p.regions.Add(new RegionData { points = new[] { 0, 0, 100, 0, 100, 100, 0, 100 }, colorId = 1 });
            return p;
        }

        [Test]
        public void SplitPath_WithGap_LeavesEmptyBandBetweenPieces()
        {
            var model = new PictureModel(OneRect(true));
            var path = new[] { new Vector2(50, 0), new Vector2(50, 100) };
            Assert.AreEqual(1, model.SplitPath(0, path, out var err, 6), err);
            Assert.AreEqual(2, model.Picture.regions.Count);
            var empty = model.Map.reg.Count(id => id == 0);
            Assert.That(empty, Is.InRange(5 * 100, 7 * 100)); // dải ~6 ô x 100 hàng
            Assert.IsTrue(model.Map.reg.Where(id => id > 0).Distinct().Count() == 2);
        }

        [Test]
        public void SplitPath_NoGap_StillTilesTheWholePiece()
        {
            var model = new PictureModel(OneRect(true));
            var path = new[] { new Vector2(50, 0), new Vector2(50, 100) };
            Assert.AreEqual(1, model.SplitPath(0, path, out var err), err);
            Assert.IsFalse(model.Map.reg.Any(id => id == 0));
        }

        [Test]
        public void RedrawBoundary_GapPicture_ExplainsLimitation()
        {
            var model = new PictureModel(TwoRectsWithGap(2, true));
            Assert.IsFalse(model.RedrawBoundary(new[] { new Vector2(49, 10), new Vector2(49, 90) }, out var err));
            StringAssert.Contains("khe", err);
        }

        [Test]
        public void RunsOf_GapPicture_SplitsRingByPieceFacedAcrossGap()
        {
            var runs = EdgeSkeleton.RunsOf(TwoRectsWithGap(2, true), 0);
            var facing = runs.Single(r => r.b == 1);
            CollectionAssert.AreEqual(new[] { new Vector2Int(49, 0), new Vector2Int(49, 100) }, facing.pts);
            Assert.IsTrue(runs.Where(r => r != facing).All(r => r.b < 0)); // phần còn lại hướng ra ngoài tranh
        }

        [Test]
        public void ReplaceRun_GapPicture_ChangesOnlyOwnPiece()
        {
            var p = TwoRectsWithGap(2, true);
            var other = (int[])p.regions[1].points.Clone();
            var model = new PictureModel(p);
            var run = EdgeSkeleton.RunsOf(model.Picture, 0).Single(r => r.b == 1);
            var chain = new List<Vector2Int> { new(49, 0), new(45, 50), new(49, 100) };
            Assert.IsTrue(model.ReplaceRun(run, chain, out var err), err);
            CollectionAssert.Contains(model.Picture.regions[0].points, 45);
            CollectionAssert.AreEqual(other, model.Picture.regions[1].points);
        }

        [Test]
        public void ReplaceRun_GapPicture_RejectsCrossingNeighbour()
        {
            var model = new PictureModel(TwoRectsWithGap(2, true));
            var run = EdgeSkeleton.RunsOf(model.Picture, 0).Single(r => r.b == 1);
            var chain = new List<Vector2Int> { new(49, 0), new(60, 50), new(49, 100) };
            Assert.IsFalse(model.ReplaceRun(run, chain, out var err));
            Assert.IsNotNull(err);
        }

        [Test]
        public void Nearest_GapPicture_CanPickOuterRun()
        {
            var p = TwoRectsWithGap(2, true);
            var runs = EdgeSkeleton.RunsOf(p, 0);
            Assert.IsNull(EdgeSkeleton.Nearest(runs, new Vector2(20, 1), 1, 3f));
            Assert.IsNotNull(EdgeSkeleton.Nearest(runs, new Vector2(20, 1), 1, 3f, true));
        }

        [Test]
        public void Merge_PictureWithoutInkGaps_StillNeedsTouchingPieces()
        {
            var model = new PictureModel(TwoRectsWithGap(2, false));
            Assert.AreEqual(-1, model.Merge(0, 1, out var err));
            Assert.IsNotNull(err);
        }
    }
}
