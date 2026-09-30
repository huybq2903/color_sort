/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-31
     */

using Falcon.Modules.Core.GameData.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Core.GameData.Editor
{
    public static class ResourceNameGenerator
    {
        [MenuItem("Falcon/Modules/Game Resources/Generate Script Names", false, 201)]
        public static void Generate()
        {
            const string filePath = "Assets/FalconAssets/Modules/Core/GameData/ResourceName.cs";
            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var  ignores = new string[] { "test", "demo", "default" };
            var resourceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(t => t.IsClass && !t.IsAbstract && typeof(AResource).IsAssignableFrom(t) && t != typeof(AResource));

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("/* This file is auto-generated. Do not modify. */");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("namespace Falcon.Modules.Core.GameData.Runtime");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("    public static class ResourceName");
            stringBuilder.AppendLine("    {");

            foreach (var type in resourceTypes)
            {
                var info = type.GetCustomAttribute<ResourceInfoAttribute>();
                if (info == null) continue;
                if (ignores.Any(ignore => info.Id.ToLower().Contains(ignore))) continue;

                var constantName = type.Name.Replace("Resource", string.Empty);
                stringBuilder.AppendLine($"        public const string {constantName} = \"{info.Id}\";");
            }

            stringBuilder.AppendLine("    }");
            stringBuilder.AppendLine("}");

            File.WriteAllText(filePath, stringBuilder.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"ResourceName.cs generated successfully at {filePath}");
        }
    }
} 