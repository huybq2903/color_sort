/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using System.Collections.Concurrent;
using System.Linq;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Registry decorator: gom mọi ILogDecorator qua DI, sort theo Priority một lần,
    /// dispatch theo concrete type của log (match IsAssignableFrom, cache per type).
    /// <br/>Decorate-once: mỗi log instance chỉ được trang trí đúng MỘT lần (flag trên PlainLog) —
    /// nhờ vậy cửa trước (FalconBigDataController) và backstop (LogScheduleService) cùng tồn tại
    /// mà không bao giờ decor đôi, và đường retry/re-enqueue không làm createId/sendId nhảy nấc.
    /// </summary>
    public class LogDecorService : MySingleton<LogDecorService>
    {
        private readonly ILogDecorator[] _decorators;
        private readonly ConcurrentDictionary<Type, ILogDecorator[]> _byType = new();

        public LogDecorService(ILogDecorator[] decorators)
        {
            _decorators = decorators.OrderBy(decorator => decorator.Priority).ToArray();
        }

        public void Decor(IDataLog log)
        {
            if (log is PlainLog plainLog)
            {
                if (plainLog.decorated) return;
                // Set flag TRƯỚC khi chạy chain: nếu 1 decorator lỗi giữa chừng thì log thiếu enrich
                // còn hơn là retry decor lại từ đầu làm counter monotonic (createId/sendId) nhảy đôi.
                plainLog.decorated = true;
            }

            foreach (var decorator in _byType.GetOrAdd(log.GetType(), BuildFor))
                decorator.Decor(log);
        }

        private ILogDecorator[] BuildFor(Type logType)
        {
            return _decorators.Where(decorator => decorator.DecorLogType.IsAssignableFrom(logType)).ToArray();
        }
    }
}
