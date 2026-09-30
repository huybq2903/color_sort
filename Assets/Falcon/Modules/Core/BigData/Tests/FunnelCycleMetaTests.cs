using System;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using NUnit.Framework;

#pragma warning disable 618 // wire-name của shape di sản vẫn là hợp đồng phải ghim

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// Bộ meta chu kỳ trên funnel (chốt owner 04/09): cycleIndex/cycleStartTs/cycleEndTs do game
    /// điền (số game phải đúng để chạy countdown UI) + funnelShape do SDK tự đóng (shape ĐANG ÁP
    /// — vai DQ bắt ca quên Register).
    /// </summary>
    public class FunnelCycleMetaTests
    {
        /// <summary>Đồng hồ đứng yên cho van — [NoAutoCreate] theo luật FakeDataPool (FReflection quét cả test asmdef).</summary>
        [NoAutoCreate]
        private class FrozenTime : ITimeRepository
        {
            public long CurrentTimeMillis => 1_700_000_000_000L;
            public TimeSpan LocalDiffDelta => TimeSpan.Zero;
            public Task Init(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private static FunnelLogService Service(FakeDataPool disk, FunnelRegistry registry = null)
        {
            return new FunnelLogService(disk, new FrozenTime(), registry ?? new FunnelRegistry(), null);
        }

        private static FunnelParam Param(string funnelName, string cycleId = null, int priority = 0)
        {
            return new FunnelParam
                { funnelName = funnelName, action = "join", priority = priority, cycleId = cycleId };
        }

        // ---- funnelShape: SDK tự thú shape đang áp ----

        [Test]
        public void WireNames_AreContract()
        {
            // Đổi các chuỗi này = SỬA HỢP ĐỒNG với loader (§H2)
            Assert.AreEqual("once_ordered", FunnelShape.OnceOrdered.ToWireName());
            Assert.AreEqual("once_per_cycle", FunnelShape.OncePerCycle.ToWireName());
            Assert.AreEqual("repeatable", FunnelShape.Repeatable.ToWireName());
        }

        [Test]
        public void Check_RegisteredOncePerCycle_StampsIt()
        {
            // Dùng AFunnelLog thuần + registry CỤC BỘ, không dựng typed-log (ctor FFilteredFunnelLog
            // tự Check qua DI Instance — dựng nó trong test là đụng container thật, đúng mìn mà
            // các test khác né với ghi chú "không dựng log thật ở đây")
            var registry = new FunnelRegistry();
            registry.Register("my_event", FunnelShape.OncePerCycle);
            var service = Service(new FakeDataPool(), registry);
            var log = new AFunnelLog(Param("my_event", cycleId: "season_5"));

            service.Check(log);

            Assert.AreEqual("once_per_cycle", log.funnelShape);
        }

        [Test]
        public void Check_UnregisteredCustomName_ConfessesFtueDefault()
        {
            // Quên Register là funnel rơi về luật ftue — field này chính là lời TỰ THÚ để DQ phía
            // kho bắt được ca đó từ data, khỏi nội suy hành vi stream
            var service = Service(new FakeDataPool());
            var log = new AFunnelLog(Param("my_custom_funnel"));

            service.Check(log);

            Assert.AreEqual("once_ordered", log.funnelShape);
        }

        [Test]
        public void Check_EventPrefix_DefaultsOncePerCycle()
        {
            // Lưới quên-Register nâng cấp 05/09: event_* rơi về OncePerCycle (lọc đúng khi có
            // cycleId) thay vì Repeatable đã nghỉ hưu
            var service = Service(new FakeDataPool());
            var log = new AFunnelLog(Param("event_sky_path", cycleId: "2026-09-05"));

            service.Check(log);

            Assert.AreEqual("once_per_cycle", log.funnelShape);
        }

        [Test]
        public void Check_StampsShape_EvenOnVetoedStep()
        {
            // Bước trùng bị vứt thì cũng chẳng sao — nhưng luật đóng dấu phải đồng nhất.
            // AFunnelLog thuần + registry cục bộ (xem test trên về mìn DI của typed-log).
            var registry = new FunnelRegistry();
            registry.Register("my_event", FunnelShape.OncePerCycle);
            var service = Service(new FakeDataPool(), registry);
            var first = new AFunnelLog(Param("my_event", "s1", priority: 10));
            var dup = new AFunnelLog(Param("my_event", "s1", priority: 10));

            Assert.IsTrue(service.Check(first).valid);
            var verdict = service.Check(dup);

            Assert.IsFalse(verdict.valid, "Bước trùng trong cùng chu kỳ phải bị veto");
            Assert.AreEqual("once_per_cycle", dup.funnelShape);
        }

        // ---- cycle meta: giữ raw, không sửa hộ ----

        [Test]
        public void CorrectValues_KeepsCycleMeta_Intact()
        {
            var param = new FunnelParam
            {
                funnelName = "event_x", action = "join",
                cycleIndex = 37, cycleStartTs = 1_000L, cycleEndTs = 2_000L
            };

            param.CorrectValues();

            Assert.AreEqual(37, param.cycleIndex);
            Assert.AreEqual(1_000L, param.cycleStartTs);
            Assert.AreEqual(2_000L, param.cycleEndTs);
        }

        [Test]
        public void CorrectValues_ReversedSchedule_WarnsButPreservesRaw()
        {
            // Lịch ngược = bug config — server phải thấy RAW mới truy được nguồn; âm thầm null hoá
            // là xoá dấu vết của chính cái bug cần tìm
            var param = new FunnelParam
            {
                funnelName = "event_x", action = "join",
                cycleStartTs = 2_000L, cycleEndTs = 1_000L
            };

            param.CorrectValues();

            Assert.AreEqual(2_000L, param.cycleStartTs);
            Assert.AreEqual(1_000L, param.cycleEndTs);
        }
    }
}
