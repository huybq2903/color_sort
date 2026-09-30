/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [NoLazy]
    public class MainThreadPool: MonoSingleton<MainThreadPool>, IThreadPool, IPioneer, IPostConstruct
    {
        private readonly MyConcurrentQueue<IMyAction> _actionQueue = new();
        private int? MainThreadId { get; set; }

        public int Size
        {
            get => 1;
            set => UtilLogger.Instance.Warning($"MainThreadPool size is constant of 1, ignoring set value {value}");
        }

        public void Add(IMyAction action)
        {
            if (IsMainThread() && action.CanInvoke())
            {
                action.Invoke();
                return;
            }
            _actionQueue.Enqueue(action);
        }

        public bool Remove(IMyAction action)
        {
            return _actionQueue.Remove(action);
        }

        public bool IsMainThread()
        {
            if (!MainThreadId.HasValue)
            {
                throw new ArithmeticException("Wtf how does MainThreadId is null");
            }

            return Thread.CurrentThread.ManagedThreadId == MainThreadId.Value;
        }

        public void OnPreContinue()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public void OnPostConstruct()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private void Update()
        {
            for (var i = 0; i < _actionQueue.Count; i++)
            {
                if (_actionQueue.TryDequeue(out var action))
                {
                    if (action.CanInvoke())
                    {
                        action.Invoke();
                        return;
                    }
                    _actionQueue.Enqueue(action);
                }
                else
                {
                    return;
                }
            }
        }
    }
}