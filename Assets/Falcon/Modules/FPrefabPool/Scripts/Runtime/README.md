# 🧩 Prefab Pool Module — Spawn / Release

## 📘 Tổng quan
**Tên:** `Prefab Pool` (CleanPool)

**Giới thiệu:**  
Đây là module Pool support auto/manual Release.

**Thành phần chính:**
- `PoolRegistry`: Quản lý root chứa pool và tạo pool theo prefab.
- `FPrefabPool<T>`: Pool theo prefab, `T : Component`.
- `PoolOptions`: Cấu hình pool (capacity, maxSize, collectionCheck, prewarm…).
- `PooledLease<T>`: dùng spawn object ngắn hạn (trong phạm vi hàm) → PoolLease + `using` → auto-release.
- `PooledHandle<T>`: dùng spawn object cần sống lâu -> PoolHandle (không `using`) -> manual release (tự gọi hàm Release).
- `IPoolable` *(optional)*: Dùng khi muốn bắt sự kiện Spawn/Release (`OnSpawned`, `OnReleased`).

---

## 🚀 Quick Start

### 1️⃣ Tạo pool từ prefab
- Với Persistent Pool (dùng xuyên suốt các scene): Gọi hàm 
        `PoolRegistry.Persistent.GetOrCreate`
- Với Temporary Pool (dùng trong scene hiện tại, qua scene khác sẽ tự giải phóng pool): Gọi hàm
    `PoolRegistry.Temporary.GetOrCreate`

```csharp
using Falcon.Modules.FPrefabPool.Runtime;

public class Example : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;

    private FPrefabPool<Transform> _bulletPool;

    private void Awake()
    {
        _bulletPool = PoolRegistry.Persistent.GetOrCreate(bulletPrefab.transform);
    }
}
```

---

### 2️⃣ Spawn object tạm thời (dùng **PoolLease**)
Dùng cho **VFX / UI fly item / projectile ngắn hạn / animation có scope rõ ràng**.

```csharp
using var lease = _bulletPool.Spawn(parent: transform);
var bullet = lease.Value;
if(bullet == null) return;

// dùng bullet...
bullet.position = transform.position;

// hết scope => auto Release
```
✅ **Ưu điểm:** khó quên `Release`.
✅ **Nhược điểm:**: ra khỏi scope sẽ Release nên nếu dùng sai object sẽ bị trả về pool sớm hơn dự kiến.

---

### 3️⃣ Spawn object sống lâu (dùng **PoolHandle**)
Dùng cho object sống lâu: *enemy, UI panel, actor...*

```csharp
var handle = _bulletPool.SpawnPersistent(parent: transform);
if(!handle.HasValue) return;

var bullet = handle.Value;
handle.Release(); 
```
✅ **Ưu điểm:** không bị auto-release khi ra khỏi scope.
✅ **Nhược điểm:**: có thể bị quên Release.
---

### 4️⃣ Cấu hình `PoolOptions`
> **`Create` vs `GetOrCreate`:** options CHỈ đi vào pool qua `Create` — gọi **một lần** lúc init, **trước** mọi `GetOrCreate`/`Spawn` cho prefab đó. `GetOrCreate` không nhận options (lazy-create bằng default tại điểm spawn). Nếu pool đã tồn tại mà còn gọi `Create` thì options bị bỏ qua + log warning. Dùng `Contains(prefab)` để guard nếu cần tránh warning.

```csharp
var options = new PoolOptions
{
    DefaultCapacity = 27,               // Bộ nhớ ban đầu cấp cho stack nội bộ
    MaxSize = 200,                      // Số instance tối đa pool giữ khi release; vượt thì destroy
    CollectionCheck = true,             // Bắt double-release / invalid get → nên bật
    PrewarmCount = 20,                  // Tạo sẵn 20 instance
    ReturnToPoolRoot = true,            // Release re-parent về holder (tránh bị destroy theo parent gameplay)
    KeepWorldPositionOnReparent = true, // Giữ world position khi reparent
};

// Cấu hình 1 lần (vd. lúc init), TRƯỚC khi Spawn:
var pool = PoolRegistry.Temporary.Create(bulletPrefab.transform, options);

// Về sau, ở điểm spawn chỉ cần resolve (không truyền options):
// var pool = PoolRegistry.Temporary.GetOrCreate(bulletPrefab.transform);
```

---

## ⚙️ Chi tiết

### 🔹 Ví dụ về sử dụng `IPoolable` để Reset Object
```csharp
public class Bullet : MonoBehaviour, IPoolable
{
    private Rigidbody _rb;

    private void Awake() => _rb = GetComponent<Rigidbody>();

    public void OnSpawned()
    {
        _rb.velocity = Vector3.zero;
    }

    public void OnReleased()
    {
        _rb.velocity = Vector3.zero;
    }
}
```

#### ⚠️ Thứ tự lifecycle khi Spawn / Release
- **Spawn:** reparent về parent đích → `SetActive(true)` (Awake/OnEnable chạy ở đây nếu lần đầu) → `IPoolable.OnSpawned`.
- **Release:** `IPoolable.OnReleased` (GO còn active, `transform.parent` vẫn là parent gameplay — **KHÔNG** phải pool holder) → reparent về holder (holder là inactive nên `OnDisable` fire ở bước này) → `SetActive(false)` set self-inactive.
  - Khi `ReturnToPoolRoot = false`: bỏ bước reparent về holder; instance ở nguyên parent cũ và chỉ `SetActive(false)`. Hệ quả: nếu parent gameplay bị destroy lúc instance đang nằm trong pool, instance sẽ chết theo — pool tự bỏ qua instance chết ở lần Spawn kế (xem "Lỗi thường gặp").

Hệ quả cần lưu ý:
- Trong `OnReleased` đừng dựa vào `transform.parent` để đoán "đã về pool".
- `OnDisable` của các component khác chạy **sau** `OnReleased`, nên reset state thì làm trong `OnReleased`, đừng làm trong `OnDisable` (vì khi pool destroy instance do vượt `MaxSize`, `OnDisable` vẫn được Unity gọi và có thể đụng vào state đã dispose).

---

### 🔹 Các API khác

`PoolRegistry`:
| API | Công dụng |
|---|---|
| `Create(prefab, options)` | Tạo + cấu hình pool (1 lần, trước Spawn). |
| `GetOrCreate(prefab)` | Resolve pool, lazy-create bằng default. |
| `Contains(prefab)` | Pool cho prefab đã tồn tại chưa (guard `Create`). |
| `PruneStale()` | Dispose + gỡ pool stale (prefab đã unload qua Addressable/AssetBundle). Gọi ở scene boundary. |
| `IsDisposed` | Registry đã chết chưa (đừng cache ref xuyên scene). |

`FPrefabPool<T>`:
| API | Công dụng |
|---|---|
| `Spawn` / `SpawnPersistent` | Lấy instance (Lease auto-release / Handle manual). |
| `Stats` | `(lifetimeCreated, inactive, active, peakActive)` — debug/monitor. Main-thread only. |
| `Clear()` | Huỷ instance INACTIVE trong pool (không đụng instance đang active). |
| `ResetPeak()` | Reset `peakActive` về `active` hiện tại (đo peak từ điểm này). |
| `IsDisposed` | Pool đã bị Dispose chưa (re-resolve qua registry nếu true). |

---

### 🔹 Dùng đúng trong async/await (UniTask)
✅ **Đúng: Lease trong toàn bộ thời gian active**
```csharp
using var lease = pool.Spawn(parent);
var fx = lease.Value;

await UniTask.Delay(300);
// hết scope => auto Release
```

❌ **Sai: Lease tạo xong bị Dispose sớm**
```csharp
var fx = pool.Spawn(parent).Value; // lease rơi ra => return sớm
await UniTask.Delay(300);
```

---

## 🧠 Best Practices
- **Prewarm** cho prefab spawn nhiều.
- **ReturnToPoolRoot = true** giúp tránh object bị destroy theo parent gameplay.
- **Không `Destroy()`** object spawn từ pool, hãy `Release()`.
- **Cache pool ref → check `IsDisposed` trước khi dùng.** `PoolRegistry.Persistent`/`Temporary` tự recreate sau Dispose (vd. scene unload với Temporary), pool ref cache sẽ stale.
```csharp
if (_cachedPool == null || _cachedPool.IsDisposed)
    _cachedPool = PoolRegistry.Persistent.GetOrCreate(bulletPrefab.transform);
```

---

## 🧩 Lỗi thường gặp

| Lỗi                                    | Nguyên nhân & Giải pháp |
|----------------------------------------|--------------------------|
| Object tự tắt sớm                      | Dùng Lease cho object sống lâu → chuyển sang Handle |
| Pool phình to                          | Quên Release → ưu tiên `using var lease = pool.Spawn()` |
| Double Release                         | Đã được generation guard chặn (log warning "stale handle"), pool không corrupt. `CollectionCheck` chỉ bắt thêm ở tầng ObjectPool |
| Log "instance bị Destroy ngoài pool"   | Destroy thẳng instance (bypass Release) **hoặc** `ReturnToPoolRoot=false` + parent gameplay bị destroy → Release qua Lease/Handle, hoặc đặt `ReturnToPoolRoot=true` cho pool sống xuyên scene |
| Object sai state (scale, alpha...) | Chưa reset → implement `IPoolable` |
| Sai parent transform                   | Truyền đúng `parent` và `keepWorldPositionOnSpawn` |

---
