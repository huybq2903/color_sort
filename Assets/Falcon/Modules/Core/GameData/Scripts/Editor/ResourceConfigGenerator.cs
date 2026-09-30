/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-12
     */


using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Core.GameData.Editor
{
    using System.IO;
    using Runtime;

    public static class ResourceConfigGenerator
    {
        [MenuItem("Falcon/Modules/Game Resources/Generate Database", false, 101)]
        public static void GenerateResourceConfigs()
        {
            var path = Path.Combine(ResourceConfigDatabase.PATH, ResourceConfigDatabase.NAME);
            path += ".asset";
            
            var database = AssetDatabase.LoadAssetAtPath<ResourceConfigDatabase>(path);
            if (database == null)
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                database = ScriptableObject.CreateInstance<ResourceConfigDatabase>();
                AssetDatabase.CreateAsset(database, path);
            }

            var resourceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(AResource).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            bool updated = false;
            var  ignores = new string[] { "test", "demo", "default" };
            foreach (var type in resourceTypes)
            {
                var attr = (ResourceInfoAttribute)Attribute.GetCustomAttribute(type, typeof(ResourceInfoAttribute));
                if (attr != null && !ignores.Any(ignore => attr.Id.ToLower().Contains(ignore)))
                {
                    var exist = database.configs.Any(c => c.id == attr.Id);
                    if (!exist)
                    {
                        database.configs.Add(new ResourceConfig
                        {
                            id = attr.Id,
                            name = attr.Id
                        });
                        updated = true;
                    }
                }
            }

            if (updated)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Database updated.");
            }
            else
            {
                Debug.Log("Database is up to date.");
            }
            
            Selection.activeObject = database;
        }

        [MenuItem("Falcon/Modules/Game Resources/Open Database", false, 102)]
        public static void OpenResourceConfigs()
        {
            var path = Path.Combine(ResourceConfigDatabase.PATH, ResourceConfigDatabase.NAME);
            path += ".asset";
            
            var database = AssetDatabase.LoadAssetAtPath<ResourceConfigDatabase>(path);

            if (database == null)
            {
                GenerateResourceConfigs();
                return;
            }

            Selection.activeObject = database;
        }
    }
} 