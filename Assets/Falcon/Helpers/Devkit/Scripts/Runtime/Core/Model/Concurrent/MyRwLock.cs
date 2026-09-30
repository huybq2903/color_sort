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
    public class MyRwLock : ReaderWriterLockSlim
    {
        public MyRwLock()
        {
        }

        public MyRwLock(LockRecursionPolicy recursionPolicy) : base(recursionPolicy)
        {
        }

        public void LockRead(Action action)
        {
            EnterUpgradeableReadLock();
            try
            {
                action();
            }
            finally
            {
                ExitUpgradeableReadLock();
            }
        }

        public T LockRead<T>(Func<T> action)
        {
            EnterUpgradeableReadLock();
            try
            {
                return action();
            }
            finally
            {
                ExitUpgradeableReadLock();
            }
        }

        public void LockWrite(Action action)
        {
            EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public T LockWrite<T>(Func<T> action)
        {
            EnterWriteLock();
            try
            {
                return action();
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public bool TryLockRead(Action action)
        {
            if (!TryEnterUpgradeableReadLock(0)) return false;
            try
            {
                action();
                return true;
            }
            finally
            {
                ExitUpgradeableReadLock();
            }
        }

        public bool TryLockRead<T>(Func<T> action, out T result)
        {
            if (!TryEnterUpgradeableReadLock(0))
            {
                result = default;
                return false;
            }

            try
            {
                result = action();
                return true;
            }
            finally
            {
                ExitUpgradeableReadLock();
            }
        }

        public bool TryLockWrite(Action action)
        {
            if (!TryEnterWriteLock(0)) return false;
            try
            {
                action();
            }
            finally
            {
                ExitWriteLock();
            }

            return true;
        }

        public bool TryLockWrite<T>(Func<T> action, out T result)
        {
            if (!TryEnterWriteLock(0))
            {
                result = default;
                return false;
            }

            try
            {
                result = action();
                return true;
            }
            finally
            {
                ExitWriteLock();
            }
        }
    }
}