/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public abstract class MyAction : IMyAction
    {
        public abstract IThreadPool ThreadPool { get; }
        public abstract Exception Exception { get; }
        public abstract ExecState State { get; }

        public virtual void Schedule()
        {
            ThreadPool.Add(this);
        }

        public virtual bool Cancel()
        {
            return ThreadPool.Remove(this);
        }

        public abstract void Invoke();

        public abstract bool CanInvoke();
    }
}