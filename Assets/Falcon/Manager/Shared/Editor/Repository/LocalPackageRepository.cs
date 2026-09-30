/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Falcon.Helpers.Devkit;
using Newtonsoft.Json;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Shared
{
    public static class LocalPackageRepository
    {
        private const string MODULES_PATH = @"Assets/Falcon/";
        private static readonly LazyVal<Dictionary<string,LocalFalconPackage>> kPackages = new (LoadPackages);
        public static Dictionary<string, LocalFalconPackage> Packages => kPackages.Value;

        public static void Refresh()
        {
            kPackages.Reset();
        }
        
        private static Dictionary<string, LocalFalconPackage> LoadPackages()
        {
            List<LocalFalconPackage> result = new List<LocalFalconPackage>();
            string[] packageFiles = Directory.GetFiles(MODULES_PATH, Configuration.PACKAGE_CONFIG_FILE, SearchOption.AllDirectories);

            foreach (string packageFile in packageFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(packageFile);
                    var packageData = JsonConvert.DeserializeObject<LocalFalconPackage>(jsonContent);
                    if (!packageData.name.StartsWith("falcon."))
                    {
                        Debug.LogWarning($"Package: {packageData.name} doesn't start with `falcon.`");
                        continue;
                    }
                    
                    packageData.packagePath = Path.GetDirectoryName(packageFile);
                    packageData.asmdefReferences = DependencyService.FindAsmdefDependencies(packageData.name, packageData.packagePath);
                    
                    //Debug.Log($"Package {packageData.name} -> Path: {packageData.packagePath}");
                    
                    result.Add(packageData);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading package from {packageFile}: {e.Message}");
                }
            }
            return result.ToDictionary(k => k.name);
        }
    }
}