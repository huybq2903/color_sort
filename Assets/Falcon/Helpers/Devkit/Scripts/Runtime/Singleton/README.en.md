**English** | [Tiếng Việt](README.md)
-----

# Unity Singleton Framework

[](https://opensource.org/licenses/MIT)

Welcome to the **Unity Singleton Framework**\! This powerful library is designed to simplify Singleton management and implement **Dependency Injection (DI)** in your Unity projects. It automates instance creation and dependency resolution, letting you focus on game logic instead of boilerplate code. The result? Cleaner, more maintainable, and highly extensible code for your core game components.

-----

## 🌟 Key Features

* **Automatic Instance Creation & Management:** Singletons are created automatically when needed, ensuring a single, unique instance exists.
* **Flexible Dependency Injection:**
    * Automatically injects other Singletons into each other using **Attributes** or **Constructors**.
    * Supports injecting **collections (Arrays/Lists)** of Singletons of the same type (implementing the same interface or inheriting from the same abstract class). The framework automatically discovers and provides all suitable instances.
* **Supports Both MonoBehaviour and Plain C\# Classes:** Offers the flexibility to integrate Singletons into any part of your architecture.
* **Post-Injection Initialization:** Provides a mechanism to execute initialization logic immediately after all dependencies have been fully injected.
* **Dependency Conflict Resolution:** Offers a mechanism to specify which Singleton to use when multiple classes can satisfy the same dependency.
-----

## 🛠️ Usage

The library distinguishes between two main types of Singletons, each with its own usage and dependency injection mechanism.

### 1\. MonoBehaviour Singletons (GameObject Components)

These are Singletons that need to function as a `Component` within Unity (e.g., requiring `Update`, `FixedUpdate`, `Coroutine`, etc.).

* **Requirement:** Inherit from the abstract class `MonoSingleton`.
* **Dependency Injection:** Use the **`[Inject]`** attribute on fields that need dependencies. The framework will automatically find and assign the dependent Singleton's instance to this field.
* **Instance Creation:** `MonoSingleton` instances will be automatically added (as Components) to a central GameObject (managed by the framework) when they are first requested.
* **Post-Injection Initialization:** If you need to perform initialization logic immediately after all dependencies have been injected, implement the `IPostInjectSingleton` interface and perform your logic in the `OnPostInject()` method.

#### Simple Example: `SoundManager` and `GameEventsHandler`

Suppose you have a `SoundManager` to manage sounds and a `GameEventsHandler` that needs to play a sound when an event occurs.

```csharp
// --- Usage Examples ---

// SoundManager.cs
using UnityEngine;
using MyUnitySingletonFramework.Core;

public class SoundManager : MonoSingleton
{
    public void PlayClickSound()
    {
        Debug.Log("Playing click sound!");
    }
}

// GameEventsHandler.cs
using UnityEngine;
using System;
using MyUnitySingletonFramework.Core;

public class GameEventsHandler : MonoSingleton, IPostInjectSingleton
{
    // Dependency Injection: SoundManager will be automatically found and assigned here
    [Inject] private SoundManager _soundManager;

    public static event Action OnGameRestart;

    // This method is called right after all [Inject] dependencies have been filled
    public void OnPostInject()
    {
        Debug.Log("GameEventsHandler: Dependencies injected. Subscribing to events...");
        OnGameRestart += HandleGameRestart;
    }

    private void HandleGameRestart()
    {
        Debug.Log("Game Restarted! Triggering sound.");
        _soundManager.PlayClickSound(); // Use the injected Singleton
    }

    public void SimulateGameRestart()
    {
        OnGameRestart?.Invoke();
    }
}
```

### 2\. Normal C\# Singletons (Plain C\# Objects)

These are Singletons that don't require Unity's MonoBehaviour functionality; they are just regular C\# classes.

* **Requirement:** Implement the `IMySingleton` interface.
* **Dependency Injection:** Dependencies are provided via the class's **constructor**. The framework will automatically resolve and provide these dependencies.
* **Instance Creation:** `IMySingleton` instances are created by calling their constructor with the resolved dependencies.
* **Multiple Constructors:** If your class has more than one constructor, mark the primary constructor that the framework should use with the **`[SingletonConstructor]`** attribute.

#### Simple Example: `Logger` and `ConfigManager`

Suppose you have a `Logger` for logging and a `ConfigManager` that depends on the `Logger` for notifications.

```csharp
// --- Usage Examples ---

// Logger.cs
using System;
using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces;

public class Logger : IMySingleton
{
    public void Log(string message)
    {
        Console.WriteLine($"[Logger] {message}");
    }

    public void LogError(string message)
    {
        Console.Error.WriteLine($"[ERROR] {message}");
    }
}

// ConfigManager.cs (Example with multiple constructors and [SingletonConstructor])
using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces;
using MyUnitySingletonFramework.Core; // For [SingletonConstructor]
using System;

public class ConfigManager : IMySingleton
{
    private readonly Logger _logger;
    private readonly string _configSource;

    // Secondary constructor (not used for main DI)
    public ConfigManager(string configSource)
    {
        _configSource = configSource;
        Console.WriteLine($"[ConfigManager] Initialized from source: {configSource}");
    }

    // Primary constructor that the framework will use to inject Logger
    [SingletonConstructor]
    public ConfigManager(Logger logger)
    {
        _logger = logger;
        _configSource = "Default Source"; // Default value when injected
        _logger.Log($"[ConfigManager] Initialized via DI with Logger. Source: {_configSource}");
    }

    public string GetSetting(string key)
    {
        _logger.Log($"Retrieving setting for key: {key}");
        // ... config retrieval logic ...
        return $"Value for {key}"; // Mocked
    }
}
```

### 3\. Injecting Singleton Collections

The library allows you to inject a collection (`Array` or `List<T>`) of all registered Singletons that implement a specific interface or inherit from an abstract class.

#### Example: `IWeapon` and `WeaponRegistry`

```csharp
// IWeapon.cs
public interface IWeapon
{
    string Name { get; }
    void Fire();
}

// Rifle.cs (is a MonoSingleton and IWeapon)
using UnityEngine;
using MyUnitySingletonFramework.Core;

public class Rifle : MonoSingleton, IWeapon
{
    public string Name => "Rifle";
    public void Fire() { Debug.Log("Rifle fires!"); }
}

// Pistol.cs (is an IMySingleton and IWeapon)
using UnityEngine;
using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces; // For IMySingleton
using MyUnitySingletonFramework.Core; // For SingletonConstructor (if needed)

public class Pistol : IMySingleton, IWeapon
{
    public string Name => "Pistol";
    public void Fire() { Debug.Log("Pistol fires!"); }
}

// WeaponRegistry.cs
using System.Collections.Generic;
using UnityEngine;
using MyUnitySingletonFramework.Core;

public class WeaponRegistry : MonoSingleton, IPostInjectSingleton
{
    // The framework will automatically find all Singletons (MonoSingleton or IMySingleton)
    // that implement IWeapon and inject them here as a List.
    [Inject] private List<IWeapon> _allWeapons;

    public void OnPostInject()
    {
        Debug.Log($"WeaponRegistry initialized with {_allWeapons.Count} weapons:");
        foreach (var weapon in _allWeapons)
        {
            Debug.Log($"- Found Weapon: {weapon.Name}");
            // weapon.Fire(); // Can call methods on each weapon
        }
    }
}
```

### 4\. Direct Static Instance Access (Limited Use Recommended)

While the framework encourages dependency injection for cleaner and more testable code, you can still directly access a Singleton's instance via a static `Instance` property.

* **For Plain C\# Singletons (IMySingleton):** Inherit from `MySingleton<SelfType>`.
    * Example: `public class AdLogService : MySingleton<AdLogService>`
    * Access: `AdLogService.Instance`
* **For MonoBehaviour Singletons (MonoSingleton):** Inherit from `MonoBehaviourSingleton<SelfType>`.
    * Example: `public class CameraController : MonoBehaviourSingleton<CameraController>`
    * Access: `CameraController.Instance`

**IMPORTANT NOTE:** Direct `Instance` access creates tight coupling and reduces the effectiveness of the Dependency Injection mechanism provided by the framework. **Avoid doing this too frequently** if possible, and prioritize using `[Inject]` or constructor injection to fully leverage the framework's benefits.

### 5\. Resolving Dependency Conflicts

When a dependency requests an `Interface` or `Abstract Class` that has more than one registered Singleton implementing/inheriting it, the framework won't know which Singleton to choose. You can resolve this ambiguity using the **`[Primary]`** attribute.

* **Mark with `[Primary]`:** Place the `[Primary]` attribute on the Singleton class that you want the framework to choose by default when multiple options exist.
* **Priority:** The `[Primary]` attribute has a `Priority` field (defaulting to 0). If multiple classes are marked `[Primary]`, the framework will select the class with the **highest `Priority`** value.

<!-- end list -->

```csharp
// In a separate file: PrimaryAttribute.cs (already defined above)
// using System;
// namespace MyUnitySingletonFramework.Core
// {
//     [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
//     public class PrimaryAttribute : Attribute
//     {
//         public int Priority { get; set; } = 0;
//         public PrimaryAttribute(int priority = 0) { Priority = priority; }
//     }
// }

// --- Usage Examples ---

// ILogger (re-defined for this example)
public interface ILogger
{
    void Log(string message);
}

// ConsoleLogger.cs
using System;
using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces; // For IMySingleton
using MyUnitySingletonFramework.Core; // For [Primary]

[Primary(priority: 10)] // This will be the preferred Logger
public class ConsoleLogger : IMySingleton, ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[ConsoleLogger] {message}");
    }
}

// FileLogger.cs
using System;
using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces; // For IMySingleton
using MyUnitySingletonFramework.Core; // For [Primary] (not needed here, just to illustrate)

// Without [Primary] or with lower Priority, it won't be chosen when ILogger is requested
public class FileLogger : IMySingleton, ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[FileLogger] {message} (written to file)");
    }
}

// MyService.cs (requests a single ILogger)
using MyUnitySingletonFramework.Core; // For [Inject]
using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces; // For IMySingleton

public class DataProcessor : IMySingleton
{
    // The framework will automatically inject ConsoleLogger here
    // because it's marked [Primary] with the highest Priority.
    [Inject] private ILogger _logger;

    [SingletonConstructor]
    public DataProcessor(ILogger logger) // Can also be constructor injected
    {
        _logger = logger;
        _logger.Log("DataProcessor initialized and ready.");
    }

    public void ProcessData(string data)
    {
        _logger.Log($"Processing data: {data}");
    }
}
```

-----

## 💡 How It Works (Overview)

The framework operates by scanning your application's assemblies to automatically identify classes that inherit from `MonoSingleton` (or `MonoBehaviourSingleton<T>`) or implement `IMySingleton` (or `MySingleton<T>`).

1.  **Automatic Registration:** When the game starts (or when triggered), the library scans your Singleton types and registers them in an internal Container.
2.  **Dependency Resolution:** When a Singleton is first requested (or when its dependencies need to be injected), the framework checks its constructor (`[SingletonConstructor]`) or fields marked with `[Inject]`. It then uses the Container to automatically create or provide instances of the dependent Singletons.
3.  **Lifecycle Management:**
    * For `MonoSingleton` (`MonoBehaviourSingleton<T>`) instances, they are added to a central managing GameObject, and their lifecycle is handled by Unity.
    * For `IMySingleton` (`MySingleton<T>`) instances, their lifecycle is fully managed by the framework.

-----

## ✅ Benefits of Using This Framework

* **Clean Code & Clear Structure:** Eliminates the need for scattered static `Singleton.Instance` calls, making your code much more readable and traceable.
* **Enhanced Testability:** Dependency injection makes it easy to mock or substitute dependencies during unit testing, making your code more robust.
* **Reduced Boilerplate:** No need to write manual code for Singleton initialization and management, saving you time and effort.
* **Flexible Extensibility:** Easily add new Singleton types and their dependencies without extensive code changes, promoting rapid and safe development.

-----