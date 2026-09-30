using Falcon.Shared.Addressable;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.BaseBooster
{
    public class UIPopupUnlockBooster : MonoBehaviour
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private TMP_Text txtName, txtDesc;
        [SerializeField] private Image imgIcon;
        public ATutorialUnlockBooster TutorialUnlock { get; set; }

        public void SetData()
        {
            var boosterType = TutorialUnlock.BoosterLogic.Type;
            imgIcon.sprite = AddressableExtensions.Load<Sprite>(boosterType);
            txtName.SetText(BoosterConfig.GetName(boosterType));
            txtDesc.SetText(BoosterConfig.GetDescription(boosterType));

            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(TutorialUnlock.NextStep);
        }
    }
}