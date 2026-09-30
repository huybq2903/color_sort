/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-13
 */

using System;
using DG.Tweening;
using Falcon.Helpers.UI;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.Mediation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Falcon.InGame.UI
{
    public class UIPopupContinue : MonoBehaviour
    {
        [SerializeField] private Button btnAds, btnGold, btnGiveUp;
        [SerializeField] private TMP_Text txtGold;
        [SerializeField] private UISelectableExtension holdToSee;
        [SerializeField] private CanvasGroup canvasGroup;
        public Action OnGiveUp { get; set; }
        public Action OnRevive { get; set; }

        private bool _isGiveUp;
        private int _goldToRevive;

        private void Awake()
        {
            btnGiveUp.onClick.AddListener(() => _isGiveUp = true);
            btnAds.onClick.AddListener(OnClickAds);
            btnGold.onClick.AddListener(OnClickGold);
            holdToSee.OnButtonPress.AddListener(OnRevealDown);
            holdToSee.OnButtonRelease.AddListener(OnRevealUp);
        }

        private void OnClickAds()
        {
            MediationHelpers.Instance.ShowRewardedVideo(PlacementAds.REVIVE, ReviveAndClose);
        }

        private void OnClickGold()
        {
            if (ResourceCollector.Instance.GetResourceValueIntInCollector("gold") < _goldToRevive)
            {
                UIWrapper.OpenPopup("UIPopupShop");
                return;
            }

            ResourceCollector.Instance.ResourceRemove("gold", _goldToRevive, null, "revive_in_game");
            ResourceCollector.Instance.SaveAndUpdateServerOfResourceData("gold");
            ReviveAndClose();
        }

        private void ReviveAndClose()
        {
            OnRevive?.Invoke();
            UIWrapper.ClosePopup(transform);
        }

        private void OnRevealUp(PointerEventData.InputButton arg0)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, 0.25f);
        }

        private void OnRevealDown(PointerEventData.InputButton arg0)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, 0.25f);
        }

        private void OnDisable()
        {
            if (_isGiveUp)
            {
                _isGiveUp = false;
                OnGiveUp?.Invoke();
            }
        }

        public void SetData(bool isCanByAds, int goldToRevive)
        {
            _goldToRevive = goldToRevive;
            txtGold.SetText("<sprite=1> " + goldToRevive.ToString("N0"));
            btnAds.gameObject.SetActive(isCanByAds);
        }
    }
}