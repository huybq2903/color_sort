/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-20
 */

using System;
using Falcon.Helpers.EventBus;
using UnityEngine;

namespace Falcon.Modules.UI.Purchase.Runtime
{
    public static class PurchaseUIHandler
    {
        private const string EVENT_START_PURCHASE = "falcon.modules.iap.start_purchase";
        private const string EVENT_PURCHASE_SUCCESS = "falcon.modules.iap.purchase_success";
        private const string EVENT_PURCHASE_FAIL = "falcon.modules.iap.purchase_fail";
        private const string EVENT_PURCHASE_TIME_OUT = "falcon.modules.iap.purchase_time_out";
        private const string EVENT_CLOSE_SUCCESS_PURCHASE = "falcon.modules.ui.purchase.close_success";
        private const string EVENT_OPEN_POPUP_NAME = "falcon.modules.core.ui_open_popup_name";
        private const string EVENT_CLOSE_POPUP_NAME = "falcon.modules.core.ui_close_popup_name";
        private const string EVENT_OPEN_WAITING = "falcon.modules.ui.purchase.open_waiting";

        public static Action onStartPurchase = OnStartPurchase;
        public static Action onCloseSuccess = OnCloseSuccess;
        public static Action onPurchaseSuccess = OnPurchaseSuccess;
        public static Action onPurchaseFail = OnPurchaseFail;
        
        private static bool isNeedCloseWaiting = false;
        
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            GameEvent.Register(EVENT_START_PURCHASE, InvokeStartPurchase, null);
            GameEvent.Register(EVENT_CLOSE_SUCCESS_PURCHASE, InvokeCloseSuccess, null);
            GameEvent.Register(EVENT_PURCHASE_SUCCESS, InvokePurchaseSuccess, null);
            GameEvent.Register(EVENT_PURCHASE_FAIL, InvokePurchaseFail, null);
            GameEvent.Register(EVENT_PURCHASE_TIME_OUT, InvokePurchaseFail, null);
            GameEvent.Register(EVENT_OPEN_WAITING, OnOpenWaiting, null);
        }
        
        private static void InvokeStartPurchase() => onStartPurchase?.Invoke();
        private static void InvokeCloseSuccess() => onCloseSuccess?.Invoke();
        private static void InvokePurchaseSuccess() => onPurchaseSuccess?.Invoke();
        private static void InvokePurchaseFail() => onPurchaseFail?.Invoke();
        
        private static void OnPurchaseFail()
        {
            isNeedCloseWaiting = true;
            GameEvent<string>.Emit(EVENT_CLOSE_POPUP_NAME, "UIPopup_PurchaseWaiting");
            GameEvent<string>.Emit(EVENT_OPEN_POPUP_NAME, "UIPopup_PurchaseFail");
        }

        private static void OnPurchaseSuccess()
        {
            GameEvent<string>.Emit(EVENT_OPEN_POPUP_NAME, "UIPopup_PurchaseSuccess");
        }

        private static void OnCloseSuccess()
        {
            isNeedCloseWaiting = true;
            GameEvent<string>.Emit(EVENT_CLOSE_POPUP_NAME, "UIPopup_PurchaseWaiting");
        }

        private static void OnStartPurchase()
        {
            isNeedCloseWaiting = false;
            GameEvent<string>.Emit(EVENT_OPEN_POPUP_NAME, "UIPopup_PurchaseWaiting");
        }
        
        private static void OnOpenWaiting()
        {
            if (isNeedCloseWaiting)
            {
                GameEvent<string>.Emit(EVENT_CLOSE_POPUP_NAME, "UIPopup_PurchaseWaiting");
                isNeedCloseWaiting = false;
            }
        }
    }
}