using System;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// Hàm đọc tiến độ funnel (Funnel.CurrentPriority/HasPassed/LastJoinDay) phải đọc ĐÚNG sổ mà
    /// van lọc trùng ghi — key seed trong test là format thật của van (OnceOrdered:
    /// <c>funnelName + priority</c>; OncePerCycle: <see cref="FunnelLogService.CycleStepKey"/>;
    /// join day: <c>funnelName + "_joinDay"</c>).
    /// <br/>Chỉ test tầng đọc nên các dep không dùng (time/registry/schedule) truyền null —
    /// ctor chỉ gán field.
    /// </summary>
    public class FunnelReadStateTests
    {
        private static FunnelLogService Service(FakeDataPool disk)
        {
            return new FunnelLogService(disk, null, null, null);
        }

        [Test]
        public void CurrentOrderedPriority_EmptyLedger_IsNull()
        {
            Assert.IsNull(Service(new FakeDataPool()).CurrentOrderedPriority("ftue"));
        }

        [Test]
        public void CurrentOrderedPriority_ReturnsHighestContiguousStep()
        {
            var disk = new FakeDataPool();
            // Van OnceOrdered ghi key funnelName+priority (liền mạch từ 0 — luật của shape)
            disk.Save("ftue0", DateTime.UtcNow);
            disk.Save("ftue1", DateTime.UtcNow);
            disk.Save("ftue2", DateTime.UtcNow);

            Assert.AreEqual(2, Service(disk).CurrentOrderedPriority("ftue"));
        }

        [Test]
        public void HasPassedOrdered_ReadsTheFilterLedger()
        {
            var disk = new FakeDataPool();
            disk.Save("ftue0", DateTime.UtcNow);

            var service = Service(disk);
            Assert.IsTrue(service.HasPassedOrdered("ftue", 0));
            Assert.IsFalse(service.HasPassedOrdered("ftue", 1), "Mốc chưa đi không được báo là đã qua");
        }

        [Test]
        public void HasPassedInCycle_ScopedByCycle_AndByAction()
        {
            var disk = new FakeDataPool();
            disk.Save(FunnelLogService.CycleStepKey("battle_pass", "season_5", "claim", 3), DateTime.UtcNow);

            var service = Service(disk);
            Assert.IsTrue(service.HasPassedInCycle("battle_pass", "season_5", "claim", 3));
            Assert.IsFalse(service.HasPassedInCycle("battle_pass", "season_6", "claim", 3),
                "Sang chu kỳ mới sổ phải sạch");
            Assert.IsFalse(service.HasPassedInCycle("battle_pass", "season_5", "milestone", 3),
                "Key gồm cả ACTION — milestone(3) và claim(3) là hai bước thật khác nhau");
        }

        [Test]
        public void LastJoinDay_NullIfNeverJoined()
        {
            Assert.IsNull(Service(new FakeDataPool()).LastJoinDayLocal("battle_pass"));
        }

        [Test]
        public void LastJoinDay_ReadsTheStampedJoin()
        {
            var disk = new FakeDataPool();
            var joinedAtUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            disk.Save("battle_pass_joinDay", joinedAtUtc);

            Assert.AreEqual(joinedAtUtc.ToLocalTime(), Service(disk).LastJoinDayLocal("battle_pass"));
        }
    }
}
