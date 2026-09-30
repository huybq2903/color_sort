// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-14

using Falcon.Modules.Core.UI.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.InGame.UI
{
    public class InGameHUD : MonoBehaviour
    {
        [SerializeField] private Button btnPause;
        [SerializeField] private TMP_Text txtLevel;

        private void Awake()
        {
            btnPause.onClick.AddListener(OnShowPopupPause);
        }

        public void SetData(int level)
        {
            txtLevel.SetText($"Lv. {level}");
        }

        private void OnShowPopupPause()
        {
            UIWrapper.OpenPopup("UIPopupPause");
        }
    }
}
