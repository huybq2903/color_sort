using Falcon.Helpers.Devkit;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit.Tests
{
    public class AtomicRefTests
    {
        [Test]
        public void Value_DefaultCtor_ReturnsDefault()
        {
            var atomic = new AtomicRef<int>();
            Assert.AreEqual(0, atomic.Value);
        }

        [Test]
        public void Value_SetThenGet_ReturnsSetValue()
        {
            var atomic = new AtomicRef<string>("a");
            atomic.Value = "b";
            Assert.AreEqual("b", atomic.Value);
        }

        [Test]
        public void SetIfNotEq_SameValue_ReturnsFalse()
        {
            var atomic = new AtomicRef<int>(5);
            Assert.IsFalse(atomic.SetIfNotEq(5));
            Assert.AreEqual(5, atomic.Value);
        }

        [Test]
        public void SetIfNotEq_DifferentValue_SetsAndReturnsTrue()
        {
            var atomic = new AtomicRef<int>(5);
            Assert.IsTrue(atomic.SetIfNotEq(7));
            Assert.AreEqual(7, atomic.Value);
        }

        [Test]
        public void CompareAndSet_ExpectMatches_UpdatesAndReturnsTrue()
        {
            var atomic = new AtomicRef<bool>(false);
            Assert.IsTrue(atomic.CompareAndSet(false, true));
            Assert.IsTrue(atomic.Value);
        }

        [Test]
        public void CompareAndSet_ExpectDoesNotMatch_ReturnsFalseAndKeepsValue()
        {
            var atomic = new AtomicRef<bool>(true);
            Assert.IsFalse(atomic.CompareAndSet(false, true));
            Assert.IsTrue(atomic.Value);
        }

        [Test]
        public void CompareAndSet_SecondClaimFails_UntilReleased()
        {
            // Mô phỏng single-flight claim/release như FConfigInitService.TryFetch
            var claimed = new AtomicRef<bool>(false);
            Assert.IsTrue(claimed.CompareAndSet(false, true));
            Assert.IsFalse(claimed.CompareAndSet(false, true));
            claimed.Value = false;
            Assert.IsTrue(claimed.CompareAndSet(false, true));
        }

        [Test]
        public void Compute_AppliesFunctionAndReturnsNewValue()
        {
            var atomic = new AtomicRef<int>(10);
            var result = atomic.Compute(v => v + 5);
            Assert.AreEqual(15, result);
            Assert.AreEqual(15, atomic.Value);
        }

        [Test]
        public void ComputeIfEqual_Matches_ComputesAndReturnsTrue()
        {
            var atomic = new AtomicRef<int>(1);
            Assert.IsTrue(atomic.ComputeIfEqual(1, v => v + 1, out var result));
            Assert.AreEqual(2, result);
            Assert.AreEqual(2, atomic.Value);
        }

        [Test]
        public void ComputeIfEqual_DoesNotMatch_ReturnsFalseAndKeepsValue()
        {
            var atomic = new AtomicRef<int>(1);
            Assert.IsFalse(atomic.ComputeIfEqual(9, v => v + 1, out var result));
            Assert.AreEqual(1, result);
            Assert.AreEqual(1, atomic.Value);
        }
    }
}
