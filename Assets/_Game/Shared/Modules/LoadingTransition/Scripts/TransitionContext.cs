/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using System;
using Cysharp.Threading.Tasks;

namespace Falcon.Shared.LoadingTransition
{
    internal readonly struct TransitionContext
    {
        public readonly string SceneName;
        public readonly TransitionType Type;
        public readonly Func<UniTask> LoadingAction;

        public TransitionContext(string sceneName, TransitionType type, Func<UniTask> loadingAction)
        {
            SceneName = sceneName;
            Type = type;
            LoadingAction = loadingAction;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(SceneName);
        public static readonly TransitionContext Empty = new(null, TransitionType.None, null);
    }
}
