/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-11
 */

using System;
using System.Collections;
using DG.Tweening;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.BaseEvents
{
    public class UIPopupEventTut : MonoBehaviour
    {
        [SerializeField] private float durationScaleOne = 0.4f;
        [SerializeField] private float durationBetween = 0.2f;
        [SerializeField] private Transform[] listShow;
        
        private Button _buttonClose;
        
        public Action OnClosePopup { get; set; }

        private void Awake()
        {
            _buttonClose = GetComponentInChildren<Button>();
            _buttonClose.onClick.AddListener(OnClose);
        }

        protected virtual void OnClose()
        {
            GameEvent<Transform>.Emit("falcon.modules.core.ui_close_popup", transform);
            OnClosePopup?.Invoke();
        }

        protected virtual void OnEnable()
        {
            foreach (var go in listShow)
            {
                go.localScale = Vector3.zero;
            }

            _buttonClose.enabled = false;
            StartCoroutine(Show());
        }
        
        private IEnumerator Show()
        {
            foreach (var goShow in listShow)
            {
                goShow.DOScale(1, durationScaleOne).SetEase(Ease.OutBack).WaitForCompletion();
                yield return new WaitForSeconds(durationBetween);
            }
            _buttonClose.enabled = true;
        }
    }
}