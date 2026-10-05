using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Falcon.InGame.LevelEditor.Tests
{
    public class PictureModelEditTests
    {
        private static List<Vector2Int> Bulge(int midX) =>
            new() { new(50, 0), new(55, 25), new(midX, 50), new(55, 75), new(50, 100) };

        [Test]
        public void ReplaceRun_UpdatesBothRegions_AndStaysWatertight()
        {
            var p = PictureFixtures.TwoRects();
            var model = new PictureModel(p);
            var run = EdgeSkeleton.RunsOf(p, 0).Single(r => r.b == 1);
            Assert.IsTrue(model.ReplaceRun(run, Bulge(60), out var err), err);

            var after = new PictureModel(p);
            Assert.AreEqual(0, after.HitTest(new Vector2(58.5f, 50.5f)));
            Assert.AreEqual(1, after.HitTest(new Vector2(62.5f, 50.5f)));
            Assert.IsTrue(after.Map.reg.All(id => id > 0));
        }

        [Test]
        public void ReplaceRun_ClearsWidthsOfEditedRegions()
        {
            var p = PictureFixtures.TwoRects();
            p.regions[0].widths = new[] { 1, 1, 1, 1 };
            p.regions[1].widths = new[] { 1, 1, 1, 1 };
            var run = EdgeSkeleton.RunsOf(p, 0).Single(r => r.b == 1);
            Assert.IsTrue(new PictureModel(p).ReplaceRun(run, Bulge(60), out var err), err);
            Assert.IsNull(p.regions[0].widths);
            Assert.IsNull(p.regions[1].widths);
        }

        [Test]
        public void ReplaceRun_RejectsCrossingOtherBoundary_AndLeavesDataUntouched()
        {
            var p = PictureFixtures.TwoRects();
            var before = (int[])p.regions[1].points.Clone();
            var run = EdgeSkeleton.RunsOf(p, 0).Single(r => r.b == 1);
            var chain = new List<Vector2Int> { new(50, 0), new(130, 50), new(50, 100) };
            Assert.IsFalse(new PictureModel(p).ReplaceRun(run, chain, out var err));
            Assert.IsNotNull(err);
            CollectionAssert.AreEqual(before, p.regions[1].points);
        }

        [Test]
        public void ReplaceRun_RejectsChainCrossingChordOfOwnRegion()
        {
            var p = new Falcon.InGame.Core.PictureProperty { width = 100, height = 100, unit = 1 };
            p.regions.Add(new Falcon.InGame.Core.RegionData { points = new[] { 0, 0, 50, 50, 0, 100 } }); // A: tam giác P, M, Q
            p.regions.Add(new Falcon.InGame.Core.RegionData { points = new[] { 0, 100, 50, 50, 0, 0, 100, 0, 100, 100 } }); // B: Q, M, P rồi phần còn lại
            var run = EdgeSkeleton.RunsOf(p, 0).Single(r => r.b == 1);
            var chain = new List<Vector2Int> { new(0, 0), new(-20, 30), new(20, 60), new(0, 100) };
            Assert.IsFalse(new PictureModel(p).ReplaceRun(run, chain, out _));
        }

        [Test]
        public void ReplaceRun_RejectsClosedRun_AndRunWithUnknownRingIndex()
        {
            var p = PictureFixtures.TwoRects();
            var model = new PictureModel(p);
            var closed = new EdgeRun { a = 0, b = 1, pts = new List<Vector2Int> { new(50, 0), new(50, 100), new(50, 0) } };
            Assert.IsFalse(model.ReplaceRun(closed, new List<Vector2Int> { new(50, 0), new(60, 50), new(50, 0) }, out _));
            var stray = new EdgeRun { a = 0, b = 1, lo = -1, hi = -1, pts = new List<Vector2Int> { new(50, 0), new(50, 100) } };
            Assert.IsFalse(model.ReplaceRun(stray, Bulge(60), out _));
        }

        [Test]
        public void ReplaceRun_RejectsOuterRunAndMovedEnds()
        {
            var p = PictureFixtures.TwoRects();
            var model = new PictureModel(p);
            var runs = EdgeSkeleton.RunsOf(p, 0);
            Assert.IsFalse(model.ReplaceRun(runs.Single(r => r.b == -1), Bulge(60), out _));
            var moved = new List<Vector2Int> { new(51, 0), new(60, 50), new(50, 100) };
            Assert.IsFalse(model.ReplaceRun(runs.Single(r => r.b == 1), moved, out _));
        }
    }
}
