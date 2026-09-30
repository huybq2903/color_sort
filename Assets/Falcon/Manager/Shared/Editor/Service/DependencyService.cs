/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-20
 */
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;


// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Shared
{
    using System.Linq;

    public static class DependencyService
    {
        public static List<string> FindAsmdefDependencies(string packageName, string packagePath)
        {
            var asmdefFiles = Directory.GetFiles(packagePath, "*.asmdef", SearchOption.AllDirectories);

            var uniqueReferences = new HashSet<string>();

            var startingNames = new string[]{ "Falcon.Modules", "Falcon.Helpers", "Falcon.Manager" };
            
            void AddRefs(string reference)
            {
                if (!string.IsNullOrEmpty(reference) && startingNames.Any(reference.StartsWith))
                {
                    // avoid refences itself
                    var flatten = GetTruePackageName(reference);
                    if (flatten != packageName)
                    {
                        uniqueReferences.Add(reference);
                    }
                }
            }

            foreach (var asmdefFile in asmdefFiles)
            {
                var asmdefFileName = Path.GetFileNameWithoutExtension(asmdefFile);
                if (!startingNames.Any(asmdefFileName.StartsWith)) continue;

                try
                {
                    var asmdefContent = File.ReadAllText(asmdefFile);
                    var asmdefData = JsonConvert.DeserializeObject<AsmdefData>(asmdefContent);

                    if (asmdefData.references != null)
                        foreach (var reference in asmdefData.references)
                            if (reference.StartsWith("GUID:"))
                            {
                                var referenceFromGuid = GetReferenceFromGuid(reference);
                                AddRefs(referenceFromGuid);
                            }
                            else
                            {
                                AddRefs(reference);
                            }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing asmdef file {asmdefFile}: {e.Message}");
                }
            }

            return new List<string>(uniqueReferences);
        }
        
        private static string GetTruePackageName(string dep)
        {
            var flatten   = dep.ToLower();
            return flatten.Replace(".runtime", "");
        }

        private static string GetReferenceFromGuid(string reference)
        {
            var guid = reference.Substring(5);

            // get asset's path by guid
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".asmdef")) return string.Empty;
            try
            {
                var refAsmdefContent = File.ReadAllText(path);
                var refAsmdefData = JsonConvert.DeserializeObject<AsmdefData>(refAsmdefContent);
                if (refAsmdefData != null && !string.IsNullOrEmpty(refAsmdefData.name)) return refAsmdefData.name;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not parse referenced asmdef file at {path}: {e.Message}");
            }

            return string.Empty;
        }

        [Serializable]
        private class AsmdefData
        {
            public string name;
            public string[] references;
        }
    }
}