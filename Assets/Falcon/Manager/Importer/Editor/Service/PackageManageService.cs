/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public class PackageManageService
    {
        private readonly Dictionary<string, FalconPackageInfo> _packageInfos;


        public PackageManageService(Dictionary<string, FalconPackageInfo> packageInfos)
        {
            _packageInfos = packageInfos;
        }

        public async Task<ICollection<IPackageInfo>> GetRequiredDependencies(FalconPackageInfo falconPackageInfo,
            CancellationToken cancellationToken, AtomicRef<string> processDescription)
        {
            return (await DependencyCheck(falconPackageInfo, cancellationToken, processDescription)).Keys;
        }

        public ICollection<FalconPackageInfo> GetUsingDependencies(FalconPackageInfo falconPackageInfo)
        {
            return _packageInfos.Values.Where(info => info.InstalledDependencies.ContainsKey(falconPackageInfo.Name))
                .ToList();
        }

        public static void Uninstall(FalconPackageInfo falconPackageInfo)
        {
            FileUtil.DeleteFileOrDirectory(falconPackageInfo.InstalledPath);
            LocalPackageRepository.Refresh();
            PackageInfoService.Refresh();
            AssetDatabase.Refresh();
        }

        public async Task Install(FalconPackageInfo falconPackageInfo, CancellationToken token,
            AtomicRef<string> processDescription)
        {
            var required = await DependencyCheck(falconPackageInfo, token, processDescription);
            List<IImportRequest> requests = new List<IImportRequest>();
            while (required.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var info = required.First(entry => entry.Value.Count == 0).Key;
                switch (info)
                {
                    case FalconPackageInfo falconPackage:
                        requests.Add(new FPackImportRequest(falconPackage));
                        break;
                    case UnityPackageInfo unityPackage:
                        requests.Add(new UnityPackImportRequest(unityPackage));
                        break;
                    default:
                        throw new InvalidOperationException("Don't know how to install package of type: " +
                                                            info.GetType().Name);
                }

                required.Remove(info);
                foreach (var set in required.Values) set.Remove(info);
            }
            await PackageImporter.Import(requests, token, processDescription);

            LocalPackageRepository.Refresh();
            PackageInfoService.Refresh();
            AssetDatabase.Refresh();
        }

        public async Task BatchInstall(List<FalconPackageInfo> modules, CancellationToken token,
            AtomicRef<string> processDescription)
        {
            Dictionary<IPackageInfo, ISet<IPackageInfo>> merged = new();
            
            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                processDescription.Value = $"Resolving dependencies ({i + 1}/{modules.Count}): {module.DisplayName}";
                await RecursionDependencyCheck(module, merged, token, processDescription);
            }

            List<IImportRequest> requests = new();
            var totalCount = merged.Count;
            var current = 0;

            while (merged.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var info = merged.First(entry => entry.Value.Count == 0).Key;
                current++;
                processDescription.Value = $"Preparing ({current}/{totalCount}): {info.DisplayName}";

                switch (info)
                {
                    case FalconPackageInfo falconPackage:
                        requests.Add(new FPackImportRequest(falconPackage));
                        break;
                    case UnityPackageInfo unityPackage:
                        requests.Add(new UnityPackImportRequest(unityPackage));
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Don't know how to install package of type: " + info.GetType().Name);
                }

                merged.Remove(info);
                foreach (var set in merged.Values) set.Remove(info);
            }

            await PackageImporter.Import(requests, token, processDescription);

            LocalPackageRepository.Refresh();
            PackageInfoService.Refresh();
            AssetDatabase.Refresh();
        }

        private async Task<Dictionary<IPackageInfo, ISet<IPackageInfo>>> DependencyCheck(
            FalconPackageInfo falconPackageInfo, CancellationToken token, AtomicRef<string> processDescription)
        {
            Dictionary<IPackageInfo, ISet<IPackageInfo>> result = new();
            await RecursionDependencyCheck(falconPackageInfo, result, token, processDescription);
            return result;
        }

        private async Task RecursionDependencyCheck(IPackageInfo packageInfo,
            Dictionary<IPackageInfo, ISet<IPackageInfo>> recursiveMap, CancellationToken token,
            AtomicRef<string> processDescription)
        {
            if (recursiveMap.ContainsKey(packageInfo)) return;

            ISet<IPackageInfo> dependents = new HashSet<IPackageInfo>();
            if (packageInfo is FalconPackageInfo falconPackageInfo)
            {
                foreach (var (name, version) in falconPackageInfo.RemoteDependencies)
                {
                    processDescription.Value = $"Checking dependencies for {name} : {version}";
                    IPackageInfo info = _packageInfos.TryGetValue(name, out var rs)
                        ? rs
                        : await UnityPackageManageService.GetUnityPackageInfo(name, token);
                    if (!info.IsInstallAtLeastVersion(version)) dependents.Add(info);
                }

                recursiveMap.Add(packageInfo, dependents);
                foreach (var info in dependents.Where(info => !recursiveMap.ContainsKey(info)))
                    await RecursionDependencyCheck(info, recursiveMap, token, processDescription);
            }
            else
            {
                recursiveMap.Add(packageInfo, new HashSet<IPackageInfo>());
            }
        }
    }
}