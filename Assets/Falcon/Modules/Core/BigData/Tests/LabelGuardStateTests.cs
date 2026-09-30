using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class LabelGuardStateTests
    {
        [Test]
        public void PlainLabel_IsAccepted()
        {
            Assert.AreEqual(LabelCheck.Ok, new LabelGuardState().Check("vip_tier", "gold", userScope: true));
        }

        [Test]
        public void ValueKeepsItsType_GuardDoesNotCare()
        {
            // §D11: nhãn gánh cả "tham số động" — bool/số phải qua được
            var guard = new LabelGuardState();

            Assert.AreEqual(LabelCheck.Ok, guard.Check("is_whale", true, userScope: true));
            Assert.AreEqual(LabelCheck.Ok, guard.Check("spend_bucket", 3, userScope: true));
        }

        [Test]
        public void ManyDistinctValues_AreFine()
        {
            // Bản trước chặn key đẻ > 20 giá trị. Nay nhãn được phép là tham số động nên chặn thế
            // là chặn đúng công dụng mới.
            var guard = new LabelGuardState();

            for (var i = 0; i < 100; i++)
                Assert.AreEqual(LabelCheck.Ok, guard.Check("score_bucket", i, userScope: true));
        }

        [Test]
        public void WireColumnName_IsRejected_AndDoesNotBurnAUserKeySlot()
        {
            // §H1: nhãn trùng tên cột hợp đồng đứng cạnh cột thật trên mọi dòng — hai số cùng tên
            // khác nghĩa. Và key bị chặn thì không được ăn mất slot trong trần 20 key.
            var guard = new LabelGuardState();

            Assert.AreEqual(LabelCheck.ReservedKey, guard.Check("win_streak", 5, userScope: true));
            Assert.AreEqual(LabelCheck.ReservedKey, guard.Check("winStreak", 5, userScope: false));

            for (var i = 0; i < LabelGuardState.MAX_USER_LABEL_KEYS; i++)
                Assert.AreEqual(LabelCheck.Ok, guard.Check("key_" + i, "v", userScope: true),
                    "Key bị chặn ở trên không được chiếm slot");
        }

        [Test]
        public void SuggestedAlternativeName_PassesFreely()
        {
            Assert.AreEqual(LabelCheck.Ok, new LabelGuardState().Check("streak_status", 2, userScope: true));
        }

        [Test]
        public void BlankKey_IsRejected()
        {
            var guard = new LabelGuardState();

            Assert.AreEqual(LabelCheck.BlankKey, guard.Check(null, "x", userScope: true));
            Assert.AreEqual(LabelCheck.BlankKey, guard.Check(string.Empty, "x", userScope: true));
        }

        [Test]
        public void LongTextValue_IsRejectedBeforeTheServerDropsIt()
        {
            var guard = new LabelGuardState();
            var tooLong = new string('x', LabelGuardState.MAX_VALUE_LENGTH + 1);

            Assert.AreEqual(LabelCheck.ValueTooLong, guard.Check("note", tooLong, userScope: true));
            Assert.AreEqual(LabelCheck.Ok,
                guard.Check("note", new string('x', LabelGuardState.MAX_VALUE_LENGTH), userScope: true));
        }

        [Test]
        public void LongValue_OnlyMattersForText()
        {
            // Trần 64 ký tự là của value CHỮ; số dài không phải chuyện đó
            Assert.AreEqual(LabelCheck.Ok, new LabelGuardState().Check("big", long.MaxValue, userScope: true));
        }

        [Test]
        public void UserKeyCap_StopsTheKeyThatWouldBeDropped()
        {
            var guard = new LabelGuardState();
            for (var i = 0; i < LabelGuardState.MAX_USER_LABEL_KEYS; i++)
                Assert.AreEqual(LabelCheck.Ok, guard.Check("key_" + i, "v", userScope: true));

            Assert.AreEqual(LabelCheck.TooManyUserKeys, guard.Check("key_over", "v", userScope: true));
        }

        [Test]
        public void UpdatingAnExistingKey_IsNotBlockedByTheCap()
        {
            var guard = new LabelGuardState();
            for (var i = 0; i < LabelGuardState.MAX_USER_LABEL_KEYS; i++) guard.Check("key_" + i, "v", userScope: true);

            Assert.AreEqual(LabelCheck.Ok, guard.Check("key_0", "v2", userScope: true));
        }

        [Test]
        public void RemovingALabel_FreesASlot()
        {
            // null = GỠ nhãn, và gỡ phải luôn đi lọt — đó là cách duy nhất hạ số key xuống
            var guard = new LabelGuardState();
            for (var i = 0; i < LabelGuardState.MAX_USER_LABEL_KEYS; i++) guard.Check("key_" + i, "v", userScope: true);

            Assert.AreEqual(LabelCheck.Ok, guard.Check("key_0", null, userScope: true));
            Assert.AreEqual(LabelCheck.Ok, guard.Check("key_new", "v", userScope: true));
        }

        [Test]
        public void RestoredKeys_CountTowardTheLifetimeCap()
        {
            // Trần 20 key của server đếm theo ĐỜI user — van nạp lại tập key từ phiên trước thì
            // phiên mới không lách được trần
            var guard = new LabelGuardState();
            var saved = new string[LabelGuardState.MAX_USER_LABEL_KEYS];
            for (var i = 0; i < saved.Length; i++) saved[i] = "old_" + i;

            guard.Restore(saved);

            Assert.AreEqual(LabelCheck.TooManyUserKeys, guard.Check("brand_new", "v", userScope: true));
            Assert.AreEqual(LabelCheck.Ok, guard.Check("old_3", "v2", userScope: true),
                "Cập nhật key cũ vẫn phải đi lọt");
            Assert.AreEqual(LabelCheck.Ok, guard.Check("old_0", null, userScope: true), "Gỡ luôn đi lọt");
            Assert.AreEqual(LabelCheck.Ok, guard.Check("brand_new", "v", userScope: true),
                "Gỡ xong thì slot trống thật");
        }

        [Test]
        public void UserKeys_SnapshotRoundTripsThroughRestore()
        {
            var first = new LabelGuardState();
            first.Check("vip_tier", "gold", userScope: true);
            first.Check("cohort", "b", userScope: true);
            first.Check("cohort", null, userScope: true);

            var second = new LabelGuardState();   // mô phỏng process mới
            second.Restore(first.UserKeys);

            CollectionAssert.AreEquivalent(new[] { "vip_tier" }, second.UserKeys,
                "Key đã gỡ không được sống lại sau restart");
        }

        [Test]
        public void InstanceLabels_HaveNoKeyCap()
        {
            // Trần 20 key là của nhãn USER (nó theo profile vào mọi dòng event); nhãn instance
            // chỉ nằm trên đúng bản tin gán nên không chịu trần đó
            var guard = new LabelGuardState();

            for (var i = 0; i < 100; i++)
                Assert.AreEqual(LabelCheck.Ok, guard.Check("key_" + i, "v", userScope: false));
        }
    }
}
