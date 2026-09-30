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
    public class DelayAction : ChainAction
    {
        private readonly long _createTime = MyTime.CurrentTimeMillis;
        private readonly TimeSpan _delayTime;

        public DelayAction(IContinuableAction baseAction, TimeSpan delayTime) : base(baseAction)
        {
            _delayTime = delayTime;
        }

        public DelayAction(Action action, TimeSpan delayTime) : this(new UnitAction(action), delayTime)
        {
        }
        
        public DelayAction(Action action, IThreadPool threadPool, TimeSpan delayTime) : this(new UnitAction(action, threadPool), delayTime)
        {
        }

        public override bool CanInvoke()
        {
            return (_createTime + _delayTime.TotalMilliseconds <= MyTime.CurrentTimeMillis) && base.CanInvoke();
        }
    }
}