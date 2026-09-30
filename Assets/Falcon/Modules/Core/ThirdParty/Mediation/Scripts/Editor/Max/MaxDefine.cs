/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-24
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.Build;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Editor
{
    [InitializeOnLoad]
    public static class MaxDefine
    {
        private const string ASMDEF_NAME = "Falcon.Modules.Core.ThirdParty.Mediation.Runtime";
        private const string CLASS_NAME = "MaxDefine";
        private const string ASMDEF_REFERENCE1 = "MaxSdk.Scripts";
        private const string ASMDEF_REFERENCE2 = "Amazon.Scripts";

        public const string WATCHED_FOLDER = "Assets/MaxSdk";
        private const string DEFINE_SYMBOL = "MAX_ENABLE";

        public const string WATCHED_FOLDER_AMAZON = "Assets/Amazon";
        private const string DEFINE_SYMBOL_AMAZON = "AMAZON_ENABLE";

        static MaxDefine()
        {
            // Kiểm tra một lần khi Editor mở
            EditorApplication.update += DelayedSync;
        }

        private static void DelayedSync()
        {
            EditorApplication.update -= DelayedSync;
            SyncDefineWithFolder();
        }

        public static void SyncDefineWithFolder()
        {
            var folderExists = IsFolderNotEmpty(WATCHED_FOLDER);
            var defines =
                PlayerSettings.GetScriptingDefineSymbols(
                    NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            var defineList = defines.Split(';').ToList();
            if (folderExists && !defineList.Contains(DEFINE_SYMBOL))
            {
                defineList.Add(DEFINE_SYMBOL);
                SetDefineSymbols(defineList);
                //add max
                UpdateReferenceToAsmdef(DEFINE_SYMBOL, true);
            }
            else if (!folderExists && defineList.Contains(DEFINE_SYMBOL))
            {
                defineList.RemoveAll(s => s == DEFINE_SYMBOL);
                SetDefineSymbols(defineList);
                //remove max
                UpdateReferenceToAsmdef(DEFINE_SYMBOL, false);
            }

            folderExists = IsFolderNotEmpty(WATCHED_FOLDER_AMAZON);
            defines =
                PlayerSettings.GetScriptingDefineSymbols(
                    NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            defineList = defines.Split(';').ToList();

            if (folderExists && !defineList.Contains(DEFINE_SYMBOL_AMAZON))
            {
                defineList.Add(DEFINE_SYMBOL_AMAZON);
                SetDefineSymbols(defineList);
                //add amazon
                UpdateReferenceToAsmdef(DEFINE_SYMBOL_AMAZON, true);
            }
            else if (!folderExists && defineList.Contains(DEFINE_SYMBOL_AMAZON))
            {
                defineList.RemoveAll(s => s == DEFINE_SYMBOL_AMAZON);
                SetDefineSymbols(defineList);
                //remove amazon
                UpdateReferenceToAsmdef(DEFINE_SYMBOL_AMAZON, false);
            }
        }

        private static bool IsFolderNotEmpty(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return false;
            var files = Directory.GetFiles(folderPath)
                .Where(f => !f.EndsWith(".meta"))
                .ToArray();
            var dirs = Directory.GetDirectories(folderPath);
            return files.Length > 0 || dirs.Length > 0;
        }

        private static void SetDefineSymbols(List<string> defineList)
        {
            var newDefineString = string.Join(";", defineList.Distinct());
            Debug.LogError(newDefineString);
            PlayerSettings.SetScriptingDefineSymbols(
                NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup),
                newDefineString);
            Debug.LogError(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
        }

        private static void UpdateReferenceToAsmdef(string define, bool isAdd)
        {
            var newReference = WATCHED_FOLDER + $"/Scripts/{ASMDEF_REFERENCE1}.asmdef";
            if (define == DEFINE_SYMBOL_AMAZON)
            {
                newReference = WATCHED_FOLDER_AMAZON + $"/Scripts/{ASMDEF_REFERENCE2}.asmdef";
            }

            var guid = "GUID:" + AssetDatabase.AssetPathToGUID(newReference);
            var guids = AssetDatabase.FindAssets($"{CLASS_NAME} t:Script");
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var dir = Path.GetDirectoryName(path);
            var pDir = Path.GetDirectoryName(dir);
            var parentDir = Path.GetDirectoryName(pDir);
            var asmdefPath = Path.Combine(parentDir, "Runtime/" + ASMDEF_NAME + ".asmdef");
            var json = File.ReadAllText(asmdefPath);
            var asmdef = JsonUtility.FromJson<AsmdefWrapper>(json);
            if (asmdef.references == null)
                asmdef.references = new List<string>();
            if (isAdd)
            {
                if (!asmdef.references.Contains(guid))
                {
                    asmdef.references.Add(guid);
                }
            }
            else
            {
                if (asmdef.references.Contains(guid))
                {
                    asmdef.references.Remove(guid);
                }
            }

            var updatedJson = JsonUtility.ToJson(asmdef, true);
            File.WriteAllText(asmdefPath, updatedJson);
            AssetDatabase.Refresh();
        }

        [System.Serializable]
        public class AsmdefWrapper
        {
            public string name;
            public string rootNamespace;
            public List<string> references;
            public List<string> includePlatforms;
            public List<string> excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public List<string> precompiledReferences;
            public bool autoReferenced;
            public List<string> defineConstraints;
            public List<string> versionDefines;
            public bool noEngineReferences;
        }
    }

// ===== WATCHER CLASS =====
    public class FolderWatcherMax : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            const string watchedFolder = MaxDefine.WATCHED_FOLDER;
            var folderWasDeleted = deletedAssets.Any(p => p.StartsWith(watchedFolder));
            var folderWasMovedOut = movedFromAssetPaths.Any(p => p.StartsWith(watchedFolder));
            var folderNoLongerExists = !Directory.Exists(watchedFolder);

            if ((folderWasDeleted || folderWasMovedOut || folderNoLongerExists) && !Directory.Exists(watchedFolder))
            {
                MaxDefine.SyncDefineWithFolder();
            }

            var folderCreatedOrMovedIn = (importedAssets.Concat(movedAssets)).Any(p => p.StartsWith(watchedFolder)) &&
                                         Directory.Exists(watchedFolder);

            const string watchedFolderAmazon = MaxDefine.WATCHED_FOLDER_AMAZON;
            var folderAmazonWasDeleted = deletedAssets.Any(p => p.StartsWith(watchedFolderAmazon));
            var folderAmazonWasMovedOut = movedFromAssetPaths.Any(p => p.StartsWith(watchedFolderAmazon));
            var folderAmazonNoLongerExists = !Directory.Exists(watchedFolderAmazon);

            if ((folderAmazonWasDeleted || folderAmazonWasMovedOut || folderAmazonNoLongerExists) &&
                !Directory.Exists(watchedFolderAmazon))
            {
                MaxDefine.SyncDefineWithFolder();
            }

            var folderAmazonCreatedOrMovedIn =
                (importedAssets.Concat(movedAssets)).Any(p => p.StartsWith(watchedFolderAmazon)) &&
                Directory.Exists(watchedFolderAmazon);

            if (folderCreatedOrMovedIn || folderAmazonCreatedOrMovedIn)
            {
                MaxDefine.SyncDefineWithFolder();
            }
        }
    }
}