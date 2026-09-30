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
    public class UIPopupPurchaseSuccess : MonoBehaviour
    {
        [SerializeField] private float timeShow;

        private void OnEnable()
        {
            StartCoroutine(CoShow());
        }

        private IEnumerator CoShow()
        {
            yield return new WaitForSeconds(timeShow);
            GameEvent<Transform>.Emit("falcon.modules.core.ui_close_popup", transform);
            GameEvent.Emit("falcon.modules.ui.purchase.close_success");
        }
    }
}