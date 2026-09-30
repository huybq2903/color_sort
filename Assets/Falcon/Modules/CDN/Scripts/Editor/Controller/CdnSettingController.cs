/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using Falcon.Helpers.Devkit;
using UnityEditor;

namespace Falcon.Modules.CDN
{
    public static class CdnSettingController
    {
        [MenuItem("Falcon/Modules/Cdn/Settings")]
        public static void MediationSettings()
        {
            Selection.activeObject = CdnSettingService.CdnSettings;
        }

        [MenuItem("Falcon/Modules/Cdn/Clear cached files")]
        public static void CdnCacheClear()
        {
            foreach (var (key, file) in
                     new LocalCdnRepository(new FLocalFileRepository()).ListFilesUsingFolderPrefix(
                         Array.Empty<string>()))
            {
                file.Delete();
                LocalFileHashRepository.DeleteFileHash(file);
                CdnLogger.Instance.Info("Delete cached file: " + key.ToJson());
            }

            CdnLogger.Instance.Info("Delete cached complete");
        }
    }
}