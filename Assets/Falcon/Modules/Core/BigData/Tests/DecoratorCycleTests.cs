using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// Canh vòng chết DI của họ decorator — lỗi mà suite model-thuần KHÔNG bắt được (Play mode mới
    /// dựng đồ thị singleton): LogScheduleService → LogDecorService → mảng decorator (dựng hết lúc
    /// boot) → decorator ctor-inject một service → service ctor-inject LogScheduleService = deadlock.
    /// Án thật 2026-08-12: AdLogService inject AdRequestService (và IapLogService inject
    /// OfferImpression/PurchaseAttempt) — nổ ngay khi mở game.
    /// </summary>
    public class DecoratorCycleTests
    {
        [Test]
        public void NoDecorator_ReachesLogScheduleService_ThroughCtors()
        {
            var assembly = typeof(LogScheduleService).Assembly;
            var decorators = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ILogDecorator).IsAssignableFrom(t))
                .ToList();
            Assert.IsNotEmpty(decorators, "Registry decorator trống thì test này đang soi nhầm assembly");

            var offenders = new List<string>();
            foreach (var decorator in decorators)
            {
                var path = FindCtorPathTo(decorator, typeof(LogScheduleService));
                if (path != null) offenders.Add(string.Join(" -> ", path.Select(t => t.Name)));
            }

            Assert.IsEmpty(offenders,
                "Decorator với tới LogScheduleService qua chuỗi ctor — deadlock lúc Play mode. " +
                "Lấy service đó LƯỜI trong Decor (Xxx.Instance) thay vì ctor-inject:\n" +
                string.Join("\n", offenders));
        }

        /// <summary>BFS trên cạnh "ctor param kiểu class cùng assembly" — trả đường đi nếu chạm đích.</summary>
        private static List<Type> FindCtorPathTo(Type from, Type target)
        {
            var assembly = target.Assembly;
            var parent = new Dictionary<Type, Type> { [from] = null };
            var queue = new Queue<Type>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var ctor in current.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                foreach (var param in ctor.GetParameters())
                {
                    var type = param.ParameterType;
                    // Chỉ đi cạnh class cụ thể cùng assembly: interface do DI bind lúc chạy, phân
                    // tích tĩnh không biết bind vào đâu — mà cả hai án thật đều là cạnh class cụ thể
                    if (!type.IsClass || type.Assembly != assembly || parent.ContainsKey(type)) continue;

                    parent[type] = current;
                    if (type == target)
                    {
                        var path = new List<Type>();
                        for (var t = type; t != null; t = parent[t]) path.Add(t);
                        path.Reverse();
                        return path;
                    }

                    queue.Enqueue(type);
                }
            }

            return null;
        }
    }
}
