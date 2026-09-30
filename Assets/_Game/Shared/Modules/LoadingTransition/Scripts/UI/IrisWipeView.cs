/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.LoadingTransition
{
    public class IrisWipeView : MonoBehaviour, ITransitionView
    {
        [SerializeField] private Image _background, _logo;
        [SerializeField] private Material _wipeInMat, _wipeOutMat;

        private readonly int _shaderCircleSize = Shader.PropertyToID("_CircleHole_Size_1");

        private UITransitionConfig _config;
        private IrisWipeConfig _irisWipeConfig;

        public void Setup(UITransitionConfig config)
        {
            _config = config;
            _irisWipeConfig = config.IrisWipeConfig[0];
        }

        public void SetTransitionIndex(int index)
        {
            _irisWipeConfig = _config.IrisWipeConfig[index];
            _background.sprite = _irisWipeConfig.IrisWipeBG;
            _logo.sprite = _irisWipeConfig.IrisWipeLogo;
            _background.SetNativeSize();
            _logo.SetNativeSize();
        }

        // Closes the iris: circle 0 → 1 (covers screen)
        public async UniTask PlayInAsync()
        {
            gameObject.SetActive(true);
            _logo.transform.localScale = Vector3.zero;
            _background.material = _wipeInMat;
            _wipeInMat.SetFloat(_shaderCircleSize, 0);

            await UniTask.WhenAll(
                AnimateLogoScaleAsync(isScalingIn: true, _irisWipeConfig.WipeInDurationLogo, _irisWipeConfig.WipeInDelayScaleLogo),
                AnimateMaterialFloatAsync(_wipeInMat, 0f, 1f, _irisWipeConfig.WipeInDurationBG)
            );
        }

        // Opens the iris: circle 1 → 0 (reveals screen)
        public async UniTask PlayOutAsync()
        {
            _logo.transform.localScale = Vector3.one;
            _background.material = _wipeOutMat;
            _wipeOutMat.SetFloat(_shaderCircleSize, 1);

            await AnimateLogoScaleAsync(isScalingIn: false, _irisWipeConfig.WipeOutDurationLogo, 0f);
            await AnimateMaterialFloatAsync(_wipeOutMat, 1f, 0f, _irisWipeConfig.WipeOutDurationBG);
            gameObject.SetActive(false);
        }

        private async UniTask AnimateLogoScaleAsync(bool isScalingIn, float duration, float delay)
        {
            var startScale = isScalingIn ? Vector3.zero : Vector3.one;
            var endScale = isScalingIn ? Vector3.one : Vector3.zero;
            var curve = isScalingIn ? _irisWipeConfig.LogoScaleInCurve : _irisWipeConfig.LogoScaleOutCurve;

            if (delay > 0)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), DelayType.UnscaledDeltaTime);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                _logo.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
                await UniTask.Yield();
            }

            _logo.transform.localScale = endScale;
        }

        private async UniTask AnimateMaterialFloatAsync(Material material, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                material.SetFloat(_shaderCircleSize, Mathf.Lerp(from, to, elapsed / duration));
                await UniTask.Yield();
            }

            material.SetFloat(_shaderCircleSize, to);
        }
    }
}
