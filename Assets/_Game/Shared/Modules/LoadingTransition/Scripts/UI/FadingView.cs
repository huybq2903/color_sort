/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Falcon.Shared.LoadingTransition
{
    public class FadingView : MonoBehaviour, ITransitionView
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private float _fadeDuration;

        public void Setup(UITransitionConfig config)
        {
            _fadeDuration = config.FadeDuration;
        }

        // Covers the screen: alpha 0 → 1
        public async UniTask PlayInAsync()
        {
            gameObject.SetActive(true);
            await AnimateAlphaAsync(0f, 1f);
        }

        // Reveals the screen: alpha 1 → 0
        public async UniTask PlayOutAsync()
        {
            await AnimateAlphaAsync(1f, 0f);
            gameObject.SetActive(false);
        }

        private async UniTask AnimateAlphaAsync(float from, float to)
        {
            float timer = 0f;
            while (timer < _fadeDuration)
            {
                _canvasGroup.alpha = Mathf.Lerp(from, to, timer / _fadeDuration);
                timer += Time.unscaledDeltaTime;
                await UniTask.Yield();
            }

            _canvasGroup.alpha = to;
        }
    }
}
