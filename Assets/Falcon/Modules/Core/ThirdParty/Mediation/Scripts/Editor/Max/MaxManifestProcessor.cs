/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-24
 */

#if MAX_ENABLE
using UnityEditor;
using UnityEditor.Android;
using System.IO;
using System.Xml;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Editor
{
    public class MaxManifestProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;
        private const string CLASS_NAME = "MaxManifestProcessor";

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            var guids = AssetDatabase.FindAssets($"{CLASS_NAME} t:Script");
            var path1 = AssetDatabase.GUIDToAssetPath(guids[0]);
            var dir = Path.GetDirectoryName(path1);
            var customPermissionFile = Path.Combine(dir, "PermisionAmazon", "manifest.xml");
            if (!File.Exists(customPermissionFile))
            {
                UnityEngine.Debug.LogWarning($"[MaxManifestProcessor] No custom XML found at {customPermissionFile}");
                return;
            }

            var mainDoc = new XmlDocument();
            mainDoc.Load(manifestPath);
            var nsMgr = new XmlNamespaceManager(mainDoc.NameTable);
            nsMgr.AddNamespace("android", "http://schemas.android.com/apk/res/android");

            var manifestNode = mainDoc.SelectSingleNode("/manifest");

            // Load custom permission file
            var customDoc = new XmlDocument();
            customDoc.Load(customPermissionFile);
            var permissions = customDoc.SelectNodes("/permissions/uses-permission");

            foreach (XmlNode permission in permissions)
            {
                string name = permission.Attributes["android:name"]?.Value;
                if (string.IsNullOrEmpty(name)) continue;

                // Check if already exists
                if (mainDoc.SelectSingleNode($"/manifest/uses-permission[@android:name='{name}']", nsMgr) == null)
                {
                    var newPerm = mainDoc.CreateElement("uses-permission");
                    newPerm.SetAttribute("name", "http://schemas.android.com/apk/res/android", name);
                    manifestNode.AppendChild(newPerm);
                    UnityEngine.Debug.Log($"[PermissionInjector] Added permission: {name}");
                }
            }

            mainDoc.Save(manifestPath);
        }
    }
}
#endif