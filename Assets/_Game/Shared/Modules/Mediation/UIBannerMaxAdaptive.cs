/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-16
 */

using Falcon.Shared.Mediation;
using UnityEngine;

namespace Falcon.Shared.Common.UI
{
    [DefaultExecutionOrder(-999)]
    public class UIBannerMaxAdaptive : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private float _bannerPx = -1f;
        private float _applied = float.NaN;

        private void Awake() => _rectTransform = GetComponent<RectTransform>();

        // Soát mỗi frame chứ không nghe event: banner show sớm thì scaleFactor/chiều cao banner còn chưa sẵn sàng.
        private void LateUpdate()
        {
            if (!_rectTransform) return;

            var padding = GetBannerPaddingCanvasUnits();
            if (Mathf.Approximately(padding, _applied)) return;
            _applied = padding;

            var offsetMin = _rectTransform.offsetMin;
            offsetMin.y = padding;
            _rectTransform.offsetMin = offsetMin;
            _rectTransform.offsetMax = Vector2.zero;
        }

        private float GetBannerPaddingCanvasUnits()
        {
            if (!MediationHelpers.Instance.IsBannerShowing) return 0f;

            if (_bannerPx < 0f)
            {
                var bannerHeightPx = Application.isEditor
                    ? Screen.height * 0.05f
                    : MaxSdkUtils.GetAdaptiveBannerHeight();

                // Chỉ chốt khi SDK trả số thật, tránh cache nhầm 0 lúc banner vừa hiện.
                if (bannerHeightPx > 0f)
                    _bannerPx = bannerHeightPx * MaxSdkUtils.GetScreenDensity() + Screen.safeArea.y;
            }

            if (_bannerPx < 0f) return 0f;

            if (!_canvas) _canvas = GetComponentInParent<Canvas>();
            var scale = _canvas ? _canvas.scaleFactor : 1f;
            return scale > 0f ? _bannerPx / scale : 0f;
        }
    }
}
