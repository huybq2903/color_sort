/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.External
{
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Newtonsoft.Json;

    public class PackageExportService
    {
        private readonly CMSService _cmsService;
        
        internal PackageExportService(CMSService cmsService)
        {
            _cmsService = cmsService;
        }
        
        internal async Task ExportPackage(AuthRegistryEntry package)
        {
            UpdateDependencies(new LocalFalconPackage(package));
            
            string exportPath = GetExportPath(package);
            Debug.Log($"Exporting {package.displayName} to {exportPath}");

            if (!string.IsNullOrEmpty(exportPath))
            {
                // unity package
                EditorUtility.DisplayProgressBar("Progress", "Export package...", 0.1f);
                AssetDatabase.ExportPackage(
                    package.packagePath,
                    exportPath,
                    ExportPackageOptions.Recurse);
                
                // json
                var metadataPath = Path.Combine(package.packagePath, "package.json");
                
                // readme
                var readmePath = Path.Combine(package.packagePath, "README.md");
                
                // changelog
                var changelogPath = Path.Combine(package.packagePath, "CHANGELOG.md");

                var exportDir = Path.GetDirectoryName(exportPath);
                var zipPath = Path.Combine(exportDir, "package.zip");
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                ZipFileService.Zip(zipPath, new[] { exportPath, metadataPath, readmePath, changelogPath });

                EditorUtility.ClearProgressBar();
                
                var multipart = new MultipartFormDataContent();
                
                await using var fileStream  = File.OpenRead(zipPath);
                
                var       fileName    = Path.GetFileName(zipPath);
                var       fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(@"application/zip");
                
                multipart.Add(fileContent, "file", fileName);
                bool uploaded = await _cmsService.Upload(new MultipartFileContent(multipart));

                if (uploaded)
                {
                    EditorUtility.DisplayDialog("Upload Succeeded", "The Package upload succeed.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Upload Failed", "The Package upload failed.", "OK");
                }
                
                fileStream.Close();
                
                // delete
                File.Delete(exportPath);
                File.Delete(zipPath);
                
                var meta = $"{exportDir}.meta";
                Directory.Delete(exportDir, true);
                File.Delete(meta);

                AssetDatabase.Refresh();
            }
        }

        private static void UpdateDependencies(LocalFalconPackage package)
        {
            var dependencies     = package.dependencies;
            var asmdefReferences = package.asmdefReferences;

            var valueToRemove = "falcon.";
            
            // Find all keys matching the condition
            var keysToRemove = dependencies
                .Where(pair => pair.Key.StartsWith(valueToRemove))
                .Select(pair => pair.Key)
                .ToList();
            
            // Remove matching keys
            foreach (var key in keysToRemove)
            {
                dependencies.Remove(key);
            }
            
            foreach (var dep in asmdefReferences)
            {
                if (TryGetPackageByAsmdefDependency(dep, out var found))
                {
                    dependencies[found.name] = found.version;
                }
            }
            
            var json = JsonConvert.SerializeObject(package, Formatting.Indented);
            var path = Path.GetFullPath(package.packagePath);
            File.WriteAllText($"{path}/package.json", json);
            AssetDatabase.Refresh();
        }

        private static string GetExportPath(AuthRegistryEntry package)
        {
            var path         = Configuration.EXPORT_PATH;
            var absolutePath = Path.GetFullPath(path);
            
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
            }

            var packageExportPath = $"{absolutePath}/{package.name}";
            if (!Directory.Exists(packageExportPath))
            {
                Directory.CreateDirectory(packageExportPath);
            }
            
            // ReSharper disable once StringLiteralTypo
            return $"{packageExportPath}/{package.name}-{package.version}.unitypackage";
        }
        
        internal static string GetTruePackageName(string dependency)
        {
            var flatten   = dependency.ToLower();
            return flatten.Replace(".runtime", "");
        }

        internal static bool TryGetPackageByAsmdefDependency(string dependency, out LocalFalconPackage package)
        {
            var runtimeValue = dependency.ToLower().Replace(".runtime", "");
            var existRuntime = LocalPackageRepository.Packages.TryGetValue(runtimeValue, out var runtimePackage);

            var editorValue = dependency.ToLower().Replace(".editor", "");
            var existEditor = LocalPackageRepository.Packages.TryGetValue(editorValue, out var editorPackage);

            if (existRuntime || existEditor)
            {
                package = existRuntime ? runtimePackage : editorPackage;
                return true;
            }

            package = null;
            return false;
        }
    }
}