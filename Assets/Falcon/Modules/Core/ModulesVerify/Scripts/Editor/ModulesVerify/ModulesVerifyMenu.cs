/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-22

using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    public class ModulesVerifyMenu
    {
        [MenuItem("Falcon/Modules Verify/Verify Modules")]
        public static void VerifyModules()
        {
            string dir = "Assets/Editor";
            string path = $"{dir}/ModuleVerifier.asset";

            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }

            ModulesVerify asset = AssetDatabase.LoadAssetAtPath<ModulesVerify>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ModulesVerify>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}