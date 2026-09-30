using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class PerfSampleStateTests
    {
        private const float FRAME_60 = 1f / 60;

        [Test]
        public void AvgFps_IsFramesOverElapsed()
        {
            var state = new PerfSampleState();
            for (var i = 0; i < 60; i++) state.AddFrame(FRAME_60);

            Assert.AreEqual(60f, state.TakeAndReset().AvgFps.Value, 0.01f);
        }

        [Test]
        public void NoSamples_LeavesEverythingEmpty()
        {
            // Không đo được thì vắng mặt khỏi payload, không gửi 0 giả (§H4)
            var summary = new PerfSampleState().TakeAndReset();

            Assert.IsNull(summary.AvgFps);
            Assert.IsNull(summary.FrameDropCount);
            Assert.IsNull(summary.MemoryWarningCount);
        }

        [Test]
        public void HeavyFrames_CountAsDrops()
        {
            var state = new PerfSampleState();
            state.AddFrame(FRAME_60);
            state.AddFrame(0.15f);
            state.AddFrame(0.3f);

            Assert.AreEqual(2, state.TakeAndReset().FrameDropCount);
        }

        [Test]
        public void FrameAtThreshold_IsNotADrop()
        {
            var state = new PerfSampleState();
            state.AddFrame(PerfSampleState.FRAME_DROP_SEC);

            Assert.AreEqual(0, state.TakeAndReset().FrameDropCount);
        }

        [Test]
        public void NotRenderingGap_IsIgnoredEntirely()
        {
            // Quay lại từ ads/background: frame đầu dài vài giây — đếm vào là bịa số giật và dìm fps
            var state = new PerfSampleState();
            for (var i = 0; i < 60; i++) state.AddFrame(FRAME_60);
            state.AddFrame(30f);

            var summary = state.TakeAndReset();
            Assert.AreEqual(60f, summary.AvgFps.Value, 0.01f);
            Assert.AreEqual(0, summary.FrameDropCount);
        }

        [Test]
        public void NonPositiveDelta_IsIgnored()
        {
            var state = new PerfSampleState();
            state.AddFrame(0f);
            state.AddFrame(-1f);

            Assert.IsNull(state.TakeAndReset().AvgFps);
        }

        [Test]
        public void MemoryWarnings_AreCountedEvenWithoutFrames()
        {
            var state = new PerfSampleState();
            state.AddMemoryWarning();
            state.AddMemoryWarning();

            var summary = state.TakeAndReset();
            Assert.AreEqual(2, summary.MemoryWarningCount);
            Assert.IsNull(summary.AvgFps);
        }

        [Test]
        public void Take_ResetsSoBulletinsDoNotDoubleCount()
        {
            // Mỗi bản tin session_data mang số liệu của đúng quãng nó khai
            var state = new PerfSampleState();
            state.AddFrame(0.2f);
            state.AddMemoryWarning();
            state.TakeAndReset();

            var second = state.TakeAndReset();
            Assert.IsNull(second.AvgFps);
            Assert.IsNull(second.FrameDropCount);
            Assert.IsNull(second.MemoryWarningCount);
        }
    }
}
