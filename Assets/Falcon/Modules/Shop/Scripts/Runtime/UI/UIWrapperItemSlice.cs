/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-17
 */

using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// Bao bọc UI cho một pack, hỗ trợ slide animation.
    /// </summary>
    public class UIWrapperItemSlice : UIWrapperItemBase
    {
        private IEnumerator _coSlide;
        private float _delayToSlide;

        private const float DELAY_EACH_ROW = 0.05f;
        private const float START_POSITION_SLIDE = 300f;
        private const float DURATION_SLIDE = 0.4f;

        private void SetDelayByPosition()
        {
            _delayToSlide = DELAY_EACH_ROW * transform.GetSiblingIndex();
            _child.gameObject.SetActive(false);
            
            if (!gameObject.activeInHierarchy) return;
            
            if (_coSlide != null) StopCoroutine(_coSlide);
            _coSlide = CoSlide();
            StartCoroutine(_coSlide);
        }

        private IEnumerator CoSlide()
        {
            yield return new WaitForSeconds(_delayToSlide);
            _child.gameObject.SetActive(true);
            _child.localPosition = Vector3.right * START_POSITION_SLIDE;

            _child.transform.DOLocalMove(Vector3.zero, DURATION_SLIDE).SetEase(Ease.OutBack)
                .SetTarget(_child);
        }

        private void OnDisable()
        {
            if (_child) _child.DOComplete();
        }

        public override void SetUpAnimation() => SetDelayByPosition();
    }
}