using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Falcon.InGame.LevelEditor.Tests
{
    public class PenStateTests
    {
        private static PenAnchor A(float x, float y, float hx = 0f, float hy = 0f) =>
            new() { pos = new Vector3(x, y, 0f), outH = new Vector3(hx, hy, 0f), inH = new Vector3(-hx, -hy, 0f) };

        [Test]
        public void Snapshot_CopiesAnchors_SoLaterEditsDoNotLeakIn()
        {
            var list = new List<PenAnchor> { A(0, 0), A(1, 1) };
            var s = new PenState(list, 1);
            list[0] = A(9, 9);
            list.Add(A(2, 2));
            Assert.AreEqual(2, s.anchors.Length);
            Assert.AreEqual(new Vector3(0, 0, 0), s.anchors[0].pos);
            Assert.AreEqual(1, s.sel);
        }

        [Test]
        public void SameAnchors_ComparesPositionsAndHandles_IgnoresSelection()
        {
            var a = new PenState(new[] { A(0, 0), A(5, 5, 1, 0) }, 0);
            Assert.IsTrue(a.SameAnchors(new PenState(new[] { A(0, 0), A(5, 5, 1, 0) }, 1)));
            Assert.IsFalse(a.SameAnchors(new PenState(new[] { A(0, 0), A(6, 5, 1, 0) }, 0)));
            Assert.IsFalse(a.SameAnchors(new PenState(new[] { A(0, 0), A(5, 5, 2, 0) }, 0)));
            Assert.IsFalse(a.SameAnchors(new PenState(new[] { A(0, 0) }, 0)));
        }

        [Test]
        public void Command_ExecuteAppliesAfter_UndoAppliesBefore()
        {
            var before = new PenState(new[] { A(0, 0) }, 0);
            var after = new PenState(new[] { A(0, 0), A(3, 3) }, 1);
            PenState applied = null;
            var cmd = new PenStateCommand(s => applied = s, before, after);
            Assert.IsTrue(cmd.Execute());
            Assert.AreSame(after, applied);
            cmd.Undo();
            Assert.AreSame(before, applied);
            Assert.IsTrue(cmd.Execute());
            Assert.AreSame(after, applied);
        }
    }
}
