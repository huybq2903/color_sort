/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-08
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

namespace Falcon.Manager.Importer
{
    public static class PackageImporter
    {
        public static Task Import(List<IImportRequest> requests, CancellationToken token, AtomicRef<string> description)
        {
            PackImportQueue.Push(requests);
            return ImportRemainingRequests(token, description);
        }

        public static async Task<List<String>> ImportRemainingRequests(CancellationToken token, AtomicRef<string> description)
        {
            List<String> remainingPackages = new List<string>();
            var importRequest = PackImportQueue.Poll();
            while (importRequest != null)
            {
                if (importRequest is UnityPackImportRequest unityPackImportRequest)
                {
                    description.Value = "Importing " + unityPackImportRequest.displayName;
                    await UnityPackageManageService.InstallPackage(unityPackImportRequest, token);
                    remainingPackages.Add(unityPackImportRequest.displayName);
                }
                else if (importRequest is FPackImportRequest fpackImportRequest)
                {
                    description.Value = "Importing " + fpackImportRequest.displayName;
                    await FPackageImporter.DownloadAndImport(fpackImportRequest, token, description);
                    remainingPackages.Add(fpackImportRequest.displayName);
                }
                else
                {
                    throw new ArithmeticException(
                        "Can't resolve import request of type " + importRequest.GetType().Name);
                }

                importRequest = PackImportQueue.Poll();
            }
            
            return remainingPackages;
        }
    }
}