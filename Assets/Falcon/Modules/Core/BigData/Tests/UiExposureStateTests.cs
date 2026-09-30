using System.Collections.Generic;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// Van gộp exposure UI (chốt owner 05/09 — per-lần-hiện CẤM lên wire): trăm impression phải
    /// thành một dòng count, CTR phía server vẫn chính xác vì chỉ là sum/sum.
    /// </summary>
    public class UiExposureStateTests
    {
        [Test]
        public void HundredImpressions_CollapseIntoOneCluster()
        {
            var state = new UiExposureState();
            for (var i = 0; i < 100; i++) state.Record("race_event_entry", FUiAction.Impression, null);

            Assert.AreEqual(1, state.Count, "Cùng (surface × action) phải về một cụm");
            Assert.AreEqual(100, state.TakeAll()[0].count);
        }

        [Test]
        public void SurfaceAndAction_SplitClusters()
        {
            // CTR ghép theo surface — impression và click phải là hai cụm riêng, surface khác
            // càng phải riêng
            var state = new UiExposureState();
            state.Record("race_event_entry", FUiAction.Impression, null);
            state.Record("race_event_entry", FUiAction.Click, null);
            state.Record("race_event_popup", FUiAction.Impression, null);

            Assert.AreEqual(3, state.Count);
        }

        [Test]
        public void Extras_FirstInClusterWins()
        {
            // Extras là ngữ cảnh BẤT BIẾN của surface — cụm giữ bản lần đầu, lần sau không đè
            var state = new UiExposureState();
            state.Record("entry", FUiAction.Impression, new Dictionary<string, object> { ["race_id"] = "r1" });
            state.Record("entry", FUiAction.Impression, new Dictionary<string, object> { ["race_id"] = "r2" });

            Assert.AreEqual("r1", state.TakeAll()[0].extraMeta["race_id"]);
        }

        [Test]
        public void TakeAll_Clears_NextStretchStartsFresh()
        {
            var state = new UiExposureState();
            state.Record("entry", FUiAction.Impression, null);
            state.TakeAll();

            Assert.AreEqual(0, state.Count, "Flush xong kho phải sạch — mỗi stretch một thế hệ cụm");
        }

        [Test]
        public void BlankSurfaceOrAction_IsIgnored()
        {
            var state = new UiExposureState();
            state.Record(null, FUiAction.Impression, null);
            state.Record("", FUiAction.Impression, null);
            state.Record("entry", null, null);

            Assert.AreEqual(0, state.Count);
        }
    }
}
