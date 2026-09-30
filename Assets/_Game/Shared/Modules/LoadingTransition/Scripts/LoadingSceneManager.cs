/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Falcon.Shared.LoadingTransition
{
    public static class LoadingSceneManager
    {
        private const string LoadingSceneName = "LoadingScene";

        private static Scene _currentScene;
        private static UniTaskCompletionSource _loadingSceneReadyTcs;
        private static TransitionContext _context;
        private static TransitionContext _queuedContext;
        private static readonly HashSet<string> _preloadingScenes = new();

        public static event Action<string> OnTransitionCompleted;
        public static Func<UniTask> DefaultLoadingActionAsync { get; set; }

        public static void Preload(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return;
            var scene = SceneManager.GetSceneByName(sceneName);
            if ((scene.IsValid() && scene.isLoaded) || _preloadingScenes.Contains(sceneName)) return;
            _preloadingScenes.Add(sceneName);
            PreloadAsync(sceneName).Forget();
        }

        private static async UniTaskVoid PreloadAsync(string sceneName)
        {
            try
            {
                await LoadSceneAdditiveAsync(sceneName);
            }
            finally
            {
                _preloadingScenes.Remove(sceneName);
            }
        }

        public static void Load(string sceneName, TransitionType type, Func<UniTask> loadingActionAsync = null)
        {
            LoadAsync(sceneName, type, loadingActionAsync).Forget();
        }

        private static async UniTask LoadAsync(string sceneName, TransitionType type, Func<UniTask> loadingActionAsync)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    Debug.LogWarning($"{nameof(LoadingSceneManager)} > Scene name is invalid.");
                    return;
                }

                if (_context.IsValid)
                {
                    TryQueueTransition(sceneName, type, loadingActionAsync);
                    return;
                }

                _context = new TransitionContext(sceneName, type, loadingActionAsync ?? DefaultLoadingActionAsync);
                _currentScene = SceneManager.GetActiveScene();

                if (type == TransitionType.Direct)
                {
                    await RunDirectTransitionAsync();
                    return;
                }

                _loadingSceneReadyTcs = new UniTaskCompletionSource();
                var ready = await LoadSceneAdditiveAsync(LoadingSceneName);
                if (!ready)
                {
                    ResetState();
                    return;
                }

                var loadingScene = SceneManager.GetSceneByName(LoadingSceneName);
                if (loadingScene.IsValid())
                    SceneManager.SetActiveScene(loadingScene);
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(LoadingSceneManager)} > Error: {e}");
                ResetState();
            }
            finally
            {
                _loadingSceneReadyTcs?.TrySetResult();
            }
        }

        private static async UniTask<bool> LoadSceneAdditiveAsync(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
                return true;

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"{nameof(LoadingSceneManager)} > Cannot load scene: '{sceneName}'");
                return false;
            }

            while (!op.isDone)
                await UniTask.Yield();

            return true;
        }

        internal static TransitionContext CurrentContext => _context;

        internal static UniTask WaitUntilReadyAsync() => _loadingSceneReadyTcs?.Task ?? UniTask.CompletedTask;

        internal static async UniTask RunLoadingActionAsync()
        {
            if (_context.LoadingAction != null) await _context.LoadingAction();
        }

        internal static void CompleteTransition()
        {
            var completedScene = _context.SceneName;
            ResetState();

            if (!string.IsNullOrWhiteSpace(completedScene))
                OnTransitionCompleted?.Invoke(completedScene);

            if (_queuedContext.IsValid)
            {
                var next = _queuedContext;
                _queuedContext = TransitionContext.Empty;
                Load(next.SceneName, next.Type, next.LoadingAction);
            }
        }

        internal static async UniTask UnloadCurrentSceneAsync()
        {
            if (!_currentScene.IsValid() || !_currentScene.isLoaded || _currentScene.name == LoadingSceneName)
                return;

            var op = SceneManager.UnloadSceneAsync(_currentScene);
            if (op != null) while (!op.isDone) await UniTask.Yield();
        }

        private static async UniTask RunDirectTransitionAsync()
        {
            var sceneName = _context.SceneName;
            // Direct transition is instant — LoadingAction is intentionally not invoked.
            var scene = SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning($"{nameof(LoadingSceneManager)} > Direct transition called without preload for '{sceneName}'. Consider calling {nameof(LoadingSceneManager)}.{nameof(Preload)}() first.");
                var ready = await LoadSceneAdditiveAsync(sceneName);
                if (!ready)
                {
                    ResetState();
                    return;
                }
            }

            if (!ActivateSceneRoot(sceneName))
            {
                Debug.LogError($"{nameof(LoadingSceneManager)} > Failed to activate scene '{sceneName}'. Transition aborted.");
                ResetState();
                return;
            }

            await UnloadCurrentSceneAsync();

            var target = SceneManager.GetSceneByName(sceneName);
            if (target.IsValid() && target.isLoaded)
                SceneManager.SetActiveScene(target);

            // CompleteTransition calls ResetState() and may start a queued transition.
            // LoadAsync's finally block runs after this returns, but _loadingSceneReadyTcs
            // is null in the Direct path so the finally is a no-op — this is safe.
            CompleteTransition();
        }

        internal static bool ActivateSceneRoot(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid()) return false;

            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.GetComponent<SceneRoot>() != null)
                {
                    go.SetActive(true);
                    return true;
                }
            }

            foreach (var go in scene.GetRootGameObjects())
                go.SetActive(true);

            return true;
        }

        private static void TryQueueTransition(string sceneName, TransitionType type, Func<UniTask> loadingActionAsync)
        {
            if (_queuedContext.IsValid)
            {
                Debug.LogWarning($"{nameof(LoadingSceneManager)} > Queue is full. Ignoring '{sceneName}'.");
                return;
            }

            _queuedContext = new TransitionContext(sceneName, type, loadingActionAsync ?? DefaultLoadingActionAsync);
            Debug.Log($"{nameof(LoadingSceneManager)} > Queued scene: '{sceneName}'.");
        }

        private static void ResetState()
        {
            _context = TransitionContext.Empty;
            _loadingSceneReadyTcs = null;
        }
    }
}
