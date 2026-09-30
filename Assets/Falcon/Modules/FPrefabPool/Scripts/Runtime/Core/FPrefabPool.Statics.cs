/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-18
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.Modules.FPrefabPool.Runtime
{
#if UNITY_EDITOR
    // Domain Reload tắt → static cache của mỗi FPrefabPool<T> giữ entries cũ qua các Play session.
    // Mỗi generic instantiation lazy-register 1 resetter để dọn cache khi vào Play.
    internal static class FPrefabPoolStaticReset
    {
        private static readonly List<Action> _resetters = new();

        internal static void Register(Action resetter)
        {
            if (resetter != null) _resetters.Add(resetter);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            for (int i = 0; i < _resetters.Count; i++) _resetters[i]?.Invoke();
            _resetters.Clear();
        }
    }
#endif

    // Static factory + cache theo (registry, prefabId). Tách khỏi instance pool logic ở FPrefabPool.cs.
    public sealed partial class FPrefabPool<T> where T : Component
    {
        private static Dictionary<(PoolRegistry registry, int prefabId), FPrefabPool<T>> _cache;

        private static Dictionary<(PoolRegistry registry, int prefabId), FPrefabPool<T>> Cache
        {
            get
            {
                if (_cache != null) return _cache;
                _cache = new Dictionary<(PoolRegistry, int), FPrefabPool<T>>();
#if UNITY_EDITOR
                FPrefabPoolStaticReset.Register(static () => _cache = null);
#endif
                return _cache;
            }
        }

        // Hot path: trả pool đang sống cho (registry, prefab), hoặc null. KHÔNG prune để giữ O(1).
        // ReferenceEquals chống Unity tái cấp instanceID sau unload (entry stale trùng ID với prefab mới).
        private static FPrefabPool<T> TryGetCached(PoolRegistry registry, T prefab, int prefabId)
        {
            if (Cache.TryGetValue((registry, prefabId), out var exist)
                && exist != null && ReferenceEquals(exist._prefab, prefab))
                return exist;
            return null;
        }

        internal static bool ContainsInternal(PoolRegistry registry, T prefab) => TryGetCached(registry, prefab, prefab.GetInstanceID()) != null;

        // Resolve ở điểm spawn, lazy-create bằng options DEFAULT. KHÔNG nhận options để tránh 2 call-site
        // cấu hình lệch nhau rồi bị drop âm thầm — options non-default phải qua CreateInternal.
        internal static FPrefabPool<T> GetOrCreateInternal(PoolRegistry registry, T prefab)
        {
            int prefabId = prefab.GetInstanceID();
            return TryGetCached(registry, prefab, prefabId) ?? CreateNew(registry, prefab, prefabId, new PoolOptions());
        }

        // Nơi DUY NHẤT options đi vào pool (gọi 1 lần lúc init). Pool đã tồn tại → vi phạm contract.
        internal static FPrefabPool<T> CreateInternal(PoolRegistry registry, T prefab, PoolOptions options)
        {
            int prefabId = prefab.GetInstanceID();
            var existing = TryGetCached(registry, prefab, prefabId);
            if (existing != null)
            {
                // Vi phạm contract: pool đã tồn tại → options bị drop. Fail-loud (log đỏ) thay vì warn âm thầm.
                Debug.LogError(
                    $"{_tag} Create('{prefab.name}') bị bỏ qua: pool đã tồn tại (options chốt tại lần tạo đầu). " +
                    "Hãy gọi Create TRƯỚC mọi GetOrCreate/Spawn cho prefab này.");
                return existing;
            }
            return CreateNew(registry, prefab, prefabId, options);
        }

        // Cold path: tạo pool mới, tranh thủ scan dọn entry stale khác (prefab đã unload). O(N) scan
        // amortize tốt vì creation đã tốn Instantiate + GameObject.
        private static FPrefabPool<T> CreateNew(PoolRegistry registry, T prefab, int prefabId, PoolOptions options)
        {
            var cache = Cache;
            PruneStaleEntries(cache);
            var created = new FPrefabPool<T>(registry, prefab, prefabId, options);
            cache[(registry, prefabId)] = created;
            return created;
        }

        private static void PruneStaleEntries(Dictionary<(PoolRegistry registry, int prefabId), FPrefabPool<T>> cache)
        {
            List<(PoolRegistry, int)> stale = null;
            foreach (var kvp in cache)
            {
                var pool = kvp.Value;
                if (pool == null || !pool._prefab)
                    (stale ??= new List<(PoolRegistry, int)>()).Add(kvp.Key);
            }

            if (stale == null) return;

            foreach (var key in stale)
            {
                // Dispose tự Remove khỏi cache cho chính nó.
                if (cache.TryGetValue(key, out var pool) && pool != null) pool.Dispose();
                else cache.Remove(key);
            }
        }
    }
}
