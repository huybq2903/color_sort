using System.Collections.Generic;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class EntityLabelStateTests
    {
        [Test]
        public void SetThenBundle_CarriesTheLabels()
        {
            var state = new EntityLabelState();
            state.Set("42", "cluster", "hard_1", out _);
            state.Set("42", "tier", 3, out _);

            var bundle = state.BundleFor("42");

            Assert.AreEqual("hard_1", bundle["cluster"]);
            Assert.AreEqual(3, bundle["tier"], "Giá trị số giữ nguyên kiểu, không ép về chữ");
        }

        [Test]
        public void LabelsAreScopedPerEntity()
        {
            var state = new EntityLabelState();
            state.Set("42", "cluster", "hard_1", out _);

            Assert.IsNull(state.BundleFor("43"), "Nhãn màn 42 không được dính sang màn 43");
        }

        [Test]
        public void NoLabel_GivesNullNotEmptyMap()
        {
            // §H4: không có thì vắng mặt khỏi payload, đừng gửi map rỗng
            Assert.IsNull(new EntityLabelState().BundleFor("1"));
        }

        [Test]
        public void NullValue_RemovesTheKey()
        {
            // Mô hình stamp không có "bản tin gỡ": gỡ = thôi kèm key trong bundle
            var state = new EntityLabelState();
            state.Set("42", "cluster", "hard_1", out _);
            state.Set("42", "tier", 3, out _);

            state.Set("42", "cluster", null, out _);

            var bundle = state.BundleFor("42");
            Assert.IsFalse(bundle.ContainsKey("cluster"));
            Assert.IsTrue(bundle.ContainsKey("tier"));
        }

        [Test]
        public void RemovingTheLastKey_MakesBundleVanish()
        {
            var state = new EntityLabelState();
            state.Set("42", "cluster", "hard_1", out _);

            state.Set("42", "cluster", null, out _);

            Assert.IsNull(state.BundleFor("42"));
        }

        [Test]
        public void BlankKey_IsRejected()
        {
            var state = new EntityLabelState();

            Assert.AreEqual(EntityLabelSet.BlankKey, state.Set("42", null, "x", out _));
            Assert.AreEqual(EntityLabelSet.BlankKey, state.Set("42", string.Empty, "x", out _));
        }

        [Test]
        public void OversizedBundle_IsRejectedAndLeavesTheOldOneIntact()
        {
            // Thà thiếu nhãn mới còn hơn để server vứt cả bundle, kéo theo nhãn cũ đang chạy tốt
            var state = new EntityLabelState();
            state.Set("42", "keep", "me", out _);

            var huge = new string('x', EntityLabelState.MAX_BUNDLE_BYTES);
            var result = state.Set("42", "huge", huge, out var bytes);

            Assert.AreEqual(EntityLabelSet.BundleTooBig, result);
            Assert.Greater(bytes, EntityLabelState.MAX_BUNDLE_BYTES, "Báo số thật để dev tự chọn bỏ cái nào");
            Assert.AreEqual("me", state.BundleFor("42")["keep"]);
            Assert.IsFalse(state.BundleFor("42").ContainsKey("huge"));
        }

        [Test]
        public void ManyShortNumericLabels_FitTheBudget()
        {
            // Van đếm BYTE chứ không đếm key — đếm key thì phạt oan nhãn số
            var state = new EntityLabelState();

            for (var i = 0; i < 12; i++)
                Assert.AreEqual(EntityLabelSet.Ok, state.Set("42", "k" + i, i, out _));

            Assert.AreEqual(12, state.BundleFor("42").Count);
        }

        [Test]
        public void OverwritingAKey_DoesNotGrowTheBundle()
        {
            var state = new EntityLabelState();
            state.Set("42", "tier", 1, out var first);
            state.Set("42", "tier", 2, out var second);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void GameLabels_StopShortOfTheReservedRoom()
        {
            // Nhãn tự do chỉ được xài tới trần TRỪ phần giữ chỗ (EntityLabelService trừ hộ, state
            // chỉ nhận trần) — nếu không, game khai đầy bundle lúc load màn rồi OnLevelStart mới
            // tới là moves_limit bị đẩy ra, mà nó vừa bị bỏ khỏi đường flat nên mất luôn.
            var state = new EntityLabelState();
            var free = EntityLabelState.MAX_BUNDLE_BYTES - EntityLabelState.RESERVED_REGISTERED_BYTES;

            var result = state.Set("42", "big", new string('x', free), out var bytes, free);

            Assert.AreEqual(EntityLabelSet.BundleTooBig, result);
            Assert.Less(bytes, EntityLabelState.MAX_BUNDLE_BYTES,
                "Chưa chạm trần wire mà đã bị chặn — đúng ý: phần còn lại là chỗ giữ");
        }

        [Test]
        public void RegisteredKey_FitsEvenWhenGameLabelsFilledTheirShare()
        {
            var state = new EntityLabelState();
            var free = EntityLabelState.MAX_BUNDLE_BYTES - EntityLabelState.RESERVED_REGISTERED_BYTES;

            // Game xài hết phần của mình trước
            while (state.Set("42", "k" + state.BundleFor("42")?.Count, "vvvvvvvvvv", out _, free)
                   == EntityLabelSet.Ok) { }
            Assert.LessOrEqual(EntityLabelState.MeasureBytes(state.BundleFor("42")), free);

            Assert.AreEqual(EntityLabelSet.Ok,
                state.Set("42", FLevelLabelKey.MOVES_LIMIT, 30, out _));
            Assert.AreEqual(EntityLabelSet.Ok,
                state.Set("42", FLevelLabelKey.TIME_LIMIT_SEC, 90, out var bytes));

            Assert.AreEqual(30, state.BundleFor("42")[FLevelLabelKey.MOVES_LIMIT]);
            Assert.LessOrEqual(bytes, EntityLabelState.MAX_BUNDLE_BYTES, "Vẫn không được vượt trần wire");
        }

        [Test]
        public void Restore_BringsLabelsBackAfterRestart()
        {
            // Không persist thì mở lại app là level event mất nhãn cho tới khi game khai lại
            var state = new EntityLabelState();

            state.Restore("42", new Dictionary<string, object> { ["cluster"] = "hard_1" });

            Assert.AreEqual("hard_1", state.BundleFor("42")["cluster"]);
        }

        [Test]
        public void BundleIsACopy_CallerCannotMutateTheCache()
        {
            var state = new EntityLabelState();
            state.Set("42", "cluster", "hard_1", out _);

            state.BundleFor("42")["cluster"] = "tampered";

            Assert.AreEqual("hard_1", state.BundleFor("42")["cluster"]);
        }
    }
}
