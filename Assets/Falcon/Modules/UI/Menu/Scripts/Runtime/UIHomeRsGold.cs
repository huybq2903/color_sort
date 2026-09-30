/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-2
*/

using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class UIHomeRsGold : MonoBehaviour
    {
        public TextMeshProUGUI txtGold;
        public Button btnGetMoreGold;

        protected virtual void OnEnable()
        {
            UpdateUI();
        }

        protected virtual void UpdateUI()
        {
            var gold = ResourceCollector.Instance.GetResourceInCollector("gold").Get;
            txtGold.SetText($"{gold}");
            btnGetMoreGold.onClick.RemoveAllListeners();
            btnGetMoreGold.onClick.AddListener(() =>
            {
                GameEvent<string>.Emit(Const.EVENT_OPEN_POPUP_NAME, "UIPopupShop");
            });
        }
    }
}
