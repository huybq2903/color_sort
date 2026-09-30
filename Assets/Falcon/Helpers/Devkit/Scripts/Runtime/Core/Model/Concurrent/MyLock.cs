/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-17
 */
using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class MyLock
    {
        private readonly object _lockObj = new();

        /// <summary>
        /// Block đến khi lock được.
        /// </summary>
        public LockKey Lock()
        {
            Monitor.Enter(_lockObj);
            return new LockKey(_lockObj);
        }

        /// <summary>
        /// Thử lock (non-blocking). Trả về true nếu lock thành công.
        /// </summary>
        public bool TryLock(out LockKey outKey, int millisecondsTimeout = 0)
        {
            if (Monitor.TryEnter(_lockObj, millisecondsTimeout))
            {
                outKey = new LockKey(_lockObj);
                return true;
            }

            outKey = null;
            return false;
        }
    }

    public sealed class LockKey : IDisposable
    {
        private object _lockObj;

        internal LockKey(object lockObj)
        {
            _lockObj = lockObj;
        }

        public void Dispose()
        {
            if (_lockObj == null) return;
            Monitor.Exit(_lockObj);
            _lockObj = null;
        }
    }
}