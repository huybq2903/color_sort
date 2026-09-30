/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-18
 */

namespace Falcon.Modules.FPrefabPool.Runtime
{
    // Immutable post-construction: pool đọc options 1 lần tại creation, mutate sau là no-op. Init-only ép
    // build trước rồi truyền vào. KHÔNG [Serializable] (init-only không serialize qua Inspector — lưu field rời).
    public sealed class PoolOptions
    {
        // Bộ nhớ ban đầu cấp cho stack nội bộ ObjectPool.
        public int DefaultCapacity { get; init; } = 32;

        // Số instance tối đa pool giữ khi release; vượt thì destroy.
        public int MaxSize { get; init; } = 256;

        // Bắt double-release / invalid get. Generation guard ở FPrefabPool đã chặn double-release ở runtime;
        // collectionCheck chỉ giữ làm lưới an toàn dev (Stack.Contains O(n) mỗi Release → tắt ở release build).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool CollectionCheck { get; init; } = true;
#else
        public bool CollectionCheck { get; init; } = false;
#endif

        // Tạo sẵn 1 lượng instance để tránh hitch.
        public int PrewarmCount { get; init; } = 0;

        // Release có re-parent về pool holder không.
        // true (default): instance về holder inactive → hierarchy gọn, sống sót khi parent gameplay bị Destroy.
        //   Cái giá: 2 SetParent mỗi chu kỳ (spawn→parent, release→holder).
        // false: release chỉ SetActive(false) tại chỗ (1 SetParent lúc spawn) → tránh Canvas rebuild.
        //   KHUYẾN NGHỊ cho pool UI dưới Canvas. Lưu ý: nếu parent gameplay bị Destroy, instance inactive sẽ
        //   chết theo → AcquireInstance tự dọn dead-entry ở lần spawn sau.
        public bool ReturnToPoolRoot { get; init; } = true;

        // CHỈ áp cho lần reparent VỀ holder lúc Release (spawn dùng tham số keepWorldPositionOnSpawn riêng).
        // Holder inactive + instance sẽ được Spawn đặt lại vị trí → giữ world-pos lúc release là recompute TRS thừa.
        // Default false để bỏ khoản này; caller nào thực sự cần giữ world-pos khi release có thể set true.
        public bool KeepWorldPositionOnReparent { get; init; } = false;
    }
}
