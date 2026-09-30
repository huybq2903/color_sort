/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-30
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Modules.Core.InAppPurchase.Runtime;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Falcon.Modules.Core.InAppPurchase.Editor
{
    public static class IAPConfigAutoLoad
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            string[] guids = AssetDatabase.FindAssets("t:SOInAppPurchaseConfig");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var cfg = AssetDatabase.LoadAssetAtPath<SOInAppPurchaseConfig>(path);
                if (!cfg) continue;

                bool changed = false;
                changed |= AppendMissingTypes<IPurchaseValidation>(ref cfg.validations);
                changed |= AppendMissingTypes<IPurchaseLog>(ref cfg.loggers);

                if (changed)
                {
                    EditorUtility.SetDirty(cfg);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[IAP] Auto-populated types into {path}");
                }
            }
        }

        private static bool AppendMissingTypes<TInterface>(ref List<TypeToggle> array)
        {
            var types = TypeCache.GetTypesDerivedFrom<TInterface>()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

            var list = array ?? new List<TypeToggle>();
            bool changed = false;

            foreach (var t in types)
            {
                if (list.Any(x => x.nameDisplay == t.Name)) continue;
                list.Add(new TypeToggle
                {
                    enabled = true, 
                    assemblyQualifiedTypeName = t.AssemblyQualifiedName, 
                    nameDisplay = t.Name
                });
                changed = true;
            }

            if (changed) array = list;
            return changed;
        }
    }
}