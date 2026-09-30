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
    public abstract class EndAction : MyAction, IEndAction
    {
        protected readonly IContinuableAction _baseAction;

        protected EndAction(IContinuableAction baseAction)
        {
            _baseAction = baseAction;
        }

        public override Exception Exception => _baseAction.Exception;
        public override ExecState State => _baseAction.State;
        public override IThreadPool ThreadPool => _baseAction.ThreadPool;

        public override void Invoke()
        {
            _baseAction.Invoke();
        }

        public override bool CanInvoke()
        {
            return _baseAction.CanInvoke();
        }
    }
}