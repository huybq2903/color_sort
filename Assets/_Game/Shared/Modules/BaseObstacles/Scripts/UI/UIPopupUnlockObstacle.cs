/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using Falcon.Modules.UnityLocalization.Runtime;
using Falcon.Shared.Addressable;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.BaseObstacle
{
    public class UIPopupUnlockObstacle : MonoBehaviour
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private TMP_Text txtName, txtDesc;
        [SerializeField] private Image imgIcon;

        public ATutorialUnlockObstacle TutorialUnlock { get; set; }

        // Icon theo addressable key = type, text theo key localize quy ước. Không cần SO map như booster.
        public void SetData()
        {
            var type = TutorialUnlock.ObstacleLogic.Type;
            imgIcon.sprite = AddressableExtensions.Load<Sprite>(type);
            txtName.SetText(ULocManager.GetLocalizedString($"name_obstacle_{type}"));
            txtDesc.SetText(ULocManager.GetLocalizedString($"description_obstacle_{type}"));

            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(TutorialUnlock.NextStep);
        }
    }
}
