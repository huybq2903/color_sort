/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Falcon.Shared.Common
{
    /// <summary>Chuỗi hành động chạy tuần tự, step đăng ký từ ngoài theo order.</summary>
    public class FlowSequence
    {
        private readonly List<Step> _steps = new();
        private readonly List<IDisposable> _subscriptions = new();

        private class Step
        {
            public int Order;
            public Func<CancellationToken, UniTask> Run;
        }

        /// <summary>Đăng ký 1 step. Dispose để bỏ đăng ký. Order nhỏ chạy trước.</summary>
        public IDisposable Add(Func<CancellationToken, UniTask> step, int order = 0)
        {
            var entry = new Step { Order = order, Run = step };
            _steps.Add(entry);

            var subscription = new Unsubscriber(this, entry);
            _subscriptions.Add(subscription);
            return subscription;
        }

        /// <summary>Step đồng bộ (không chờ gì).</summary>
        public IDisposable Add(Action step, int order = 0) => Add(_ => { step(); return UniTask.CompletedTask; }, order);

        /// <summary>Step kiểu callback: gọi next() khi xong. Dùng được với bất kỳ object/API nào.</summary>
        public IDisposable Add(Action<Action> step, int order = 0) => Add(ct =>
            {
                var tcs = new UniTaskCompletionSource();
                var registration = ct.Register(() => tcs.TrySetCanceled(ct));
                step(() => { registration.Dispose(); tcs.TrySetResult(); });
                return tcs.Task;
            }, order);

        private CancellationTokenSource _cts;

        /// <summary>Dừng chuỗi đang chạy. Gọi được từ trong step (revive xong khỏi hiện popup thua) lẫn từ ngoài.</summary>
        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>Chạy chuỗi, tự huỷ lần chạy trước. Trả false nếu bị Stop/huỷ. Step lỗi chỉ log rồi đi tiếp.</summary>
        public async UniTask<bool> Run(CancellationToken ct = default)
        {
            Stop();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _cts.Token;

            foreach (var step in _steps.OrderBy(s => s.Order).ToArray()) // ToArray: step có thể tự gỡ đăng ký
            {
                if (token.IsCancellationRequested) return false;
                try
                {
                    await step.Run(token);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[FlowSequence] step order {step.Order} lỗi: {e}");
                }
            }

            return !token.IsCancellationRequested;
        }

        /// <summary>Dừng chuỗi đang chạy và gỡ toàn bộ step đã đăng ký.</summary>
        public void Clear()
        {
            Stop();
            foreach (var subscription in _subscriptions.ToArray()) // ToArray: Dispose có sửa _subscriptions
                subscription.Dispose();
            _subscriptions.Clear();
            _steps.Clear();
        }

        private class Unsubscriber : IDisposable
        {
            private readonly FlowSequence _flow;
            private readonly Step _entry;
            public Unsubscriber(FlowSequence flow, Step entry) { _flow = flow; _entry = entry; }

            public void Dispose()
            {
                _flow._steps.Remove(_entry);
                _flow._subscriptions.Remove(this);
            }
        }
    }

    /// <summary>Registry theo tên, để module không cần biết nhau (giống GameEvent).</summary>
    public static class Flow
    {
        private static readonly Dictionary<string, FlowSequence> _flows = new();

        public static FlowSequence Of(string id)
        {
            if (!_flows.TryGetValue(id, out var flow)) _flows[id] = flow = new FlowSequence();
            return flow;
        }

        /// <summary>Clear mọi flow (vd: rời scene in-game).</summary>
        public static void ClearAll()
        {
            foreach (var flow in _flows.Values) flow.Clear();
            _flows.Clear();
        }
    }
}
