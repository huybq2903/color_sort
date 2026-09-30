/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using System;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Falcon.Shared.LoadingTransition
{
    public class UITransitionController : MonoBehaviour
    {
        [SerializeField] private ProgressBarView _progressBarView;
        [SerializeField] private FadingView _fadingView;
        [SerializeField] private IrisWipeView _irisWipeView;
        [SerializeField] private Camera _camera;

        private void Awake()
        {
            Setup();
        }

        private void Start()
        {
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            try
            {
                await LoadingSceneManager.WaitUntilReadyAsync();
                await PlayTransitionAsync();
                GameEvent.Emit("falcon.modules.ui.loading_transition_complete");
                await UnloadSelfAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                LoadingSceneManager.CompleteTransition();
            }
        }

        private void Setup()
        {
            var config = Resources.Load<UITransitionConfig>(UITransitionConfig.SETTINGS_NAME);
            if (config == null)
            {
                Debug.LogError("UITransitionConfig not found. Create it via: Falcon > Modules > UI > Transition > Config");
                return;
            }

            _progressBarView.Setup(config);
            _fadingView.Setup(config);
            _irisWipeView.Setup(config);

            _progressBarView.gameObject.SetActive(false);
            _fadingView.gameObject.SetActive(false);
            _irisWipeView.gameObject.SetActive(false);
            _camera.gameObject.SetActive(LoadingSceneManager.CurrentContext.Type != TransitionType.IrisWipe);
        }

        private async UniTask PlayTransitionAsync()
        {
            switch (LoadingSceneManager.CurrentContext.Type)
            {
                case TransitionType.ProgressBar: await PlayProgressBarAsync(); break;
                case TransitionType.IrisWipe: await PlayIrisWipeAsync(); break;
                case TransitionType.Fading: await PlayFadingAsync(); break;
                default: await PlayNoneAsync(); break;
            }
        }

        private async UniTask PlayProgressBarAsync()
        {
            _camera.gameObject.SetActive(true);
            await _progressBarView.PlayInAsync();
            await LoadingSceneManager.UnloadCurrentSceneAsync();
            await _progressBarView.LoadingProgressAsync();
            _camera.gameObject.SetActive(false);
            await UniTask.Delay(300);
            await _progressBarView.PlayOutAsync();
        }

        private async UniTask PlayIrisWipeAsync()
        {
            await _irisWipeView.PlayInAsync();
            await LoadingSceneManager.UnloadCurrentSceneAsync();
            _camera.gameObject.SetActive(true);
            await UniTask.Delay(300);
            await LoadTargetSceneAsync();
            _camera.gameObject.SetActive(false);
            await _irisWipeView.PlayOutAsync();
        }

        private async UniTask PlayFadingAsync()
        {
            _camera.gameObject.SetActive(false);
            await _fadingView.PlayInAsync();
            await LoadingSceneManager.UnloadCurrentSceneAsync();
            _camera.gameObject.SetActive(true);
            await UniTask.Delay(300);
            await LoadTargetSceneAsync();
            _camera.gameObject.SetActive(false);
            await _fadingView.PlayOutAsync();
        }

        private static async UniTask PlayNoneAsync()
        {
            await LoadingSceneManager.UnloadCurrentSceneAsync();
            await LoadTargetSceneAsync();
        }

        private async UniTask UnloadSelfAsync()
        {
            var scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            var op = SceneManager.UnloadSceneAsync(scene);
            if (op != null)
                await AwaitOperationAsync(op);
        }

        private static async UniTask LoadTargetSceneAsync()
        {
            var sceneName = LoadingSceneManager.CurrentContext.SceneName;
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                await LoadingSceneManager.RunLoadingActionAsync();
                LoadingSceneManager.ActivateSceneRoot(sceneName);
                return;
            }

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"{nameof(UITransitionController)} > Cannot load scene: '{sceneName}'");
                return;
            }

            op.allowSceneActivation = false;
            await UniTask.WaitUntil(() => op.progress >= PreloadReadyThreshold - 0.001f);
            await LoadingSceneManager.RunLoadingActionAsync();
            op.allowSceneActivation = true;
            await AwaitOperationAsync(op);
            LoadingSceneManager.ActivateSceneRoot(sceneName);
        }

        private const float PreloadReadyThreshold = 0.9f;

        private static async UniTask AwaitOperationAsync(AsyncOperation op)
        {
            while (!op.isDone)
                await UniTask.Yield();
        }
    }

    public interface ITransitionView
    {
        void Setup(UITransitionConfig config);
        UniTask PlayInAsync();
        UniTask PlayOutAsync();
    }
}
