using System.Threading;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Falcon.Shared.Common
{
    public class SpinePlayer : MonoBehaviour
    {
        private ISkeletonComponent skeletonComponent; // SkeletonAnimation (2D) hoặc SkeletonGraphic (UI)

        private ISkeletonComponent Comp => skeletonComponent ??= GetComponent<ISkeletonComponent>();

        /// <summary>AnimationState, tự Initialize nếu chưa valid</summary>
        public Spine.AnimationState State
        {
            get
            {
                switch (Comp)
                {
                    case null: return null;
                    case SkeletonRenderer r when !r.valid: r.Initialize(false); break;
                    case SkeletonGraphic g when !g.IsValid: g.Initialize(false); break;
                }
                return (Comp as IAnimationStateComponent)?.AnimationState;
            }
        }

        public Skeleton Skeleton => State == null ? null : Comp.Skeleton;

        public float TimeScale
        {
            get => State?.TimeScale ?? 1f;
            set { if (State != null) State.TimeScale = value; }
        }

        public void PlayOne(string animationName) => Play(animationName, false);
        public void PlayLoop(string animationName) => Play(animationName, true);

        /// <summary>Play animation, trả về TrackEntry (null nếu không có)</summary>
        public TrackEntry Play(string animationName, bool loop = true, int track = 0)
            => State?.SetAnimation(track, animationName, loop);

        /// <summary>Nối animation sau animation hiện tại</summary>
        public TrackEntry Queue(string animationName, bool loop = false, float delay = 0f, int track = 0)
            => State?.AddAnimation(track, animationName, loop, delay);

        /// <summary>Play và chờ animation chạy xong 1 vòng</summary>
        public UniTask PlayAsync(string animationName, CancellationToken cancellationToken = default)
            => Play(animationName, false).WaitForCompleteAsync(true, cancellationToken);

        /// <summary>Chờ event trong animation hiện tại</summary>
        public UniTask WaitForEventAsync(string eventName, CancellationToken cancellationToken = default)
            => State.WaitForEventAsync(eventName, cancellationToken);

        public void Stop(int track = 0) => State?.SetEmptyAnimation(track, 0f);

        public void SetSkin(string skinName)
        {
            var skeleton = Skeleton;
            if (skeleton == null) return;
            skeleton.SetSkin(skinName);
            skeleton.SetSlotsToSetupPose();
            State?.Apply(skeleton);
        }
    }
}
