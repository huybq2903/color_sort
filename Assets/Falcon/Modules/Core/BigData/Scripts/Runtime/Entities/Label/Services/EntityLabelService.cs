/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-11
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Giữ nhãn của VẬT và cấp bundle cho pipeline đóng dấu (§D11). KHÔNG bắn event nào —
    /// nhãn đi ké event của chính vật đó, y khuôn <c>playTurnId</c> đi ké mọi log.
    /// <br/>Có persist: nhãn phải sống qua restart, không thì người chơi mở lại app là event
    /// mất nhãn cho tới khi game khai lại.
    /// <br/>Khoá vật là CHUỖI và luôn kèm kind: <c>offerId</c> vốn là chuỗi, và level "42" với
    /// offer "42" phải là hai kho khác nhau.
    /// </summary>
    public class EntityLabelService : MySingleton<EntityLabelService>
    {
        private const string PERSIST_PREFIX = "Analytic_EntityLabels_";

        private readonly EntityLabelState _state = new();
        private readonly HashSet<string> _restored = new();
        private readonly object _lock = new();
        private readonly IDataPool _dataPool;

        public EntityLabelService(IDataPool dataPool)
        {
            _dataPool = dataPool;
        }

        /// <summary>
        /// Khai một nhãn cho VẬT. Không bắn gì ngay — giá trị nằm trong cache và đi ké mọi event
        /// của vật đó từ lúc này. <paramref name="value"/> = null là GỠ.
        /// </summary>
        public void Set(LabelEntityKind kind, string entityId, string labelKey, object value)
        {
            Set(kind, entityId, labelKey, value, registered: false);
        }

        /// <summary>
        /// Khai nhãn ĐÃ ĐĂNG KÝ (tên do SDK cấp — <see cref="FLevelLabelKey"/>). Khác bản công khai
        /// ở chỗ nó được xài trọn trần byte: chỗ đã giữ sẵn nên thứ tự gọi không quyết định ai sống.
        /// </summary>
        internal void SetRegistered(LabelEntityKind kind, string entityId, string labelKey, object value)
        {
            Set(kind, entityId, labelKey, value, registered: true);
        }

        /// <summary>
        /// Bundle đóng dấu lên event của vật này (null nếu vật chưa có nhãn).
        /// Gọi từ pipeline decor — xem <see cref="LevelLogDecorService"/> / <see cref="OfferLogDecorService"/>.
        /// </summary>
        public Dictionary<string, object> BundleFor(LabelEntityKind kind, string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;

            var storeKey = StoreKey(kind, entityId);
            lock (_lock)
            {
                RestoreOnce(storeKey);
                return _state.BundleFor(storeKey);
            }
        }

        private void Set(LabelEntityKind kind, string entityId, string labelKey, object value, bool registered)
        {
            if (string.IsNullOrEmpty(entityId))
            {
                AnalyticLogger.Instance.Warning($"Nhãn vật {kind} bỏ qua: entityId rỗng.");
                return;
            }

            var storeKey = StoreKey(kind, entityId);
            var budget = BudgetFor(kind, registered);

            lock (_lock)
            {
                RestoreOnce(storeKey);
                var result = _state.Set(storeKey, labelKey, value, out var bytes, budget);
                switch (result)
                {
                    case EntityLabelSet.BlankKey:
                        AnalyticLogger.Instance.Warning("Nhãn vật bỏ qua: labelKey rỗng.");
                        return;

                    case EntityLabelSet.BundleTooBig:
                        AnalyticLogger.Instance.Warning(
                            $"Nhãn '{labelKey}' của {kind} {entityId} bỏ qua: bundle {bytes}B, trần " +
                            $"{budget}B. Bundle này đi kèm MỌI event của vật — nên vượt trần là server " +
                            "vứt cả bundle. Bỏ bớt nhãn cũ hoặc rút ngắn giá trị." +
                            (budget == EntityLabelState.MAX_BUNDLE_BYTES
                                ? string.Empty
                                : $" (Trần wire là {EntityLabelState.MAX_BUNDLE_BYTES}B, " +
                                  $"{EntityLabelState.RESERVED_REGISTERED_BYTES}B giữ cho nhãn hợp đồng.)"));
                        return;

                    default:
                        Persist(storeKey);
                        return;
                }
            }
        }

        /// <summary>
        /// Chỗ giữ cho nhãn hợp đồng CHỈ trừ vào loại vật thực sự có nhãn hợp đồng — trừ ở loại
        /// không có thì chỉ bóp oan phần của game.
        /// </summary>
        private static int BudgetFor(LabelEntityKind kind, bool registered)
        {
            if (registered || kind != LabelEntityKind.Level) return EntityLabelState.MAX_BUNDLE_BYTES;
            return EntityLabelState.MAX_BUNDLE_BYTES - EntityLabelState.RESERVED_REGISTERED_BYTES;
        }

        private static string StoreKey(LabelEntityKind kind, string entityId)
        {
            return kind + "_" + entityId;
        }

        /// <summary>
        /// Nạp lười theo từng vật, không nạp cả bộ lúc khởi động: game có thể khai nhãn cho hàng
        /// trăm màn, mà một phiên chỉ chạm vài màn.
        /// </summary>
        private void RestoreOnce(string storeKey)
        {
            if (!_restored.Add(storeKey)) return;

            var json = _dataPool.GetOrDefault<string>(PERSIST_PREFIX + storeKey, null);
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                _state.Restore(storeKey, JsonConvert.DeserializeObject<Dictionary<string, object>>(json));
            }
            catch (Exception e)
            {
                AnalyticLogger.Instance.Warning($"Không đọc được nhãn đã lưu của {storeKey}: {e.Message}");
            }
        }

        private void Persist(string storeKey)
        {
            var bundle = _state.BundleFor(storeKey);
            _dataPool.Compute<string>(PERSIST_PREFIX + storeKey,
                _ => bundle == null ? null : JsonConvert.SerializeObject(bundle));
        }
    }
}
