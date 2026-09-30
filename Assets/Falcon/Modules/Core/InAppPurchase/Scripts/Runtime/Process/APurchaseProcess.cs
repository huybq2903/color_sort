/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho toàn bộ quy trình xử lý giao dịch.
    /// Quản lý tiến trình, xác thực và trạng thái.
    /// Subclass cần triển khai các xử lý kết quả.
    /// </summary>
    public abstract class APurchaseProcess
    {
        /// <summary>
        /// Các trạng thái có thể xảy ra khi xác thực.
        /// </summary>
        public enum State
        {
            /// <summary>Giao dịch thành công</summary>
            Purchased = 0,

            /// <summary>Phát hiện dấu hiệu bất thường</summary>
            Fraud = 1,

            /// <summary>Giao dịch thất bại</summary>
            Failed,
        }

        protected Action<Product> _onValidationFinished;
        
        /// <summary>
        /// Product đang được xử lý.
        /// </summary>
        public Product PurchasedProduct { get; private set; }
        
        /// <summary>
        /// Token để huỷ các thao tác bất đồng bộ.
        /// </summary>
        protected CancellationTokenSource Cts { get; private set; }
        
        /// <summary>
        /// Vị trí gọi giao dịch trong UI/game (ví dụ: "Shop_Popup", "Level_Reward").
        /// </summary>
        public string Where { get; protected set; }
        
        /// <summary>
        /// Nhãn tuỳ chọn để phân biệt các giao dịch có cùng productId.
        /// Dùng làm hậu tố sau productId khi gửi log lên
        /// </summary>
        public string Why { get; protected set; }
        
        /// <summary>
        /// Bắt đầu quá trình mua hàng với sản phẩm chỉ định.
        /// Gửi yêu cầu xác thực.
        /// Nếu chưa đăng kí hệ thống xác thực nào thì sẽ mặc định là giao dịch thành công.
        /// </summary>
        internal virtual void Start(Product product, Action<Product> onFinish)
        {
            ReleaseCts();

            Cts = new CancellationTokenSource();
            PurchasedProduct = product;
            _onValidationFinished = onFinish;
            if (!IAPManager.IsAnyPurchaseValidation)
            {
                OnReceiveValidationAsync(State.Purchased);
                return;
            }
            foreach (var purchaseValidation in IAPManager.listPurchaseValidation)
            {
                purchaseValidation.SendValidate(this);
            }
        }
        
        private async void OnReceiveValidationAsync(State state)
        {
            try
            {
                await Task.Delay(100, Cts.Token);
                OnReceiveValidation(state);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void Log()
        {
            if (!IAPManager.IsLogPurchaseInEditor && Application.isEditor) return;
            
            foreach (var logger in IAPManager.listPurchaseLogger)
            {
                logger.Log(this);
            }
        }
        
        /// <summary>
        /// Xử lý kết quả xác thực (thành công, thất bại hoặc gian lận) từ các validator bên ngoài.
        /// Gọi callback tương ứng và kết thúc quy trình giao dịch.
        /// Nếu giao dịch thành công sẽ gửi log giao dịch
        /// </summary>
        /// <param name="state">Trạng thái kết quả xác thực.</param>
        public void OnReceiveValidation(State state)
        {
            if (Cts == null) return;
            switch (state)
            {
                case State.Purchased: Log(); OnValidationSucceeded(); break;
                case State.Failed: OnPurchaseFailed(); break;
                case State.Fraud: OnHackDetected(); break;
            }
            ReleaseCts();
            _onValidationFinished?.Invoke(PurchasedProduct);
        }

        public void OnForceFailed() => OnPurchaseFailed();

        /// <summary>
        /// Huỷ và giải phóng token bất đồng bộ.
        /// </summary>
        internal void ReleaseCts()
        {
            if (Cts == null) return;
            Cts.Cancel();
            Cts.Dispose();
            Cts = null;
        }

        /// <summary>
        /// Kiểm tra ID giao dịch có trùng với sản phẩm hiện tại.
        /// </summary>
        public bool HasTransactionID(string id)
        {
            if (PurchasedProduct == null) return false;
            return PurchasedProduct.transactionID == id;
        }
        
        /// <summary>
        /// Được gọi khi xác thực thành công.
        /// </summary>
        protected abstract void OnValidationSucceeded();
        
        /// <summary>
        /// Được gọi khi phát hiện bất thường.
        /// </summary>
        protected abstract void OnHackDetected();
        
        /// <summary>
        /// Được gọi khi giao dịch thất bại.
        /// </summary>
        protected abstract void OnPurchaseFailed();
    }
}