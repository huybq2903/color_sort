/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Falcon.Shared.LoadingTransition
{
    public class ProgressBarView : MonoBehaviour, ITransitionView
    {
        [SerializeField] private Image _progressBar;

        private const float FadeDuration = 0.2f;
        private const float PreloadReadyThreshold = 0.9f;

        private float _currentValue;
        private float _animationSpeed;
        private CanvasGroup _canvasGroup;

        public void Setup(UITransitionConfig config)
        {
            _animationSpeed = config.AnimationSpeed;
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public async UniTask PlayInAsync()
        {
            gameObject.SetActive(true);
            _currentValue = 0;
            if (_progressBar != null) _progressBar.fillAmount = 0;
            DoFade(0f, 1f, 0);
            await UniTask.WaitForSeconds(FadeDuration);
        }

        public async UniTask PlayOutAsync()
        {
            await AnimateProgressToAsync(1f);
            DoFade(1f, 0f);
            await UniTask.WaitForSeconds(FadeDuration);
            gameObject.SetActive(false);
        }

        private void UpdateProgress(float progress)
        {
            _currentValue = progress;
            if (_progressBar != null) _progressBar.fillAmount = Easing.OutCubic(_currentValue);
        }

        private void DoFade(float from, float to, float duration = FadeDuration)
        {
            DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, to, duration).From(from);
        }

        private async UniTask AnimateProgressToAsync(float target)
        {
            while (_currentValue < target - 0.001f)
            {
                UpdateProgress(Mathf.MoveTowards(_currentValue, target, Time.unscaledDeltaTime * _animationSpeed));
                await UniTask.Yield();
            }

            UpdateProgress(target);
        }

        internal async UniTask LoadingProgressAsync()
        {
            const float LoadingActionPhase = 0.3f;

            var sceneName = LoadingSceneManager.CurrentContext.SceneName;
            var displayed = 0f;
            var velocity = 0f;

            var scene = SceneManager.GetSceneByName(sceneName);
            var sceneAlreadyLoaded = scene.IsValid() && scene.isLoaded;

            var loadingActionTask = LoadingSceneManager.RunLoadingActionAsync();
            while (loadingActionTask.Status == UniTaskStatus.Pending)
            {
                var target = LoadingActionPhase - 0.02f;
                displayed = Mathf.Max(displayed, Mathf.SmoothDamp(displayed, target, ref velocity, 0.2f, Mathf.Infinity, Time.unscaledDeltaTime));
                UpdateProgress(displayed);
                await UniTask.Yield();
            }
            await loadingActionTask;

            while (displayed < LoadingActionPhase - 0.001f)
            {
                displayed = Mathf.MoveTowards(displayed, LoadingActionPhase, Time.unscaledDeltaTime * _animationSpeed);
                UpdateProgress(displayed);
                await UniTask.Yield();
            }
            displayed = LoadingActionPhase;
            UpdateProgress(displayed);

            if (sceneAlreadyLoaded)
            {
                LoadingSceneManager.ActivateSceneRoot(sceneName);
                UpdateProgress(1f);
                return;
            }

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"{nameof(ProgressBarView)} > Cannot load scene: '{sceneName}'");
                return;
            }

            op.allowSceneActivation = false;

            while (op.progress < PreloadReadyThreshold - 0.001f)
            {
                var sceneProgress = Mathf.Min(op.progress / PreloadReadyThreshold, 1f);
                var target = LoadingActionPhase + (1f - LoadingActionPhase) * sceneProgress;
                displayed = Mathf.Max(displayed, Mathf.SmoothDamp(displayed, target, ref velocity, 0.3f, Mathf.Infinity, Time.unscaledDeltaTime));
                UpdateProgress(displayed);
                await UniTask.Yield();
            }

            op.allowSceneActivation = true;
            while (!op.isDone)
            {
                displayed = Mathf.Max(displayed, Mathf.SmoothDamp(displayed, 1f, ref velocity, 0.15f, Mathf.Infinity, Time.unscaledDeltaTime));
                UpdateProgress(displayed);
                await UniTask.Yield();
            }

            LoadingSceneManager.ActivateSceneRoot(sceneName);
            while (displayed < 1f - 0.01f)
            {
                displayed = Mathf.Max(displayed, Mathf.SmoothDamp(displayed, 1f, ref velocity, 0.15f, Mathf.Infinity, Time.unscaledDeltaTime));
                UpdateProgress(displayed);
                await UniTask.Yield();
            }
            UpdateProgress(1f);
        }
    }
}
