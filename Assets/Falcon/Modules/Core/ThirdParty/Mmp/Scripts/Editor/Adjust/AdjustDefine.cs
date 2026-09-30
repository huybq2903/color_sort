/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-16
 */

using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Editor
{
    [InitializeOnLoad]
    public static class AdjustDefine
    {
        private const string ASMDEF_NAME = "Falcon.Modules.Core.ThirdParty.Mmp.Runtime";
        private const string CLASS_NAME = "AdjustDefine";
        private const string ASMDEF_REFERENCE = "AdjustSdk.Scripts";

        public const string WATCHED_FOLDER = "Assets/Adjust";
        private const string DEFINE_SYMBOL = "ADJUST_ENABLE";

        static AdjustDefine()
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
            bool folderExists = IsFolderNotEmpty(WATCHED_FOLDER);
            var defines =
                PlayerSettings.GetScriptingDefineSymbols(
                    NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            var defineList = defines.Split(';').ToList();

            if (folderExists && !defineList.Contains(DEFINE_SYMBOL))
            {
                defineList.Add(DEFINE_SYMBOL);
                SetDefineSymbols(defineList);
                //add adjust
                UpdateReferenceToAsmdef(true);
            }
            else if (!folderExists && defineList.Contains(DEFINE_SYMBOL))
            {
                defineList.RemoveAll(s => s == DEFINE_SYMBOL);
                SetDefineSymbols(defineList);
                //remove adjust
                UpdateReferenceToAsmdef(false);
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
            PlayerSettings.SetScriptingDefineSymbols(
                NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup),
                newDefineString);
        }

        private static void UpdateReferenceToAsmdef(bool isAdd)
        {
            var newReference = WATCHED_FOLDER + $"/Scripts/{ASMDEF_REFERENCE}.asmdef";

            var guid = "GUID:" + AssetDatabase.AssetPathToGUID(newReference);
            var guids = AssetDatabase.FindAssets($"{CLASS_NAME} t:Script");
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var dir = Path.GetDirectoryName(path);
            var pDir = Path.GetDirectoryName(dir);
            var parentDir = Path.GetDirectoryName(pDir);
            var asmdefPath = Path.Combine(parentDir, "Runtime/" + ASMDEF_NAME + ".asmdef");
            Debug.Log("path : " + asmdefPath);
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
    public class FolderWatcherAdjust : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            const string watchedFolder = AdjustDefine.WATCHED_FOLDER;
            var folderWasDeleted = deletedAssets.Any(p => p.StartsWith(watchedFolder));
            var folderWasMovedOut = movedFromAssetPaths.Any(p => p.StartsWith(watchedFolder));
            var folderNoLongerExists = !Directory.Exists(watchedFolder);

            if ((folderWasDeleted || folderWasMovedOut || folderNoLongerExists) && !Directory.Exists(watchedFolder))
            {
                AdjustDefine.SyncDefineWithFolder();
            }

            var folderCreatedOrMovedIn = (importedAssets.Concat(movedAssets)).Any(p => p.StartsWith(watchedFolder)) &&
                                         Directory.Exists(watchedFolder);

            if (folderCreatedOrMovedIn)
            {
                AdjustDefine.SyncDefineWithFolder();
            }
        }
    }
}