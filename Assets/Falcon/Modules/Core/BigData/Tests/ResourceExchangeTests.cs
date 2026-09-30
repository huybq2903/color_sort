using System.Linq;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class ResourceExchangeTests
    {
        private static ResourceParam Item(string itemId, long amount = 1) =>
            new() { itemId = itemId, currency = itemId, itemType = "test", amount = amount };

        [Test]
        public void AllLegs_ShareOneId()
        {
            // "Mua 3 booster bằng 500 gold": 2 event rời, không có id chung thì không tính được giá thật
            var gold = Item("gold", 500);
            var booster = Item("booster", 3);

            ResourceExchange.Combine(new[] { gold }, new[] { booster });

            Assert.IsNotNull(gold.exchangeId);
            Assert.AreEqual(gold.exchangeId, booster.exchangeId);
        }

        [Test]
        public void Bundle_MultiLegs_ShareOneId()
        {
            // gems → gold + booster + heart
            var gems = Item("gems", 100);
            var outs = new[] { Item("gold"), Item("booster"), Item("heart") };

            ResourceExchange.Combine(new[] { gems }, outs);

            Assert.AreEqual(1, outs.Select(p => p.exchangeId).Append(gems.exchangeId).Distinct().Count());
        }

        [Test]
        public void FlowType_IsSetBySide()
        {
            var sink = Item("gold");
            var source = Item("booster");

            ResourceExchange.Combine(new[] { sink }, new[] { source });

            Assert.AreEqual(FlowType.Sink, sink.flowType);
            Assert.AreEqual(FlowType.Source, source.flowType);
        }

        [Test]
        public void GameSuppliedId_WinsForWholeExchange()
        {
            // Game đã có id giao dịch từ server → dùng luôn, khớp được với dữ liệu bên đó
            var sink = Item("gold");
            sink.exchangeId = "server-tx-42";
            var source = Item("booster");

            ResourceExchange.Combine(new[] { sink }, new[] { source });

            Assert.AreEqual("server-tx-42", sink.exchangeId);
            Assert.AreEqual("server-tx-42", source.exchangeId);
        }

        [Test]
        public void TwoExchanges_DoNotShareId()
        {
            var first = Item("gold");
            var second = Item("gold");

            ResourceExchange.Combine(new[] { first }, null);
            ResourceExchange.Combine(new[] { second }, null);

            Assert.AreNotEqual(first.exchangeId, second.exchangeId);
        }

        [Test]
        public void NullSides_AreTolerated()
        {
            Assert.IsEmpty(ResourceExchange.Combine(null, null));
            Assert.AreEqual(1, ResourceExchange.Combine(null, new[] { Item("gold") }).Count);
        }

        [Test]
        public void NullEntries_AreDropped()
        {
            var result = ResourceExchange.Combine(new[] { Item("gold"), null }, null);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void OrderIsSinksThenSources()
        {
            var result = ResourceExchange.Combine(new[] { Item("gold") }, new[] { Item("booster") });

            Assert.AreEqual("gold", result[0].itemId);
            Assert.AreEqual("booster", result[1].itemId);
        }
    }
}
