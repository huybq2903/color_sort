/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-12
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Kho state lượt mua + sổ transactionId PENDING — vai GIỮ STATE tách khỏi
    /// <see cref="PurchaseAttemptService"/> (vai bắn log) sau án deadlock DI 12/08, cùng khuôn
    /// <see cref="AdViewCache"/>/<see cref="OfferImpressionCache"/>.
    /// <br/>Sổ PENDING nằm ở đây (chứ không ở service) vì decorator log mua phải tra được nó —
    /// xem <see cref="TryConsumePendingTransaction"/>.
    /// </summary>
    public class PurchaseAttemptCache : MySingleton<PurchaseAttemptCache>
    {
        private const string PERSIST_PENDING_TXN = "Analytic_PendingIapTxn";
        private const int MAX_PENDING_TXN = 32;

        private readonly PurchaseAttemptState _state = new();
        private readonly object _lock = new();
        private readonly IDataPool _dataPool;

        public PurchaseAttemptCache(IDataPool dataPool)
        {
            _dataPool = dataPool;
        }

        /// <summary>Id của lượt mua ĐANG mở (null nếu không có).</summary>
        public string OpenAttemptId
        {
            get { lock (_lock) return _state.OpenAttemptId; }
        }

        /// <summary>Ảnh chụp lượt mua đang mở (bản sao; null nếu không có).</summary>
        public PurchaseAttemptSnapshot TakeSnapshot()
        {
            lock (_lock) return _state.TakeSnapshot();
        }

        /// <inheritdoc cref="PurchaseAttemptState.Open"/>
        public (string attemptId, bool replaced) Open(IapPurchaseAttemptParam param)
        {
            lock (_lock) return _state.Open(param);
        }

        /// <inheritdoc cref="PurchaseAttemptState.TakeFail"/>
        public (IapPurchaseFailParam param, string attemptId) TakeFail(IapPurchaseFailReason reason)
        {
            lock (_lock) return _state.TakeFail(reason);
        }

        /// <summary>Giao dịch thành công — trả purchaseAttemptId nếu khớp lượt đang mở.</summary>
        public string TryAttributeSuccess(string productId)
        {
            lock (_lock) return _state.TryAttributeSuccess(productId);
        }

        /// <summary>
        /// Ghi sổ một giao dịch Google PENDING (persist, FIFO trần <see cref="MAX_PENDING_TXN"/>)
        /// — để khi nó hoàn tất nhiều ngày sau không bị gán nhầm vào lượt mua mới cùng sản phẩm
        /// (bẫy #4 §D4).
        /// </summary>
        public void RegisterPendingTransaction(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;

            _dataPool.Compute<string[]>(PERSIST_PENDING_TXN, saved =>
            {
                // FIFO có trần: pending là ca hiếm, 32 là dư — sổ không được phình vô hạn
                var list = new List<string>(saved ?? Array.Empty<string>());
                list.Remove(transactionId);
                list.Add(transactionId);
                if (list.Count > MAX_PENDING_TXN) list.RemoveAt(0);
                return list.ToArray();
            });
            // PENDING là ca hiếm nhưng sống nhiều NGÀY — Compute chỉ ghi RAM, không đẩy xuống đĩa
            // ngay thì kill trong 5 phút kế là mất sổ và giao dịch hoàn tất sau bị gán nhầm.
            _dataPool.TrySync();
        }

        /// <summary>
        /// Giao dịch này có phải một PENDING cũ vừa hoàn tất không — nếu phải thì XOÁ khỏi sổ và
        /// trả true, caller phải BỎ QUA việc gán attempt id (khớp theo productId sẽ ăn nhầm lượt
        /// mua mới cùng sản phẩm đang mở; server tự nối bằng phễu theo product/user/time).
        /// </summary>
        public bool TryConsumePendingTransaction(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return false;

            var found = false;
            _dataPool.Compute<string[]>(PERSIST_PENDING_TXN, saved =>
            {
                if (saved == null) return null;
                var list = new List<string>(saved);
                found = list.Remove(transactionId);
                return list.Count == 0 ? null : list.ToArray();
            });
            return found;
        }
    }
}
