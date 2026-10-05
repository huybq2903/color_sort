using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Falcon.InGame.LevelEditor.Tests
{
    public class PictureModelMultiTests
    {
        [Test]
        public void MergeMany_ChainOfThree_BecomesOneRegionCoveringFrame()
        {
            var merged = PictureModel.MergeMany(PictureFixtures.ThreeRects(), new[] { 0, 2, 1 }, out var err);
            Assert.IsNotNull(merged, err);
            Assert.AreEqual(1, merged.regions.Count);
            var after = new PictureModel(merged);
            Assert.IsTrue(after.Map.reg.All(id => id == 1));
        }

        [Test]
        public void MergeMany_NonAdjacent_FailsAndLeavesSourceUntouched()
        {
            var p = PictureFixtures.ThreeRects();
            Assert.IsNull(PictureModel.MergeMany(p, new[] { 0, 2 }, out var err));
            Assert.IsNotNull(err);
            Assert.AreEqual(3, p.regions.Count);
        }

        [Test]
        public void RegionsInRect_ReturnsEveryRegionTouchedByRect()
        {
            var model = new PictureModel(PictureFixtures.TwoRects());
            CollectionAssert.AreEquivalent(new[] { 0 }, model.RegionsInRect(new Vector2(10, 10), new Vector2(20, 20)));
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, model.RegionsInRect(new Vector2(60, 20), new Vector2(40, 10)));
        }

        [Test]
        public void RegionsInRect_OutsideFrame_IsEmpty()
        {
            var model = new PictureModel(PictureFixtures.TwoRects());
            Assert.IsEmpty(model.RegionsInRect(new Vector2(200, 200), new Vector2(300, 300)));
            Assert.IsEmpty(model.RegionsInRect(new Vector2(-50, -50), new Vector2(-10, -10)));
        }
    }
}
