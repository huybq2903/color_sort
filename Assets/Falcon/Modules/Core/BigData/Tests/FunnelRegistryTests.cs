using NUnit.Framework;

// Shape Repeatable nghỉ hưu 05/09 nhưng vocab di sản còn chạy trên nó — test file này ghim đúng
// hành vi di sản nên cố ý dùng member obsolete.
#pragma warning disable 618

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class FunnelRegistryTests
    {
        [Test]
        public void ContractFunnels_AreRegisteredOutOfTheBox()
        {
            // Dev game dùng hằng số là chạy đúng luật, không phải khai lại
            var registry = new FunnelRegistry();

            Assert.IsTrue(registry.IsRepeatable(FFunnelName.BattlePass));
            Assert.IsTrue(registry.IsRepeatable(FFunnelName.DailyQuest));
            Assert.IsTrue(registry.IsRepeatable(FFunnelName.PiggyBank));
            Assert.IsTrue(registry.IsRepeatable(FFunnelName.LuckyWheel));
        }

        [Test]
        public void Ftue_StaysOnceOrdered()
        {
            Assert.IsFalse(new FunnelRegistry().IsRepeatable(FFunnelName.Ftue));
        }

        [Test]
        public void UnregisteredFunnel_KeepsLegacyRules()
        {
            // Game đang chạy không được đổi hành vi vì bản cập nhật này
            Assert.IsFalse(new FunnelRegistry().IsRepeatable("game_specific_funnel"));
        }

        [Test]
        public void EventFunnels_DefaultOncePerCycle_WithoutRegistering()
        {
            // Lưới quên-Register nâng cấp 05/09: event_* rơi về OncePerCycle (bước có cycleId
            // được lọc đúng luôn; thiếu cycleId van tự hạ pass-through) — không còn là
            // Repeatable "lặp tự do" nữa
            var registry = new FunnelRegistry();
            Assert.IsFalse(registry.IsRepeatable(FFunnelName.Event("halloween")));
            Assert.AreEqual(FunnelShape.OncePerCycle, registry.ShapeOf(FFunnelName.Event("halloween")));
        }

        [Test]
        public void ShapeOf_ThreeWay_ResolvesLikeIsRepeatableDid()
        {
            var registry = new FunnelRegistry();
            registry.Register("clan_season", FunnelShape.OncePerCycle);

            Assert.AreEqual(FunnelShape.OnceOrdered, registry.ShapeOf(FFunnelName.Ftue));
            Assert.AreEqual(FunnelShape.Repeatable, registry.ShapeOf(FFunnelName.BattlePass));
            Assert.AreEqual(FunnelShape.OncePerCycle, registry.ShapeOf("clan_season"));
            Assert.AreEqual(FunnelShape.OncePerCycle, registry.ShapeOf(FFunnelName.Event("tet")),
                "event_* chưa đăng ký rơi về OncePerCycle (lưới nâng cấp 05/09 — có cycleId là " +
                "lọc đúng luôn, thiếu cycleId van tự hạ pass-through)");
            Assert.AreEqual(FunnelShape.OnceOrdered, registry.ShapeOf("unregistered"),
                "Tên lạ giữ nguyên luật cũ");
        }

        [Test]
        public void OncePerCycle_IsNotFreeRepeatable()
        {
            // IsRepeatable nghĩa là LẶP TỰ DO (không lọc gì) — OncePerCycle có lọc nên không được
            // rơi vào nhánh đó
            var registry = new FunnelRegistry();
            registry.Register("clan_season", FunnelShape.OncePerCycle);

            Assert.IsFalse(registry.IsRepeatable("clan_season"));
        }

        [Test]
        public void GameCanRegisterItsOwn()
        {
            var registry = new FunnelRegistry();

            registry.Register("clan_war", FunnelShape.Repeatable);

            Assert.IsTrue(registry.IsRepeatable("clan_war"));
        }

        [Test]
        public void LatestRegistrationWins()
        {
            var registry = new FunnelRegistry();

            registry.Register(FFunnelName.BattlePass, FunnelShape.OnceOrdered);

            Assert.IsFalse(registry.IsRepeatable(FFunnelName.BattlePass));
        }

        [Test]
        public void BlankName_IsIgnored()
        {
            var registry = new FunnelRegistry();

            registry.Register(null, FunnelShape.Repeatable);
            registry.Register(string.Empty, FunnelShape.Repeatable);

            Assert.IsFalse(registry.IsRepeatable(null));
            Assert.IsFalse(registry.IsRepeatable(string.Empty));
        }
    }

    /// <summary>
    /// Key state lọc trùng của OncePerCycle — ghim hai bài học bằng test thay vì bằng trí nhớ.
    /// </summary>
    public class FunnelCycleKeyTests
    {
        [Test]
        public void SameStep_DifferentAction_AreDifferentKeys()
        {
            // milestone(3) rồi claim(3) là HAI bước thật cùng priority — key bỏ action là tái tạo
            // đúng cái bug làm luật ftue gãy với battle pass
            Assert.AreNotEqual(
                FunnelLogService.CycleStepKey("battle_pass", "season_5", FFunnelAction.Milestone, 3),
                FunnelLogService.CycleStepKey("battle_pass", "season_5", FFunnelAction.Claim, 3));
        }

        [Test]
        public void SameStep_DifferentCycle_AreDifferentKeys()
        {
            // Sang mùa mới tự sạch — không cần reset gì
            Assert.AreNotEqual(
                FunnelLogService.CycleStepKey("battle_pass", "season_5", FFunnelAction.Milestone, 3),
                FunnelLogService.CycleStepKey("battle_pass", "season_6", FFunnelAction.Milestone, 3));
        }

        [Test]
        public void SameEverything_IsTheSameKey()
        {
            Assert.AreEqual(
                FunnelLogService.CycleStepKey("battle_pass", "season_5", FFunnelAction.Claim, 3),
                FunnelLogService.CycleStepKey("battle_pass", "season_5", FFunnelAction.Claim, 3));
        }
    }
}
