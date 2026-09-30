using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Falcon.Helpers.Devkit;
using NUnit.Framework;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class TieredUnsentLogRepositoryTests
    {
        private FLocalFileRepository _fileRepository;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _fileRepository = new FLocalFileRepository();
            _folder = "test_unsent_" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            var path = _fileRepository.GetFolder(_folder);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        private TieredUnsentLogRepository NewRepo()
        {
            return new TieredUnsentLogRepository(_fileRepository, _folder, false);
        }

        private static DataWrapper Wrapper(int i)
        {
            return new DataWrapper("{}", i, "evt_" + i, "pkg", "android");
        }

        private static void AssertSequence(IEnumerable<DataWrapper> wrappers, int from, int count)
        {
            var list = wrappers.ToList();
            Assert.AreEqual(count, list.Count);
            for (var i = 0; i < count; i++)
                Assert.AreEqual("evt_" + (from + i), list[i].@event, $"Sai thứ tự FIFO tại vị trí {i}");
        }

        [Test]
        public void PeekAndDrain_WithinRamCap_IsFifo()
        {
            var repo = NewRepo();
            for (var i = 0; i < 10; i++) repo.Enqueue(Wrapper(i));

            AssertSequence(repo.PeekAll(), 0, 10);
            AssertSequence(repo.Drain(4), 0, 4);
            AssertSequence(repo.PeekAll(), 4, 6);
        }

        [Test]
        public void OverRamCap_SpillsToSegmentFiles_AndDrainAllPreservesFifo()
        {
            var repo = NewRepo();
            for (var i = 0; i < 1200; i++) repo.Enqueue(Wrapper(i));

            // 500 nằm RAM, 700 tràn ra ngoài → ít nhất 3 segment 200 message đã xuống đĩa
            Assert.GreaterOrEqual(
                Directory.GetFiles(_fileRepository.GetFolder(_folder)).Length, 3,
                "Phần vượt RAM cap phải được spill thành segment file");

            // PeekAll chỉ trả tầng RAM (batch gửi tối đa RAM_CAP message/lần)
            AssertSequence(repo.PeekAll(), 0, 500);

            // DrainAll rút xuyên 2 tầng, đúng thứ tự enqueue
            AssertSequence(repo.DrainAll(), 0, 1200);
            Assert.AreEqual(0, repo.PeekAll().Count);
        }

        [Test]
        public void DrainThenPeek_RefillsFromSegmentsInOrder()
        {
            var repo = NewRepo();
            for (var i = 0; i < 800; i++) repo.Enqueue(Wrapper(i));

            AssertSequence(repo.Drain(500), 0, 500);
            // Sau khi RAM cạn, PeekAll phải nạp tiếp từ segment cũ nhất — nối đúng mạch FIFO
            var refilled = repo.PeekAll();
            Assert.Greater(refilled.Count, 0);
            AssertSequence(refilled.Take(200), 500, 200);
        }

        [Test]
        public void OnPostStop_PersistsQueue_NewInstanceRestoresIt()
        {
            var repo = NewRepo();
            for (var i = 0; i < 3; i++) repo.Enqueue(Wrapper(i));
            repo.OnPostStop();

            // Mô phỏng app bị kill lúc pause → lần chạy sau dựng instance mới từ cùng folder
            var restored = NewRepo();
            AssertSequence(restored.PeekAll(), 0, 3);
        }

        [Test]
        public void EnqueueWhileStopping_IsDurable()
        {
            var repo = NewRepo();
            repo.Enqueue(Wrapper(0));
            repo.OnPostStop();
            // AppPauseLogCollector enqueue log (session, heartbeat...) sau khi stop đã bắt đầu
            repo.Enqueue(Wrapper(1));
            repo.Enqueue(Wrapper(2));

            var restored = NewRepo();
            AssertSequence(restored.PeekAll(), 0, 3);
        }

        [Test]
        public void OnPreContinue_DiscardsHeadSnapshot_NoDuplicateOnNextRun()
        {
            var repo = NewRepo();
            repo.Enqueue(Wrapper(0));
            repo.OnPostStop();
            repo.OnPreContinue();

            // App resume rồi mới chết: RAM là nguồn chân lý, head snapshot phải bị bỏ
            // để message (có thể đã gửi sau resume) không bị nạp lại lần sau.
            var restored = NewRepo();
            Assert.AreEqual(0, restored.PeekAll().Count);
        }

        [Test]
        public void Remove_OnlySearchesInMemoryTiers()
        {
            var repo = NewRepo();
            var wrapper = Wrapper(0);
            repo.Enqueue(wrapper);
            Assert.IsTrue(repo.Remove(wrapper));
            Assert.IsFalse(repo.Remove(wrapper));
            Assert.AreEqual(0, repo.PeekAll().Count);
        }
    }
}
