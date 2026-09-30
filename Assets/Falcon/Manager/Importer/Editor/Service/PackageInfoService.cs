/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Manager.Shared;
using Sirenix.Utilities;
// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public static class PackageInfoService
    {
        private static readonly AtomicRef<Dictionary<string, FalconPackageInfo>> kAtomic = new(null);
        public static bool TryGetInfos(CMSService cmsService, out Dictionary<string, FalconPackageInfo> packageInfos)
        {
            packageInfos = kAtomic.Compute(dict =>
            {
                if(dict != null) return dict;
                if (!RemotePackageRepository.TryGetRemotePackages(cmsService, out var remote))
                {
                    return null;
                }

                return ExtractInfos(remote);
            });
            return packageInfos != null;
        }

        private static Dictionary<string, FalconPackageInfo> ExtractInfos(Dictionary<string, RemoteFalconPackage> remote)
        {
            Dictionary<string, FalconPackageInfo> result = new Dictionary<string, FalconPackageInfo>();

            var localPackages = LocalPackageRepository.Packages;
            var remotePackages = remote;

            var packages = new HashSet<string>(localPackages.Keys);
            packages.AddRange(remotePackages.Keys);

            foreach (var package in packages)
            {
                if (remotePackages.TryGetValue(package, out var remotePackage))
                {
                    // no versions found, ignore
                    if (remotePackage.Versions.Count == 0) continue;
                        
                    result.Add(package, localPackages.TryGetValue(package, out var localPackage)
                        ? new FalconPackageInfo(remotePackage, localPackage)
                        : new FalconPackageInfo(remotePackage));
                }
                else
                {
                    result.Add(package, new FalconPackageInfo(localPackages[package]));
                }
            }

            return result;
        }

        public static void Refresh()
        {
            kAtomic.Value = null;
        }
    }
}