/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Xử lý giao dịch mua mới.
    /// Bao gồm xác thực, timeout, thành công, gian lận và huỷ.
    /// Có hỗ trợ fallback khi timeout.
    /// </summary>
    public class BuyProcess : APurchaseProcess
    {
        private readonly Action<Product> _onSuccessProduct;
        private readonly Action _onSuccess;
        private readonly Action _onFail;
        private readonly float _timeOut;

        private bool _isTimeOut;

        public bool IsStarted { get; private set; }

        /// <summary>
        /// Bắt đầu quy trình mua, thiết lập timeout.
        /// </summary>
        internal override void Start(Product product, Action<Product> onFinish)
        {
            base.Start(product, onFinish);
            if (Cts != null)
            {
                TimeOutAsync(_timeOut);
            }
            IsStarted = true;
        }

        public BuyProcess(Action onSuccess, Action onFail, float timeOut, string where, string why)
        {
            _onSuccess = onSuccess;
            _onFail = onFail;
            _timeOut = timeOut;
            Where = where;
            Why = why;
        }

        public BuyProcess(Action<Product> onSuccess, Action onFail, float timeOut, string where, string why)
        {
            _onSuccessProduct = onSuccess;
            _onFail = onFail;
            _timeOut = timeOut;
            Where = where;
            Why = why;
        }

        protected override void OnValidationSucceeded()
        {
            if (_isTimeOut)
            {
                IAPManager.OnPurchaseRestoreInBackground(PurchasedProduct);
                return;
            }

            _onSuccess?.Invoke();
            _onSuccessProduct?.Invoke(PurchasedProduct);
            IAPManager.OnPurchaseSuccess(PurchasedProduct);
            GameEvent<(string currencyCode, double price)>.Emit(IAPConstant.EVENT_PURCHASE_SUCCESS,
                (
                    PurchasedProduct.metadata.isoCurrencyCode,
                    (double)PurchasedProduct.metadata.localizedPrice
                ));
            GameEvent<double>.Emit(IAPConstant.EVENT_PURCHASE_SUCCESS_USD, IAPManager.GetUsdPriceDouble(PurchasedProduct.definition.id));
            GameEvent.Emit(IAPConstant.EVENT_PURCHASE_SUCCESS);
        }

        protected override void OnHackDetected()
        {
            IAPManager.OnHackDetected(PurchasedProduct);
        }

        protected override void OnPurchaseFailed()
        {
            _onFail?.Invoke();
            IAPManager.OnPurchaseFailed(PurchaseFailureReason.Unknown);
        }

        private void OnTimeout()
        {
            _onValidationFinished?.Invoke(PurchasedProduct);
            _isTimeOut = true;
            IAPManager.OnPurchaseTimeOut(PurchasedProduct);
        }

        private async void TimeOutAsync(float time)
        {
            try
            {
                await Task.Delay((int)(time * 1000), Cts.Token);
                if (IAPManager.IsAnyPurchaseValidation &&
                    IAPManager.IsPurchaseSuccessIfTimeout)
                {
                    OnReceiveValidation(State.Purchased);
                }
                else
                {
                    OnTimeout();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
