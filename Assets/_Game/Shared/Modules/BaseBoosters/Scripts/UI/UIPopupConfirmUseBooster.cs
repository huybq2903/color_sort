using Falcon.Helpers.EventBus;
using Falcon.Shared.Addressable;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.BaseBooster
{
    public class UIPopupConfirmUseBooster : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtName, txtDesc;
        [SerializeField] private Image imgIcon;
        public Button btnConfirm;

        public ATutorialConfirmUseBooster ConfirmUseTut { get; set; }

        private TMP_Text txtBtn;

        private void Awake()
        {
            txtBtn = btnConfirm.GetComponentInChildren<TMP_Text>();
        }

        public void SetData()
        {
            var logic = ConfirmUseTut.BoosterLogic;
            imgIcon.sprite = AddressableExtensions.Load<Sprite>(logic.Type);
            txtName.SetText(BoosterConfig.GetName(logic.Type));
            txtBtn.SetText(BoosterConfig.GetName(logic.Type));
            txtDesc.SetText(BoosterConfig.GetDescription(logic.Type));
            GameEvent<bool>.Emit($"falcon.game.ui_{logic.Type}_highlight", true);

            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(ConfirmUseTut.NextStep);
        }

        private void OnDisable()
        {
            GameEvent<bool>.Emit($"falcon.game.ui_{ConfirmUseTut.BoosterLogic.Type}_highlight", false);
        }
    }
}