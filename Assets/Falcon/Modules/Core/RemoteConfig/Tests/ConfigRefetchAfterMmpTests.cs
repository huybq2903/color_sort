using System;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig.Tests
{
    public class ConfigRefetchAfterMmpTests
    {
        private const string FALCON_MMP_STARTED = "falcon_mmp_started";

        // TUYỆT ĐỐI KHÔNG khai subclass của ARefetchConfigAfterMmp trong test:
        // FReflection sẽ quét thấy nó ở editor play mode của project game (Tests folder đi kèm framework)
        // → container tự tạo + inject vào service thật → project bị opt-in refetch ngoài ý muốn.
        // Service chỉ kiểm tra optIns.Length nên mảng chứa phần tử null là đủ cho test.
        private static ARefetchConfigAfterMmp[] OptIns(int count) => new ARefetchConfigAfterMmp[count];

        // NoAutoCreate: TestableService cũng là IMySingleton (kế thừa service thật) — không có attribute này
        // thì container ở editor play của game có thể tự tạo nó y như vụ TestOptIn ở trên.
        [NoAutoCreate]
        private class TestableService : ConfigRefetchAfterMmpService
        {
            public bool initDone = true;
            public int refetchCalls;
            public Action scheduledAction;

            public TestableService(ARefetchConfigAfterMmp[] optIns) : base(null, optIns)
            {
            }

            protected override bool IsInitDone => initDone;

            protected override void ScheduleAfterInit(Action action)
            {
                scheduledAction = action;
            }

            protected override Task<bool> DoRefetch()
            {
                refetchCalls++;
                return Task.FromResult(true);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // GameEvent là registry static toàn cục — dọn để test không rò listener sang nhau
            GameEvent.UnregisterAll(FALCON_MMP_STARTED);
        }

        [Test]
        public void NoOptIn_DoesNotListen_ZeroBehavior()
        {
            var service = new TestableService(OptIns(0));
            service.OnPostConstruct();

            GameEvent.Emit(FALCON_MMP_STARTED);

            Assert.AreEqual(0, service.refetchCalls, "Không opt-in thì tuyệt đối không có hành vi gì");
            Assert.IsNull(service.scheduledAction);
        }

        [Test]
        public void OptIn_RefetchesWhenMmpStarted_AfterInitDone()
        {
            var service = new TestableService(OptIns(1));
            service.OnPostConstruct();

            GameEvent.Emit(FALCON_MMP_STARTED);

            Assert.AreEqual(1, service.refetchCalls);
        }

        [Test]
        public void OptIn_MmpStartedBeforeInitDone_DeferredUntilInitCompletes()
        {
            var service = new TestableService(OptIns(1))
            {
                initDone = false
            };
            service.OnPostConstruct();

            GameEvent.Emit(FALCON_MMP_STARTED);
            Assert.AreEqual(0, service.refetchCalls, "Init chưa xong thì chưa được fetch (tránh đụng single-flight với fetch của Init)");
            Assert.IsNotNull(service.scheduledAction, "Phải được xếp lịch chạy sau khi init xong");

            service.scheduledAction();
            Assert.AreEqual(1, service.refetchCalls);
        }

        [Test]
        public void MultipleOptIns_StillSingleListener_SingleRefetch()
        {
            // 2 subclass lỡ khai trong project → vẫn chỉ 1 listener, 1 lần refetch
            var service = new TestableService(OptIns(2));
            service.OnPostConstruct();

            GameEvent.Emit(FALCON_MMP_STARTED);

            Assert.AreEqual(1, service.refetchCalls);
        }

        [Test]
        public void BeforeMmpStarted_NoRefetch()
        {
            var service = new TestableService(OptIns(1));
            service.OnPostConstruct();

            Assert.AreEqual(0, service.refetchCalls, "Chỉ refetch khi MMP thực sự init xong");
        }
    }
}
