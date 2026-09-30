using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Modules.UnityLocalization.Runtime;
using Falcon.Shared.Addressable;
using Falcon.Shared.Mediation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Falcon.Shared.Common;

namespace Falcon.Shared.BaseBooster
{
    public class UIPopupBuyBooster : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtName, txtDesc, txtGold;
        [SerializeField] private Button btnAds, btnBuy;
        [SerializeField] private Image imgIcon;

        public string BoosterType { get; set; }
        protected virtual int GoldPrice => BoosterConfig.GoldPrice(BoosterType);

        protected virtual void Awake()
        {
            btnAds.onClick.AddListener(ShowAds);
            btnBuy.onClick.AddListener(OnBuyByGold);
        }

        protected virtual void ShowAds()
        {
            MediationHelpers.Instance.ShowRewardedVideo($"buy_{BoosterType}", OnBuySuccess);
        }

        protected virtual void OnBuyByGold()
        {
            if (ResourceCollector.Instance.GetResourceValueIntInCollector("gold") < GoldPrice)
            {
                UIWrapper.OpenPopup("UIPopupShop");
                return;
            }

            ResourceCollector.Instance.ResourceRemove("gold", GoldPrice, null, $"buy_{BoosterType}_in_game");
            ResourceCollector.Instance.SaveAndUpdateServerOfResourceData("gold");
            OnBuySuccess();
        }

        protected virtual void OnBuySuccess()
        {
            UIWrapper.ClosePopup(transform);
            ResourceCollector.Instance.ResourceAdd(BoosterType, 1, null, "popup_buy_booster");
            ResourceCollector.Instance.SaveAndUpdateServerOfResourceData(BoosterType);
            GameEvent.Emit($"falcon.game.ui_{BoosterType}_update");
            GameEvent<string>.Emit(GameKeys.USE_BOOSTER, BoosterType);
        }

        public virtual void UpdateUI(bool isCanBuyByAds)
        {
            txtName.SetText(BoosterConfig.GetName(BoosterType));
            txtDesc.SetText(BoosterConfig.GetDescription(BoosterType));
            txtGold.SetText($"{ULocManager.GetLocalizedString("buy")}\n<sprite name=\"icon-gold\"> {GoldPrice:N0}");
            imgIcon.sprite = AddressableExtensions.Load<Sprite>(BoosterType);

            btnAds.gameObject.SetActive(isCanBuyByAds);
        }
    }
}