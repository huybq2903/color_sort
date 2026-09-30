/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-17
 */

using UnityEngine;

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// UI Base bọc cho một pack, kế thừa để hỗ trợ làm hoạt ảnh
    /// </summary>
    public abstract class UIWrapperItemBase : MonoBehaviour
    {
        protected RectTransform _rect, _child;
        
        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }
        
        /// <summary>Gắn một item con vào wrapper.</summary>
        public void SetChild(GameObject item)
        {
            _child = item.GetComponent<RectTransform>();
            _child.SetParent(transform);
            _child.anchorMin = _child.anchorMax = Vector2.one * 0.5f;
            _child.transform.localPosition = Vector3.zero;
            _rect.sizeDelta = _child.sizeDelta;
        }

        private void LateUpdate()
        {
            if (!_child) return;

            _rect.sizeDelta = _child.sizeDelta;
        }
        
        /// <summary>
        /// Kế thừa để tự làm aniamtion
        /// </summary>
        public abstract void SetUpAnimation();
    }
}