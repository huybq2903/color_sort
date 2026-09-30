/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-04-20
 */

using System;
using System.IO;
using UnityEngine;

namespace Falcon.Manager.Shared
{
    public static class InstalledTemplateRepository
    {
        private const string INSTALL_INFO_PATH = "Assets/GameTemplate/installed-template.json";

        public static InstalledTemplateInfo Load()
        {
            if (!File.Exists(INSTALL_INFO_PATH))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(INSTALL_INFO_PATH);
                return JsonUtility.FromJson<InstalledTemplateInfo>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load installed template info: {e.Message}");
                return null;
            }
        }

        public static void Save(string templateName, string version)
        {
            var info = new InstalledTemplateInfo
            {
                templateName = templateName,
                version = version,
                installDate = DateTime.Now.ToString("yyyy-MM-dd")
            };

            try
            {
                string directory = Path.GetDirectoryName(INSTALL_INFO_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(info, true);
                File.WriteAllText(INSTALL_INFO_PATH, json);

                UnityEditor.AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save installed template info: {e.Message}");
            }
        }
    }
}
