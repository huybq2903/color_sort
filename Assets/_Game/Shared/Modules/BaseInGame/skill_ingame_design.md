# Unity In-Game Core — Reusable Architecture Guide

> Adapted from production code in a shipped mobile game.
> Patterns extracted and generalized for use across any Unity game project.

---

## 1. Overview

This document describes a modular in-game core architecture designed for **cross-level reuse**. Instead of writing unique code per level, each level composes a set of shared modules. The patterns here are **generic** — they apply to puzzle games, platformers, RPGs, tower defense, or any genre that uses levels with entities, state management, and event-driven gameplay.

---

## 2. Directory Structure

Three layers, from most reusable to most disposable: **Shared modules** (cross-project) → **InGame/Core** (every level has it) → **feature folders** (Obstacles, Booster — removable).

```
_Game/
├── Shared/Modules/           # Cross-project base. Copy sang game khác không cần sửa.
│   ├── BaseInGame/           # AEntityBehaviour, LevelData/LevelRuntime, InGameManager, ISubManager, [Inject]
│   ├── BaseObstacles/        # ABaseObstacleLogic, AObstacleManager
│   ├── BaseBoosters/         # ABoosterLogic, ABoosterManager, UI booster dùng chung
│   └── BaseLevelEditor/      # Command, ModeAction, imgui root
│
└── InGame/
    ├── Core/                 # Thứ CHẮC CHẮN có trong mọi màn, mọi build
    │   ├── Common/           # Enum, ColorConfig, interface (IBlockable...), material, model, prefab dùng chung
    │   ├── Manager/          # Các ISubManager: InGameLevel, InGameWinLose, InGameColor, InGameEffect...
    │   ├── <EntityA>/        # 1 folder / 1 entity: Behaviour + Data + Runtime (+ Effects, Renderer)
    │   ├── <EntityB>/
    │   └── LevelParam/       # Tham số cân bằng của level
    │
    ├── Obstacles/            # Mỗi obstacle 1 folder, tắt/xoá được mà Core vẫn chạy
    │   ├── InGameObstaclesManager.cs
    │   ├── ObstacleConst.cs
    │   └── O<Name>/Scripts/  # <Name>ObstacleData.cs + <Name>ObstacleLogic.cs (+ behaviour riêng)
    │
    ├── Booster/              # Mỗi booster 1 logic, tắt/xoá được mà Core vẫn chạy
    │   └── Scripts/{Data, Logic, UI}
    │
    ├── LevelEditor/          # Phần editor game-specific, dựng trên BaseLevelEditor
    ├── Tutorials/
    └── UI/{Prefabs, Scripts} # HUD + popup in-game
```

### 2.1 Cái gì vào Core, cái gì không

| Vào `Core/` | Ra folder riêng |
|---|---|
| Entity xuất hiện ở **mọi** level (bus, ô/tile, pax, board) | Entity chỉ xuất hiện khi bật 1 feature |
| Manager mà không có nó thì màn không load được | Cơ chế bật/tắt theo level hoặc theo remote config |
| Luật thắng/thua, input, camera | Booster, obstacle, event mùa vụ |

**Phép thử:** xoá cả folder `Obstacles/` và `Booster/` — nếu level vẫn load và chơi được thì Core đúng ranh giới. Nếu compile lỗi, có code Core đang gọi ngược lên feature → sửa lại bằng event hoặc interface trong `Core/Common/`.

### 2.2 Chiều phụ thuộc (một chiều, không có ngoại lệ)

```
Shared/Modules  ←  InGame/Core  ←  Obstacles / Booster / Tutorials / UI
```

- Feature được `using Falcon.InGame.Core` thoải mái.
- Core **không bao giờ** `using Falcon.InGame.Obstacles` hay `.Booster`.
- Core cần phản ứng với feature → phát `GameEvent` / `Messenger<T>`, hoặc khai báo interface trong `Core/Common/` cho feature implement.

### 2.3 Một entity = một folder

```
Core/Bus/
├── BusBehaviour.cs          # MonoBehaviour, kế thừa AEntityBehaviour<BusData, BusRuntime>
├── BusData.cs               # Config từ level JSON, read-only lúc chạy
├── BusRuntimeProperty.cs    # State mutable, save được
├── BusDataProperty.cs       # Mảnh data cấp level (danh sách bus, thứ tự...)
└── BusEffects.cs            # VFX / animation, tách khỏi Behaviour
```

Đặt tên bám entity, không bám vai trò: `BusEffects` chứ không phải `EffectsManager` nằm ở `Common/`.

### 2.4 Một feature = một folder

Obstacle và booster đi theo cùng một khuôn — base ở `Shared/Modules`, cụ thể ở `InGame/`:

```
Obstacles/OLockBus/Scripts/
├── LockBusObstacleData.cs   # : ABaseObstacleData — property trong level JSON
├── LockBusObstacleLogic.cs  # : ABaseObstacleLogic<D,R> — KHÔNG phải MonoBehaviour
└── LockCaseBehaviour.cs     # Visual do logic tự spawn, tự dọn trong Dispose()
```

Thêm obstacle mới = thêm 1 folder + 1 attribute `[PropertyDataType(...)]`. Không sửa file nào có sẵn.

---

## 3. Core Patterns

### 3.1 Entity Abstraction — Data/Runtime Split

**The pattern:** Separate **level design data** (serialized, read-only) from **runtime state** (mutable, saveable).

```csharp
// Generic base: TData = serialized config, TRuntime = mutable live state
public abstract class AEntityBehaviour<TData, TRuntime> : MonoBehaviour
    where TData : new()
    where TRuntime : new()
{
    // TData: loaded once from level config, never mutated at runtime
    public TData Data { get; private set; }

    // TRuntime: mutable game state, NonSerialized for manual save/load
    public TRuntime Runtime { get; private set; }
}
```

**Concrete example — a character entity:**
```csharp
// Serialized config (editable in editor or loaded from level file)
public class CharacterData
{
    public string id;
    public Vector2[] path;       // path coordinates
    public int health;
    public int speed;
}

// Mutable live state (serialized for save/load)
public class CharacterRuntime
{
    public int currentHealth;
    public int direction;        // -1, 0, 1
    public bool isAlive;
    public float moveProgress;   // 0.0 to 1.0 along path
}

// MonoBehaviour that ties them together
public class CharacterBehaviour : AEntityBehaviour<CharacterData, CharacterRuntime>
{
    // Rendering is delegated, never mixed into the entity
    public CharacterRenderer Renderer { get; private set; }

    public override void Setup(CharacterData data)
    {
        base.Setup(data);
        Runtime.currentHealth = Data.health;
        Runtime.isAlive = true;
    }
}
```

**Key rules:**
1. `TData` fields are serializable and loaded from level data. **Never mutate them at runtime.**
2. `TRuntime` fields contain only mutable game state. Mark with `[NonSerialized]` if you handle your own serialization.
3. Rendering, physics, and VFX are always in separate classes (`CharacterRenderer`, `CharacterPhysics`).
4. Use a dummy `NullData` type for entities that don't need serialized config (UI panels, temporary spawn points).

**Why this matters:**
- Level designers can tweak `TData` without touching runtime code.
- Save/load only serializes `TRuntime`, which is a predictable, bounded set of fields.
- `TData` is testable in EditMode (no Unity runtime needed).
- Prevents accidental data mutation bugs — the compiler enforces the split.

---

### 3.2 Manager Lifecycle — SubManager Pattern

**The pattern:** A central host `MonoBehaviour` auto-collects sibling modules, injects their dependencies, then initializes them. Each module implements a minimal contract. There is **no manual `Register`/`Get`** — the host discovers managers via `GetComponents` and wires dependencies by reflection.

```csharp
public interface ISubManager
{
    // Default empty body — called once after all managers are collected AND injected.
    // All managers may safely call each other here.
    public void Initialized() { }
}

// Marks a field to be filled with the matching ISubManager during Awake.
[AttributeUsage(AttributeTargets.Field)]
public sealed class InjectAttribute : Attribute { }

[DefaultExecutionOrder(-100)]  // runs before other components' Awake
public class InGameManager : MonoBehaviour
{
    private readonly Dictionary<Type, ISubManager> _managers = new();

    private void Awake()
    {
        // 1. Collect every ISubManager living on the same GameObject.
        foreach (var manager in GetComponents<ISubManager>())
            _managers[manager.GetType()] = manager;

        // 2. Inject [Inject] fields on every manager (all are registered now).
        foreach (var manager in _managers.Values)
            Inject(manager);

        // 3. Initialize — every dependency is already wired.
        foreach (var manager in _managers.Values)
        {
            try { manager.Initialized(); }
            catch (Exception e) { Debug.LogWarning($"Init failed {manager.GetType().Name}: {e}"); }
        }
    }

    // Walk the type hierarchy, fill each [Inject] field with the manager of its field type.
    private void Inject(ISubManager target)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (var t = target.GetType(); t != null; t = t.BaseType)
            foreach (var field in t.GetFields(flags))
            {
                if (!Attribute.IsDefined(field, typeof(InjectAttribute))) continue;
                if (_managers.TryGetValue(field.FieldType, out var dep))
                    field.SetValue(target, dep);
                else
                    Debug.LogWarning($"[Inject] {t.Name}.{field.Name}: no manager {field.FieldType.Name}");
            }
    }
}
```

**Lifecycle:**
```
1. InGameManager.Awake() collects all sibling ISubManager components (GetComponents)
2. Inject() fills every [Inject] field by field type — dependencies resolved up front
3. Initialized() called on each — managers subscribe to events, load assets, spawn prefabs
4. Level unloads → each manager's OnDisable unsubscribes and cleans up
```

**Manager decomposition (single responsibility each):**

| Category | Lives in | Examples |
|----------|----------|----------|
| **Level** | `Core/Manager/` | `InGameLevel` — data loading, resume logic, runtime persistence |
| **World** | `Core/Manager/` | `InGameWorld` — spatial layout, tile mapping |
| **Camera** | `Core/Manager/` | `InGameCamera` — pan, zoom, bounds fitting |
| **Input** | `Core/Manager/` | `InGameInput` — touch/mouse, raycasting, selection |
| **Entities** | `Core/Manager/` | `InGame<Entity>Manager` — pool, spawn, lifecycle |
| **Gameplay** | `Core/Manager/` | `InGameWinLose` — win/lose conditions, scoring, hints |
| **Obstacles** | `Obstacles/` | `InGameObstaclesManager` — chủ sở hữu mọi obstacle logic |
| **Booster** | `Booster/Scripts/Logic/` | `InGameBoosterManager` — chủ sở hữu mọi booster logic |
| **Tutorial** | `Tutorials/Scripts/` | `InGameTutorialsManager` — step activation, auto-start checks |

Feature manager (obstacle/booster/tutorial) vẫn là `ISubManager` trên cùng host GameObject và vẫn `[Inject]` được manager của Core — chỉ là **file nằm ngoài Core**, và Core không biết chúng tồn tại. Chúng cũng là **cửa duy nhất** để logic feature (plain C# class, không `[Inject]` được) chạm tới Core:

```csharp
// InGameObstaclesManager — gom dependency rồi mở property cho logic dùng
[Inject] private InGameLevel _levelManager;
public InGameLevel LevelManager => _levelManager;
```

**Dependency resolution:**
```csharp
// Declare the dependency as an [Inject] field — the host fills it before Initialized().
// No FindObjectOfType, no manual Get<T>() lookups.
[Inject] private InGameLevel _levelManager;
[Inject] private ColorManager _colorManager;

public void Initialized()
{
    // _levelManager / _colorManager are already wired here.
}
```

**Why this instead of DI:**
- Constructor injection doesn't work with Unity components on GameObjects, so a host-driven `[Inject]` pass fills fields by type instead.
- Two passes (inject all, then `Initialized()` all) mean every manager sees its dependencies already resolved — no ordering juggling, no null guards.
- Easy to extend: add a new manager component to the host GameObject and declare its deps with `[Inject]`. No registration call, no refactoring of existing code.
- Cost: one reflection scan of fields per manager at `Awake`. Negligible for a few dozen managers; cache `type → fields` only if it ever shows up in a profile.

---

### 3.3 Event System

**The pattern:** Use `Messenger<T>` for typed, in-process signals and `GameEvent<T>` for string-keyed global events. Prefer small value struct messages for gameplay signals so event payloads stay cheap and type-safe.

**Typed Messenger events:**
```csharp
// Event structs defined near the entity/system that produces them
public struct OnEntitySpawned
{
    public readonly string entityId;

    public OnEntitySpawned(string entityId)
    {
        this.entityId = entityId;
    }
}

public struct OnPlayerInput
{
    public readonly Vector2 direction;

    public OnPlayerInput(Vector2 direction)
    {
        this.direction = direction;
    }
}

// Emit — type-safe, no string key
Messenger<OnEntitySpawned>.Emit(new OnEntitySpawned("character_01"));

// Register / unregister with the same handler
Messenger<OnEntitySpawned>.Register(OnEntitySpawnedHandler);
Messenger<OnEntitySpawned>.Unregister(OnEntitySpawnedHandler);
```

**Global string-keyed events (cross-module):**
```csharp
// Emit
GameEvent.Emit("game.level.start");
GameEvent<int>.Emit("game.resource.gold", 100);

// Register with a listener owner for editor tracking
GameEvent.Register("game.level.start", OnLevelStart, this);
GameEvent.Unregister("game.level.start", OnLevelStart, this);
```

**Event struct conventions:**
- Always `public struct`, always `readonly` fields
- Name: `On` + verb + noun (`OnEntitySpawned`, `OnLevelComplete`)
- Never use classes for events (GC allocation)
- Keep structs small (1-2 fields max)
- Define event structs in the same file or namespace as the system that produces them
- If fields are `readonly`, provide a constructor. Do not use object initializers for readonly fields.

**Messenger lifecycle rules:**
1. Register listeners in `Initialized()`, `OnEnable()`, or the method that owns the listener lifetime.
2. Always unregister in the matching cleanup method (`OnDisable()`, `OnDestroy()`, or manager teardown).
3. `Messenger<T>` has no owner parameter and no automatic cleanup, so leaked handlers stay in the static delegate.
4. Use `Messenger<T>` only when publisher and listener agree on the message type at compile time.

**When to use which:**
| Pattern | Use when |
|---------|----------|
| `Messenger<T>` typed event | Same-module or same-subsystem communication where the message type is the contract |
| `GameEvent<T>` string-keyed | Cross-module communication where systems share an event name constant |

---

### 3.4 Object Pooling

**The pattern:** Pre-instantiate objects and reuse them to eliminate `Instantiate`/`Destroy` during gameplay.

```csharp
// Generic pool — works with any MonoBehaviour
var pool = new ObjectPool<EntityBehaviour>(prefab, parent);

// Acquire
var entity = pool.Get();
entity.Setup(data);

// Release (return to pool, deactivate)
pool.Release(entity);
```

**When to pool:**
- Entities that spawn frequently (bullets, enemies, particles, items)
- UI elements that show/hide repeatedly (damage numbers, tooltips)
- Temporary systems (off-screen indicators, debug visuals)

**Pool configuration:**
- Set a `parent` transform during pooling to keep the hierarchy clean.
- Pool prefabs should start deactivated; the pool activates them on `Get()`.
- Always call `Release()` even if the object is "dead" — the pool handles cleanup.

---

### 3.5 Level Runtime — Persistent State

**The pattern:** Store gameplay state in a typed property system that survives level reloads and scene transitions.

```csharp
// Each property is a serializable container for a specific domain
public class StatsRuntimeProperty : PropertyRuntime
{
    public float durationPlayed;
    public int goldSpent;
    public int revivesUsed;
    public Dictionary<string, int> boostersUsed;
}

public class EntityStateRuntimeProperty : PropertyRuntime
{
    public List<EntityRuntimeData> entities;  // Per-entity snapshot
}
```

**Usage:**
```csharp
// Read
var stats = LevelRuntime.GetProperty<StatsRuntimeProperty>();
stats.durationPlayed += Time.deltaTime;

// Write
LevelRuntime.SetProperty(stats);

// Clear at level end
LevelRuntime.ClearAllProperties();
```

**Property types you'll need:**
| Property | Contains |
|----------|----------|
| `StatsRuntimeProperty` | Score, duration, currency, boosts |
| `EntityStateRuntimeProperty` | Per-entity live state (health, position, state) |
| `FlagsRuntimeProperty` | Boolean flags for tutorial steps, unlocks, achievements |

**Why this matters:**
- Survives `LoadScene` (unlike MonoBehaviour state which is lost).
- Type-safe — `GetProperty<T>()` returns a strongly-typed object.
- Extensible — add a new property class without touching existing code.
- Serialize/deserialize the whole `LevelRuntime` for save/load or replay.

---

## 4. Patterns by Game Genre

| Genre | Which patterns to prioritize | Skip or simplify |
|-------|-----------------------------|------------------|
| **Platformer** | Entity abstraction, manager lifecycle, object pooling, event system | Level runtime only if save/load needed |
| **RPG** | Level runtime, entity abstraction, event system | Object pooling optional (fewer frequent spawns) |
| **Tower Defense** | Entity abstraction, event system | Object pooling if enemy spawn is fast |
| **Card Game** | Level runtime, event system | Object pooling optional |
| **Endless Runner** | Entity abstraction, object pooling, event system | Level runtime for high scores only |

**Rule of thumb:** Start with patterns 3.1–3.4 (entity, manager, events, pooling). Add Level Runtime only if you need save/load across scene loads.

---

## 5. Anti-Patterns to Avoid

### Don't mutate TData at runtime

```csharp
// BAD — TData is level config, should be read-only
entity.Data.health = 50;

// GOOD — Use TRuntime for mutable state
entity.Runtime.currentHealth = 50;
```

### Don't use classes for events

```csharp
// BAD — Allocates on GC heap
public class OnEntitySpawned { public string entityId; }

// GOOD — Stack-allocated payload with explicit initialization
public struct OnEntitySpawned
{
    public readonly string entityId;

    public OnEntitySpawned(string entityId)
    {
        this.entityId = entityId;
    }
}
```

### Don't skip unregistering events

Every `Messenger<T>.Register` must have a corresponding `Messenger<T>.Unregister` in the matching cleanup method. Every `GameEvent.Register` / `GameEvent<T>.Register` must also be paired with the matching `Unregister`. Leaked static subscriptions can keep old scene objects alive and trigger null reference exceptions after scene changes.

### Don't use FindObjectOfType for cross-manager access

```csharp
// BAD — Brittle, slow, hard to test
var grid = FindObjectOfType<GridManager>();

// GOOD — Injected dependency, filled by the host before Initialized()
[Inject] private GridManager _grid;
```

### Don't mix rendering into entity classes

```csharp
// BAD — Entity class knows about sprites, shaders, animations
public class CharacterBehaviour
{
    public SpriteRenderer sprite;
    public void PlayAttackAnimation() { ... }
}

// GOOD — Rendering delegated to separate class
public class CharacterBehaviour : AEntityBehaviour<CharacterData, CharacterRuntime>
{
    public CharacterRenderer Renderer { get; private set; }
}
```

### Don't let Core reference a feature folder

```csharp
// BAD — Core/Manager/InGameWinLose.cs
using Falcon.InGame.Obstacles;
if (_obstacles.GetLogic<LockBusObstacleLogic>()?.IsCleared == true) Win();

// GOOD — Core chỉ biết một điều kiện trừu tượng, feature tự đăng ký vào
public interface IWinCondition { bool IsSatisfied { get; } }   // Core/Common/
_conditions.Add(this);                                          // obstacle logic tự add
```

Một `using` ngược chiều là đủ để `Obstacles/` không xoá được nữa, và game sau phải kéo theo obstacle của game này.

### Don't create a God Manager

Keep managers focused. A manager that handles win/lose conditions, tutorial logic, analytics, AND popup management is too big. Split it.

---

## 6. Code Quality Conventions

| Category | Convention |
|----------|------------|
| **Event structs** | `public struct`, `readonly` fields, `On` prefix (`OnEntitySpawned`) |
| **Entity classes** | `XxxBehaviour` for MonoBehaviour, `XxxData` for config, `XxxRuntime` for live state |
| **Manager classes** | `XxxManager` (e.g. `GridManager`, `InputManager`) |
| **Gameplay** | `XxxManager` (e.g. `GameStateManager`, `GameManager`) |
| **Constants** | Static class with descriptive names, **no magic strings** |
| **Unity editor debug** | `#if UNITY_EDITOR` blocks for test buttons and debug GUI |
| **Null checks** | Early return pattern, `GetValueOrDefault()` for dictionaries |
| **Async** | Prefer `UniTask` over `async void`. Use `AttachExternalCancellation` with `CancellationTokenSource`. |
| **Naming** | Use consistent naming: `Setup(data)` for initialization, `OnDisable()` for cleanup |

---

## 7. Getting Started Checklist

`Shared/Modules/BaseInGame` + `BaseObstacles` + `BaseBoosters` đã có sẵn — copy nguyên folder, đừng viết lại. Việc còn lại:

1. **Dựng khung thư mục** — `InGame/Core/{Common, Manager}`, để trống `Obstacles/` và `Booster/` cho tới khi thật sự cần.
2. **Xác định entity Core** — thứ có mặt ở mọi level. Mỗi cái một folder trong `Core/`.
3. **Viết entity đầu tiên** — `PlayerBehaviour : AEntityBehaviour<PlayerData, PlayerRuntime>`, tách `PlayerEffects` ngay từ đầu.
4. **Viết `InGameLevel`** — load `LevelData`, dựng `LevelRuntime`, phát event vòng đời.
5. **Thêm manager** — mỗi trách nhiệm một component trên host GameObject, deps khai bằng `[Inject]`, code khởi tạo nằm trong `Initialized()`.
6. **Nối event** — `Messenger<T>` trong cùng subsystem, `GameEvent<T>` khi bắn qua module khác.
7. **Thêm object pool** — chỉ cho entity spawn liên tục.
8. **Thêm obstacle/booster đầu tiên** — folder riêng, kế thừa base ở `Shared/Modules`, không đụng vào Core.
9. **Kiểm tra ranh giới** — xoá tạm `Obstacles/` và `Booster/`, phải vẫn compile và chơi được.

---

## 8. Dependencies

The patterns described here have **minimal dependencies**:

| Dependency | Purpose | Can replace with |
|------------|---------|------------------|
| `UniTask` | Async/await without coroutines | `async/await` + `Task`, or Unity coroutines |
| `DOTween` | Animation | Unity `Tween`, `LeanTween`, or manual `Mathf.Lerp` |
| `Odin Inspector` | `[ShowInInspector]`, `[Button]` | Custom `[CustomEditor]` scripts |
| `Addressables` | Asset loading | `Resources.Load`, `AssetBundle` |
| `TMPro` | Text rendering | Unity `UILabel`, `NGUI` |

**Zero required dependencies:** The core patterns (entity abstraction, manager lifecycle, event system, object pooling, level runtime) work with **pure Unity** — no third-party packages needed.

---

## 9. File Reference (Canonical Locations)

**Shared/Modules — không sửa khi làm game mới:**

| File | Purpose |
|------|---------|
| `BaseInGame/Manager/ISubManager.cs` | Manager lifecycle contract (`Initialized()`) |
| `BaseInGame/Manager/InjectAttribute.cs` | `[Inject]` field marker for dependency injection |
| `BaseInGame/Manager/InGameManager.cs` | Host: collects managers, injects deps, calls `Initialized()` |
| `BaseInGame/Behavior/AEntityBehaviour.cs` | Entity base — data/runtime split |
| `BaseInGame/Data/LevelData.cs` | Level config schema (entities + properties) |
| `BaseInGame/Data/LevelRuntime.cs` | Typed property state persistence |
| `BaseInGame/Data/PropertyData.cs` / `PropertyRuntime.cs` | Per-feature slice of level data / state |
| `BaseObstacles/Scripts/Logic/ABaseObstacleLogic.cs` | Obstacle base — `Prepare()` / `Initialize()` / `Dispose()` |
| `BaseObstacles/Scripts/Logic/AObstacleManager.cs` | Discovers obstacle logics by `PropertyDataType` |
| `BaseBoosters/Scripts/Logic/ABoosterLogic.cs` | Booster base — unlock level, cost, confirm flow |
| `BaseBoosters/Scripts/Logic/ABoosterManager.cs` | Booster registry + resource wiring |
| `Common/FObjectPool.cs` | Generic object pooling |
| `Common/Center/Messenger.cs` | Typed in-process event bus |

**InGame — viết mới mỗi game:**

| File | Purpose |
|------|---------|
| `Core/Manager/InGameLevel.cs` | Level data loading, resume, runtime |
| `Core/Manager/InGameWinLose.cs` | Win/lose conditions |
| `Core/Manager/InGame<X>.cs` | Một manager một trách nhiệm, prefix `InGame` |
| `Core/Common/GameEnums.cs` | Enum dùng chung của gameplay |
| `Core/Common/Scripts/*.cs` | Interface, config, blocker — Core-level, không entity nào sở hữu |
| `Core/<Entity>/<Entity>Behaviour.cs` | Entity + data + runtime split |
| `Core/<Entity>/<Entity>Effects.cs` | VFX/animation, tách khỏi entity |
| `Obstacles/O<Name>/Scripts/<Name>ObstacleLogic.cs` | Một obstacle, plain C# class |
| `Booster/Scripts/Logic/Booster<Name>Logic.cs` | Một booster, plain C# class |
