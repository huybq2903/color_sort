using NUnit.Framework;

namespace Falcon.InGame.LevelEditor.Tests
{
    public class PictureModelMiscTests
    {
        [Test]
        public void Vectorised_RequiresUnitAboveOneAndTidyAndSmoothing()
        {
            var p = PictureFixtures.TwoRects();
            Assert.IsFalse(PictureModel.Vectorised(p));
            p.unit = 3;
            Assert.IsTrue(PictureModel.Vectorised(p));
            p.gen.tidy = false;
            Assert.IsFalse(PictureModel.Vectorised(p));
            p.gen.tidy = true;
            p.gen.curveSmooth = 0f;
            Assert.IsFalse(PictureModel.Vectorised(p));
        }

        [Test]
        public void RemoveLineAt_RemovesLineAndWidth_AndRejectsBadIndex()
        {
            var p = PictureFixtures.TwoRects();
            p.lines.Add(new[] { 0, 0, 10, 10 });
            p.lines.Add(new[] { 5, 5, 20, 20 });
            p.lineWidths.Add(0);
            p.lineWidths.Add(3);
            var model = new PictureModel(p);
            Assert.IsTrue(model.RemoveLineAt(0));
            Assert.AreEqual(1, p.lines.Count);
            Assert.AreEqual(3, p.lineWidths[0]);
            Assert.IsFalse(model.RemoveLineAt(5));
            Assert.IsFalse(model.RemoveLineAt(-1));
        }
    }
}
