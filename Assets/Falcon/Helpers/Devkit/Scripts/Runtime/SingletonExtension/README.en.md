**English** | [Tiếng Việt](README.md)
-----
# Unity Singleton Lifecycle Management Module



[](https://opensource.org/licenses/MIT)



Welcome to the **Unity Singleton Lifecycle Management Module**\! This module extends the core Unity Singleton Framework by providing structured lifecycle management for your Singletons. It allows you to define clear initialization, pre-continuation, post-termination, and **scene transition** logic for your Singletons, ensuring specific execution orders based on their dependencies.



-----



## 🌟 Key Features



* **Structured Initialization (`IInit`):** Define asynchronous startup logic for Singletons.

* **Pre-Continue Hooks (`IPioneer`):** Execute code when the application resumes from a paused state.

* **Post-Stop Hooks (`ITerminal`):** Perform cleanup or finalization tasks when the application pauses or stops.

* **Scene Transition Hooks (`ISceneSingleton`):** Respond to scene changes within your application.

* **Dependency-Aware Execution Order:** Automatically handles the order of execution for lifecycle methods based on your Singleton's dependencies.



-----



## 🚀 Getting Started



To use this module, simply ensure you have the core [Unity Singleton Framework](https://www.google.com/search?q=https://github.com/your-repo/your-singleton-framework%23unity-singleton-framework) set up in your project.



-----



## 🛠️ Usage



This module introduces three key interfaces that your Singletons (both MonoBehaviour-based and plain C\#) can implement to hook into specific application lifecycle events.



### 1\. Initializing Singletons with `IInit`



Any Singleton (whether inheriting from `MonoSingleton` or implementing `IMySingleton`) that also implements `IInit` will have its `Init(CancellationToken)` method called automatically during Unity startup.



* **Purpose:** Ideal for asynchronous initialization tasks, loading data, or setting up external SDKs.

* **Execution Order:** `Init` methods are called in a **bottom-up** fashion. This means if Singleton A depends on Singleton B (via `[Inject]` or constructor injection), B's `Init` will be called and awaited before A's `Init`. This ensures all dependencies are fully initialized and ready before their consumers begin their own initialization.



<!-- end list -->



```csharp

// Example Implementation

using UnityEngine;

// Assuming your core framework types and namespaces are accessible

using MyUnitySingletonFramework.Core;

using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces; // For IMySingleton

using System.Threading;

using System.Threading.Tasks;



// A simple Logger for demonstration

public class Logger : IMySingleton

{

public void Log(string message) { Debug.Log(message); }

}



// SaveService.cs (A plain C# Singleton that needs initialization)

public class SaveService : IMySingleton, IInit

{

// Logger will be injected via constructor, so it's ready when Init is called

[Inject] private Logger _logger;



// IInit requires a Task-returning Init method

public async Task Init(CancellationToken cancellationToken = default)

{

_logger.Log("[SaveService.Init] Initializing save system...");

await Task.Delay(100, cancellationToken); // Simulate async work

_logger.Log("[SaveService.Init] Save system ready.");

}

}



// GameDataService.cs (A MonoBehaviour Singleton that depends on SaveService)

public class GameDataService : MonoSingleton, IInit, IPostInjectSingleton

{

// SaveService will be injected before OnPostInject and Init are called

[Inject] private SaveService _saveService;

[Inject] private Logger _logger;



public void OnPostInject()

{

_logger.Log("[GameDataService.OnPostInject] Dependencies injected for GameDataService.");

}



// IInit method for GameDataService

public async Task Init(CancellationToken cancellationToken = default)

{

_logger.Log("[GameDataService.Init] Initializing game data...");

// Since SaveService is a dependency, its Init will be called and awaited BEFORE this Init method.

await Task.Delay(50, cancellationToken); // Simulate async work

_logger.Log("[GameDataService.Init] Game data ready.");

}

}

```



### 2\. Handling App Resumption with `IPioneer`



Singletons implementing `IPioneer` will have their `OnPreContinue()` method called when the user returns to the game after the application was paused.



* **Purpose:** Ideal for re-initializing external SDKs, refreshing data, or resuming game state that might have been suspended.

* **Execution Order:** `OnPreContinue()` methods are called in a **bottom-up** fashion. If Singleton A depends on Singleton B, B's `OnPreContinue()` will be called before A's `OnPreContinue()`. This ensures lower-level systems are ready before higher-level systems resume.



<!-- end list -->



```csharp

// Example Implementation (building on previous example)



// SaveService.cs (updated to implement IPioneer)

// using UnityEngine;

// using MyUnitySingletonFramework.Core;

// using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces;

// using System.Threading;

// using System.Threading.Tasks;

// // Assume Logger class exists as before



public class SaveService : IMySingleton, IInit, IPioneer

{

[Inject] private Logger _logger;

// ... Init method ...



public void OnPreContinue()

{

_logger.Log("[SaveService.OnPreContinue] App is resuming. Checking save state.");

}

}



// GameDataService.cs (updated to implement IPioneer)

// using UnityEngine;

// using MyUnitySingletonFramework.Core;

// using System.Threading;

// using System.Threading.Tasks;

// // Assume Logger, SaveService classes exist as before



public class GameDataService : MonoSingleton, IInit, IPioneer, IPostInjectSingleton

{

[Inject] private SaveService _saveService;

[Inject] private Logger _logger;

// ... Init and OnPostInject methods ...



public void OnPreContinue()

{

_logger.Log("[GameDataService.OnPreContinue] App is resuming. Reloading active game data.");

// SaveService.OnPreContinue will be called before this method, as GameDataService depends on SaveService.

}

}

```



### 3\. Handling App Termination with `ITerminal`



Singletons implementing `ITerminal` will have their `OnPostStop()` method called when the application pauses or stops.



* **Purpose:** Ideal for saving game state, flushing analytics data, or performing cleanup operations before the application exits or goes to the background.

* **Execution Order:** `OnPostStop()` methods are called in a **top-down** fashion. This means if Singleton A depends on Singleton B, A's `OnPostStop()` will be called *before* B's `OnPostStop()`. This ensures that services that *use* other services have a chance to finalize their work before those underlying services are torn down.



<!-- end list -->



```csharp

// Example Implementation (building on previous example)



// SaveService.cs (updated to implement ITerminal)

// using UnityEngine;

// using MyUnitySingletonFramework.Core;

// using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces;

// using System.Threading;

// using System.Threading.Tasks;

// // Assume Logger class exists as before



public class SaveService : IMySingleton, IInit, IPioneer, ITerminal

{

[Inject] private Logger _logger;

// ... Init, OnPreContinue methods ...



public void OnPostStop()

{

_logger.Log("[SaveService.OnPostStop] App pausing. Finalizing save operations.");

}

}



// GameDataService.cs (updated to implement ITerminal)

// using UnityEngine;

// using MyUnitySingletonFramework.Core;

// using System.Threading;

// using System.Threading.Tasks;

// // Assume Logger, SaveService classes exist as before



public class GameDataService : MonoSingleton, IInit, IPioneer, ITerminal, IPostInjectSingleton

{

[Inject] private SaveService _saveService;

[Inject] private Logger _logger;

// ... Init, OnPostInject, OnPreContinue methods ...



public void OnPostStop()

{

_logger.Log("[GameDataService.OnPostStop] App pausing. Saving current game state.");

// This will be called BEFORE SaveService.OnPostStop, as GameDataService depends on SaveService.

}

}

```



### 4\. Responding to Scene Changes with `ISceneSingleton`



Singletons implementing `ISceneSingleton` will have their `OnNewScene(Scene oldScene, Scene newScene)` method called whenever a scene transition occurs (e.g., loading a new scene).



* **Purpose:** Ideal for scene-specific setup, cleanup, or data loading/unloading that needs to happen during scene changes.

* **Execution Order:** `OnNewScene()` methods are called in a **bottom-up** fashion. This means if Singleton A depends on Singleton B, B's `OnNewScene()` will be called before A's `OnNewScene()`. This ensures that lower-level scene-related services are prepared before higher-level services react to the new scene.



<!-- end list -->



```csharp

// Example Implementation



// AudioManager.cs (A MonoBehaviour Singleton that reacts to scene changes)

using UnityEngine;

using UnityEngine.SceneManagement; // Required for Scene

using MyUnitySingletonFramework.Core;

using Falcon.Falcon.Modules.Core.BigData.Singleton.Runtime.Model.Singletons.Interfaces;



public class AudioManager : MonoSingleton, ISceneSingleton

{

// Assume Logger is an injected dependency

[Inject] private Logger _logger;



public void OnNewScene(Scene oldScene, Scene newScene)

{

_logger.Log($"[AudioManager.OnNewScene] Scene changed from '{oldScene.name}' to '{newScene.name}'. Adjusting music for new scene.");

// Example: load new background music based on newScene.name

}

}



// UIController.cs (A MonoBehaviour Singleton that reacts to scene changes, depends on AudioManager)

using UnityEngine;

using UnityEngine.SceneManagement; // Required for Scene

using MyUnitySingletonFramework.Core;



public class UIController : MonoSingleton, ISceneSingleton, IPostInjectSingleton

{

// AudioManager will be injected before OnPostInject and OnNewScene

[Inject] private AudioManager _audioManager;

[Inject] private Logger _logger;



public void OnPostInject()

{

_logger.Log("[UIController.OnPostInject] Dependencies injected for UIController.");

}



public void OnNewScene(Scene oldScene, Scene newScene)

{

_logger.Log($"[UIController.OnNewScene] Scene changed from '{oldScene.name}' to '{newScene.name}'. Updating UI for new scene.");

// AudioManager.OnNewScene will be called BEFORE this method, as UIController depends on AudioManager.

// Example: update UI elements like scene name display

}

}

```



## How It Works (Under the Hood)



This module integrates with Unity's application lifecycle events (`OnApplicationQuit`, `OnApplicationPause`, `SceneManager.sceneLoaded`, `SceneManager.sceneUnloaded`, etc.) via a core listener component (likely an internal `MonoBehaviour` managed by the framework). During framework initialization, it identifies all registered Singletons that implement `IInit`, `IPioneer`, `ITerminal`, or `ISceneSingleton`. It then orchestrates the execution of their respective methods at the appropriate times, respecting the specified dependency-aware ordering. This ensures a consistent and predictable flow for your application's startup, shutdown, pause/resume, and scene transition routines.



## Benefits



* **Structured Lifecycle Management:** Provides clear, well-defined entry and exit points for your Singleton logic during various application events.

* **Guaranteed Execution Order:** Ensures that your initialization, cleanup, and scene-specific routines run in the correct sequence based on dependencies, avoiding race conditions or trying to access uninitialized components.

* **Reduced Manual Hookup:** Eliminates the need to manually subscribe to Unity's lifecycle events or create custom managers for startup/shutdown/scene logic.

* **Improved Maintainability:** Centralizes lifecycle concerns within the Singletons themselves, making your codebase easier to understand and evolve.

* **Asynchronous Initialization Support:** The `IInit` interface allows for non-blocking startup tasks, improving game responsiveness.

