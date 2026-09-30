using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
namespace Falcon.Modules.Core.Network.Editor
{
    public class NetworkMenu : UnityEditor.Editor
    {
        private const string SETTINGS_PATH = "Assets/FalconAssets/Modules/Core/Network";
        private const string SETTINGS_NAME = "NetworkSettings";

        [MenuItem("Falcon/Modules/Network Settings")]
        public static void NetworkSettings()
        {
            string resourceFolderPath = Path.Combine(SETTINGS_PATH, "Resources");
            string assetPath = Path.Combine(resourceFolderPath, SETTINGS_NAME + ".asset");

            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(resourceFolderPath))
            {
                Directory.CreateDirectory(resourceFolderPath);
                AssetDatabase.Refresh();
            }

            // Load hoặc tạo mới asset
            var settings = Resources.Load<NetworkSettings>(SETTINGS_NAME);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<NetworkSettings>();
                AssetDatabase.CreateAsset(settings, assetPath);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
            }

            Selection.activeObject = settings;
        }
    }
}