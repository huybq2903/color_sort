/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-16
 */

using System.Collections;
using Falcon.Helpers.EventBus;
using UnityEngine;

namespace Falcon.Modules.UI.Purchase.Runtime
{
    public class UIPopupPurchaseWaiting : MonoBehaviour
    {
        private void OnEnable()
        {
            StartCoroutine(CoCloseAfterFrame());
        }

        private IEnumerator CoCloseAfterFrame()
        {
            yield return null;
            GameEvent.Emit("falcon.modules.ui.purchase.open_waiting");
        }
    }
}