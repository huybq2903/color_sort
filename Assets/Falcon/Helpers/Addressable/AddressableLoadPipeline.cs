/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-05
 */

using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Falcon.Helpers.Addressable
{
    /// <summary>
    /// Hỗ trợ load theo path hoặc AssetReference qua delegate.
    /// </summary>
    internal static class AddressableLoadPipeline
    {
        // @by-design: convention của module (author minhddv) — private const dùng _ prefix giống private field, không phải lỗi naming
        private const int _MAX_RETRY_COUNT = 3;
        private const int _RETRY_BASE_DELAY_MS = 100; // backoff tuyến tính: delay = base * attempt

        // Map AsyncOperationStatus → tên cho log lỗi/retry: dùng nameof nên 0 box/reflection/alloc.
        private static string StatusName(AsyncOperationHandle handle)
            => !handle.IsValid() ? "InvalidHandle"
                : handle.Status switch
                {
                    AsyncOperationStatus.Succeeded => nameof(AsyncOperationStatus.Succeeded),
                    AsyncOperationStatus.Failed    => nameof(AsyncOperationStatus.Failed),
                    AsyncOperationStatus.None      => nameof(AsyncOperationStatus.None),
                    _                              => "Unknown",
                };

        // Lỗi vĩnh viễn (key sai / lỗi lập trình / ép kiểu) → retry vô nghĩa, fail-fast. Lỗi I/O còn lại coi là transient (vẫn retry).
        // root: exception đã unwrap để log đúng root cause (không phải AggregateException wrapper).
        private static bool IsNonTransientFailure(Exception ex, out Exception root)
        {
            while (ex is AggregateException agg && agg.InnerException != null)
                ex = agg.InnerException;
            root = ex;
            return ex is InvalidKeyException or ArgumentException or NullReferenceException or InvalidCastException;
        }

        // Release đối xứng theo nguồn load: reference-load → ReleaseAsset() (dọn luôn m_Operation); ngược lại Addressables.Release.
        // Trả true nếu đã release; nhận handle? để pipeline + AddressableHelper (ReleaseCore/CancelPreloadCore) dùng chung 1 nguồn.
        internal static bool ReleaseHandleOrReference(AsyncOperationHandle? handle, AssetReference reference)
        {
            if (reference != null) { reference.ReleaseAsset(); return true; }
            if (handle.HasValue && handle.Value.IsValid()) { Addressables.Release(handle.Value); return true; }
            return false;
        }

        // @by-design: trả System.Threading.Tasks.Task (KHÔNG UniTask) vì loading task được cache trong _loadingTasks
        // và await bởi NHIỀU caller cùng lúc (dedup). UniTask là struct single-await → await lần 2 ném; Task multi-await mới đúng.
        internal static async Task<AsyncOperationHandle> LoadWithRetryAndCache<T>(
            AddressableAssetKey key,
            Func<AsyncOperationHandle<T>> createLoad,
            AssetReference reference = null) where T : UnityEngine.Object
        {
            try
            {
                for (int attempt = 1; attempt <= _MAX_RETRY_COUNT; attempt++)
                {
                    AsyncOperationHandle<T> handle = default;
                    Exception caughtEx = null;

                    try
                    {
                        handle = createLoad(); // throw đồng bộ vẫn được catch → fail-fast/retry. Có thể hoàn tất đồng bộ (sync-success/throw) ⇒ StoreLoadingTaskIfPending phải guard !IsCompleted.
                        // @by-design: giữ .Task thay vì await handle để khỏi thêm dependency UniTask.Addressables vào asmdef dùng-chung; alloc đã dedup + trên I/O path nên không đáng đổi.
                        await handle.Task;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        caughtEx = ex; // chỉ bắt; log chi tiết để sau cổng non-transient (tránh cảnh báo "lần thử x/y" cho lỗi fail-fast)
                    }

                    if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        if ((UnityEngine.Object)handle.Result != null)
                        {
                            var stale = AddressableStateCache.StoreLoadedHandle<T>(key, handle, reference);
                            if (stale.handle.HasValue && stale.handle.Value.IsValid())
                            {
                                bool releasedSelf = stale.handle.Value.Equals(handle); // cancel-pending: StoreLoadedHandle trả lại chính handle vừa load
                                ReleaseHandleOrReference(stale.handle.Value, stale.reference); // đối xứng: reference-load → ReleaseAsset()
                                if (releasedSelf) return default; // asset đã bị free → không còn gì để trả
                            }

                            return handle;
                        }

                        Debug.LogError(
                            $"{nameof(AddressableLoadPipeline)} > Load [{key.Value}] Succeeded nhưng Result null (asset thiếu/đã huỷ hoặc không phải {typeof(T).Name}).");
                        ReleaseHandleOrReference(handle, reference);
                        return default;
                    }

                    // Lỗi không-transient (key/asset không có trong catalog, hoặc lỗi lập trình/cast) → fail-fast, bỏ các lần retry còn lại.
                    var failure = caughtEx ?? (handle.IsValid() ? handle.OperationException : null);
                    if (IsNonTransientFailure(failure, out var rootFailure))
                    {
                        // Tách nhãn key-error (address sai) vs lỗi lập trình (NRE/cast) → log đúng root cause, không gắn nhầm "key sai".
                        bool isKeyError = rootFailure is InvalidKeyException or ArgumentException;
                        string label = isKeyError ? "Key/asset không hợp lệ" : "Lỗi lập trình khi load (không phải key sai)";
                        Debug.LogError($"{nameof(AddressableLoadPipeline)} > {label} [{key.Value}] " +
                                       $"({typeof(T).Name}): {rootFailure.GetType().Name}: {rootFailure.Message} → fail-fast, bỏ {_MAX_RETRY_COUNT - attempt} lần thử còn lại.");
                        if (handle.IsValid()) ReleaseHandleOrReference(handle, reference);
                        return default;
                    }

                    // transient: giờ mới log chi tiết exception (nếu có) kèm khung lần-thử — non-transient đã fail-fast ở trên.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (caughtEx != null)
                        Debug.LogWarning(
                            $"{nameof(AddressableLoadPipeline)} > Ngoại lệ khi tải [{key.Value}] dưới dạng {typeof(T).Name} " +
                            $"(lần thử {attempt}/{_MAX_RETRY_COUNT}): {(attempt < _MAX_RETRY_COUNT ? caughtEx.Message : caughtEx.ToString())}");
#endif

                    var statusStr = StatusName(handle);
                    if (handle.IsValid())
                        ReleaseHandleOrReference(handle, reference); // đối xứng + dọn m_Operation trước retry
                    if (attempt < _MAX_RETRY_COUNT)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning(
                            $"{nameof(AddressableLoadPipeline)} > Tải asset thất bại: {key.Value} dưới dạng {typeof(T).Name} (lần thử {attempt}/{_MAX_RETRY_COUNT}, trạng thái: {statusStr}). Đang thử lại...");
#endif
                        await UniTask.Delay(_RETRY_BASE_DELAY_MS * attempt, DelayType.Realtime);
                    }
                    else
                    {
                        Debug.LogError(
                            $"{nameof(AddressableLoadPipeline)} > Vượt quá giới hạn thử lại ({_MAX_RETRY_COUNT}) cho asset: {key.Value} dưới dạng {typeof(T).Name} (trạng thái: {statusStr})");
                    }
                }

                return default;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Debug.LogError(
                    $"{nameof(AddressableLoadPipeline)} > Ngoại lệ ngoài dự kiến khi load [{key.Value}] dưới dạng {typeof(T).Name}: {ex}");
                return default;
            }
            finally
            {
                AddressableStateCache.RemoveLoadingTask(key, typeof(T));
            }
        }
    }
}