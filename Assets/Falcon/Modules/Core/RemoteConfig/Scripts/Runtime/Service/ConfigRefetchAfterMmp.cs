/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-30
 */

using System;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    /// <summary>
    /// OPT-IN: game khai báo 1 subclass rỗng của class này là sau khi MMP init xong
    /// (event "falcon_mmp_started"), SDK sẽ tự TryFetch remote config MỘT lần nữa —
    /// bản fetch này mang user params mới nhất (gồm các param attribution do MMP đóng góp)
    /// nên server lọc/segment config theo MMP được. Không subclass = không có gì chạy
    /// (mặc định giữ nguyên: config độc lập MMP để không chặn performance).
    /// <code>
    /// // Trong project game, chỉ cần đúng 1 dòng:
    /// public class MyConfigRefetchAfterMmp : ARefetchConfigAfterMmp { }
    /// </code>
    /// <br/>⚠ CHỈ AN TOÀN để lấy CONFIG VALUES — KHÔNG AN TOÀN cho A/B TESTING
    /// (variant có thể đổi giữa phiên sau khi user đã exposed → số liệu experiment hỏng).
    /// Đọc kỹ RefetchConfigAfterMmp.md trước khi dùng.
    /// </summary>
    public abstract class ARefetchConfigAfterMmp : IMySingleton
    {
    }

    [NoLazy]
    public class ConfigRefetchAfterMmpService : IMySingleton, IPostConstruct
    {
        private const string FALCON_MMP_STARTED = "falcon_mmp_started";

        private readonly FConfigInitService _configInitService;
        private readonly ARefetchConfigAfterMmp[] _optIns;

        public ConfigRefetchAfterMmpService(
            FConfigInitService configInitService,
            ARefetchConfigAfterMmp[] optIns)
        {
            _configInitService = configInitService;
            _optIns = optIns;
        }

        public void OnPostConstruct()
        {
            // Không ai opt-in → không đăng ký listener, zero hành vi, zero chi phí
            if (_optIns.Length == 0) return;
            GameEvent.Register(FALCON_MMP_STARTED, OnMmpStarted, null);
        }

        private void OnMmpStarted()
        {
            // Đợi init phase xong rồi mới refetch: tránh đụng single-flight với lần fetch của Init
            if (IsInitDone) Refetch();
            else ScheduleAfterInit(Refetch);
        }

        private void Refetch()
        {
            _ = RefetchAsync();
        }

        private async Task RefetchAsync()
        {
            var success = await DoRefetch();
            if (success)
                BaseSystemLogger.Instance.Info("Remote config refetched after MMP init (OnUpdateFromNet fired)");
            else
                BaseSystemLogger.Instance.Warning("Remote config refetch after MMP init failed or another fetch was running");
        }

        // 3 seam môi trường (static/global) mở virtual để test override — không đổi behavior runtime
        protected virtual bool IsInitDone => InitService.AllInitState.IsDone();

        protected virtual void ScheduleAfterInit(Action action)
        {
            new WaitInit(action).Schedule();
        }

        protected virtual Task<bool> DoRefetch()
        {
            return _configInitService.TryFetch();
        }
    }
}
