using Falcon.Helpers.Devkit;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit.Tests
{
    public class LazyValTests
    {
        [Test]
        public void Value_SupplierCalledOnce_AndCached()
        {
            var calls = 0;
            var lazy = new LazyVal<int>(() =>
            {
                calls++;
                return 42;
            });

            Assert.AreEqual(42, lazy.Value);
            Assert.AreEqual(42, lazy.Value);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Reset_ForcesRecompute()
        {
            var calls = 0;
            var lazy = new LazyVal<int>(() => ++calls);

            Assert.AreEqual(1, lazy.Value);
            lazy.Reset();
            Assert.AreEqual(2, lazy.Value);
            Assert.AreEqual(2, calls);
        }
    }
}
