/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-05-13
*/

namespace Falcon.Helpers.UI
{
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;

    [ExecuteAlways]
    public class UITMPClone : MonoBehaviour
    {
        [InfoBox("This component if apply = true, always update run-time", InfoMessageType.Warning)]
        public bool apply = true;

        [SerializeField]
        private TMP_Text _target;

        [SerializeField]
        private TMP_Text _template;

        private RectTransform _srcRect;
        private RectTransform _dstRect;

        private Vector2 _cachedAnchorMin;
        private Vector2 _cachedAnchorMax;
        private Vector2 _cachedPivot;
        private Vector2 _cachedAnchoredPosition;
        private Vector2 _cachedSizeDelta;
        private Quaternion _cachedRotation;
        private Vector3 _cachedScale;
        private string _cachedText;

        private void Awake()
        {
            if (apply) Apply();
        }

        private void CacheRects()
        {
            if (_template != null) _srcRect = _template.rectTransform;
            if (_target != null) _dstRect = _target.rectTransform;
        }

        private void Update()
        {
            if (_srcRect == null || _dstRect == null)
            {
                CacheRects();
                return;
            }

            if (apply && HasRectChanged())
            {
                ApplyCachedRect();
            }

            if (_template != null && _template.text != _cachedText)
            {
                ApplyCachedText();
            }
        }

        private bool HasRectChanged()
        {
            return _srcRect.anchorMin != _cachedAnchorMin
                || _srcRect.anchorMax != _cachedAnchorMax
                || _srcRect.pivot != _cachedPivot
                || _srcRect.anchoredPosition != _cachedAnchoredPosition
                || _srcRect.sizeDelta != _cachedSizeDelta
                || _srcRect.localRotation != _cachedRotation
                || _srcRect.localScale != _cachedScale;
        }

        private void ApplyCachedRect()
        {
            _cachedAnchorMin = _srcRect.anchorMin;
            _cachedAnchorMax = _srcRect.anchorMax;
            _cachedPivot = _srcRect.pivot;
            _cachedAnchoredPosition = _srcRect.anchoredPosition;
            _cachedSizeDelta = _srcRect.sizeDelta;
            _cachedRotation = _srcRect.localRotation;
            _cachedScale = _srcRect.localScale;

            _dstRect.anchorMin = _cachedAnchorMin;
            _dstRect.anchorMax = _cachedAnchorMax;
            _dstRect.pivot = _cachedPivot;
            _dstRect.anchoredPosition = _cachedAnchoredPosition;
            _dstRect.sizeDelta = _cachedSizeDelta;
            _dstRect.localRotation = _cachedRotation;
            _dstRect.localScale = _cachedScale;
        }

        private void ApplyCachedText()
        {
            _cachedText = _template.text;
            _target.text = _cachedText;
        }

        [Button]
        public void Apply()
        {
            if (_target == null || _template == null) return;
            CacheRects();
            ApplyCachedRect();
            ApplyCachedText();
        }
    }
}
