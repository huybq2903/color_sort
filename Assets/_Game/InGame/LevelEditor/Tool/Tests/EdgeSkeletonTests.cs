using System.Linq;
using Falcon.InGame.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falcon.InGame.LevelEditor.Tests
{
    public class EdgeSkeletonTests
    {
        [Test]
        public void RunsOf_SplitsWhereNeighbourChanges()
        {
            var runs = EdgeSkeleton.RunsOf(PictureFixtures.TwoRects(), 0);
            Assert.AreEqual(2, runs.Count);
            var shared = runs.Single(r => r.b == 1);
            CollectionAssert.AreEqual(new[] { new Vector2Int(50, 0), new Vector2Int(50, 100) }, shared.pts);
            Assert.AreEqual(4, runs.Single(r => r.b == -1).pts.Count);
        }

        [Test]
        public void RunsOf_RecordsRingIndicesOfRunEnds()
        {
            var shared = EdgeSkeleton.RunsOf(PictureFixtures.TwoRects(), 0).Single(r => r.b == 1);
            Assert.AreEqual(1, shared.lo);
            Assert.AreEqual(2, shared.hi);
        }

        [Test]
        public void RunsOf_RegionWithoutNeighbours_IsOneClosedRun()
        {
            var p = new PictureProperty { width = 100, height = 100, unit = 1 };
            p.regions.Add(new RegionData { points = new[] { 0, 0, 100, 0, 100, 100, 0, 100 } });
            var runs = EdgeSkeleton.RunsOf(p, 0);
            Assert.AreEqual(1, runs.Count);
            Assert.AreEqual(-1, runs[0].b);
            Assert.AreEqual(5, runs[0].pts.Count);
            Assert.AreEqual(runs[0].pts[0], runs[0].pts[4]);
        }

        [Test]
        public void RunsOf_NeighbourMustShareEdge_NotJustTwoVertices()
        {
            var p = new PictureProperty { width = 100, height = 100, unit = 1 };
            p.regions.Add(new RegionData { points = new[] { 0, 0, 100, 100, 0, 100 } }); // A: P, Q, M
            p.regions.Add(new RegionData { points = new[] { 0, 0, 100, 0, 100, 100, 50, 50 } }); // B: có P và Q nhưng không liền nhau
            p.regions.Add(new RegionData { points = new[] { 100, 100, 0, 0, 50, -50 } }); // C: P-Q liền nhau
            var runs = EdgeSkeleton.RunsOf(p, 0);
            Assert.AreEqual(2, runs.Single(r => r.b >= 0).b);
        }

        [Test]
        public void RunsOf_IgnoresHoles()
        {
            var p = PictureFixtures.TwoRects();
            p.regions[0].holes = new System.Collections.Generic.List<int[]> { new[] { 10, 10, 20, 10, 20, 20, 10, 20 } };
            var runs = EdgeSkeleton.RunsOf(p, 0);
            Assert.AreEqual(2, runs.Count);
            Assert.IsTrue(runs.All(r => r.pts.All(v => v.x != 10 && v.x != 20)));
        }

        private static EdgeRun Arc(int unit)
        {
            var run = new EdgeRun { a = 0, b = 1, pts = new System.Collections.Generic.List<Vector2Int>() };
            for (var i = 0; i <= 40; i++)
            {
                var t = i / 40f * Mathf.PI * 0.5f;
                run.pts.Add(Vector2Int.RoundToInt(new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * 50f * unit));
            }
            return run;
        }

        [Test]
        public void FitSample_StaysWithinToleranceAndKeepsEnds()
        {
            const int unit = 4;
            var run = Arc(unit);
            var cubics = EdgeSkeleton.Fit(run, unit);
            Assert.That(cubics.Count, Is.InRange(1, 6));
            var sampled = EdgeSkeleton.Sample(cubics, unit, 1f);
            Assert.AreEqual(run.pts[0], sampled[0]);
            Assert.AreEqual(run.pts[run.pts.Count - 1], sampled[sampled.Count - 1]);
            foreach (var v in run.pts)
            {
                var best = float.MaxValue;
                for (var i = 0; i + 1 < sampled.Count; i++)
                    best = Mathf.Min(best, PictureModel.DistToSegment((Vector2)v / unit, (Vector2)sampled[i] / unit, (Vector2)sampled[i + 1] / unit));
                Assert.Less(best, EdgeSkeleton.FitTolerance * 1.5f);
            }
        }

        [Test]
        public void FitSample_TwoPointRun_IsOneStraightCubicWithExactEnds()
        {
            var run = new EdgeRun { a = 0, b = 1, pts = new System.Collections.Generic.List<Vector2Int> { new(50, 0), new(50, 100) } };
            var cubics = EdgeSkeleton.Fit(run, 1);
            Assert.AreEqual(1, cubics.Count);
            var sampled = EdgeSkeleton.Sample(cubics, 1, 1f);
            Assert.AreEqual(new Vector2Int(50, 0), sampled[0]);
            Assert.AreEqual(new Vector2Int(50, 100), sampled[sampled.Count - 1]);
            Assert.IsTrue(sampled.All(v => v.x == 50));
        }

        [Test]
        public void Fit_DuplicateConsecutivePoints_DoesNotProduceNaN()
        {
            var run = new EdgeRun { a = 0, b = 1, pts = new System.Collections.Generic.List<Vector2Int> { new(0, 0), new(0, 0), new(10, 5), new(10, 5), new(20, 0) } };
            var cubics = EdgeSkeleton.Fit(run, 1);
            Assert.Greater(cubics.Count, 0);
            foreach (var c in cubics)
                Assert.IsFalse(float.IsNaN(c.p1.x) || float.IsNaN(c.p1.y) || float.IsNaN(c.p2.x) || float.IsNaN(c.p2.y));
        }

        [Test]
        public void Nearest_PicksSharedRunWithinRadiusOnly()
        {
            var runs = EdgeSkeleton.RunsOf(PictureFixtures.TwoRects(), 0);
            Assert.AreEqual(1, EdgeSkeleton.Nearest(runs, new Vector2(52f, 50f), 1, 6f).b);
            Assert.IsNull(EdgeSkeleton.Nearest(runs, new Vector2(25f, 50f), 1, 6f));
        }
    }
}
