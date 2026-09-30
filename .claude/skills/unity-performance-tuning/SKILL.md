---
name: unity-performance-tuning
description: Use when optimizing Unity game performance, profiling bottlenecks, or reviewing graphics/UI/physics/script/asset/player settings for CPU, GPU, and memory efficiency.
---

# Unity Performance Tuning

Reference từ [Unity Performance Tuning Bible](https://cyberagentgameentertainment.github.io/UnityPerformanceTuningBible/) của CyberAgent — cập nhật cho Unity 6 / URP 17.

> Luôn profile trên **thiết bị thật**, không dùng Editor.

---

## Profiling Tools

| Tool | Dùng để |
|------|---------|
| **Unity Profiler** | Frame-by-frame CPU/Memory |
| **Profile Analyzer** | So sánh before/after (avg, median) |
| **Frame Debugger** | Draw calls, batching, overdraw |
| **Memory Profiler** | Memory leak, reference tree |
| **Xcode / Android Studio** | Native profiling trên device |
| **RenderDoc** | GPU chi tiết (Windows) |

---

## Graphics

### Draw Calls
- **Static Batching**: object tĩnh, cùng material
- **SRP Batcher**: shader phải khai báo `UnityPerMaterial` CBUFFER đúng chuẩn
- **GPU Instancing**: mesh lặp lại — enable per material (xem thêm GPU Resident Drawer bên dưới)
- **SpriteAtlas**: gom sprite → giảm draw call
- Dynamic Batching: **tắt mặc định trong URP** vì SRP Batcher hiệu quả hơn nhiều; không nên dùng

### GPU Resident Drawer *(Unity 6 / URP 17)*
Hệ thống batch + GPU-instance tự động cho toàn bộ GameObject đủ điều kiện, không cần setup per-material. Có thể giảm CPU frame time tới 50%.
- Bật trong URP Asset → **GPU Resident Drawer = Instanced Drawing**
- Đặt **BatchRendererGroup Variants = Keep All** trong Shader Stripping
- Yêu cầu rendering path **Forward+**

### Forward+ Rendering Path *(URP 14+)*
- Xóa giới hạn 8 light/object → hỗ trợ đến 256 light/camera (desktop)
- Là prerequisite cho GPU Resident Drawer và GPU Occlusion Culling
- Đặt trong URP Asset → Rendering → Rendering Path

### Overdraw
- Giảm transparent area, dùng dithering thay alpha blend
- Kiểm tra bằng Scene View → **Overdraw** mode

### Culling
- **Occlusion Culling (baked)**: bake visibility cho scene phức tạp
- **GPU Occlusion Culling** *(Unity 6)*: Hi-Z test chạy hoàn toàn trên GPU, không cần bake — yêu cầu GPU Resident Drawer + Forward+
- **LOD Group**: swap mesh detail theo khoảng cách camera
- Back-face culling: disable qua shader settings

### Shader
- Dùng `half` (16-bit) thay `float` (32-bit) khi có thể
- Tính toán nặng ở vertex shader thay fragment
- Precompile **ShaderVariantCollection** để tránh spike runtime

### Lighting
- Bake **Lightmap** cho scene tĩnh
- **Adaptive Probe Volumes (APV)** *(Unity 6)*: thay thế Light Probe Group thủ công — tự động đặt probe, hỗ trợ streaming và Sky Occlusion; khuyến nghị cho project mới
- Giảm Shadow Distance trong Quality Settings
- Hard shadow < Soft shadow về performance
- Pseudo shadow (plate polygon) cho game stylized

### Resolution & Upscaling
- `Screen.SetResolution()` để scale theo device
- Fixed DPI mode trong Player Settings
- **STP (Spatial-Temporal Post-processing)** *(Unity 6 / URP 17)*: render ở resolution thấp hơn + temporal upscaling lên native — giảm tải GPU đáng kể, đặc biệt trên mobile

---

## Script — Unity

| Vấn đề | Fix |
|--------|-----|
| Empty `Start()`/`Update()` | Xóa — Unity vẫn cache và gọi chúng |
| `GetComponent<T>()` mỗi frame | Cache trong `Awake()` |
| `transform` access mỗi frame | Cache vào biến local |
| So sánh `tag`/`name` lặp lại | Cache string |
| Animator/Shader string params | `StringToHash()` / `PropertyToID()` |
| `Debug.Log` trong build | Dùng `[Conditional]` attribute |
| `Renderer.material` | Tạo clone → phải `Destroy()` thủ công |
| Position + Rotation riêng lẻ | Dùng `SetPositionAndRotation()` |

- `[BurstCompile]` cho Job System: ~5-6x nhanh hơn
- `Texture2D`, `Sprite`, `Material`, `PlayableGraph`: phải `Destroy()` thủ công, GC không tự giải phóng

---

## Script — C#

### GC Allocation
- Pre-allocate `List<T>`, tránh `new` trong hot path
- **Object Pool**: dùng `UnityEngine.Pool.ObjectPool<T>` (built-in từ Unity 2021.1) thay tự implement
  - Cũng có `ListPool<T>`, `CollectionPool<T>`, `DictionaryPool<T,V>`

### Collections
- Array nhanh hơn `List<T>` ~2.3x
- Cache `.Count` trước vòng lặp
- `foreach` trên `List<T>` variable **không allocate** (đã fix từ Unity 5.6); chỉ tránh khi cast sang `IEnumerable<T>`

### Tránh trong hot path
| Thứ cần tránh | Lý do |
|---------------|-------|
| **LINQ** | Chậm hơn loop tới 19x, allocate nhiều |
| **String concat** | Dùng `StringBuilder` với capacity đặt sẵn |
| **Lambda capture member/local** | Dùng static method thay thế |

### async/await
- Dùng **UniTask v2** thay `Task` (zero-allocation, không có SynchronizationContext overhead)
- Tránh `async` cho method có thể complete synchronously
- **Unity 6**: dùng `destroyCancellationToken` built-in trên `MonoBehaviour`/`GameObject` thay `GetCancellationTokenOnDestroy()`

### Advanced
- `stackalloc` + `Span<T>`: allocate array trên stack, không tốn GC
- `sealed` class: compiler dùng direct call thay virtual
- `[MethodImpl(AggressiveInlining)]`: inline method nhỏ gọi nhiều lần (Unity 2020.2+)

---

## UI

| Vấn đề | Giải pháp |
|--------|----------|
| Toàn bộ Canvas rebuild | Tách Canvas: tĩnh vs động |
| Layout recalc tốn kém | Thay `VerticalLayoutGroup`/`GridLayoutGroup` bằng RectTransform anchor |
| Raycast không cần thiết | Disable **Raycast Target** mặc định |
| `SetActive()` chậm | Disable Canvas component hoặc `CanvasGroup.alpha = 0` |
| TextMeshPro string alloc | Dùng `SetText()` + ZString |
| UnityWhite draw riêng | Thêm pixel trắng vào SpriteAtlas |

- `RectMask2D`: cho mask hình chữ nhật (shader-based), disable khi không dùng

---

## Asset

### Texture
- Disable **Read/Write** (bật → double memory)
- Disable **Mip Maps** cho UI/2D sprite
- Luôn **compress** texture
- Aniso Level: để 1 trừ khi cần

### Mesh
- Disable **Read/Write**
- Bật **Vertex Compression** (float → half)
- **Optimize Mesh Data**: xóa vertex channel không dùng

### Animation
- Giảm **Skin Weights** theo tier device
- Bật **Anim. Compression** (key frame reduction)
- Culling Mode: **"Cull Completely"** khi an toàn

### Particle System
- Giới hạn **Max Particles**
- Sub Emitters: theo dõi kỹ, có thể spike count mạnh
- Noise module: bắt đầu với **Low** quality

### Audio
| Loại | Load Type | Format |
|------|-----------|--------|
| SFX | Decompress On Load | ADPCM |
| Voice | Compressed In Memory | Vorbis |
| BGM | Streaming | Vorbis |
| SFX mono | Force To Mono | — |

### Khác
- `Resources/` folder: tránh dùng nhiều — tăng startup time
- `ScriptableObject`: `[PreferBinarySerialization]` cho data lớn
- `renderer.material`: tạo clone → `Destroy()` trong `OnDestroy()`

---

## Physics

| Setting | Khuyến nghị |
|---------|------------|
| `Physics.simulationMode` *(Unity 6)* | `SimulationMode.Script` khi không cần physics tự động — thay thế `autoSimulation` đã obsolete |
| Fixed Timestep | Gần target FPS |
| Maximum Allowed Timestep | Đặt giới hạn để tránh spiral |
| `Physics.reuseCollisionCallbacks` | `true` → giảm GC *(legacy flag trong Unity 6, vẫn hoạt động)* |
| `Physics.autoSyncTransforms` | `false` → giảm sync overhead |

> `Physics.autoSimulation` đã **obsolete** trong Unity 6. Dùng `Physics.simulationMode = SimulationMode.FixedUpdate / Update / Script`.

**Collider priority:** Sphere > Capsule > Box > Mesh (tránh Mesh trừ bắt buộc)

**Raycast:** Luôn set `maxDistance` + `layerMask`. Dùng `RaycastNonAlloc` thay `RaycastAll`.

**Collision Matrix:** Uncheck layer pairs không tương tác với nhau.

**Collision Detection:** Discrete mặc định. Continuous chỉ cho dynamic-static. Tránh Continuous Dynamic.

**Static Collider:** Không move/toggle trong gameplay.

---

## Player Settings

| Setting | Khuyến nghị |
|---------|------------|
| Scripting Backend | **IL2CPP** (tốt hơn Mono về runtime) |
| C++ Compiler | **Master** cho production build |
| Managed Stripping Level | Bắt đầu với **Minimal** (IL2CPP); tăng dần; dùng `link.xml` cho reflection code. Các mức: Disabled → Minimal → Low → Medium → High |
| Accelerometer (iOS) | Giảm Hz hoặc tắt nếu không dùng |

---

## AssetBundle

> Với **project mới**, nên dùng **Addressables** (layer trên AssetBundle) — tự động quản lý grouping, dependency, và memory. Raw AssetBundle API vẫn dùng được khi cần kiểm soát tối đa.

**Phân nhóm:**
- Asset dùng cùng lúc → gom vào 1 bundle
- Asset dùng chung nhiều nơi → tách ra bundle riêng để tránh duplicate

**Loading API:**
| API | Khi nào dùng |
|-----|-------------|
| `LoadFromFile()` | Mặc định — nhanh nhất, ít memory nhất |
| `LoadFromMemory()` | Trường hợp đặc biệt — overhead cao |
| `LoadFromStream()` | Bundle mã hóa cần giải mã khi load |

**Unload:**
- `Unload(true)`: khuyến nghị — an toàn hơn, giữ bundle khi asset đang dùng
- `Unload(false)`: cần cleanup thủ công, dễ leak và duplicate

**Giới hạn concurrent bundles:**
- Max **150–200** bundle loaded đồng thời
- Theo dõi file descriptor limit của OS ("Too many open files")
- `PersistentManager.Remapper`: memory từ bundle đã unload được recycle, không freed hoàn toàn

---

## Third Party

### DOTween
- `SetAutoKill(false)` cho animation lặp lại nhiều → reuse với `Restart()`
- Phải gọi `Kill()` thủ công khi dùng `SetAutoKill(false)` để tránh leak
- `SetLink(gameObject)`: tween tự destroy khi GameObject bị destroy
- Dùng `[DOTween]` GameObject trong Editor để monitor tween đang chạy

### UniRx
- Luôn giữ `IDisposable` từ `Subscribe()` và gọi `Dispose()` khi xong
- `AddTo(this)` trong MonoBehaviour: tự cleanup khi object bị destroy

### UniTask
- Dùng **v2** (v2.5.10): zero-allocation cho toàn bộ async method
- **Unity 6**: dùng `destroyCancellationToken` property built-in thay `GetCancellationTokenOnDestroy()`
- Dùng **UniTask Tracker** (Window menu) để monitor task đang waiting và phát hiện leak

---

## Fundamentals — Kiến thức nền

### C# Memory
| Khái niệm | Ghi nhớ |
|-----------|---------|
| **GC (Boehm)** | Scan toàn bộ heap, không generational → spike "Stop the World" |
| **Incremental GC** | Mặc định ON trong Unity 6 — chia nhỏ GC qua nhiều frame, nhưng không loại bỏ allocation cost |
| **Value type (struct)** | Allocate trên stack, không GC overhead, nhưng copy khi pass |
| **Boxing** | Value type cast sang object/interface → alloc trên heap → GC |

### Unity Architecture
- **Native ↔ Managed boundary**: mỗi lần gọi API từ C# xuống C++ đều có overhead → cache kết quả
- **Main thread**: game logic. **Render thread**: giao tiếp GPU. Physics có thể chạy trên worker thread
- **IL2CPP**: compile IL → C++ → native, performance tốt hơn Mono trên 64-bit

### Mobile Hardware
- **SoC**: CPU + GPU + DSP trên cùng chip → chia sẻ bandwidth, thermal limit chung
- **big.LITTLE cores**: core mạnh + core tiết kiệm điện — Unity chạy chủ yếu trên big cores
- **Storage read ~100MB/s** vs RAM — load nhiều file nhỏ chậm hơn ít file lớn
