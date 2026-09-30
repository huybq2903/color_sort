/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-16
*/

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Falcon.Helpers.UI
{
    public class ScrollRectContentLimiter : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [InfoBox("Recommended: Use movement type Clamped for ScrollRect. This component will limit the content position when it reaches the bottom of the ScrollRect")]
        public bool enableLimit = true;
        public ScrollRect scrollRect;

        [Range(0f, 500f)]
        public float offsetFirst = 175f;

        [Range(0f, 500f)]
        public float offsetLast = 175f;

        public float lerpSpeed = 0.175f;

        private bool isDragging = false;

        private void Awake()
        {
            // Invert offsetFirst to match the anchoredPosition.y direction
            offsetFirst = -offsetFirst;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        private void LateUpdate()
        {
            if (enableLimit)
            {
                LimitContentPosition();
            }
        }

        public void SetEnableLimit(bool enable)
        {
            enableLimit = enable;
        }

        private void LimitContentPosition()
        {
            if (!enableLimit) return;
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            if (isDragging) return;

            var viewport = scrollRect.viewport;
            var contentRect = scrollRect.content;
            var contentPosY = contentRect.anchoredPosition.y;
            var contentHeight = contentRect.rect.height;
            var viewportHeight = viewport.rect.height;

            if (contentPosY > offsetFirst)
            {
                scrollRect.StopMovement();
                var targetPosY = offsetFirst;
                contentRect.anchoredPosition = Vector2.Lerp(contentRect.anchoredPosition, new Vector2(contentRect.anchoredPosition.x, targetPosY), lerpSpeed);
            }
            else if (contentPosY <= -(contentHeight - viewportHeight - offsetLast))
            {
                scrollRect.StopMovement();
                var targetPosY = -(contentHeight - viewportHeight - offsetLast);
                contentRect.anchoredPosition = Vector2.Lerp(contentRect.anchoredPosition, new Vector2(contentRect.anchoredPosition.x, targetPosY), lerpSpeed);
            }
        }
    }
}
