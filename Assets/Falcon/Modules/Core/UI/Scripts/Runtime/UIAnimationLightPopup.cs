/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

namespace Falcon.Modules.Core.UI.Runtime
{
    public class UIAnimationLightPopup : UIAnimation
    {
        [Header("Settings")]
        public Transform viewport;  

        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        public AnimationCurve scaleShowCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.2541534f, 0.7062241f, 2.5f, 2.5f),
            new Keyframe(1f, 0.9947891f, -1.039176f, -1.039176f)
        );
        
        public AnimationCurve scaleHideCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.2541534f, 0.7062241f, 2.5f, 2.5f),
            new Keyframe(1f, 0.9947891f, -1.039176f, -1.039176f)
        );

        [InfoBox("Scale factor applied at the start of the show animation.")]
        public float scaleFactorStart = 0.75f;

        protected CanvasGroup _canvasGroup;
        private Coroutine _currentAnimation;

        public override void Init(UIBase parentUIBase)
        {
            //Cache
            _canvasGroup = parentUIBase.canvasGroup;
            if (viewport == null) viewport = parentUIBase.transform.Find("Viewport");
        }

        public override void Show(UIBase parentUIBase)
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
            _currentAnimation = StartCoroutine(ShowAnimation());
        }

        public override void Hide(UIBase parentUIBase)
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
            _currentAnimation = StartCoroutine(HideAnimation());
        }

        private IEnumerator ShowAnimation()
        {
            float durationAnimation = isOverrideDuration ? durationShow : duration;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * scaleFactorStart;
            Vector3 endScale = Vector3.one;

            // Both animations run together
            while (elapsed < durationAnimation)
            {
                elapsed += Time.unscaledDeltaTime;
                
                // Fade animation (shorter duration)
                float fadeT = Mathf.Clamp01(elapsed / durationAnimation);
                float alpha = fadeCurve.Evaluate(fadeT);
                _canvasGroup.alpha = alpha;
                
                // Scale animation (full duration)
                if (viewport != null)
                {
                    float scaleT = Mathf.Clamp01(elapsed / durationAnimation);
                    float curveValue = scaleShowCurve.Evaluate(scaleT);
                    viewport.localScale = Vector3.LerpUnclamped(startScale, endScale, curveValue);
                }
                
                yield return null;
            }
            
            // Ensure final values
            _canvasGroup.alpha = 1f;
            if (viewport != null)
            {
                viewport.localScale = Vector3.one;
            }

            _currentAnimation = null;
        }

        private IEnumerator HideAnimation()
        {
            float durationAnimation = isOverrideDuration ? durationHide : duration;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one;
            Vector3 endScale = Vector3.zero;

            // Both animations run together
            while (elapsed < durationAnimation)
            {
                elapsed += Time.unscaledDeltaTime;

                // Fade with curve (reversed)
                float fadeT = Mathf.Clamp01(elapsed / durationAnimation);
                float fadeValue = fadeCurve.Evaluate(1f - fadeT);
                _canvasGroup.alpha = fadeValue;

                // Scale with curve
                if (viewport != null)
                {
                    float scaleT = Mathf.Clamp01(elapsed / durationAnimation);
                    float curveValue = scaleHideCurve.Evaluate(1f - scaleT);
                    viewport.localScale = Vector3.LerpUnclamped(startScale, endScale, 1f - curveValue);
                }

                yield return null;
            }

            // Ensure final values
            _canvasGroup.alpha = 0f;
            if (viewport != null)
            {
                viewport.localScale = Vector3.zero;
                viewport.localScale = Vector3.one; // Reset to original scale
            }

            _currentAnimation = null;
        }
    }
}
