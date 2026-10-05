using NUnit.Framework;
using UnityEngine;

namespace Falcon.InGame.LevelEditor.Tests
{
    public class PictureModelLineTests
    {
        private static readonly Vector2[] Stroke = { new(10, 10), new(40, 20), new(70, 15) };

        [Test]
        public void AddLine_StoresThickness_ReplaceKeepsIt()
        {
            var model = new PictureModel(PictureFixtures.TwoRects());
            Assert.IsTrue(model.AddLine(Stroke, -1, 150));
            Assert.AreEqual(150, model.Picture.lineThickness[0]);
            Assert.IsTrue(model.AddLine(Stroke, 0));
            Assert.AreEqual(150, model.Picture.lineThickness[0]);
        }

        [Test]
        public void SetLineThickness_ChangesOnlyThatLine_AndClampsNegative()
        {
            var model = new PictureModel(PictureFixtures.TwoRects());
            model.AddLine(Stroke);
            model.AddLine(Stroke);
            Assert.IsTrue(model.SetLineThickness(1, 220));
            CollectionAssert.AreEqual(new[] { 0, 220 }, model.Picture.lineThickness);
            model.SetLineThickness(1, -5);
            Assert.AreEqual(0, model.Picture.lineThickness[1]);
            Assert.IsFalse(model.SetLineThickness(5, 100));
        }

        [Test]
        public void RemoveLineAt_KeepsThicknessAlignedWithLines()
        {
            var model = new PictureModel(PictureFixtures.TwoRects());
            model.AddLine(Stroke, -1, 100);
            model.AddLine(Stroke, -1, 200);
            model.RemoveLineAt(0);
            CollectionAssert.AreEqual(new[] { 200 }, model.Picture.lineThickness);
        }

        [Test]
        public void Clone_CopiesThickness()
        {
            var model = new PictureModel(PictureFixtures.TwoRects());
            model.AddLine(Stroke, -1, 130);
            CollectionAssert.AreEqual(new[] { 130 }, model.Picture.Clone().lineThickness);
        }
    }
}
