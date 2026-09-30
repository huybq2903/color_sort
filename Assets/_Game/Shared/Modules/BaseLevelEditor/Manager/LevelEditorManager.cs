/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-19
 */

using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Shared.BaseLevelEditor
{
    [DefaultExecutionOrder(-100)]
    public class LevelEditorManager : MonoBehaviour
    {
        private static LevelEditorManager instance;
        private readonly Dictionary<Type, IEditorManager> DictionaryManager = new();

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.developerConsoleEnabled = false;
            Debug.unityLogger.logEnabled = true;
#else
            Debug.unityLogger.logEnabled = false;
#endif
            instance = this;

            var managers = GetComponents<IEditorManager>();
            foreach (var manager in managers)
            {
                DictionaryManager[manager.GetType()] = manager;
            }
        }

        private void Start()
        {
            foreach (var manager in DictionaryManager.Values)
            {
                manager.Initialized();
            }
        }

        private void OnDestroy()
        {
            instance = null;
        }

        public static T Get<T>() where T : class, IEditorManager
        {
            if (!instance) return null;
            return instance.DictionaryManager.ContainsKey(typeof(T))
                ? instance.DictionaryManager[typeof(T)] as T
                : null;
        }

        public static bool TryGet<T>(out T manager) where T : class, IEditorManager
        {
            manager = Get<T>();
            return manager != null;
        }
        
        public static void Register<T>(T manager) where T : class, IEditorManager
        {
            instance.DictionaryManager[typeof(T)] = manager;
        }
    }
}