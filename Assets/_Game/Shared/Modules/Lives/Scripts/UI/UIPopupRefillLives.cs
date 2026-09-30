// Author: Bui Quang Huy
// Company: Falcon Games

using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.Common;
using Falcon.Shared.Common.Time;
using Falcon.Shared.Mediation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.Lives
{
    /// <summary>Popup nạp tim: xem quảng cáo được 1 tim, hoặc trả vàng để đầy.</summary>
    public class UIPopupRefillLives : MonoBehaviour
    {
        private const string GOLD = "gold";
        private const string PLACE = "refill_lives";

        [SerializeField] private Button btnClose, btnAds, btnBuy;
        [SerializeField] private TMP_Text txtQuantity, txtCountdown, txtPrice, txtAmountAds, txtAmoundFull;
        [SerializeField] private GameObject objInfinity, objCountdown;

        private string _tickKey;
        private WrapperLives _wrapper;

        // Giây và chế độ đang hiển thị, để không SetText lại y hệt
        private long _shownSeconds = long.MinValue;
        private bool _shownUnlimited;

        private static int GoldPrice => LivesConfig.Get("price_refill_lives");
        private static int AdsAmount => LivesConfig.Get("ads_amount_refill_lives");
        private static int LevelStartAds => LivesConfig.Get("level_start_ads_refill_lives");

        private void Awake()
        {
            _tickKey = $"lives_popup_{GetInstanceID()}";
            _wrapper = Center.GetOrCreate<WrapperLives>();
            if (btnClose) btnClose.onClick.AddListener(OnClickClose);
            if (btnAds) btnAds.onClick.AddListener(OnClickAds);
            if (btnBuy) btnBuy.onClick.AddListener(OnClickBuy);
        }

        private void OnEnable()
        {
            _wrapper.OnChanged += Refresh;
            _shownSeconds = long.MinValue;
            WrapperTime.AddTick(_tickKey, long.MaxValue, _ => RefreshCountdown(), null);
            Refresh();
        }

        private void OnDisable()
        {
            _wrapper.OnChanged -= Refresh;
            // Xoá đồng bộ, RemoveTick chỉ xếp hàng nên enable lại sẽ chồng thêm delegate
            WrapperTime.RemoveAndEndTick(_tickKey);
        }

        private void Refresh()
        {
            var unlimited = _wrapper.IsUnlimited;
            var isMax = _wrapper.IsMax;

            if (txtQuantity)
            {
                txtQuantity.gameObject.SetActive(!unlimited);
                if (!unlimited) txtQuantity.SetText($"{_wrapper.Quantity}/{_wrapper.Max}");
            }

            if (objInfinity) objInfinity.SetActive(unlimited);
            if (txtPrice) txtPrice.SetText($"<sprite name=\"icon-gold\"> {GoldPrice:N0}");
            if (txtAmountAds) txtAmountAds.SetText($"{AdsAmount}");
            if (txtAmoundFull) txtAmoundFull.SetText($"{_wrapper.Max}");

            // Đầy hoặc đang vô hạn thì không còn gì để nạp
            var canRefill = !unlimited && !isMax;
            if (btnAds)
            {
                // Chỉ mời xem ads khi không đủ vàng mua đầy và đã qua level bật ads
                var enoughGold = ResourceCollector.Instance.GetResourceValueIntInCollector(GOLD) >= GoldPrice;
                var level = GameRequest<int>.Request(GameKeys.GET_LEVEL);
                btnAds.gameObject.SetActive(!enoughGold && level >= LevelStartAds);
                btnAds.interactable = canRefill;
            }
            if (btnBuy) btnBuy.interactable = canRefill;

            RefreshCountdown();
        }

        private void RefreshCountdown()
        {
            if (txtCountdown == null) return;

            var unlimited = _wrapper.IsUnlimited;
            var visible = unlimited || !_wrapper.IsMax;

            if (objCountdown.activeSelf != visible) objCountdown.SetActive(visible);
            if (!visible) return;

            var seconds = unlimited ? _wrapper.UnlimitedSecondsLeft : _wrapper.SecondsToNextRegen;
            if (seconds == _shownSeconds && unlimited == _shownUnlimited) return;

            _shownSeconds = seconds;
            _shownUnlimited = unlimited;
            txtCountdown.SetText(unlimited ? seconds.ToTime() : seconds.ToTimeMinute());
        }

        private void OnClickAds()
        {
            if (_wrapper.IsMax || _wrapper.IsUnlimited) return;

            MediationHelpers.Instance.ShowRewardedVideo(PLACE, () => _wrapper.Add(AdsAmount));
        }

        private void OnClickBuy()
        {
            if (_wrapper.IsMax || _wrapper.IsUnlimited) return;

            if (ResourceCollector.Instance.GetResourceValueIntInCollector(GOLD) < GoldPrice)
            {
                UIWrapper.OpenPopup("UIPopupShop");
                return;
            }

            ResourceCollector.Instance.ResourceRemove(GOLD, GoldPrice, null, PLACE);
            ResourceCollector.Instance.SaveAndUpdateServerOfResourceData(GOLD);

            _wrapper.Add(_wrapper.Max - _wrapper.Quantity);
            OnClickClose();
        }

        private void OnClickClose() => UIWrapper.ClosePopup(transform);
    }
}
