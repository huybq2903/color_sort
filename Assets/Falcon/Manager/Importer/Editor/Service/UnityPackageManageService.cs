/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.PackageManager;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public static class UnityPackageManageService
    {
        public static async Task InstallPackage(UnityPackImportRequest request, CancellationToken token = default)
        {
            var addRequest = Client.Add(request.name + "@" + request.latestVersion);
            while (addRequest.IsCompleted)
            {
                token.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            if (addRequest.Error != null) throw new ApplicationException(addRequest.Error.message);
        }

        public static async Task<UnityPackageInfo> GetUnityPackageInfo(string packageName,
            CancellationToken cancellationToken = default)
        {
            return new UnityPackageInfo(await GetRemotePackageInfo(packageName, cancellationToken),
                await GetLocalPackageInfo(packageName, cancellationToken));
        }

        private static async Task<PackageInfo> GetRemotePackageInfo(string packageName,
            CancellationToken cancellationToken = default)
        {
            var searchRequest = Client.Search(packageName);
            while (!searchRequest.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (searchRequest.Error != null) throw new ApplicationException(searchRequest.Error.message);

            var requestResult = searchRequest.Result;
            if (requestResult.Length == 0)
                throw new ApplicationException("Can't find any unity package with the name of: " + packageName);
            return requestResult[0];
        }

        private static async Task<PackageInfo> GetLocalPackageInfo(string packageName,
            CancellationToken cancellationToken = default)
        {
            var searchRequest = Client.List();
            while (!searchRequest.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (searchRequest.Error != null) throw new ApplicationException(searchRequest.Error.message);
            return searchRequest.Result.FirstOrDefault(info =>
                string.Equals(info.name, packageName, StringComparison.Ordinal));
        }
    }
}