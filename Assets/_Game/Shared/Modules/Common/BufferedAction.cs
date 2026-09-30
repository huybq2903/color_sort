using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Falcon.Shared.Common
{
    public class BufferedAction : IDisposable
    {
        private CancellationTokenSource _cts;
        private Action _pendingAction;
        private bool _disposed;

        public bool IsPending => _cts != null;

        public void Schedule(float delaySeconds, Action action)
        {
            if (_disposed) return;

            Cancel();
            if (action == null) return;

            _pendingAction = action;
            _cts = new CancellationTokenSource();
            RunAsync(delaySeconds, _cts).Forget();
        }

        public void Cancel()
        {
            ClearCurrent(true);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            ClearCurrent(true);
        }

        private async UniTask RunAsync(float delaySeconds, CancellationTokenSource cts)
        {
            try
            {
                await UniTask.WaitForSeconds(delaySeconds).AttachExternalCancellation(cts.Token);
                if (!ReferenceEquals(_cts, cts)) return;

                var action = _pendingAction;
                ClearCurrent(false);
                action?.Invoke();
            }
            catch (OperationCanceledException)
            {
                ClearIfCurrent(cts);
            }
        }

        private void ClearCurrent(bool cancelDelay)
        {
            var cts = _cts;
            _cts = null;
            _pendingAction = null;

            if (cts == null) return;
            if (cancelDelay && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }

            cts.Dispose();
        }

        private void ClearIfCurrent(CancellationTokenSource cts)
        {
            if (!ReferenceEquals(_cts, cts)) return;

            ClearCurrent(false);
        }
    }
}
