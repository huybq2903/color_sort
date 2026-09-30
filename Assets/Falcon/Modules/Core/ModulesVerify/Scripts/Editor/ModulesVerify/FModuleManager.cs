/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-24


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Falcon.Helpers.FReflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    public class FModuleManager
    {
        private static FModuleManager instance = null;

        public static FModuleManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FModuleManager();
                    instance.ScanVerifiers();
                    instance.ScanFalconModules();
                }

                return instance;
            }
        }

        public Dictionary<string, FModule> modules = new Dictionary<string, FModule>();
        public Dictionary<string, IModuleVerify> verifiers = new Dictionary<string, IModuleVerify>();

        private void ScanVerifiers()
        {
            verifiers.Clear();

            var types = FReflection.Instance.GetTypes()
                .Where(t => typeof(IModuleVerify).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<FAModuleVerifyAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.name))
                {
                    if (!verifiers.ContainsKey(attr.name))
                    {
                        verifiers[attr.name] = (IModuleVerify)Activator.CreateInstance(type);
                    }
                }
            }
        }

        private void ScanFalconModules()
        {
            string root = "Assets/Falcon/";
            if (!Directory.Exists(root))
            {
                Debug.LogError("❌ Folder 'Assets/Falcon/' not found.");
                return;
            }

            string[] asmdefGUIDs = AssetDatabase.FindAssets("t:asmdef", new[] { root });

            foreach (var guid in asmdefGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string normalizedPath = path.Replace('\\', '/').ToLower();

                // ✅ Bỏ qua nếu nằm trong thư mục Lib hoặc Editor
                if (normalizedPath.Contains("/lib/")
                    || normalizedPath.Contains("/editor")
                    || normalizedPath.Contains("/test")
                    || normalizedPath.Contains("/demo")
                    || normalizedPath.Contains("/tools")
                   )
                    continue;

                string json = File.ReadAllText(path);
                AsmdefData data = JsonUtility.FromJson<AsmdefData>(json);

                if (string.IsNullOrEmpty(data.name))
                    continue;

                FModule mod = new()
                {
                    name = data.name,
                    path = path,
                    dependencies = new List<string>(),
                    status = true
                };

                string modulePath = Path.GetDirectoryName(path);
                while (File.Exists(modulePath + "/package.json") == false &&
                       !string.IsNullOrEmpty(modulePath) &&
                       modulePath != "Assets")
                {
                    modulePath = Path.GetDirectoryName(modulePath);
                }

                string packagePath = modulePath + "/package.json";
                if (File.Exists(packagePath))
                {
                    string jsonConfig = File.ReadAllText(packagePath);
                    PackageJson package = JsonConvert.DeserializeObject<PackageJson>(jsonConfig);
                    if (package != null && package.author != null)
                        mod.author = package.author.name;
                }

                if (data.references != null)
                {
                    foreach (var r in data.references)
                    {
                        string refName = ResolveReferenceToName(r);
                        if (!string.IsNullOrEmpty(refName) && refName.Contains("Falcon"))
                        {
                            mod.dependencies.Add(refName);
                        }
                    }
                }

                foreach (var verifier in verifiers)
                {
                    mod.verified[verifier.Key] = verifier.Value.VerifyModules(mod);
                    if (mod.verified[verifier.Key].Item1 == false)
                        mod.status = false;
                }

                modules[mod.name] = mod;
            }
        }

        private string ResolveReferenceToName(string reference)
        {
            if (reference.StartsWith("GUID:"))
            {
                string guid = reference.Substring(5);
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var asm = JsonUtility.FromJson<AsmdefData>(json);
                    return asm.name;
                }
            }

            return reference;
        }


    }


    [System.Serializable]
    class AsmdefData
    {
        public string name;
        public string[] references;
    }

    [System.Serializable]
    class PackageJson
    {
        public string name;
        public string displayName;
        public string version;
        public string unity;
        public string description;
        public AuthorInfo author;
        public Dictionary<string, string> dependencies;
    }

    [System.Serializable]
    class AuthorInfo
    {
        public string name;
        public string email;
    }
}