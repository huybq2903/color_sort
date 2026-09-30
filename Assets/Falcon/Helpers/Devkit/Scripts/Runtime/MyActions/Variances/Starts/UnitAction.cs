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
    public class UnitAction : StartAction, IStartAction
    {
        private readonly Action _action;

        public override Exception Exception => _except;

        private ExecState _state = ExecState.NotStarted;
        private Exception _except;

        public UnitAction(Action action, IThreadPool threadPool)
        {
            _action = action;
            ThreadPool = threadPool;
        }
        
        public UnitAction(Action action):this(action, GlobalThreadPool.Instance)
        {
        }

        public override IThreadPool ThreadPool { get; }
        public override ExecState State => _state;

        public override void Invoke()
        {
            try
            {
                _state = ExecState.Processing;
                _action();
                _state = ExecState.Succeed;
            } catch (Exception e)
            {
                _except = e;
                _state = ExecState.Failed;
            }
        }
    }
}