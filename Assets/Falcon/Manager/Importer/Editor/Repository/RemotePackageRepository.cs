/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Manager.Shared;
using UnityEngine;
// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public static class RemotePackageRepository
    {
        private static readonly AtomicRef<(ExecState, Dictionary<string, RemoteFalconPackage>)> kState = new ((ExecState.NotStarted, null));

        public static bool TryGetRemotePackages(CMSService cmsService, out Dictionary<string,RemoteFalconPackage> remotePackages)
        {
            TryLoad(cmsService);
            var (execState, list) = kState.Value;
            if (execState.IsDone())
            {
                remotePackages = list;
                return true;
            }

            remotePackages = null;
            return false;
        }

        public static void Refresh(CMSService cmsService)
        {
            kState.Value = (ExecState.NotStarted, null);
            TryLoad(cmsService);
        }
        
        private static void TryLoad(CMSService cmsService)
        {
            kState.Compute(pair =>
            {
                if (!pair.Item1.CanStart()) return pair;
                FetchRemotePackages(cmsService).ContinueWith(t => {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        Debug.LogError("Failed to load packages from remote: " + t.Exception.Message);
                        kState.Value = (ExecState.Failed, null);
                    }
                    else
                    {
                        kState.Value = (ExecState.Succeed, t.Result);
                    }
                });
                return (ExecState.Processing, null);
            });
        }

        private static async Task<Dictionary<string, RemoteFalconPackage>> FetchRemotePackages(CMSService cmsService)
        {
            var entries = await cmsService.GetModules();
            return entries.Select(entry => new RemoteFalconPackage(entry.Key, entry.Value)).ToDictionary(k => k.Name, k => k);
        }
    }
}