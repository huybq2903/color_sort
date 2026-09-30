/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-08
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public static class FPackageImporter
    {
        private static readonly FLocalFileRepository kFileRepository = new();

        public static async Task DownloadAndImport(FalconPackageInfo falconPackageInfo, CancellationToken token,
            AtomicRef<string> processDescription)
        {
            await DownloadAndImport(new FPackImportRequest(falconPackageInfo), token, processDescription);
        }

        public static async Task DownloadAndImport(FPackImportRequest packRequest,
            CancellationToken token,
            AtomicRef<string> processDescription)
        {
            var fTempFile = kFileRepository.GetTempFile(GUID.Generate().ToString().Replace("-", "") + ".unitypackage");
            var response = await new GetRequest(packRequest.latestInfo.url).Execute(token);
            var streamBody = await response.StreamBody();
            processDescription.Value = $"Downloading {packRequest.displayName}";
            await fTempFile.SaveAsync(streamBody, token: token);
            processDescription.Value = $"Downloading {packRequest.displayName} Completed";
            streamBody.Close();
            response.Close();
            GC.WaitForPendingFinalizers();
            await Task.Yield();
            Debug.Log(fTempFile.FilePath);
            processDescription.Value = $"Importing {packRequest.displayName}";

            if (packRequest.installedPath != null && Directory.Exists(packRequest.installedPath)) 
                FileUtil.DeleteFileOrDirectory(packRequest.installedPath);

            AssetDatabase.ImportPackage(fTempFile.FilePath, false);
            await Task.Yield();
            Debug.Log("Package Import finish: " + packRequest.displayName);
            fTempFile.Delete();
        }
    }
}