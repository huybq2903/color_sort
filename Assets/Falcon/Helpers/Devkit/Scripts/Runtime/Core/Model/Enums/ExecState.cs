/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// Represents the possible execution states of an asynchronous operation, task, or process.
    /// </summary>
    public enum ExecState
    {
        /// <summary>
        /// The operation has not yet begun.
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// The operation is currently ongoing.
        /// </summary>
        Processing = 1,

        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        Succeed = 2,

        /// <summary>
        /// The operation terminated due to an error.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// The operation was stopped prematurely by a cancellation request.
        /// </summary>
        Cancelled = 4
    }

    /// <summary>
    /// Provides extension methods for the <see cref="ExecState"/> enum,
    /// offering convenient ways to check the state of an execution.
    /// </summary>
    public static class ExecStateExtensions
    {
        /// <summary>
        /// Determines whether an operation in the current <see cref="ExecState"/> can be started or restarted.
        /// </summary>
        /// <param name="execState">The current execution state.</param>
        /// <returns><see langword="true"/> if the operation can be started or restarted; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// An operation can typically be started or restarted if it is in a <see cref="ExecState.NotStarted"/>,
        /// <see cref="ExecState.Failed"/>, or <see cref="ExecState.Cancelled"/> state.
        /// </remarks>
        public static bool CanStart(this ExecState execState)
        {
            return execState is ExecState.NotStarted or ExecState.Failed or ExecState.Cancelled;
        }

        /// <summary>
        /// Determines whether an operation in the current <see cref="ExecState"/> completed successfully.
        /// </summary>
        /// <param name="execState">The current execution state.</param>
        /// <returns><see langword="true"/> if the operation succeeded; otherwise, <see langword="false"/>.</returns>
        public static bool IsSuccess(this ExecState execState)
        {
            return execState == ExecState.Succeed;
        }

        /// <summary>
        /// Determines whether an operation in the current <see cref="ExecState"/> has completed, regardless of the outcome.
        /// </summary>
        /// <param name="execState">The current execution state.</param>
        /// <returns><see langword="true"/> if the operation is in a completed state; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// An operation is considered "done" if its state is <see cref="ExecState.Succeed"/>,
        /// <see cref="ExecState.Failed"/>, or <see cref="ExecState.Cancelled"/>.
        /// </remarks>
        public static bool IsDone(this ExecState execState)
        {
            return execState is ExecState.Succeed or ExecState.Failed or ExecState.Cancelled;
        }
    }
}