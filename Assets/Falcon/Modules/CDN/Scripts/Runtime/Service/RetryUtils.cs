/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Falcon.Modules.CDN
{
    public static class RetryUtils
    {
        public static void Retry(Action action, int retryAttempts)
        {
            var loops = Math.Max(0, retryAttempts) + 1;
            Exception exception = null;
            for (var i = 0; i < loops; i++)
                try
                {
                    action();
                    return;
                }
                catch (Exception e)
                {
                    exception = e;
                }

            if (exception != null) throw exception;
        }

        public static T Retry<T>(Func<T> action, int retryAttempts)
        {
            var loops = Math.Max(0, retryAttempts) + 1;
            Exception exception = null;
            for (var i = 0; i < loops; i++)
                try
                {
                    return action();
                }
                catch (Exception e)
                {
                    exception = e;
                }

            if (exception != null) throw exception;
            return default;
        }

        public static async Task RetryTask(Func<Task> action, int retryAttempts, CancellationToken cancellationToken = default)
        {
            var loops = Math.Max(0, retryAttempts) + 1;
            Exception exception = null;
            for (var i = 0; i < loops; i++)
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await action();
                    return;
                }
                catch (Exception e)
                {
                    exception = e;
                }

            if (exception != null) throw exception;
        }

        public static async Task<T> RetryTask<T>(Func<Task<T>> action, int retryAttempts, CancellationToken cancellationToken = default)
        {
            var loops = Math.Max(0, retryAttempts) + 1;
            Exception exception = null;
            for (var i = 0; i < loops; i++)
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await action();
                }
                catch (Exception e)
                {
                    exception = e;
                }

            if (exception != null) throw exception;
            return default;
        }
    }
}