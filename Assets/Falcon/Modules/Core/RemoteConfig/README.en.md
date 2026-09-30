**English** | [Tiếng Việt](README.md)
-----

# Falcon Remote Config & A/B Testing Framework

[](https://www.google.com/search?q=%5Bhttps://opensource.org/licenses/MIT%5D\(https://opensource.org/licenses/MIT\))

Welcome to the **Falcon Remote Config & A/B Testing Framework**\! This powerful framework provides robust capabilities to manage and access **dynamic configuration variables** from a remote server, alongside facilitating flexible **A/B Testing**. With this framework, you can change application behavior, roll out new features, and run experiments without needing to update the app.

-----

## 🌟 Key Features

* **Dynamic Configuration Retrieval**: Automatically loads and updates configuration variables from a remote server.
* **A/B Testing Integration**: Easily run A/B tests by delivering different configurations to specific user segments.
* **Fallback Mechanism**: Provides safe default values when server configurations are unreachable or a variable is undefined.
* **Type-Safe Access**: Access configuration variables with type safety through custom configuration classes.
* **Event-Driven Updates**: Notifies your application when configurations are updated, allowing for dynamic reactions.
* **Simplified Access**: Primarily designed for easy access via a static `Instance`, while still compatible with dependency injection.


> 📌 **Refetching config after MMP init** (MMP-filtered configs — opt-in, NOT safe for A/B testing): see [RefetchConfigAfterMmp.md](RefetchConfigAfterMmp.md) (Vietnamese).

-----

## 🚀 Getting Started

### 1\. Defining Client-Side Configurations

To use dynamic configurations, you'll need to create a class that inherits from the **`IFalconConfig`** interface. This class will contain public fields that mirror the remote configurations defined on your server. You can also declare default values for these fields.

**Important Notes:**

* **Matching Names**: The names of your remote configurations on the server and the field names in your client-side class **must match exactly** (case-sensitive).
* **Public Access**: All configuration fields must have a **`public`** access modifier.
* **Serializable**: Your configuration class must be marked with the **`[Serializable]`** attribute.
* **Parameterless Constructor**: The class must have a **parameterless constructor**. This is essential for the framework to create an instance of your configuration class and populate it with data from the server.
* **Type Compatibility**: The data type of your client-side fields must be **compatible** with the data type of the remote config on the server (e.g., server `string` to client `string`, server `int` to client `int` or `long`). Incompatible values from the server will cause the instance creation to fail.

<!-- end list -->

```csharp
// Example: Defining a GameConfig class
using System; // Required for [Serializable]
using Falcon.Modules.Core.RemoteConfig; // IFalconConfig lives here

[Serializable]
public class GameConfig : IFalconConfig
{
    // A parameterless constructor is required (automatically generated if no other constructors exist)
    public GameConfig() { }

    // 'config1' (int) from server
    public int config1 = 10; // Default value if server doesn't provide or on error

    // 'config2' (string) from server
    public string config2 = "Default String Value"; // Default value

    // You can add more configuration fields here
    public bool enableTutorial = true;
    public float gameSpeedMultiplier = 1.0f;
}
```

### 2\. Framework Initialization

The `FConfigController` (the core of this framework) will automatically initialize during application startup, handling the process of fetching and managing configurations.

-----

## 🛠️ Usage

The framework's functionality is primarily accessed through the static **`FConfigController.Instance`**.

```csharp
// FConfigController (core framework class)
// This class will be provided by the framework and manages config loading internally.
public class FConfigController : MySingleton<FConfigController>
{

    /// <summary>
    /// Checks the configuration update status from the server.
    /// </summary>
    public ExecState InitState { get; }

    /// <summary>
    /// Event triggered when the system successfully updates configurations from the server.
    /// </summary>
    public event Action OnUpdateFromNet;

    /// <summary>
    /// Gets the ID of the A/B test campaign the client is currently running (if any).
    /// </summary>
    public string RunningAbTesting { get; }

    /// <summary>
    /// Gets all configurations as a Dictionary<string, object>.
    /// Includes both A/B test and non-test configurations.
    /// </summary>
    public Dictionary<string, object> Configs { get; }

    /// <summary>
    /// Gets all non-A/B test configurations as a Dictionary<string, object>.
    /// </summary>
    public Dictionary<string, object> NonTestConfigs { get; }

    /// <summary>
    /// Gets all A/B test configurations as a Dictionary<string, object>.
    /// </summary>
    public Dictionary<string, object> TestingConfigs { get; }

    /// <summary>
    /// Retrieves an instance of an IFalconConfig class with values populated from the server.
    /// </summary>
    /// <typeparam name="T">The configuration class to retrieve (must implement IFalconConfig and have a parameterless constructor).</typeparam>
    /// <returns>An instance of the configuration class T with values from the server.</returns>
    public T Config<T>() where T : IFalconConfig, new()

    /// <summary>
    /// Re-fetches remote config from the server on demand (outside the automatic fetch at Init).
    /// Only one fetch runs at a time — concurrent calls return false immediately (single-flight).
    /// Returns true when the config was fetched and saved successfully (OnUpdateFromNet has fired).
    /// A null-safe static variant is also available: FConfig.TryFetch().
    /// </summary>
    public Task<bool> TryFetch(CancellationToken cancellationToken = default)
}
```

```csharp
// Example: refresh config right before entering the shop screen
public async Task OpenShop()
{
    var refreshed = await FConfigController.Instance.TryFetch();
    if (!refreshed)
    {
        // Another fetch is already running, no network, or the request failed —
        // just proceed with the currently cached configs.
    }
    var shopConfig = FConfigController.Instance.Config<ShopConfig>();
    // ... open shop with shopConfig
}
```

### 1\. Accessing Dynamic Configurations

You can easily access configuration values using `FConfigController.Instance`.

```csharp
// Example: Using FConfigController.Instance in your game logic
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // For Dictionary

public class GameLogic : MonoBehaviour // Or any other component/class
{
    // You can directly access FConfigController.Instance
    // Or inject it if you're using a Dependency Injection system.
    // For simplicity, we'll use the static Instance here.

    async void Start()
    {
        Debug.Log("[GameLogic] Waiting for Falcon Remote Config to load...");

        // Wait until the config has finished loading (Succeed, Failed, or Cancelled)
        while (!FConfigController.Instance.InitState.IsDone())
        {
            await Task.Delay(100); // Wait a bit before re-checking
            // Optionally, include a CancellationToken if you want to allow cancellation
        }

        if (FConfigController.Instance.InitState.IsSuccess())
        {
            Debug.Log("[GameLogic] Falcon Remote Config loaded successfully.");
            ApplyDynamicConfigs();
        }
        else
        {
            Debug.LogError($"[GameLogic] Falcon Remote Config loading failed or was cancelled. State: {FConfigController.Instance.InitState}");
        }

        // Subscribe to configuration updates
        FConfigController.Instance.OnUpdateFromNet += HandleConfigUpdated;
    }

    private void HandleConfigUpdated()
    {
        Debug.Log("[GameLogic] Falcon Remote Config updated! Applying changes.");
        ApplyDynamicConfigs();
    }

    private void ApplyDynamicConfigs()
    {
        // Retrieve an instance of your custom config class
        GameConfig currentConfig = FConfigController.Instance.Config<GameConfig>();

        Debug.Log($"[Config] config1: {currentConfig.config1}");
        Debug.Log($"[Config] config2: {currentConfig.config2}");
        Debug.Log($"[Config] Enable Tutorial: {currentConfig.enableTutorial}");
        Debug.Log($"[Config] Game Speed Multiplier: {currentConfig.gameSpeedMultiplier}");

        // Get the running A/B Test campaign ID
        string runningABTest = FConfigController.Instance.RunningAbTesting;
        if (!string.IsNullOrEmpty(runningABTest))
        {
            Debug.Log($"[ABTest] Running A/B Test Campaign: {runningABTest}");
        }

        // Access configs as Dictionaries if needed
        Debug.Log("--- Non-Test Configs (as Dictionary) ---");
        foreach (var entry in FConfigController.Instance.NonTestConfigs)
        {
            Debug.Log($"- {entry.Key}: {entry.Value}");
        }
        
        Debug.Log("--- Testing Configs (as Dictionary) ---");
        foreach (var entry in FConfigController.Instance.TestingConfigs)
        {
            Debug.Log($"- {entry.Key}: {entry.Value}");
        }

        // ... Apply these values to your game logic ...
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (FConfigController.Instance != null)
        {
            FConfigController.Instance.OnUpdateFromNet -= HandleConfigUpdated;
        }
    }
}
```

### 2\. Implementing A/B Testing

The framework allows you to define A/B test variables within your dynamic configurations. Users will receive different variants based on their user ID or assigned test group.

The framework automatically resolves the appropriate variant when you retrieve your `IFalconConfig` instance or access the `TestingConfigs` dictionary.

```csharp
// Example: Accessing A/B test variables (e.g., if they are not part of your GameConfig class)
using UnityEngine;
using System.Collections.Generic;

public class ABTestManager : MonoBehaviour
{
    void Start()
    {
        // Subscribe to configuration updates
        FConfigController.Instance.OnUpdateFromNet += ApplyABTestConfigs;
        // Apply configs immediately if already loaded
        ApplyABTestConfigs(); 
    }

    private void ApplyABTestConfigs()
    {
        // Get A/B test values from the TestingConfigs Dictionary
        // This is useful if A/B test variables aren't strictly mapped to a custom IFalconConfig class.
        if (FConfigController.Instance.TestingConfigs.TryGetValue("GameDifficulty", out object difficultyObj))
        {
            string gameDifficulty = difficultyObj.ToString();
            Debug.Log($"[ABTest] Current Game Difficulty: {gameDifficulty}");
        }

        if (FConfigController.Instance.TestingConfigs.TryGetValue("ShopLayout", out object layoutObj))
        {
            string shopLayout = layoutObj.ToString();
            Debug.Log($"[ABTest] Current Shop Layout: {shopLayout}");
        }

        // ... Apply logic based on these A/B test variables ...
    }

    void OnDestroy()
    {
        if (FConfigController.Instance != null)
        {
            FConfigController.Instance.OnUpdateFromNet -= ApplyABTestConfigs;
        }
    }
}
```

-----

## 💡 How It Works (Overview)

1.  **Configuration Loading**: When `FConfigController` initializes (typically at application startup), it fetches configurations from a remote server. The loading status is tracked via the **`InitState`** property.
2.  **Parsing & Storage**: The framework parses the server's JSON response, populating instances of your `IFalconConfig` classes (`Config<T>()`) and internal dictionaries (`Configs`, `NonTestConfigs`, `TestingConfigs`). Caching mechanisms may be in place to ensure availability even offline.
3.  **A/B Test Resolution**: For A/B test variables, the framework uses a unique user ID or other segmentation logic to determine and provide the correct variant to the client. The **`RunningAbTesting`** property indicates the active A/B test campaign.
4.  **Updates**: Configs are fetched **once per app session** during initialization — there is no periodic polling. If you need a fresh fetch at a specific moment (e.g. before entering a shop screen), call **`FConfigController.Instance.TryFetch()`** (or the null-safe `FConfig.TryFetch()`); only one fetch runs at a time — concurrent calls return `false` immediately. After every successful fetch the **`OnUpdateFromNet`** event is triggered, allowing your application to react dynamically.

-----

## ✅ Benefits

* **Rapid Deployment**: Change app behavior and content without submitting new builds to app stores.
* **Flexible Experimentation**: Conduct A/B tests, validate new features, and optimize user experience in real-time.
* **Centralized Control**: Manage all configurations and experiments from a single server platform.
* **Reduced Risk**: Safely roll out new features to a small user segment before wider release.
* **Continuous Optimization**: Iteratively improve your application based on real-world test data.
* **Clear State Management**: The **`InitState`** property and **`OnUpdateFromNet`** event provide clear feedback on configuration loading and update processes.
