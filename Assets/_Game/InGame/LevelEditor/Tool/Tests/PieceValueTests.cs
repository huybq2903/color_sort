using System.Collections.Generic;
using System.Linq;
using Falcon.InGame.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falcon.InGame.LevelEditor.Tests
{
    public class PieceValueTests
    {
        [Test]
        public void Assign_SkipsManualRegions_AndRecomputesTheRest()
        {
            var p = PictureFixtures.TwoRects();
            p.regions[0].value = 50;
            p.regions[0].valueManual = true;
            p.regions[1].value = 50;
            PieceValue.Assign(p);
            Assert.AreEqual(50, p.regions[0].value);
            Assert.AreEqual(PieceValue.Suggest(p, p.regions[1]), p.regions[1].value);
            Assert.AreNotEqual(50, p.regions[1].value);
        }

        [Test]
        public void Suggest_MatchesWhatAssignWouldSetOnAnAutoRegion()
        {
            var p = PictureFixtures.TwoRects();
            var suggested = PieceValue.Suggest(p, p.regions[0]);
            PieceValue.Assign(p);
            Assert.AreEqual(suggested, p.regions[0].value);
        }

        [Test]
        public void Clamp_RoundsToStepAndKeepsRange()
        {
            Assert.AreEqual(50, PieceValue.Clamp(0));
            Assert.AreEqual(50, PieceValue.Clamp(74));
            Assert.AreEqual(100, PieceValue.Clamp(76));
            Assert.AreEqual(300, PieceValue.Clamp(999));
        }

        [Test]
        public void Clone_CopiesValueManual()
        {
            var p = PictureFixtures.TwoRects();
            p.regions[1].valueManual = true;
            var c = p.Clone();
            Assert.IsFalse(c.regions[0].valueManual);
            Assert.IsTrue(c.regions[1].valueManual);
        }

        [Test]
        public void MergeMany_ResetsManualFlagOfMergedRegion()
        {
            var p = PictureFixtures.ThreeRects();
            p.regions[0].valueManual = true;
            p.regions[0].value = 300;
            var merged = PictureModel.MergeMany(p, new[] { 0, 1 }, out var err);
            Assert.IsNotNull(merged, err);
            Assert.IsFalse(merged.regions[0].valueManual);
        }

        [Test]
        public void SplitPath_ResetsManualFlagOfSplitRegion()
        {
            var p = PictureFixtures.TwoRects();
            p.regions[0].valueManual = true;
            var model = new PictureModel(p);
            var path = new List<Vector2> { new(25, -10), new(25, 110) };
            Assert.Greater(model.SplitPath(0, path, out var err), 0, err);
            Assert.IsTrue(p.regions.Take(1).All(r => !r.valueManual));
        }

        [Test]
        public void ReplaceRun_KeepsManualFlag()
        {
            var p = PictureFixtures.TwoRects();
            p.regions[0].valueManual = true;
            var run = EdgeSkeleton.RunsOf(p, 0).Single(r => r.b == 1);
            var chain = new List<Vector2Int> { new(50, 0), new(55, 25), new(60, 50), new(55, 75), new(50, 100) };
            Assert.IsTrue(new PictureModel(p).ReplaceRun(run, chain, out var err), err);
            Assert.IsTrue(p.regions[0].valueManual);
        }
    }
}
