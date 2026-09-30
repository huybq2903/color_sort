/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
*/

using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Falcon.Modules.UI.Level.Runtime
{
    public class ScaleOnPressButton : Button
    {
        [SerializeField, Tooltip("Scale hiện tại của button sẽ được nhân với _scaleFactor")]
        private Vector2 _scaleFactor = new Vector2(0.96f, 0.93f);

        private Vector3 _oldScale;

        protected override void Awake()
        {
            base.Awake();
            _oldScale = transform.localScale;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            transform.DOScale(new Vector3(_oldScale.x * _scaleFactor.x, _oldScale.y * _scaleFactor.y, 1f), 0.1f).SetUpdate(true);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            transform.DOScale(_oldScale, 0.1f).SetUpdate(true);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            transform.DOScale(_oldScale, 0.1f).SetUpdate(true);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            transition = Transition.None;
            navigation = Navigation.defaultNavigation;
        }
#endif
    }
}