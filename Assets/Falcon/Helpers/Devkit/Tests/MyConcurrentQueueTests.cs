using System.Linq;
using Falcon.Helpers.Devkit;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit.Tests
{
    public class MyConcurrentQueueTests
    {
        [Test]
        public void EnqueueDequeue_IsFifo()
        {
            var queue = new MyConcurrentQueue<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(3, queue.Dequeue());
        }

        [Test]
        public void TryDequeue_EmptyQueue_ReturnsFalse()
        {
            var queue = new MyConcurrentQueue<string>();
            Assert.IsFalse(queue.TryDequeue(out _));
        }

        [Test]
        public void EnqueueAll_AddsAllItemsInOrder()
        {
            var queue = new MyConcurrentQueue<int>();
            queue.EnqueueAll(new[] { 1, 2, 3 });
            Assert.AreEqual(3, queue.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, queue.ToList());
        }

        [Test]
        public void DrainAll_ReturnsAllInOrderAndEmptiesQueue()
        {
            var queue = new MyConcurrentQueue<int>(new[] { 1, 2, 3 });
            var drained = queue.DrainAll();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, drained);
            Assert.IsTrue(queue.IsEmpty);
        }

        [Test]
        public void Drain_TakesFromHeadOnly()
        {
            // TryFlush của BigData dựa vào hành vi này: PeekAll rồi Drain(count) phải bỏ đúng các phần tử đầu
            var queue = new MyConcurrentQueue<int>(new[] { 1, 2, 3 });
            var drained = queue.Drain(2);
            CollectionAssert.AreEqual(new[] { 1, 2 }, drained);
            Assert.AreEqual(1, queue.Count);
            Assert.AreEqual(3, queue.Peek());
        }

        [Test]
        public void Remove_ExistingItem_RemovesAndReturnsTrue()
        {
            var queue = new MyConcurrentQueue<int>(new[] { 1, 2, 3 });
            Assert.IsTrue(queue.Remove(2));
            CollectionAssert.AreEqual(new[] { 1, 3 }, queue.ToList());
        }

        [Test]
        public void Remove_MissingItem_ReturnsFalse()
        {
            var queue = new MyConcurrentQueue<int>(new[] { 1 });
            Assert.IsFalse(queue.Remove(99));
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void Enumeration_ListsItemsWithoutRemoving()
        {
            var queue = new MyConcurrentQueue<int>(new[] { 1, 2 });
            var snapshot = queue.ToList();
            CollectionAssert.AreEqual(new[] { 1, 2 }, snapshot);
            Assert.AreEqual(2, queue.Count);
        }
    }
}
