/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-04-24
     */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    /// <summary>
    /// Service for scanning and loading ScriptableObject configs from FalconAssets/Modules/
    /// </summary>
    public class ModuleSettingsService
    {
        private const string FALCON_ASSETS_ROOT = "Assets/FalconAssets/Modules";
        
        private List<ModuleInfo>                      _cachedModules     = new();
        private Dictionary<string, List<SOAssetInfo>> _moduleAssetsCache = new();

        /// <summary>
        /// Re-scan disk for modules and their asset files
        /// </summary>
        public void Reload()
        {
            _cachedModules.Clear();
            _moduleAssetsCache.Clear();

            if (!AssetDatabase.IsValidFolder(FALCON_ASSETS_ROOT))
                return;

            var moduleGuids = AssetDatabase.FindAssets("t:folder", new[] { FALCON_ASSETS_ROOT });
            
            foreach (var guid in moduleGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var relativePath = path.Substring(FALCON_ASSETS_ROOT.Length + 1);
                if (relativePath.Contains('/'))
                    continue;

                var moduleInfo = new ModuleInfo
                {
                    Name = Path.GetFileName(path),
                    AssetPath = path
                };
                _cachedModules.Add(moduleInfo);

                var assets = FindAssetFiles(path);
                if (assets.Count > 0)
                    _moduleAssetsCache[path] = assets;
            }
        }

        public List<ModuleInfo> GetAllModules() => _cachedModules;

        public List<SOAssetInfo> GetModuleAssets(string moduleName)
        {
            var module = _cachedModules.FirstOrDefault(m => m.Name == moduleName);
            if (module == null || !_moduleAssetsCache.TryGetValue(module.AssetPath, out var assets))
                return new List<SOAssetInfo>();
            return assets;
        }

        /// <summary>
        /// All configs across every module (cached instances, safe for reference equality).
        /// </summary>
        public List<SOAssetInfo> GetAllAssets()
        {
            var result = new List<SOAssetInfo>();
            foreach (var assets in _moduleAssetsCache.Values)
                result.AddRange(assets);
            return result;
        }

        public List<SOAssetInfo> GetModuleAssetsAtPath(string modulePath)
        {
            return _moduleAssetsCache.TryGetValue(modulePath, out var assets) 
                ? assets 
                : new List<SOAssetInfo>();
        }

        public bool HasConfigs(string moduleName)
        {
            var module = _cachedModules.FirstOrDefault(m => m.Name == moduleName);
            return module != null && _moduleAssetsCache.ContainsKey(module.AssetPath);
        }

        private List<SOAssetInfo> FindAssetFiles(string modulePath)
        {
            var result = new List<SOAssetInfo>();
            var guids = AssetDatabase.FindAssets("", new[] { modulePath });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    continue;

                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj == null)
                    continue;

                if (!typeof(ScriptableObject).IsAssignableFrom(obj.GetType()))
                    continue;

                if (path.Contains("/.meta"))
                    continue;

                result.Add(new SOAssetInfo
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    AssetPath = path,
                    Instance = obj
                });
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return result;
        }
    }
}