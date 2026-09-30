/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-05
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Falcon.Helpers.Addressable
{
    /// <summary>
    /// State nội bộ (loading tasks + cached instance), tự khoá ⇒ atomic, an toàn reentrant continuation.
    /// Vẫn phải gọi từ MAIN THREAD (Unity release/destroy ngoài lock). Caller không cần/không thể tự lock.
    /// </summary>
    internal static class AddressableStateCache
    {
        private sealed class CachedHandleEntry
        {
            public AsyncOperationHandle handle;
            public int refCount;

            /// <summary>!= null ⇒ asset load qua AssetReference → release đối xứng bằng ReleaseAsset().</summary>
            public AssetReference reference;
        }

        /// <summary>Kết quả của <see cref="Release"/>: handle/reference/instance cần dọn NGOÀI lock + cờ chẩn đoán.</summary>
        internal readonly struct ReleaseOutcome
        {
            public readonly AsyncOperationHandle? handleToRelease;
            public readonly AssetReference reference;
            public readonly bool keyExisted;
            public readonly bool typeFound;

            /// <summary>true ⇒ Release gọi khi refCount đã 0 (preload chưa Load, hoặc over-release) → caller fail-loud.</summary>
            public readonly bool overReleased;
            public readonly GameObject instanceToDestroy;

            public ReleaseOutcome(AsyncOperationHandle? handleToRelease, AssetReference reference,
                bool keyExisted, bool typeFound, bool overReleased, GameObject instanceToDestroy)
            {
                this.handleToRelease = handleToRelease;
                this.reference = reference;
                this.keyExisted = keyExisted;
                this.typeFound = typeFound;
                this.overReleased = overReleased;
                this.instanceToDestroy = instanceToDestroy;
            }
        }

        // @by-design: _lock BẮT BUỘC reentrant (Monitor) — factory() re-enter ở sync-completion; đừng đổi sang SemaphoreSlim.
        private static readonly object _lock = new();
        private static readonly Dictionary<AddressableAssetKey, Dictionary<Type, CachedHandleEntry>> _operations = new();
        private static readonly Dictionary<AddressableAssetKey, Dictionary<Type, Task<AsyncOperationHandle>>> _loadingTasks = new();
        private static readonly Dictionary<AddressableAssetKey, GameObject> _cachedInstances = new();

        // CancelPreload gọi khi preload còn in-flight (chưa có entry) → ghi nhận ở đây; StoreLoadedHandle free ngay khi load xong (chống leak câm). Demand mới (Load/Preload) xoá ý định này.
        private static readonly HashSet<(AddressableAssetKey key, Type type)> _cancelPendingPreloads = new();

        // Load demand đang in-flight (chưa có entry để đọc refCount) → CancelPreload phải coi như stillInUse, đừng huỷ ngang (đối xứng nhánh refCount>0).
        private static readonly HashSet<(AddressableAssetKey key, Type type)> _loadDemanded = new();

        // Mode prefab đã dùng cho mỗi key (true=shared/cached, false=instance/riêng) → phát hiện trộn 2 mode trên 1 key (refCount commingle). Runtime: fail-loud cả ở release.
        private static readonly Dictionary<AddressableAssetKey, bool> _prefabModes = new();

        // Domain Reload tắt (CLAUDE.md) ⇒ static state sống xuyên phiên Play → reset mỗi lần vào Play (giống DIGlobal).
        // Release handle còn valid TRƯỚC khi clear (phòng ResourceManager giữ asset xuyên phiên → orphan); IsValid()-guard ⇒ no-op nếu đã invalid.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            lock (_lock)
            {
                foreach (var byType in _operations.Values)
                foreach (var entry in byType.Values)
                    if (entry.handle.IsValid())
                        AddressableLoadPipeline.ReleaseHandleOrReference(entry.handle, entry.reference);

                _operations.Clear();
                _loadingTasks.Clear();
                _cachedInstances.Clear();   // instance là scene-object, đã chết khi scene unload → chỉ cần quên
                _cancelPendingPreloads.Clear();
                _loadDemanded.Clear();
                _prefabModes.Clear();
            }
        }

        #region Coarse operations (self-locking)

        /// <summary>
        /// Atomic: nếu đã có asset hợp lệ trong cache → tăng refCount và trả về qua <paramref name="cached"/> (return true).
        /// Ngược lại lấy (hoặc tạo bằng <paramref name="factory"/>) loading task để caller await (return false).
        /// Gộp cache-hit + get-or-create-load trong 1 lock nên không còn khe hở giữa hai bước.
        /// </summary>
        internal static bool TryAcquireOrStartLoad<T>(
            AddressableAssetKey key, Func<Task<AsyncOperationHandle>> factory,
            out T cached, out Task<AsyncOperationHandle> loadingTask) where T : UnityEngine.Object
        {
            cached = null;
            loadingTask = null;
            lock (_lock)
            {
                if (TryAcquireExistingNoLock(key, out cached))
                    return true;
                _cancelPendingPreloads.Remove((key, typeof(T))); // Load demand → huỷ ý định CancelPreload đang chờ (nếu có)
                _loadDemanded.Add((key, typeof(T)));             // Load demand in-flight → CancelPreload không phá ngang task này
                if (!TryGetLoadingTask(key, typeof(T), out loadingTask))
                {
                    // @by-design: factory() trong lock để dedup atomic — ra ngoài lock ⇒ 2 thread miss tạo task trùng.
                    loadingTask = factory();
                    StoreLoadingTaskIfPending(key, typeof(T), loadingTask);
                }
                return false;
            }
        }

        /// <summary>Atomic: sau khi load xong, re-check entry rồi tăng refCount (return false nếu entry không còn hợp lệ).</summary>
        internal static bool TryAcquireLoaded<T>(AddressableAssetKey key, out T result) where T : UnityEngine.Object
        {
            lock (_lock)
                return TryAcquireExistingNoLock(key, out result);
        }

        /// <summary>Atomic: chỉ khởi động load nếu chưa có asset hợp lệ và chưa có loading task (dùng cho Preload, không đụng refCount).</summary>
        internal static void StartLoadingIfNeeded<T>(AddressableAssetKey key, Func<Task<AsyncOperationHandle>> factory)
            where T : UnityEngine.Object
        {
            lock (_lock)
            {
                _cancelPendingPreloads.Remove((key, typeof(T))); // preload demand mới → huỷ ý định CancelPreload đang chờ (nếu có)
                if (TryGetEntry(key, typeof(T), out var entry) && entry.handle.IsValid() &&
                    entry.handle.Status == AsyncOperationStatus.Succeeded)
                    return;
                if (TryGetLoadingTask(key, typeof(T), out _))
                    return;
                StoreLoadingTaskIfPending(key, typeof(T), factory());
            }
        }

        /// <summary>
        /// Atomic: gán handle vừa load vào entry. Nếu entry đang giữ một handle hợp lệ khác → trả handle cũ
        /// kèm reference cũ để caller release đối xứng NGOÀI lock.
        /// </summary>
        internal static (AsyncOperationHandle? handle, AssetReference reference) StoreLoadedHandle<T>(
            AddressableAssetKey key, AsyncOperationHandle handle, AssetReference reference)
            where T : UnityEngine.Object
        {
            lock (_lock)
            {
                // preload bị CancelPreload khi đang load & không có demand chen vào → free ngay, đừng để entry resident refCount=0.
                // invariant: pending ⇒ chưa từng có entry hợp lệ cho key/type (in-flight ⇒ StartLoadingIfNeeded không thấy resident) ⇒ không bỏ sót stale.
                if (_cancelPendingPreloads.Remove((key, typeof(T))))
                    return (handle, reference); // pipeline release đối xứng; không caller nào await task này (Load demand đã xoá pending)

                var entry = GetOrCreateEntry(key, typeof(T));
                bool hasStale = entry.handle.IsValid() && !entry.handle.Equals(handle);
                AsyncOperationHandle? staleHandle = hasStale ? entry.handle : null;
                AssetReference staleRef = hasStale ? entry.reference : null;
                entry.handle = handle;
                entry.reference = reference;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                WarnIfResidentUnderOtherKey(key, handle);
#endif
                return (staleHandle, staleRef);
            }
        }

        internal static void RemoveLoadingTask(AddressableAssetKey key, Type type)
        {
            lock (_lock)
            {
                _loadDemanded.Remove((key, type));          // task kết thúc → hết cửa sổ in-flight của Load demand
                _cancelPendingPreloads.Remove((key, type)); // load FAIL không vào StoreLoadedHandle → dọn pending tại đây (success đã dọn trước)
                if (!_loadingTasks.TryGetValue(key, out var byType))
                    return;

                byType.Remove(type);

                if (byType.Count == 0)
                    _loadingTasks.Remove(key);
            }
        }

        /// <summary>Atomic: trả về cached instance còn sống; tự dọn key nếu instance đã bị fake-null.</summary>
        internal static bool TryGetCachedInstance(AddressableAssetKey key, out GameObject instance)
        {
            lock (_lock)
            {
                instance = null;

                if (!_cachedInstances.TryGetValue(key, out var go))
                    return false;

                if (go == null)
                {
                    _cachedInstances.Remove(key);
                    Debug.LogError($"{nameof(AddressableStateCache)} > cached instance [{key}] bị huỷ ngoài luồng Release " +
                                   "→ asset refCount bị orphan (leak). Hãy Release thay vì Destroy trực tiếp.");
                    return false;
                }

                instance = go;
                return true;
            }
        }

        internal static void SetCachedInstance(AddressableAssetKey key, GameObject instance)
        {
            lock (_lock)
                _cachedInstances[key] = instance;
        }

        /// <summary>
        /// Trừ refcount cho type. Khi refcount về 0 trả về handle cần release VÀ <c>reference</c> (nếu là reference-load
        /// → caller dùng ReleaseAsset() cho đối xứng). Kèm: key có tồn tại, type có khớp (typeFound = false ⇒ release sai
        /// generic type → caller fail-loud), và cached instance cần Destroy (nếu bị evict). Release &amp; Destroy NGOÀI lock.
        /// </summary>
        internal static ReleaseOutcome Release(AddressableAssetKey key, Type targetType)
        {
            lock (_lock)
            {
                AsyncOperationHandle? handleToRelease = null;
                AssetReference reference = null;
                bool removedTargetType = false;
                bool keyExisted = _operations.TryGetValue(key, out var byType);

                if (!keyExisted)
                    return default; // keyExisted=false, typeFound=false → "not found"

                bool typeFound = byType.TryGetValue(targetType, out var entry);
                bool overReleased = false;
                if (typeFound)
                {
                    if (entry.refCount > 0)
                        entry.refCount--;
                    else
                        overReleased = true; // refCount đã 0 ⇒ Release không cặp Load (preload chưa Load, hoặc over-release)
                    if (entry.refCount == 0)
                    {
                        if (entry.handle.IsValid())
                        {
                            handleToRelease = entry.handle;
                            reference = entry.reference;
                        }
                        byType.Remove(targetType);
                        removedTargetType = true;
                    }
                }

                bool keyEmptied = byType.Count == 0;
                if (keyEmptied)
                    _operations.Remove(key);

                GameObject instanceToDestroy = null;
                if (keyEmptied || (removedTargetType && targetType == typeof(GameObject)))
                {
                    _cachedInstances.TryGetValue(key, out instanceToDestroy);
                    _cachedInstances.Remove(key);
                    _prefabModes.Remove(key); // key giải phóng hết → quên mode để lần tái dùng (khác mode) sau không báo nhầm
                }

                return new ReleaseOutcome(handleToRelease, reference,
                    keyExisted: true, typeFound: typeFound, overReleased: overReleased,
                    instanceToDestroy: instanceToDestroy);
            }
        }

        /// <summary>
        /// Huỷ preload chưa Load: chỉ free khi entry còn sống &amp; refCount==0 (đối xứng <see cref="StartLoadingIfNeeded{T}"/>).
        /// refCount>0 ⇒ asset đang được Load → no-op, trả stillInUse=true để caller cảnh báo. Release NGOÀI lock.
        /// Preload đang in-flight (chưa có entry) → ghi nhận cancel-pending; <see cref="StoreLoadedHandle{T}"/> free ngay khi load xong.
        /// </summary>
        internal static (AsyncOperationHandle? handle, AssetReference reference, bool stillInUse) CancelPreload(
            AddressableAssetKey key, Type targetType)
        {
            lock (_lock)
            {
                if (!_operations.TryGetValue(key, out var byType) || !byType.TryGetValue(targetType, out var entry))
                {
                    // entry chưa có: task còn in-flight.
                    if (TryGetLoadingTask(key, targetType, out _))
                    {
                        if (_loadDemanded.Contains((key, targetType)))
                            return (null, null, true); // có Load đang chờ task này → stillInUse, đừng huỷ ngang
                        _cancelPendingPreloads.Add((key, targetType)); // preload thuần → StoreLoadedHandle free khi load xong (chống leak câm)
                    }
                    return (null, null, false);
                }
                if (entry.refCount > 0)
                    return (null, null, true);

                AsyncOperationHandle? handle = entry.handle.IsValid() ? entry.handle : null;
                var reference = entry.reference;
                byType.Remove(targetType);
                if (byType.Count == 0)
                    _operations.Remove(key);
                return (handle, reference, false);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal static int GetRefCount(AddressableAssetKey key, Type type)
        {
            lock (_lock)
                return TryGetEntry(key, type, out var entry) ? entry.refCount : 0;
        }
#endif

        // Ghi mode prefab lần đầu cho key; fail-loud nếu lần sau dùng mode khác (shared↔instance) trên cùng key → refCount commingle. Runtime: bắt cả ở release.
        internal static void AssertPrefabMode(AddressableAssetKey key, bool shared)
        {
            lock (_lock)
            {
                if (!_prefabModes.TryGetValue(key, out var prev)) _prefabModes[key] = shared;
                else if (prev != shared)
                    Debug.LogError($"{nameof(AddressableStateCache)} > Key [{key}] bị load bằng CẢ LoadSharedPrefab (shared) lẫn " +
                                   "LoadPrefabInstance (instance) → refCount GameObject asset commingle, vòng đời shared instance bị ghim. Mỗi key chỉ nên dùng 1 mode.");
            }
        }

        #endregion

        #region Granular helpers (private — luôn chạy dưới _lock)

        /// <summary>Phải gọi trong lock(_lock). Tăng refCount nếu entry hợp lệ (đã lọc Unity fake-null).</summary>
        private static bool TryAcquireExistingNoLock<T>(AddressableAssetKey key, out T result) where T : UnityEngine.Object
        {
            result = null;
            if (!TryGetEntry(key, typeof(T), out var entry) || !entry.handle.IsValid() ||
                entry.handle.Status != AsyncOperationStatus.Succeeded)
                return false;
            if (entry.handle.Result is not T typed)
                return false; // type-mismatch: defensive, giữ nguyên hành vi cũ
            if ((UnityEngine.Object)typed == null)
            {
                // asset bị Destroy ngoài luồng Release → fail-loud + dọn entry sạch (reload sau refCount=0), đối xứng cached-instance.
                Debug.LogError($"{nameof(AddressableStateCache)} > asset [{key}] ({typeof(T).Name}) bị Destroy ngoài luồng Release → refCount orphan. Hãy Release thay vì Destroy asset trực tiếp.");
                if (entry.handle.IsValid())
                    AddressableLoadPipeline.ReleaseHandleOrReference(entry.handle, entry.reference);
                if (_operations.TryGetValue(key, out var byType) && byType.Remove(typeof(T)) && byType.Count == 0)
                    _operations.Remove(key);
                return false;
            }
            entry.refCount++;
            result = typed;
            return true;
        }

        private static bool TryGetEntry(AddressableAssetKey key, Type type, out CachedHandleEntry entry)
        {
            entry = null;
            return _operations.TryGetValue(key, out var byType) && byType.TryGetValue(type, out entry);
        }

        // @by-design: re-alloc inner dict mỗi load-sau-release OK — không per-frame, lọt thỏm trong I/O load.
        private static TInner GetOrAdd<TInner>(Dictionary<AddressableAssetKey, TInner> dict, AddressableAssetKey key)
            where TInner : new()
        {
            if (!dict.TryGetValue(key, out var inner)) { inner = new TInner(); dict[key] = inner; }
            return inner;
        }

        private static CachedHandleEntry GetOrCreateEntry(AddressableAssetKey key, Type type)
        {
            var byType = GetOrAdd(_operations, key);
            if (byType.TryGetValue(type, out var entry)) return entry;

            entry = new CachedHandleEntry();
            byType[type] = entry;
            return entry;
        }

        private static bool TryGetLoadingTask(AddressableAssetKey key, Type type, out Task<AsyncOperationHandle> task)
        {
            task = null;
            return _loadingTasks.TryGetValue(key, out var byType) && byType.TryGetValue(type, out task);
        }

        // Chỉ store khi task CHƯA completed: nếu factory() chạy xong sync, finally→RemoveLoadingTask đã chạy trước
        // ⇒ store task đã-completed sẽ kẹt _loadingTasks[key] vĩnh viễn (key stuck null). Dùng chung 2 call-site.
        private static void StoreLoadingTaskIfPending(AddressableAssetKey key, Type type, Task<AsyncOperationHandle> task)
        {
            if (!task.IsCompleted)
                GetOrAdd(_loadingTasks, key)[type] = task;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Dev-only: 1 asset resident dưới 2 key khác nhau (path-address vs reference-GUID) → refCount xé lẻ 2 cache, dễ under-release khiến asset kẹt loaded.
        // @by-design: scan O(N) chỉ chạy dev-build, N nhỏ → chi phí không đáng kể.
        private static void WarnIfResidentUnderOtherKey(AddressableAssetKey key, AsyncOperationHandle handle)
        {
            if (!handle.IsValid() || handle.Result is not UnityEngine.Object asset || asset == null) return;
            foreach (var keyGroup in _operations)
            {
                if (keyGroup.Key.Equals(key)) continue;
                foreach (var entry in keyGroup.Value.Values)
                    if (entry.handle.IsValid() && ReferenceEquals(entry.handle.Result, asset))
                    {
                        Debug.LogError($"{nameof(AddressableStateCache)} > Asset '{asset.name}' resident dưới CẢ [{key}] và [{keyGroup.Key}] " +
                                       "(load bằng cả path lẫn AssetReference) → refCount xé lẻ 2 cache, dễ under-release khiến asset kẹt loaded. Chỉ dùng 1 đường load.");
                        return;
                    }
            }
        }
#endif

        #endregion
    }
}
