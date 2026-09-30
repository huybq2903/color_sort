/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-05
 */

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Falcon.Helpers.Addressable
{
    public static partial class AddressableHelper
    {
        #region Core Load/Release

        private static async UniTask<T> LoadCore<T>(
            AddressableAssetKey key, Func<AsyncOperationHandle<T>> loadFunc,
            AssetReference reference = null)
            where T : UnityEngine.Object
        {
            if (!key.IsValid)
            {
                Debug.LogError($"{nameof(AddressableHelper)} > Load được gọi với key không hợp lệ: {key}");
                return null;
            }

            // @by-design: closure chỉ dựng trên cache-miss (đường có I/O lấn át alloc) — fast-path resident ở LoadByKey đã không-closure.
            if (AddressableStateCache.TryAcquireOrStartLoad<T>(key,
                    () => AddressableLoadPipeline.LoadWithRetryAndCache(key, loadFunc, reference),
                    out var cached, out var loadingTask))
                return cached;

            var loadedHandle = await loadingTask;
            if (!loadedHandle.IsValid() || loadedHandle.Status != AsyncOperationStatus.Succeeded)
                return null;

            if (AddressableStateCache.TryAcquireLoaded<T>(key, out var result))
                return result;

            // Load Succeeded nhưng acquire fail. Fail-loud thay vì trả null câm; liệt kê nguyên nhân khả dĩ để khỏi quy chụp khi debug.
            Debug.LogError($"{nameof(AddressableHelper)} > Load [{key}] ({typeof(T).Name}) Succeeded nhưng acquire thất bại. " +
                           "Nguyên nhân khả dĩ: concurrent Load+Release đồng bộ (entry biến mất), type không khớp entry, " +
                           "hoặc asset bị Destroy ngoài luồng (xem log AddressableStateCache phía trên). Trả null.");
            return null;
        }

        // Lõi load theo key đã validate. Fast-path: asset đã resident → +1 refCount đồng bộ,
        // không dựng closure/delegate cho đường nóng này.
        private static UniTask<T> LoadByKey<T>(AddressableAssetKey key) where T : UnityEngine.Object
        {
            if (AddressableStateCache.TryAcquireLoaded<T>(key, out var hit))
                return UniTask.FromResult(hit);
            return LoadCore(key, () => Addressables.LoadAssetAsync<T>(key.Value));
        }

        private static UniTask<T> LoadByKey<T>(AddressableAssetKey key, AssetReference reference)
            where T : UnityEngine.Object
        {
            if (AddressableStateCache.TryAcquireLoaded<T>(key, out var hit))
                return UniTask.FromResult(hit);
            return LoadCore(key, reference.LoadAssetAsync<T>, reference);
        }

        // Bọc một load để hỗ trợ huỷ: nếu ct có thể huỷ và đã huỷ sau khi acquire xong → nhả refCount vừa +1 rồi ném OCE.
        // ct mặc định (CancellationToken.None) KHÔNG huỷ được → trả thẳng load, không dựng thêm async state machine (giữ fast-path).
        private static UniTask<T> AcquireOrRollback<T>(UniTask<T> load, AddressableAssetKey key, CancellationToken ct)
            where T : UnityEngine.Object
            => ct.CanBeCanceled ? AcquireOrRollbackCore(load, key, ct) : load;

        private static async UniTask<T> AcquireOrRollbackCore<T>(
            UniTask<T> load, AddressableAssetKey key, CancellationToken ct) where T : UnityEngine.Object
        {
            var asset = await load;
            if (ct.IsCancellationRequested)
            {
                if ((UnityEngine.Object)asset != null) ReleaseCore<T>(key); // chỉ nhả khi đã acquire (asset != null ⇒ refCount đã +1)
                ct.ThrowIfCancellationRequested();
            }
            return asset;
        }

        // @by-design: preload setup-time (không per-frame) → closure alloc không đáng tối ưu.
        private static void PreloadCore<T>(
            AddressableAssetKey key, Func<AsyncOperationHandle<T>> loadFunc,
            AssetReference reference = null)
            where T : UnityEngine.Object
        {
            if (!key.IsValid) return;
            AddressableStateCache.StartLoadingIfNeeded<T>(key,
                () => AddressableLoadPipeline.LoadWithRetryAndCache(key, loadFunc, reference));
        }

        private static void ReleaseCore<T>(AddressableAssetKey key) where T : UnityEngine.Object
        {
            if (!key.IsValid) return;

            var result = AddressableStateCache.Release(key, typeof(T));

            if (result.instanceToDestroy != null)
                UnityEngine.Object.Destroy(result.instanceToDestroy);

            // reference-load → ReleaseAsset(); else handle hợp lệ → Addressables.Release. Không release được mới rẽ nhánh chẩn đoán.
            if (!AddressableLoadPipeline.ReleaseHandleOrReference(result.handleToRelease, result.reference))
            {
                if (!result.keyExisted)
                    Debug.LogWarning($"{nameof(AddressableHelper)} > Asset không tìm thấy hoặc đã được release: {key}");
                else if (!result.typeFound)
                    Debug.LogError($"{nameof(AddressableHelper)} > Release '{key}' với type {typeof(T).Name} không khớp entry nào → " +
                                   $"hoặc Load bằng generic type khác (asset sẽ LEAK, hãy Release đúng type đã Load), " +
                                   $"hoặc đã Release type này nhiều hơn số lần Load (over-release).");
            }

            if (result.overReleased)
                Debug.LogError($"{nameof(AddressableHelper)} > Release '{key}' khi refCount đã 0 (asset PRELOAD nhưng CHƯA từng Load) → " +
                               "preload ĐÃ được free. Lần sau dùng CancelPreload cho ý định này; muốn dùng asset → Load trước khi Release.");
        }

        // Huỷ một preload chưa Load (đối xứng PreloadCore): free handle im lặng nếu refCount==0.
        // refCount>0 ⇒ asset đang được Load → no-op + cảnh báo (đường đó dùng Release).
        private static void CancelPreloadCore<T>(AddressableAssetKey key) where T : UnityEngine.Object
        {
            if (!key.IsValid) return;
            var (handle, reference, stillInUse) = AddressableStateCache.CancelPreload(key, typeof(T));
            if (stillInUse)
            {
                Debug.LogWarning($"{nameof(AddressableHelper)} > CancelPreload '{key}' bị bỏ qua: asset đang được Load (refCount>0). Dùng Release.");
                return;
            }
            AddressableLoadPipeline.ReleaseHandleOrReference(handle, reference);
        }

        #endregion

        #region Validation Helpers

        private static bool TryValidatePath(string path, out AddressableAssetKey key,
            bool warnOnly = false, [CallerMemberName] string caller = null)
        {
            if (AddressableAssetKey.TryFromPath(path, out key)) return true;
            var msg = $"{nameof(AddressableHelper)} > {caller} được gọi với path null/rỗng/toàn khoảng trắng.";
            if (warnOnly) Debug.LogWarning(msg);
            else Debug.LogError(msg);
            return false;
        }

        private static bool TryValidateReference(AssetReference reference, out AddressableAssetKey key,
            bool warnOnly = false, [CallerMemberName] string caller = null)
        {
            if (AddressableAssetKey.TryFromReference(reference, out key)) return true;
            var msg = $"{nameof(AddressableHelper)} > {caller} được gọi với reference null hoặc không hợp lệ.";
            if (warnOnly) Debug.LogWarning(msg);
            else Debug.LogError(msg);
            return false;
        }

        #endregion

        #region Public API (String Path)

        /// <remarks>ct huỷ giữa chừng: asset vẫn load xong rồi mới release (Addressables không abort in-flight), caller nhận OperationCanceledException.
        /// ct huỷ-sẵn: huỷ ngay, không tốn lượt load.</remarks>
        public static UniTask<T> Load<T>(string path, CancellationToken ct = default) where T : UnityEngine.Object
            => ct.IsCancellationRequested ? UniTask.FromCanceled<T>(ct)
                : TryValidatePath(path, out var key) ? AcquireOrRollback(LoadByKey<T>(key), key, ct) : UniTask.FromResult<T>(null);

        public static UniTask<GameObject> Load(string path, CancellationToken ct = default) => Load<GameObject>(path, ct);

        public static void PreloadPrefab(string path)
        {
            if (TryValidatePath(path, out var key))
                PreloadCore(key, () => Addressables.LoadAssetAsync<GameObject>(key.Value));
        }

        public static void PreloadScriptableObject<T>(string path) where T : ScriptableObject
        {
            if (TryValidatePath(path, out var key))
                PreloadCore<T>(key, () => Addressables.LoadAssetAsync<T>(key.Value));
        }

        public static UniTask<T> LoadScriptableObject<T>(string path, CancellationToken ct = default) where T : ScriptableObject => Load<T>(path, ct);

        /// <summary>Release SO đã Load bằng <see cref="LoadScriptableObject{T}(string,CancellationToken)"/> (asset cache dưới <typeparamref name="T"/>).</summary>
        public static void ReleaseScriptableObject<T>(string path) where T : ScriptableObject => Release<T>(path);

        public static void Release<T>(string path) where T : UnityEngine.Object
        {
            if (TryValidatePath(path, out var key, warnOnly: true)) ReleaseCore<T>(key);
        }

        public static void Release(string path) => Release<GameObject>(path);

        /// <summary>Huỷ preload (chưa Load) đã tạo bằng <see cref="PreloadPrefab"/>/<see cref="PreloadScriptableObject{T}"/> — free asset, không bắn error như Release.</summary>
        public static void CancelPreload<T>(string path) where T : UnityEngine.Object
        {
            if (TryValidatePath(path, out var key, warnOnly: true)) CancelPreloadCore<T>(key);
        }

        public static void CancelPreload(string path) => CancelPreload<GameObject>(path);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Trả về refcount hiện tại của asset trong cache (0 nếu chưa load hoặc không tồn tại key).
        /// Dùng để phát hiện load không có release tương ứng. Chỉ build trong Editor/Development build.
        /// </summary>
        public static int GetRefCount<T>(string path) where T : UnityEngine.Object
        {
            if (!AddressableAssetKey.TryFromPath(path, out var key)) return 0;
            return AddressableStateCache.GetRefCount(key, typeof(T));
        }

        public static int GetRefCount(string path) => GetRefCount<GameObject>(path);

        /// <summary>Như <see cref="GetRefCount{T}(string)"/> nhưng theo <see cref="AssetReference"/> (cache reference-key tách biệt với path-key).</summary>
        public static int GetRefCountByReference<T>(AssetReference reference) where T : UnityEngine.Object
        {
            if (!AddressableAssetKey.TryFromReference(reference, out var key)) return 0;
            return AddressableStateCache.GetRefCount(key, typeof(T));
        }

        public static int GetRefCountByReference(AssetReference reference) => GetRefCountByReference<GameObject>(reference);
#endif

        #endregion

        #region Public API (AssetReference, non-overload names)

        // NAMESPACE (by-design): path-key (address) ≠ reference-key (GUID) → 2 cache TÁCH BIỆT, đừng load 1 asset bằng cả hai (2 bản resident).
        // Không unify được (runtime thiếu canonicalize address↔GUID đồng bộ). Reference-load release đối xứng qua ReleaseAsset() (xem ReleaseCore).

        /// <inheritdoc cref="Load{T}(string,CancellationToken)"/>
        public static UniTask<T> LoadByReference<T>(AssetReference reference, CancellationToken ct = default) where T : UnityEngine.Object
            => ct.IsCancellationRequested ? UniTask.FromCanceled<T>(ct)
                : TryValidateReference(reference, out var key) ? AcquireOrRollback(LoadByKey<T>(key, reference), key, ct) : UniTask.FromResult<T>(null);

        public static UniTask<GameObject> LoadByReference(AssetReference reference, CancellationToken ct = default) =>
            LoadByReference<GameObject>(reference, ct);

        public static void PreloadPrefabByReference(AssetReference reference)
        {
            if (TryValidateReference(reference, out var key))
                PreloadCore(key, reference.LoadAssetAsync<GameObject>, reference);
        }

        public static void PreloadScriptableObjectByReference<T>(AssetReference reference) where T : ScriptableObject
        {
            if (TryValidateReference(reference, out var key))
                PreloadCore(key, reference.LoadAssetAsync<T>, reference);
        }

        public static UniTask<T> LoadScriptableObjectByReference<T>(AssetReference reference, CancellationToken ct = default) where T : ScriptableObject =>
            LoadByReference<T>(reference, ct);

        /// <summary>Release SO đã Load bằng <see cref="LoadScriptableObjectByReference{T}(AssetReference,CancellationToken)"/>.</summary>
        public static void ReleaseScriptableObjectByReference<T>(AssetReference reference) where T : ScriptableObject
            => ReleaseByReference<T>(reference);

        public static void ReleaseByReference<T>(AssetReference reference) where T : UnityEngine.Object
        {
            if (TryValidateReference(reference, out var key, warnOnly: true)) ReleaseCore<T>(key);
        }

        public static void ReleaseByReference(AssetReference reference) => ReleaseByReference<GameObject>(reference);

        /// <summary>Như <see cref="CancelPreload{T}(string)"/> nhưng theo <see cref="AssetReference"/>.</summary>
        public static void CancelPreloadByReference<T>(AssetReference reference) where T : UnityEngine.Object
        {
            if (TryValidateReference(reference, out var key, warnOnly: true)) CancelPreloadCore<T>(key);
        }

        public static void CancelPreloadByReference(AssetReference reference) => CancelPreloadByReference<GameObject>(reference);

        #endregion

        #region Load Prefab (Instantiate)

        /// <summary>
        /// Logic LoadPrefab dùng chung cho path và AssetReference. TryGetCachedInstance/SetCachedInstance tự khoá nội bộ.
        /// </summary>
        private static async UniTask<T> LoadPrefab<T>(
            AddressableAssetKey key, AssetReference reference, Transform parent,
            bool reuseCachedInstance, CancellationToken ct) where T : Component
        {
            ct.ThrowIfCancellationRequested(); // token đã-cancel: bỏ ngay, không tốn lượt load (đối xứng Load<T>)

            var prefab = reference != null
                ? await LoadByKey<GameObject>(key, reference)
                : await LoadByKey<GameObject>(key);
            if (prefab == null) return null;
            AddressableStateCache.AssertPrefabMode(key, reuseCachedInstance);

            // Huỷ trong lúc load → nhả refCount (+1 của LoadByKey) TRƯỚC khi Instantiate, tránh instance mồ côi.
            if (ct.IsCancellationRequested)
            {
                ReleaseCore<GameObject>(key);
                ct.ThrowIfCancellationRequested();
            }

            if (reuseCachedInstance && AddressableStateCache.TryGetCachedInstance(key, out var cached))
            {
                // @by-design: GetComponent mỗi cache-hit OK — chỉ ở shared path, không per-frame; memoize không bõ.
                var existing = cached.GetComponent<T>();
                if ((UnityEngine.Object)existing == null)
                {
                    Debug.LogError(
                        $"{nameof(AddressableHelper)} > Instance đã cache [{key.Value}] thiếu component {typeof(T).Name}.");
                    ReleaseCore<GameObject>(key); // rollback +1 refCount của LoadByKey (instance cache dùng chung → để ReleaseCore quyết định Destroy)
                    return null;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // @by-design: defensive future shared-có-parent (nay parent=null)
                if (parent != null && cached.transform.parent != parent)
                    Debug.LogWarning($"{nameof(AddressableHelper)} > LoadPrefab [{key.Value}] cache-hit: tham số 'parent' bị bỏ qua " +
                                     "(instance dùng chung giữ parent gốc). Re-parent thủ công nếu cần.");
#endif
                return existing;
            }

            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            if (!instance.TryGetComponent<T>(out var component))
            {
                Debug.LogError(
                    $"{nameof(AddressableHelper)} > Prefab đã instantiate [{key.Value}] thiếu component {typeof(T).Name}.");
                UnityEngine.Object.Destroy(instance); // chưa cache → tự Destroy, tránh để lại instance mồ côi
                ReleaseCore<GameObject>(key);          // rollback +1 refCount của LoadByKey
                return null;
            }

            if (reuseCachedInstance)
                AddressableStateCache.SetCachedInstance(key, instance);
            return component;
        }

        /// <summary>
        /// Load + Instantiate prefab DÙNG-CHUNG tại <paramref name="path"/>: CACHE &amp; ref-count theo key — mọi lần gọi
        /// share CÙNG 1 instance, tự Destroy khi <see cref="Release(string)"/> cuối đưa refCount về 0. Caller CHỈ Release, KHÔNG tự Destroy.
        /// KHÔNG nhận parent: instance dùng chung chỉ có 1 chỗ đặt → placement do hệ thống quyết, không phải từng caller.
        /// Cần kiểm soát parent ⇒ bạn muốn instance RIÊNG ⇒ dùng <see cref="LoadPrefabInstance{T}(string,Transform,CancellationToken)"/>.
        /// </summary>
        public static UniTask<T> LoadSharedPrefab<T>(string path, CancellationToken ct = default) where T : Component
            => TryValidatePath(path, out var key)
                ? LoadPrefab<T>(key, null, null, reuseCachedInstance: true, ct)
                : UniTask.FromResult<T>(null);

        /// <summary>Release shared prefab đã Load bằng <see cref="LoadSharedPrefab{T}(string,CancellationToken)"/> (asset keyed dưới GameObject).</summary>
        public static void ReleaseSharedPrefab(string path) => Release<GameObject>(path);

        /// <summary>
        /// Load asset rồi Instantiate instance MỚI dưới <paramref name="parent"/> mỗi lần gọi, KHÔNG cache.
        /// Caller SỞ HỮU instance → phải tự <c>Object.Destroy(instance)</c> VÀ gọi <see cref="Release(string)"/> để nhả asset handle
        /// (mode này KHÔNG có leak-detection; vd <c>UIPopupLoader</c> tự quản instance theo LifeTimePolicy).
        /// </summary>
        public static UniTask<T> LoadPrefabInstance<T>(string path, Transform parent = null, CancellationToken ct = default) where T : Component
            => TryValidatePath(path, out var key)
                ? LoadPrefab<T>(key, null, parent, reuseCachedInstance: false, ct)
                : UniTask.FromResult<T>(null);

        /// <summary>Cleanup cho <see cref="LoadPrefabInstance{T}(string,Transform,CancellationToken)"/>: Destroy instance + nhả asset handle trong 1 call.</summary>
        public static void ReleasePrefabInstance<T>(string path, T instance) where T : Component
        {
            if (instance) UnityEngine.Object.Destroy(instance.gameObject); // implicit-bool: lọc Unity fake-null
            Release<GameObject>(path);
        }

        /// <summary>Như <see cref="LoadSharedPrefab{T}(string,CancellationToken)"/> nhưng load theo <see cref="AssetReference"/>.</summary>
        public static UniTask<T> LoadSharedPrefabByReference<T>(AssetReference reference, CancellationToken ct = default) where T : Component
            => TryValidateReference(reference, out var key)
                ? LoadPrefab<T>(key, reference, null, reuseCachedInstance: true, ct)
                : UniTask.FromResult<T>(null);

        /// <summary>Release shared prefab đã Load bằng <see cref="LoadSharedPrefabByReference{T}(AssetReference,CancellationToken)"/> (asset keyed dưới GameObject).</summary>
        public static void ReleaseSharedPrefabByReference(AssetReference reference)
            => ReleaseByReference<GameObject>(reference);

        /// <summary>Như <see cref="LoadPrefabInstance{T}(string,Transform,CancellationToken)"/> nhưng load theo <see cref="AssetReference"/>.</summary>
        public static UniTask<T> LoadPrefabInstanceByReference<T>(
            AssetReference reference, Transform parent = null, CancellationToken ct = default) where T : Component
            => TryValidateReference(reference, out var key)
                ? LoadPrefab<T>(key, reference, parent, reuseCachedInstance: false, ct)
                : UniTask.FromResult<T>(null);

        /// <summary>Như <see cref="ReleasePrefabInstance{T}"/> nhưng theo <see cref="AssetReference"/>.</summary>
        public static void ReleasePrefabInstanceByReference<T>(AssetReference reference, T instance) where T : Component
        {
            if (instance) UnityEngine.Object.Destroy(instance.gameObject);
            ReleaseByReference<GameObject>(reference);
        }

        [Obsolete("Tách rõ intent: dùng LoadSharedPrefab (shared, cached, không parent) hoặc LoadPrefabInstance (instance mới, có parent). " +
                  "Overload này bỏ qua âm thầm 'parent' khi cache-hit với reuseCachedInstance:true.", error: true)]
        public static UniTask<T> LoadPrefab<T>(string path, Transform parent = null, bool reuseCachedInstance = true,
            CancellationToken ct = default)
            where T : Component
            => TryValidatePath(path, out var key)
                ? LoadPrefab<T>(key, null, parent, reuseCachedInstance, ct)
                : UniTask.FromResult<T>(null);

        [Obsolete("Tách rõ intent: dùng LoadSharedPrefabByReference (shared, cached, không parent) hoặc LoadPrefabInstanceByReference (instance mới, có parent). " +
                  "Overload này bỏ qua âm thầm 'parent' khi cache-hit với reuseCachedInstance:true.", error: true)]
        public static UniTask<T> LoadPrefabByReference<T>(
            AssetReference reference, Transform parent = null, bool reuseCachedInstance = true,
            CancellationToken ct = default) where T : Component
            => TryValidateReference(reference, out var key)
                ? LoadPrefab<T>(key, reference, parent, reuseCachedInstance, ct)
                : UniTask.FromResult<T>(null);

        #endregion
    }
}
