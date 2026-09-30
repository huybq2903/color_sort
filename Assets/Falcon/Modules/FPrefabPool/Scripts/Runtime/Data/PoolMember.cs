/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-18
 */

using System;
using UnityEngine;

namespace Falcon.Modules.FPrefabPool.Runtime
{
    internal sealed class PoolMember : MonoBehaviour
    {
        // Bump mỗi Spawn/Release để invalidate Lease/Handle đã copy ra ngoài. Source of truth cho release safety.
        // Auto-property: backing field không bị Unity serialize → không leak runtime state qua prefab.
        internal uint Generation { get; set; }

        // Đang được caller giữ (Spawn chưa Release) hay không. Dùng cho OnDestroy detect bypass-release.
        internal bool IsActive { get; set; }

        // Cache IPoolable resolved 1 lần. CONVENTION: IPoolable phải có sẵn trên prefab, KHÔNG add/remove runtime.
        // [NonSerialized] phòng Unity serializer hiểu nhầm.
        [NonSerialized] internal IPoolable Poolable;

        // Ref tới component T của instance này. Pool lưu PoolMember (không phải T) nên Get() trả thẳng member;
        // hot path Spawn lấy instance qua field này thay vì GetInstanceID + dict lookup. [NonSerialized] phòng serializer.
        [NonSerialized] internal Component Component;

        // Bypass detection chạy ở MỌI build (kể cả release-config): lỗi này phải bắt được cả khi tester chạy
        // bản build không tick Development Build. Chỉ log đỏ, không sửa state (fail-loud).
        // App quit / Stop Play-mode → Unity batch-destroy toàn bộ (gồm cả DontDestroyOnLoad của Persistent
        // registry, vốn luôn scene.isLoaded == true). Cờ này phân biệt teardown hệ thống với bypass thật.
        private static bool s_isQuitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetQuitFlag()
        {
            s_isQuitting = false;
            Application.quitting -= OnAppQuitting; // tránh tích luỹ handler khi Domain Reload tắt
            Application.quitting += OnAppQuitting;
        }

        private static void OnAppQuitting() => s_isQuitting = true;

        // Bắt caller Destroy thẳng instance đang active thay vì Release qua Lease/Handle. Chỉ log (fail-loud).
        private void OnDestroy()
        {
            if (!IsActive) return;    // inactive: trong pool / đã release / pool tự teardown → hợp lệ
            if (s_isQuitting) return; // quit / exit play-mode → batch destroy, không phải caller bypass

            // Scene unload thường: Unity batch-destroy scene → không phải caller bypass.
            if (!gameObject.scene.isLoaded) return;

            Debug.LogError(
                $"[PrefabPool] Instance '{name}' bị Destroy trực tiếp khi đang active — bypass Release. " +
                "Hãy trả qua PooledLease/PooledHandle.Release() thay vì Destroy GameObject. " +
                "Hệ quả: PoolStats.Active/peakActive sẽ overcount tới khi entry treo được dọn ở lần Release/Spawn kế.");
        }
    }
}
