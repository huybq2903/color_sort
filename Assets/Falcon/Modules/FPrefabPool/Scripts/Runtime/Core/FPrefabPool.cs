/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-18
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Falcon.Modules.FPrefabPool.Runtime
{
    public sealed partial class FPrefabPool<T> : IDisposable, IPoolInternal where T : Component
    {
        private static readonly string _tag = $"[PrefabPool<{typeof(T).Name}>]";

        private readonly PoolRegistry _registry;

        // Snapshot flag từ PoolOptions tại construction; không giữ ref tới PoolOptions sau ctor.
        private readonly bool _returnToPoolRoot;
        private readonly bool _keepWorldPositionOnReparent;

        private readonly T _prefab;
        private readonly int _prefabId;
        private readonly int _prefabGoId;
        private readonly Transform _holder;

        private ObjectPool<PoolMember> _pool;

        // Tracking PoolMember theo instanceID để tránh GetComponent mỗi Spawn/Release.
        // Pre-size tại ctor (max DefaultCapacity/PrewarmCount) → tránh rehash khi Prewarm lúc load.
        private readonly Dictionary<int, PoolMember> _memberByInstance;

        private int _lifetimeCreated;
        private int _peakActive;

        private bool _disposed;
        private bool _staleLogged; // chống spam: chỉ log 1 lần khi spawn trên pool đã stale (prefab unload)

        // active = số instance đang track trừ số đang nằm trong pool. Dẫn xuất → không cần counter bookkeeping.
        private int Active => _memberByInstance.Count - (_pool?.CountInactive ?? 0);

        // Caller cache pool ref check trước khi dùng (PoolRegistry tự recreate sau Dispose → ref cache stale).
        public bool IsDisposed => _disposed;

        // Prefab nguồn đã unload (Unity-null) → pool stale, PoolRegistry.PruneStale sẽ dọn.
        bool IPoolInternal.IsStale => !_prefab;

        // GameObject instanceID của prefab — chung cho mọi T của cùng prefab (xem IPoolInternal).
        int IPoolInternal.PrefabGameObjectId => _prefabGoId;

        private FPrefabPool(PoolRegistry registry, T prefab, int prefabId, PoolOptions options)
        {
            _registry = registry;
            _prefab = prefab;
            _prefabId = prefabId;
            _prefabGoId = prefab.gameObject.GetInstanceID();

            _returnToPoolRoot = options.ReturnToPoolRoot;
            _keepWorldPositionOnReparent = options.KeepWorldPositionOnReparent;

            // Khởi tạo TRƯỚC Prewarm (Prewarm → CreateInstance → add vào dict).
            _memberByInstance = new Dictionary<int, PoolMember>(
                Mathf.Max(options.DefaultCapacity, options.PrewarmCount));

            _holder = new GameObject($"[Pool] {typeof(T).Name} | {_prefabId}").transform;
            _holder.SetParent(_registry.Root, false);

            BuildPool(options);

            // Register TRƯỚC Prewarm: nếu Prewarm throw, registry vẫn biết pool để cleanup.
            _registry.RegisterPool(this);

            Prewarm(options.PrewarmCount, Mathf.Max(1, options.MaxSize));
        }

        /// <summary>Snapshot số liệu pool. Main-thread only.</summary>
        public PoolStats Stats => new PoolStats(_lifetimeCreated, _pool?.CountInactive ?? 0, Active, _peakActive);

        private void BuildPool(PoolOptions options)
        {
            // actionOnGet/Release = null CHỦ ĐÍCH: lifecycle nằm ở AcquireInstance/DoRelease (cần context parent
            // + xử lý throw mà ObjectPool không cấp). Nhờ đó Prewarm gọi Get/Release thuần cũng không chạy lifecycle.
            int maxSize = Mathf.Max(1, options.MaxSize);
            _pool = new ObjectPool<PoolMember>(
                createFunc: CreateInstance,
                actionOnGet: null,
                actionOnRelease: null,
                actionOnDestroy: OnDestroyInstance,
                collectionCheck: options.CollectionCheck,
                // Tính cả PrewarmCount → stack không realloc khi Prewarm (đồng bộ pre-size _memberByInstance ở ctor).
                // Clamp theo maxSize: DefaultCapacity/PrewarmCount > MaxSize chỉ pre-alloc thừa (pool không giữ quá maxSize).
                defaultCapacity: Mathf.Clamp(Mathf.Max(options.DefaultCapacity, options.PrewarmCount), 0, maxSize),
                maxSize: maxSize
            );
        }

        private PoolMember CreateInstance()
        {
            // Throw thay vì return null: ObjectPool.Get làm m_CountAll++ ngay sau createFunc, null sẽ phantom count.
            if (!_prefab || !_holder)
                throw new InvalidOperationException(
                    $"{_tag} Không thể Instantiate: prefab destroyed={!_prefab}, holder destroyed={!_holder}.");

            var inst = Object.Instantiate(_prefab, _holder, false);
            if (!inst)
                throw new InvalidOperationException($"{_tag} Object.Instantiate trả null cho prefab '{_prefab.name}'.");

            inst.gameObject.SetActive(false);

            // Re-use PoolMember sẵn có (phòng prefab bị nhiễm component) để tránh 2 component trên cùng GO.
            if (!inst.TryGetComponent<PoolMember>(out var member))
                member = inst.gameObject.AddComponent<PoolMember>();
            member.Generation = 0;
            member.Component = inst;                          // cache T → hot path khỏi GetInstanceID + dict lookup
            inst.TryGetComponent<IPoolable>(out member.Poolable);
            _memberByInstance[inst.GetInstanceID()] = member; // dict vẫn giữ (key theo T id) cho teardown/prune

            _lifetimeCreated++;
            return member;
        }

        // Nhận PoolMember (pool element). member còn sống khi pool tự destroy excess → member.Component (T) cũng sống.
        private void OnDestroyInstance(PoolMember member)
        {
            if (ReferenceEquals(member, null)) return;
            if (!ReferenceEquals(member.Component, null))
                _memberByInstance.Remove(member.Component.GetInstanceID());
            if (member) Object.Destroy(member.gameObject);
        }

        /// <summary>Spawn object ngắn hạn, trả Lease để dùng `using` => auto-release.</summary>
        /// <returns>
        /// Lease hợp lệ khi thành công. Khi FAIL (pool disposed / instance bị Destroy ngoài pool / OnSpawned throw)
        /// trả về <c>default</c>: <see cref="PooledLease{T}.HasValue"/> == false và <c>Value</c> == null.
        /// Luôn check <c>HasValue</c> hoặc dùng <c>TryGet(out var v)</c> TRƯỚC khi đọc <c>Value</c>.
        /// </returns>
        public PooledLease<T> Spawn(Transform parent = null, bool keepWorldPositionOnSpawn = false)
            => TryAcquire(nameof(Spawn), parent, keepWorldPositionOnSpawn, out var inst, out var member, out var gen)
                ? new PooledLease<T>(this, inst, member, gen)
                : default;

        /// <summary>Spawn object dùng lâu, chủ động gọi Release.</summary>
        /// <returns>
        /// Handle hợp lệ khi thành công. Khi FAIL (pool disposed / instance bị Destroy ngoài pool / OnSpawned throw)
        /// trả về <c>default</c>: <see cref="PooledHandle{T}.HasValue"/> == false và <c>Value</c> == null.
        /// Luôn check <c>HasValue</c> hoặc dùng <c>TryGet(out var v)</c> TRƯỚC khi đọc <c>Value</c>.
        /// </returns>
        public PooledHandle<T> SpawnPersistent(Transform parent = null, bool keepWorldPositionOnSpawn = false)
            => TryAcquire(nameof(SpawnPersistent), parent, keepWorldPositionOnSpawn, out var inst, out var member, out var gen)
                ? new PooledHandle<T>(this, inst, member, gen)
                : default;

        // Gom disposed-check + acquire cho Spawn/SpawnPersistent (1 nguồn sự thật). caller giữ đúng tên method cho log.
        private bool TryAcquire(string caller, Transform parent, bool keepWorldPosition,
            out T inst, out PoolMember member, out uint gen)
        {
            inst = null; member = null; gen = 0;
            if (_pool == null) { LogDisposedAccess(caller); return false; }

            // Prefab nguồn đã unload → pool stale: tạo instance mới sẽ throw trong createFunc. Fail-loud 1 lần
            // (cờ chống spam) thay vì throw+log mỗi spawn; instance còn sót là orphan → dọn bằng PruneStale.
            if (!_prefab)
            {
                if (!_staleLogged)
                {
                    _staleLogged = true;
                    Debug.LogError($"{_tag} {caller} failed: prefab nguồn đã unload (pool stale). " +
                        "Gọi PruneStale ở scene boundary; đừng spawn sau khi release asset.");
                }
                return false;
            }

            inst = AcquireInstance(parent, keepWorldPosition, out gen, out member);
            return inst != null;
        }

        // Lấy instance + chạy spawn-lifecycle: reparent → SetActive(true) → OnSpawned. Trả null nếu fail.
        // Caller tự đảm bảo _pool != null.
        private T AcquireInstance(Transform parent, bool keepWorldPosition, out uint generation, out PoolMember member)
        {
            generation = 0;
            member = null;

            // Pop instance còn sống. Pool lưu PoolMember → Get() trả thẳng member, lấy T qua member.Component
            // (bỏ GetInstanceID + dict lookup khỏi hot path). Với ReturnToPoolRoot=false, instance inactive có thể
            // bị Destroy theo parent gameplay → bỏ qua (dọn entry treo) rồi thử cái khác/tạo mới. Bounded = CountInactive + 1.
            T inst = null;
            int maxAttempts = _pool.CountInactive + 1;
            try
            {
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    member = _pool.Get();

                    // true C# null: createFunc lẽ ra phải throw thay vì trả null → corrupt invariant, fail-loud.
                    if (ReferenceEquals(member, null))
                    {
                        Debug.LogError($"{_tag} ObjectPool.Get trả C# null (vi phạm invariant createFunc throw).");
                        member = null;
                        return null;
                    }

                    inst = member.Component as T;
                    if (inst) break;                                // Unity-alive

                    // fake-null (dead inactive: GO bị Destroy ngoài pool) → dọn tracking, thử tiếp.
                    // LƯU Ý: pop-and-drop KHÔNG đi qua actionOnDestroy nên ObjectPool.CountAll không giảm
                    // → _pool.CountActive/CountAll của ObjectPool KHÔNG đáng tin. PoolStats dùng Active (derived từ
                    // _memberByInstance.Count - CountInactive) nên vẫn chính xác: drop làm cả 2 cùng giảm 1, triệt tiêu.
                    if (!ReferenceEquals(member.Component, null))
                        _memberByInstance.Remove(member.Component.GetInstanceID());
                    member = null;
                    inst = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{_tag} Exception in ObjectPool.Get: {ex}");
                member = null;
                return null;
            }

            // member đến THẲNG từ pool → không còn nhánh "không được track": inst==null nghĩa là toàn bộ inactive đã chết.
            if (inst == null || member == null)
            {
                Debug.LogError($"{_tag} Spawn fail: không lấy được instance hợp lệ (instance bị Destroy ngoài pool).");
                member = null;
                return null;
            }

            member.IsActive = true;
            // bump + capture NGAY (trước SetActive): re-entrant Release trong Awake/OnEnable sẽ bump gen tiếp,
            // spawnGen giữ nguyên giá trị lần spawn này → phát hiện được ở check bên dưới.
            uint spawnGen = ++member.Generation;

            // reparent TRƯỚC SetActive(true) để Awake/OnEnable thấy đúng parent (parent là local → re-entrant an toàn).
            inst.transform.SetParent(parent, keepWorldPosition);
            inst.gameObject.SetActive(true);

            // Awake/OnEnable (fire bởi SetActive) re-entrant Release chính instance này → gen đã bị bump tiếp.
            // Instance đã về pool inactive: KHÔNG chạy OnSpawned, KHÔNG trả handle "sống giả".
            if (member.Generation != spawnGen)
            {
                Debug.LogWarning($"{_tag} Spawn '{inst.name}' bị Release ngay trong Awake/OnEnable — bỏ qua.");
                member = null;
                return null;
            }

            // Editor-only: check runtime add/remove IPoolable. Là hot path (mỗi spawn) nên KHÔNG để chạy ở build —
            // vi phạm convention deterministic, lặp lại mỗi spawn → Editor bắt được sớm khi dev iterate.
#if UNITY_EDITOR
            inst.TryGetComponent<IPoolable>(out var currentPoolable);
            if (!ReferenceEquals(currentPoolable, member.Poolable))
                Debug.LogError($"{_tag} IPoolable trên '{inst.name}' đổi runtime — phải có sẵn trên prefab, không add/remove runtime.");
#endif

            // Capture cho OnSpawned: nếu OnSpawned re-entrant release chính instance này, gen bị bump tiếp
            // → Lease vẫn mang đúng spawnGen, release stale sau đó tự no-op.
            generation = spawnGen;

            if (member.Poolable != null)
            {
                try
                {
                    member.Poolable.OnSpawned();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{_tag} Exception in IPoolable.OnSpawned for '{inst.name}': {ex}");
                    // OnSpawned throw → instance chưa spawn xong: trả về pool nhưng KHÔNG gọi OnReleased.
                    DoRelease(inst, member, runReleasedCallback: false);
                    return null;
                }
            }

            // OnSpawned re-entrant Release chính instance → gen đã bị bump tiếp, instance đã về pool inactive.
            // Đối xứng với guard Awake/OnEnable: KHÔNG trả lease/handle "sống giả".
            if (member.Generation != spawnGen)
            {
                Debug.LogWarning($"{_tag} Spawn '{inst.name}' bị Release ngay trong OnSpawned — bỏ qua.");
                member = null;
                return null;
            }

            // Track peak ở mọi build: cost O(1) (1 phép so int) không đáng kể, đổi lại PoolStats.peakActive
            // đúng cả ở release (telemetry/analytics đọc được giá trị thật thay vì 0).
            int active = Active;
            if (active > _peakActive) _peakActive = active;
            return inst;
        }

        // Còn trỏ ĐÚNG instance của lần spawn này không (chưa Release/respawn). Generation là source of truth.
        // member==null: C# null (default handle) hoặc Unity-null (GO đã Destroy) → không còn live.
        internal bool IsLive(PoolMember member, uint expectedGeneration)
            => member != null && member.Generation == expectedGeneration;

        // Release đi qua Lease/Handle. Generation guard chặn double-release và release nhầm sau respawn.
        // member do Lease/Handle giữ thẳng (không tra dict) → bỏ GetInstanceID + lookup khỏi hot path.
        internal void ReleaseFromHandle(T inst, PoolMember member, uint expectedGeneration)
        {
            if (_disposed) return;                    // pool teardown → no-op
            if (ReferenceEquals(inst, null)) return;  // default Lease/Handle → no-op

            // member Unity-null → instance bị Destroy ngoài pool (bypass Destroy, hoặc ReturnToPoolRoot=false +
            // parent gameplay destroyed). Dọn entry treo + cảnh báo; KHÔNG đẩy instance đã chết vào _pool.Release.
            // Cold path → GetInstanceID ở đây chấp nhận được (chỉ chạy khi instance đã chết).
            if (member == null)
            {
                if (_memberByInstance.Remove(inst.GetInstanceID()))
                    Debug.LogWarning(
                        $"{_tag} Release: instance đã bị Destroy ngoài pool — đã dọn tracking. Ưu tiên Release qua Lease/Handle (đừng Destroy trực tiếp).");
                return;
            }

            // Generation mismatch → stale handle. Warning (không Error) vì explicit double-Release()
            // hoặc handle giữ qua respawn cũng vào đây — đều là misuse "mềm", không phá invariant pool.
            // (double-Dispose `using` KHÔNG vào đây: PooledLease.Dispose idempotent qua HasValue.)
            if (member.Generation != expectedGeneration)
            {
                Debug.LogWarning(
                    $"{_tag} Release ignored on '{inst.name}': stale handle (expected gen={expectedGeneration}, current={member.Generation}).");
                return;
            }

            DoRelease(inst, member);
        }

        // Release-lifecycle + đẩy về pool: OnReleased → reparent holder → SetActive(false) → _pool.Release.
        // Gọi từ ReleaseFromHandle (đã qua guard) và AcquireInstance khi OnSpawned throw. _pool != null.
        // runReleasedCallback=false: OnSpawned throw → instance chưa spawn xong, KHÔNG chạy OnReleased (contract đối xứng).
        private void DoRelease(T inst, PoolMember member, bool runReleasedCallback = true)
        {
            member.Generation++; // invalidate Lease/Handle còn trỏ tới instance này
            member.IsActive = false;

            if (runReleasedCallback && member.Poolable != null)
            {
                // OnReleased throw không được làm gãy chuỗi reparent + SetActive + _pool.Release bên dưới.
                try
                {
                    member.Poolable.OnReleased();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{_tag} Exception in IPoolable.OnReleased for '{inst.name}': {ex}");
                }
            }

            if (_returnToPoolRoot && inst && _holder)
                inst.transform.SetParent(_holder, _keepWorldPositionOnReparent);

            if (inst)
                inst.gameObject.SetActive(false);

            try
            {
                _pool.Release(member); // pool element là PoolMember; member luôn sống ở đây (đã qua guard)
            }
            catch (Exception ex)
            {
                Debug.LogError($"{_tag} Exception in ObjectPool.Release for '{inst.name}': {ex}");
            }
        }

        private void LogDisposedAccess(string method)
        {
            string prefabName = _prefab != null ? _prefab.name : "<destroyed>";
            Debug.LogError(
                $"{_tag} {method} failed: pool đã Dispose (prefab='{prefabName}'). Đừng cache pool xuyên scene — " +
                "gọi PoolRegistry.Temporary.GetOrCreate tại điểm dùng; cần xuyên scene → PoolRegistry.Persistent.");
        }

        // Pop `count` instance khỏi stack rồi release lại tất cả → đẩy instance về holder inactive (actionOnGet/
        // Release = null nên Get/Release ở đây chỉ cấp phát + đẩy về stack). Gom 1 lần để Get không trả lại đúng
        // cái vừa Release trong cùng loop. cleanupDead=true: gặp fake-null (instance bị Destroy ngoài pool) thì
        // bỏ tracking + KHÔNG trả lại pool (Compact). finally đảm bảo mọi instance đã pop đều được release (kể cả
        // khi Get throw giữa loop). Degrade gracefully: Get fail thì dừng sớm, KHÔNG ném ra ngoài.
        private void DrainInactive(int count, bool cleanupDead, string opName)
        {
            if (count <= 0) return;

            // Scratch rented từ ListPool (đã clear sẵn) → bỏ alloc mỗi Compact/Prewarm. DrainInactive không
            // re-entrant (Get/Release có actionOnGet/Release=null → không chạy user callback) nên rent/return an toàn.
            var live = ListPool<PoolMember>.Get();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    PoolMember member;
                    try { member = _pool.Get(); }
                    catch (Exception ex) { Debug.LogError($"{_tag} {opName} dừng sớm tại {i}/{count}: {ex}"); break; }

                    if (member) { live.Add(member); continue; }                          // Unity-alive → giữ, đẩy lại
                    // fake-null → bỏ tracking, KHÔNG trả lại pool (như AcquireInstance: CountAll của ObjectPool
                    // không giảm → đừng đọc CountActive/CountAll của _pool; PoolStats dùng Active derived nên không ảnh hưởng).
                    if (cleanupDead && !ReferenceEquals(member, null) && !ReferenceEquals(member.Component, null))
                        _memberByInstance.Remove(member.Component.GetInstanceID());
                }
            }
            finally
            {
                for (int i = 0; i < live.Count; i++)
                {
                    try { _pool.Release(live[i]); }
                    catch (Exception ex) { Debug.LogError($"{_tag} {opName} release lỗi: {ex}"); }
                }
                ListPool<PoolMember>.Release(live); // trả lại pool + auto-clear (không giữ ref PoolMember sống)
            }
        }

        // Tạo sẵn instance tránh hitch. count > maxSize: phần dư sẽ bị ObjectPool destroy ngay khi release → clamp.
        private void Prewarm(int count, int maxSize)
            => DrainInactive(Mathf.Min(count, maxSize), cleanupDead: false, opName: "Prewarm");

        /// <summary>
        /// Dọn entry chết (instance inactive bị Destroy ngoài pool, vd. ReturnToPoolRoot=false + parent gameplay
        /// destroyed) khỏi CẢ _memberByInstance LẪN slot fake-null trong stack ObjectPool. Cold path — gọi tại
        /// scene boundary cho pool ReturnToPoolRoot=false; instance đang active (caller giữ qua Lease/Handle) không bị đụng.
        /// </summary>
        public void Compact()
        {
            if (_pool == null) return;
            // Chỉ pop đúng số inactive hiện có → KHÔNG kích createFunc tạo instance thừa (bound như AcquireInstance).
            DrainInactive(_pool.CountInactive, cleanupDead: true, opName: "Compact");
        }

        // Huỷ instance INACTIVE trong stack. Instance đang active (caller giữ qua Lease/Handle) không bị đụng.
        public void Clear() => _pool?.Clear();

        // Reset peak về active hiện tại — đo peak "từ điểm này" (vd. end-of-level dashboard).
        public void ResetPeak() => _peakActive = Active;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Clear chỉ destroy instance inactive → entry còn lại trong _memberByInstance là active, dọn tay.
            _pool?.Clear();
            _pool = null;

            foreach (var kvp in _memberByInstance)
            {
                var member = kvp.Value;
                if (member == null) continue;

                member.IsActive = false; // pool chủ động teardown → tắt cờ để OnDestroy không báo bypass
                member.Generation++;     // invalidate Lease/Handle ngay (Object.Destroy bị hoãn tới cuối frame)

                if (member.Poolable != null)
                {
                    try
                    {
                        member.Poolable.OnReleased();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{_tag} Exception in IPoolable.OnReleased during Dispose: {ex}");
                    }
                }

                if (member.gameObject != null)
                    Object.Destroy(member.gameObject);
            }
            _memberByInstance.Clear();

            // Chỉ remove khi entry đúng là pool này — chống Unity tái cấp instanceID (entry mới trùng key).
            var cacheKey = (_registry, _prefabId);
            if (_cache != null && _cache.TryGetValue(cacheKey, out var cached) && ReferenceEquals(cached, this))
                _cache.Remove(cacheKey);

            // Tự rời _pools để registry không treo ref/holder tới khi PruneStale chạy tay (no-op nếu registry
            // đang teardown/prune — xem PoolRegistry.UnregisterPool).
            _registry?.UnregisterPool(this);

            if (_holder != null)
                Object.Destroy(_holder.gameObject);
        }
    }
}
