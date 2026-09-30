/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-1
*/

using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using Falcon.Helpers.Singleton;

namespace Falcon.Modules.UI.Toast.Runtime
{
    public class UIToast : Singleton<UIToast>
    {
        public GameObject toastItem;

        protected List<GameObject> _listToastItem = new();
        protected UIToastConfig _config;

        protected override void Awake()
        {
            base.Awake();

            //Cache
            _config = Resources.Load<UIToastConfig>("SO_UI_ToastConfig");

            if (toastItem != null) toastItem.SetActive(false);
        }

        protected virtual void OnEnable()
        {
            GameEvent<string>.Register(Const.EVENT_OPEN_UI, Show, this);
        }

        protected virtual void OnDisable()
        {
            GameEvent<string>.Unregister(Const.EVENT_OPEN_UI, Show, this);
        }

        public virtual void Show(string content)
        {
            if (toastItem == null)
            {
                Debug.LogError("Chưa gán ToastItem prefab.");
                return;
            }

            // Nếu đã quá số lượng -> remove toast trên cùng
            if (_listToastItem.Count >= _config.maxItemInstance)
            {
                var oldest = _listToastItem[^1];
                _listToastItem.RemoveAt(_listToastItem.Count - 1);

                DOTween.Kill(oldest);
                var cg = oldest.GetComponent<CanvasGroup>();
                Sequence hideSeq = DOTween.Sequence();
                hideSeq.Join(oldest.transform.DOScale(Vector3.zero, _config.durationScale).SetEase(Ease.InBack));
                if (cg != null)
                    hideSeq.Join(cg.DOFade(0f, _config.durationScale));
                hideSeq.OnComplete(() => Destroy(oldest, _config.lifetime));
            }

            // Instantiate toast mới
            var cloneToast = Instantiate(toastItem, transform);
            cloneToast.transform.localScale = Vector3.zero;
            cloneToast.transform.localPosition = Vector3.zero;
            cloneToast.SetActive(true);

            cloneToast.GetComponent<UIToastItem>()?.SetItemData(content);

            var canvasGroup = cloneToast.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            _listToastItem.Insert(0, cloneToast);

            UpdateToastPositions();

            // Fade + scale in
            Sequence showSeq = DOTween.Sequence();
            showSeq.Join(cloneToast.transform.DOScale(Vector3.one, _config.durationScale).SetEase(_config.easeScaleIn));
            if (canvasGroup != null)
                showSeq.Join(canvasGroup.DOFade(1f, _config.durationScale));

            ResetToastLifetime(cloneToast);
        }

        protected virtual void UpdateToastPositions()
        {
            for (int i = 0; i < _listToastItem.Count; i++)
            {
                var targetPos = Vector3.up * i * _config.spacingItem;
                _listToastItem[i].transform.DOLocalMove(targetPos, _config.durationMoveUp).SetEase(_config.easeMoveUp);
            }
        }

        protected virtual void ResetToastLifetime(GameObject toast)
        {
            DOTween.Kill(toast);

            var canvasGroup = toast.GetComponent<CanvasGroup>();

            DOVirtual.DelayedCall(_config.lifetime, () =>
            {
                if (toast == null) return;

                Sequence hideSeq = DOTween.Sequence();
                hideSeq.Join(toast.transform.DOScale(Vector3.zero, _config.durationScale).SetEase(_config.easeScaleOut));
                if (canvasGroup != null)
                    hideSeq.Join(canvasGroup.DOFade(0f, _config.durationScale));

                hideSeq.OnComplete(() =>
                {
                    _listToastItem.Remove(toast);
                    Destroy(toast, _config.lifetime);
                    UpdateToastPositions();
                });
            }).SetId(toast);
        }

        [Button]
        protected virtual void Test()
        {
            GameEvent<string>.Emit(Const.EVENT_OPEN_UI, "Toast Content Here ! " + Random.Range(0, 9999));
        }
    }
}
