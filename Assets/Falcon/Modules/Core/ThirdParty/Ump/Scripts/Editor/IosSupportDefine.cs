/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-16
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.Build;

namespace Falcon.Modules.Core.ThirdParty.Ump.Editor
{
    [InitializeOnLoad]
    public static class IosSupportDefine
    {
        private const string DEFINE_SYMBOL = "UNITY_ADS_IOS_SUPPORT_ENABLE";
        private const string ASMDEF_NAME = "Falcon.Modules.Core.ThirdParty.Ump.Runtime";
        private const string CLASS_NAME = "IosSupportDefine";
        private const string ASMDEF_REFERENCE = "Unity.Advertisement.IosSupport";

        static IosSupportDefine()
        {
            // Kiểm tra một lần khi Editor mở
            EditorApplication.update += DelayedSync;
        }

        private static void DelayedSync()
        {
            EditorApplication.update -= DelayedSync;
            SyncDefineWithFolder();
        }

        private static void SyncDefineWithFolder()
        {
            var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("manifest.json not found.");
                return;
            }

            var content = File.ReadAllText(manifestPath);
            var defines =
                PlayerSettings.GetScriptingDefineSymbols(
                    NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            var defineList = defines.Split(';').ToList();
            if (content.Contains("\"com.unity.ads.ios-support\""))
            {
                defineList.Add(DEFINE_SYMBOL);
                SetDefineSymbols(defineList);
                UpdateReferenceToAsmdef(true);
            }
            else
            {
                defineList.RemoveAll(s => s == DEFINE_SYMBOL);
                SetDefineSymbols(defineList);
                UpdateReferenceToAsmdef(false);
            }
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
            var asmdefGuids = AssetDatabase.FindAssets($"{ASMDEF_REFERENCE} t:asmdef");
            string targetGuid = null;
            foreach (var t in asmdefGuids)
            {
                var pathT = AssetDatabase.GUIDToAssetPath(t);
                var filename = Path.GetFileNameWithoutExtension(pathT);

                if (filename == ASMDEF_REFERENCE)
                {
                    targetGuid = t;
                    break;
                }
            }

            if (string.IsNullOrEmpty(targetGuid))
            {
                // Debug.LogError($"Chưa cài package {ASMDEF_REFERENCE}.");
                return;
            }

            var guid = "GUID:" + targetGuid;
            var guids = AssetDatabase.FindAssets($"{CLASS_NAME} t:Script");
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var dir = Path.GetDirectoryName(path);
            var parentDir = Path.GetDirectoryName(dir);
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
}