/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-10-16
     */


using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Falcon.Manager.External
{
    public class LocalPackageCreationService
    {
        private const string TemplatesPath           = @"Assets/Falcon/Manager/External/Editor/Templates";
        private const string PackageJsonTemplateName = @"package.json.template";
        private const string ChangelogTemplateName   = @"CHANGELOG.md.template";
        private const string ReadmeTemplateName      = @"README.md.template";
        
        public bool CreateModule(string id, string displayName, out string modulePath)
        {
            try
            {
                var moduleFolderName = Regex.Replace(displayName, @"\s+", "");
                modulePath = Path.Combine("Assets/Falcon/Modules", moduleFolderName);

                if (Directory.Exists(modulePath))
                {
                    Debug.LogError($"Directory already exists: {modulePath}");
                    return false;
                }

                Directory.CreateDirectory(modulePath);

                // Create package.json
                CreateFileFromTemplate(
                    Path.Combine(TemplatesPath, PackageJsonTemplateName),
                    Path.Combine(modulePath, "package.json"),
                    new Dictionary<string, string> { { "{{package-id}}", id }, { "{{package-name}}", displayName } }
                );
                
                // Create CHANGELOG.md
                CreateFileFromTemplate(
                    Path.Combine(TemplatesPath, ChangelogTemplateName),
                    Path.Combine(modulePath, "CHANGELOG.md"), new Dictionary<string, string>( ){ }
                );

                // Create README.md
                CreateFileFromTemplate(
                    Path.Combine(TemplatesPath, ReadmeTemplateName),
                    Path.Combine(modulePath, "README.md"),
                    new Dictionary<string, string> { { "{{package-name}}", displayName } }
                );
                
                AssetDatabase.Refresh();
                Debug.Log($"Successfully created module at {modulePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create module: {e}");
                modulePath = string.Empty;
                return false;
            }
        }

        private static void CreateFileFromTemplate(string templatePath, string outPath, Dictionary<string, string> replacements)
        {
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found at {templatePath}");
            }

            var content = File.ReadAllText(templatePath);
            foreach (var r in replacements)
            {
                content = content.Replace(r.Key, r.Value);
            }
            File.WriteAllText(outPath, content);
        }
    }
}
