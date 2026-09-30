/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface IMyAction
    {
        IThreadPool ThreadPool { get; }
        Exception Exception { get; }
        ExecState State { get; }

        /// <summary>
        ///     Call this schedule and the function will arrange itself to be executed,
        /// </summary>
        void Schedule();

        bool Cancel();

        void Invoke();

        bool CanInvoke();
    }

    public interface IContinuableAction : IMyAction
    {
    }

    public interface IFollowAction : IMyAction
    {
    }

    public interface IStartAction : IContinuableAction
    {
    }

    public interface IChainAction : IContinuableAction, IFollowAction
    {
    }

    public interface IEndAction : IFollowAction
    {
    }

    public static class MyActionExtensions
    {
        public static async Task WaitTillDone(this IMyAction action, CancellationToken cancellationToken = default)
        {
            while (!action.State.IsDone())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
        
        public static async Task WaitTillSuccess(this IMyAction action, CancellationToken cancellationToken = default)
        {
            while (!action.State.IsDone())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (!action.State.IsSuccess())
            {
                throw action.Exception;
            }
        }
    }
}