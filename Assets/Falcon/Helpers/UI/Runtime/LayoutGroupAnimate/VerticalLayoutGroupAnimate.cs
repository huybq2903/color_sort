using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

namespace Falcon.Helpers.UI
{
    [AddComponentMenu("UI/Layout/Vertical Layout Group Animate")]
    public class VerticalLayoutGroupAnimate : VerticalLayoutGroup
    {
        [Header("Settings")]
        public float animationDuration = 0.5f;
        public Ease animationEase = Ease.OutQuad;

        private bool _isFirstLayout = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            _isFirstLayout = true;
        }

        public void ForceAnimateLayout()
        {
            // Hủy tất cả các tween đang chạy trên các con
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                if (child != null)
                {
                    child.DOKill();
                }
            }
            
            _isFirstLayout = false;
            SetLayoutVertical();
        }

        public override void SetLayoutVertical()
        {
            if (!Application.isPlaying)
            {
                base.SetLayoutVertical();
                return;
            }

            if (_isFirstLayout)
            {
                base.SetLayoutVertical();
                _isFirstLayout = false;
                return;
            }

            // Lưu vị trí ban đầu vào một danh sách cục bộ
            var startPositions = new List<Vector2>();
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                if (child != null)
                {
                    startPositions.Add(child.anchoredPosition);
                }
            }

            // Để lớp cơ sở tính toán vị trí mới
            base.SetLayoutVertical();

            // Diễn hoạt từ vị trí cũ đến vị trí mới
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                if (child != null && i < startPositions.Count)
                {
                    Vector2 startPos = startPositions[i];
                    Vector2 endPos = child.anchoredPosition;
                    
                    // Nếu vị trí không thay đổi thì bỏ qua
                    if (startPos == endPos) continue;

                    // Đặt lại vị trí ban đầu và bắt đầu diễn hoạt
                    child.anchoredPosition = startPos;
                    child.DOAnchorPosY(endPos.y, animationDuration).SetEase(animationEase);
                }
            }
        }
    }
}
