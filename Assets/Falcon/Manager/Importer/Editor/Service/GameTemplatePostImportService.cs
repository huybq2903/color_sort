/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-15
 */

namespace Falcon.Manager.Importer.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

    [InitializeOnLoad]
    public static class GameTemplatePostImportService
    {
        private static readonly string[] kRequiredDefines = { "MAX_ENABLE", "APPSFLYER_ENABLE" };

        private static readonly NamedBuildTarget[] kTargets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone, // Windows/Standalone
        };

        private const  string   kScenesDir  = "Assets/GameTemplate/Scenes";
        private static readonly string[] kSceneNames = { "Boot", "Main", "GamePlay" };

        static GameTemplatePostImportService()
        {
            AssetDatabase.importPackageCompleted -= OnImportComplete;
            AssetDatabase.importPackageCompleted += OnImportComplete;
        }

        private static void OnImportComplete(string packageName)
        {
            if (!GameTemplateImportLogic.IsGameTemplatePackage(packageName)) return;

            var addedDefines = ApplyDefineSymbols();
            var addedScenes  = ApplyScenes();
            ReportResult(packageName, addedDefines, addedScenes);
        }

        private static List<string> ApplyDefineSymbols()
        {
            var added = new List<string>();
            foreach (var target in kTargets)
            {
                var current = PlayerSettings.GetScriptingDefineSymbols(target);
                var merge   = GameTemplateImportLogic.MergeDefines(current, kRequiredDefines);
                if (merge.Added.Count == 0) continue;

                PlayerSettings.SetScriptingDefineSymbols(target, merge.Result);
                foreach (var sym in merge.Added)
                    added.Add($"{sym} ({target.TargetName})");
            }
            return added;
        }

        private static List<string> ApplyScenes()
        {
            var candidates = new List<string>();
            foreach (var name in kSceneNames)
            {
                var path = $"{kScenesDir}/{name}.unity";
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[GameTemplateImportService] Scene does not exist, skipping: {path}");
                    continue;
                }
                candidates.Add(path);
            }

            var scenes        = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var existingPaths  = scenes.ConvertAll(s => s.path);
            var toAdd          = GameTemplateImportLogic.ScenesToAdd(existingPaths, candidates);
            if (toAdd.Count == 0) return toAdd;

            foreach (var path in toAdd)
                scenes.Add(new EditorBuildSettingsScene(path, true));

            EditorBuildSettings.scenes = scenes.ToArray();
            return toAdd;
        }

        private static void ReportResult(string packageName, List<string> addedDefines, List<string> addedScenes)
        {
            if (addedDefines.Count == 0 && addedScenes.Count == 0)
            {
                Debug.Log($"[GameTemplateImportService] '{packageName}': nothing changed - no modifications needed (define & scene already present).");
                return;
            }

            var sb = new StringBuilder();
            if (addedDefines.Count > 0)
            {
                sb.AppendLine("Define symbols added:");
                foreach (var d in addedDefines) sb.AppendLine($"  • {d}");
            }
            if (addedScenes.Count > 0)
            {
                sb.AppendLine("Scene added to Build Settings:");
                foreach (var s in addedScenes) sb.AppendLine($"  • {s}");
            }

            Debug.Log($"[GameTemplateImportService] '{packageName}':\n{sb}");
            EditorUtility.DisplayDialog("Game Template Imported", sb.ToString(), "OK");
        }
    }
}
