using System.Threading;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;

namespace Falcon.Shared.Common
{
    public static class SpineExtensions
    {
        private sealed class CancellationRegistrationHolder
        {
            public CancellationTokenRegistration registration;
        }

        public static UniTask WaitForEventAsync(this AnimationState state, string eventName,
            CancellationToken cancellationToken = default)
        {
            if (state == null || string.IsNullOrEmpty(eventName)) return UniTask.CompletedTask;

            var tcs = new UniTaskCompletionSource();
            var ctrHolder = new CancellationRegistrationHolder();

            void Cleanup()
            {
                state.Event -= Handler;
                ctrHolder.registration.Dispose();
            }

            void Handler(TrackEntry trackEntry, Event e)
            {
                if (e?.Data?.Name != eventName) return;
                Cleanup();
                tcs.TrySetResult();
            }

            state.Event += Handler;

            if (cancellationToken.CanBeCanceled)
            {
                ctrHolder.registration = cancellationToken.Register(() =>
                {
                    Cleanup();
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }

        public static UniTask WaitForEventAsync(this SkeletonAnimation skeletonAnimation, string eventName,
            CancellationToken cancellationToken = default)
        {
            if (!skeletonAnimation) return UniTask.CompletedTask;
            return skeletonAnimation.AnimationState.WaitForEventAsync(eventName, cancellationToken);
        }

        public static UniTask WaitForEventAsync(this AnimationState state, EventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (state == null || eventData == null) return UniTask.CompletedTask;

            var tcs = new UniTaskCompletionSource();
            var ctrHolder = new CancellationRegistrationHolder();

            void Cleanup()
            {
                state.Event -= Handler;
                ctrHolder.registration.Dispose();
            }

            void Handler(TrackEntry trackEntry, Event e)
            {
                if (e?.Data != eventData) return;
                Cleanup();
                tcs.TrySetResult();
            }

            state.Event += Handler;

            if (cancellationToken.CanBeCanceled)
            {
                ctrHolder.registration = cancellationToken.Register(() =>
                {
                    Cleanup();
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }

        public static UniTask WaitForEventAsync(this SkeletonAnimation skeletonAnimation, EventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (!skeletonAnimation) return UniTask.CompletedTask;
            return skeletonAnimation.AnimationState.WaitForEventAsync(eventData, cancellationToken);
        }

        public static UniTask WaitForCompleteAsync(this TrackEntry trackEntry, bool includeEndEvent = false,
            CancellationToken cancellationToken = default)
        {
            if (trackEntry == null) return UniTask.CompletedTask;

            var tcs = new UniTaskCompletionSource();
            var ctrHolder = new CancellationRegistrationHolder();

            void Cleanup()
            {
                trackEntry.Complete -= OnComplete;
                if (includeEndEvent) trackEntry.End -= OnEnd;
                ctrHolder.registration.Dispose();
            }

            void OnComplete(TrackEntry entry)
            {
                Cleanup();
                tcs.TrySetResult();
            }

            void OnEnd(TrackEntry entry)
            {
                Cleanup();
                tcs.TrySetResult();
            }

            trackEntry.Complete += OnComplete;
            if (includeEndEvent) trackEntry.End += OnEnd;

            if (cancellationToken.CanBeCanceled)
            {
                ctrHolder.registration = cancellationToken.Register(() =>
                {
                    Cleanup();
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }

        public static UniTask WaitForEndAsync(this TrackEntry trackEntry, CancellationToken cancellationToken = default)
        {
            if (trackEntry == null) return UniTask.CompletedTask;

            var tcs = new UniTaskCompletionSource();
            var ctrHolder = new CancellationRegistrationHolder();

            void Cleanup()
            {
                trackEntry.End -= OnEnd;
                ctrHolder.registration.Dispose();
            }

            void OnEnd(TrackEntry entry)
            {
                Cleanup();
                tcs.TrySetResult();
            }

            trackEntry.End += OnEnd;

            if (cancellationToken.CanBeCanceled)
            {
                ctrHolder.registration = cancellationToken.Register(() =>
                {
                    Cleanup();
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }
    }
}