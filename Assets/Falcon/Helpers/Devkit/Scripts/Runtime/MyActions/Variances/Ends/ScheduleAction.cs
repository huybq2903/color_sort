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
    public class ScheduleAction : EndAction, IDisposable
    {
        private long _invokableTime;
        private readonly AtomicRef<ExecState> _stateRef = new(ExecState.NotStarted);
        public TimeSpan TimeSpan { get; set; }

        public override ExecState State => _stateRef.Value;

        public ScheduleAction(IContinuableAction baseAction, TimeSpan timeSpan, TimeSpan delay = default) : base(
            baseAction)
        {
            TimeSpan = timeSpan;
            _invokableTime = (long)(MyTime.CurrentTimeMillis + delay.TotalMilliseconds);
        }

        public ScheduleAction(Action action, TimeSpan timeSpan, TimeSpan delay = default) : this(new UnitAction(action),
            timeSpan, delay)
        {
        }
        
        public ScheduleAction(Action action,IThreadPool threadPool, TimeSpan timeSpan, TimeSpan delay = default) : this(new UnitAction(action, threadPool), timeSpan, delay)
        {
        }

        public void Dispose()
        {
            Cancel();
        }

        public override void Invoke()
        {
            if (!_stateRef.CompareAndSet(ExecState.NotStarted, ExecState.Processing))
            {
                return;
            }
            _invokableTime += (long)TimeSpan.TotalMilliseconds;
            base.Invoke();
            _stateRef.Compute(state =>
            {
                if (state != ExecState.Processing) return state;
                Schedule();
                return ExecState.NotStarted;

            });
        }

        public override bool CanInvoke()
        {
            return _invokableTime <= MyTime.CurrentTimeMillis && base.CanInvoke();
        }

        public override bool Cancel()
        {
            _stateRef.Value = ExecState.Cancelled;
            return base.Cancel();
        }
    }
}