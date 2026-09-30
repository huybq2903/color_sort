/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-16
 */

using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.UI.Purchase.Runtime
{
    public class UIPopupPurchaseFail : MonoBehaviour
    {
        [SerializeField] private Button bContinue, bExit;
        
        private void Awake()
        {
            bContinue.onClick.AddListener(ClickBack);
            bExit.onClick.AddListener(ClickBack);
        }

        private void ClickBack()
        {
            GameEvent<Transform>.Emit("falcon.modules.core.ui_close_popup", transform);
        }
    }
}