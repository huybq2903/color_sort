/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-18
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Falcon.Modules.FPrefabPool.Runtime
{
    // Bắt OnDestroy của Root để Dispose registry khi scene unload / app quit.
    internal sealed class PoolRegistryCleanup : MonoBehaviour
    {
        private PoolRegistry _registry;

        internal void Initialize(PoolRegistry registry) => _registry = registry;

        // Root đang trong destroy chain (đây là lý do OnDestroy fire) → Dispose variant SKIP Destroy(Root).
        private void OnDestroy() => _registry?.DisposeFromOnDestroy();
    }

    public sealed class PoolRegistry : IDisposable
    {
        /// <summary>Registry sống suốt vòng đời app (DontDestroyOnLoad). Dùng cho pool xuyên scene.</summary>
        // Main-thread only: ??= không thread-safe formally nhưng ctor gọi Unity API (chỉ chạy main thread).
        public static PoolRegistry Persistent => _persistent ??= new PoolRegistry("[Pools] Persistent", true);
        private static PoolRegistry _persistent;

        /// <summary>
        /// Registry gắn với scene ĐANG ACTIVE tại lần first-access (nơi Root GameObject được tạo); scene đó unload
        /// → Root destroy → Dispose, lần truy cập kế tự recreate (ref cache cũ sẽ stale).
        /// LƯU Ý: nếu first-access xảy ra khi scene thường trú (vd. Boot) đang active, Root sống tới khi scene đó
        /// unload → pooled instance không được reclaim ở boundary gameplay. Với prefab nhẹ (toast/UI) là vô hại;
        /// nhưng KHÔNG nên pool prefab nặng (model 3D, particle, số lượng lớn) qua Temporary nếu Root có thể rơi
        /// vào scene thường trú — cần kiểm soát reclaim thì pool có chủ đích + Dispose tay tại boundary.
        /// Xuyên scene: luôn GetOrCreate tại điểm dùng, đừng cache pool ref.
        /// </summary>
        public static PoolRegistry Temporary => _temporary ??= new PoolRegistry("[Pools] Temporary", false);
        private static PoolRegistry _temporary;

#if UNITY_EDITOR
        // Domain Reload tắt → static field giữ instance cũ (Root đã bị Unity huỷ) qua các Play session.
        // Dispose để dọn pool + đặt _disposed=true cho mọi stale ref fail-safe.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticOnEnterPlay()
        {
            try { _persistent?.Dispose(); }
            catch (Exception ex) { Debug.LogException(ex); }

            try { _temporary?.Dispose(); }
            catch (Exception ex) { Debug.LogException(ex); }

            _persistent = null;
            _temporary = null;
        }
#endif

        private readonly List<IPoolInternal> _pools = new();
        private bool _disposed;
        private bool _pruningPools; // chặn UnregisterPool re-entrant khi PruneStale đang RemoveAt thủ công

        // Caller cache registry ref dùng cái này để biết khi nào ref đã chết (getter ??= tự recreate sau Dispose).
        public bool IsDisposed => _disposed;

        // Holder gốc của mọi pooled instance; chỉ FPrefabPool<T> (cùng asmdef) parent vào đây. internal để
        // bịt đường external code reparent/SetActive(true) làm vỡ invariant "Root inactive ⇒ instance trong pool inactive".
        internal Transform Root { get; }

        private PoolRegistry(string rootName, bool dontDestroyOnLoad)
        {
            var go = new GameObject(rootName);

            // Cleanup gắn cho cả Persistent lẫn Temporary để Dispose chạy đủ kể cả khi Root bị destroy bất ngờ.
            go.AddComponent<PoolRegistryCleanup>().Initialize(this);

            if (dontDestroyOnLoad)
                Object.DontDestroyOnLoad(go);
            // Temporary: cache pool invalid khi scene unload — cross-scene phải dùng Persistent (xem doc property).
            // Không log proactive: misuse đã được fail-loud tại điểm thật (LogDisposedAccess / ThrowIfDisposed).

            // Holder inactive → instance trong pool luôn inactive-in-hierarchy (Awake/OnEnable chờ tới khi Spawn).
            go.SetActive(false);

            Root = go.transform;
        }

        // Resolve ở điểm spawn, lazy-create options DEFAULT. Non-default phải qua Create() (1 luồng duy nhất).
        // PERF: hot spawn loop NÊN cache ref FPrefabPool<T> trả về (trong cùng scene) để bỏ GetInstanceID + dict
        // lookup mỗi spawn; check pool.IsDisposed trước khi dùng. CHỈ cấm cache XUYÊN SCENE (ref sẽ stale).
        /// <remarks>
        /// QUAN TRỌNG: mỗi prefab chỉ được pool dưới DUY NHẤT 1 component type T. _cache static tách riêng theo T,
        /// nên cùng 1 prefab pool dưới 2 type khác nhau sẽ tạo pool TRÙNG (gấp đôi instance/memory) — RegisterPool
        /// log đỏ phát hiện nhưng KHÔNG tự ngăn. Hãy nhất quán 1 type cho mỗi prefab.
        /// </remarks>
        public FPrefabPool<T> GetOrCreate<T>(T prefab) where T : Component
        {
            ThrowIfDisposed();
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            return FPrefabPool<T>.GetOrCreateInternal(this, prefab);
        }

        // Nơi DUY NHẤT cấu hình options. Gọi 1 lần TRƯỚC mọi GetOrCreate/Spawn; pool đã có → options bị bỏ qua + warn.
        /// <remarks>Một prefab chỉ pool dưới DUY NHẤT 1 component type T — xem <see cref="GetOrCreate{T}"/>.</remarks>
        public FPrefabPool<T> Create<T>(T prefab, PoolOptions options) where T : Component
        {
            ThrowIfDisposed();
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            options ??= new PoolOptions();
            return FPrefabPool<T>.CreateInternal(this, prefab, options);
        }

        // Guard cho Create (chỉ gọi khi absent) → tránh warning "đã tồn tại" khi pool sống xuyên scene/lần Play.
        public bool Contains<T>(T prefab) where T : Component
        {
            if (_disposed || prefab == null) return false;
            return FPrefabPool<T>.ContainsInternal(this, prefab);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PoolRegistry),
                    "Registry đã Dispose. Đừng cache ref xuyên scene — lấy lại qua PoolRegistry.Persistent/Temporary.");
        }

        internal void RegisterPool(IPoolInternal pool)
        {
            if (pool == null) return;
            if (_disposed) { pool.Dispose(); return; }

            // Phát hiện cùng 1 prefab bị pool dưới nhiều component type T (mỗi T có _cache static riêng nên
            // không tự thấy nhau) → tạo pool TRÙNG, gấp đôi instance. Scan O(N) ở cold path (chỉ lúc tạo pool);
            // chạy mọi build (gồm release) cho nhất quán fail-loud với PoolMember.OnDestroy.
            int goId = pool.PrefabGameObjectId;
            for (int i = 0; i < _pools.Count; i++)
            {
                if (_pools[i] != null && _pools[i].PrefabGameObjectId == goId)
                {
                    Debug.LogError(
                        $"[PoolRegistry] Prefab (GO instanceID={goId}) đã được pool dưới component type khác — " +
                        "đang tạo pool TRÙNG (gấp đôi instance/memory). Hãy dùng nhất quán 1 component type cho mỗi prefab.");
                    break;
                }
            }

            _pools.Add(pool);
        }

        // Pool tự gọi khi Dispose để rời _pools ngay → không treo ref/holder tới khi PruneStale chạy tay.
        internal void UnregisterPool(IPoolInternal pool)
        {
            // _disposed: CleanupAllPools đang foreach _pools. _pruningPools: PruneStale đang RemoveAt thủ công.
            if (pool == null || _disposed || _pruningPools) return;
            _pools.Remove(pool); // O(N), cold path (chỉ lúc pool Dispose)
        }

        /// <summary>
        /// Dispose + gỡ các pool stale (prefab unload) hoặc đã Dispose. Gọi ở scene boundary hoặc sau khi
        /// Addressable/AssetBundle release prefab pooled. Trả về số pool đã dọn.
        /// </summary>
        public int PruneStale()
        {
            if (_disposed) return 0;

            int pruned = 0;
            // Duyệt ngược để RemoveAt an toàn. _pruningPools chặn pool.Dispose() tự gỡ khỏi _pools (UnregisterPool)
            // giữa vòng → tránh double-remove; ở đây RemoveAt thủ công nắm quyền dọn _pools.
            _pruningPools = true;
            try
            {
                for (int i = _pools.Count - 1; i >= 0; i--)
                {
                    var pool = _pools[i];
                    if (pool == null) { _pools.RemoveAt(i); continue; }
                    if (!pool.IsDisposed && !pool.IsStale) continue;

                    if (!pool.IsDisposed)
                    {
                        try { pool.Dispose(); }
                        catch (Exception ex) { Debug.LogException(ex); }
                    }

                    _pools.RemoveAt(i);
                    pruned++;
                }
            }
            finally { _pruningPools = false; }
            return pruned;
        }

        public void Dispose() => DisposeInternal(false);

        // Path Cleanup.OnDestroy: Root đang bị Unity destroy → skip Object.Destroy(Root) tránh re-destroy.
        internal void DisposeFromOnDestroy() => DisposeInternal(true);

        private void DisposeInternal(bool fromOnDestroy)
        {
            if (_disposed) return;
            _disposed = true;

            CleanupAllPools();

            if (this == _temporary) _temporary = null;
            else if (this == _persistent) _persistent = null;

            if (!fromOnDestroy && Root != null)
                Object.Destroy(Root.gameObject);
        }

        private void CleanupAllPools()
        {
            foreach (var pool in _pools)
            {
                try { pool?.Dispose(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            _pools.Clear();
        }
    }
}
