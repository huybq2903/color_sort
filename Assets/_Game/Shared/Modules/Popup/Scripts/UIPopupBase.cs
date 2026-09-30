/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-26
 */

using System;
using DG.Tweening;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.UI.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Falcon.Shared.EasyPopup
{
    public abstract class UIPopupBaseCloseButton<P> : UIPopupBase<P> where P : UIPopup
    {
        [SerializeField] protected Button btnBack;

        protected virtual void Awake()
        {
            btnBack.onClick.AddListener(OnClickBack);
        }
    }
    public abstract class UIPopupBase<P> : UIPopupBase where P : UIPopup
    {
        public static void Show(Action<P> callback = null)
        {
            UIWrapper.OpenPopup(typeof(P).Name, trans =>
            {
                callback?.Invoke(trans.GetComponent<P>());
            });
        }

        public static P Get
        {
            get
            {
                var ls = UIWrapper.Manager?.GetAllPopupStack();
                if (ls == null) return null;
                foreach (var p in ls)
                {
                    if (p is P uiPopup) return uiPopup;
                }

                return null;
            }
        }

        public static void Hide()
        {
            UIWrapper.ClosePopup(typeof(P).Name);
        }
    }
    public abstract class UIPopupBase : UIPopup
    {
        public bool isFadeHide = true;
        private RectTransform _rectFade;

        protected virtual void OnUIPopupEnable() { }

        public virtual void OnClickBack()
        {
            UIWrapper.ClosePopup(transform);
        }

        public override void ChangeVisibility(bool isVisible)
        {
            if (!_initialized) InitializeElements();

            if (this is not IPopupNotFade)
            {
                if (isVisible)
                {
                    if (!_rectFade)
                    {
                        var go = GameRequest<GameObject>.Request(UIFadePopupPool.REQUEST_GET_FADE);
                        _rectFade = go.GetComponent<RectTransform>();
                        _rectFade.SetParent(UIWrapper.Manager.rootStorePopup);
                        _rectFade.SetAsLastSibling();
                        _rectFade.localPosition = Vector3.zero;
                        _rectFade.localScale = Vector3.one;
                        _rectFade.anchorMin = Vector2.zero;
                        _rectFade.anchorMax = Vector2.one;
                        _rectFade.offsetMin = Vector2.zero;
                        _rectFade.offsetMax = Vector2.zero;
                    }
                    transform.SetAsLastSibling();

                    var imgFade = _rectFade.GetComponent<Image>();
                    var buttonFade = _rectFade.GetComponent<Button>();
                    if (uiAnimation)
                    {
                        var duration = uiAnimation.isOverrideDuration ? uiAnimation.durationShow : uiAnimation.duration;
                        imgFade.DOFade(0.85f, duration).From(0.3f).SetEase(Ease.OutCubic);
                    }
                    else
                    {
                        imgFade.DOFade(0.85f, 0f).SetEase(Ease.OutCubic);
                    }

                    buttonFade.onClick.RemoveAllListeners();
                    if (this is IPopupBackOnFade)
                    {
                        buttonFade.enabled = true;
                        buttonFade.onClick.AddListener(OnClickBack);
                    }
                    else
                    {
                        buttonFade.enabled = false;
                    }
                }
                else if (isFadeHide)
                {
                    var imgFade = _rectFade.GetComponent<Image>();
                    if (uiAnimation is UIAnimationLightPopup lightPopup)
                        imgFade.DOFade(0, hidingTime).SetEase(lightPopup.fadeCurve);
                    else
                        imgFade.DOFade(0, hidingTime).SetEase(Ease.OutCubic);
                }
            }

            base.ChangeVisibility(isVisible);
        }

        protected override void DeactivateMe()
        {
            GameEvent<GameObject>.Emit(UIFadePopupPool.EVENT_RETURN_FADE, _rectFade.gameObject);
            _rectFade = null;
            transform.SetAsFirstSibling();
            base.DeactivateMe();
        }

        protected virtual void Update()
        {
#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnClickBack();
            }
#endif
        }
    }

    public interface IPopupNotFade { }
    public interface IPopupBackOnFade { }
}
