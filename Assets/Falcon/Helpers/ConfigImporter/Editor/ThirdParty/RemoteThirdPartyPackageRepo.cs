/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-07
     */


namespace Falcon.Helpers.ConfigImporter.Editor
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Falcon.Helpers.Devkit;
    using UnityEngine;

    public static class RemoteThirdPartyPackageRepo
	{
		private static readonly AtomicRef<(ExecState, Dictionary<string, RemoteThirdPartyPackages>)> kState = new ((ExecState.NotStarted, null));

        static RemoteThirdPartyPackageRepo()
        {
            TryLoad();
        }
        
        internal static bool TryGetRemotePackages(out Dictionary<string, RemoteThirdPartyPackages> remotePackages)
        {
            TryLoad();
            var (execState, list) = kState.Value;
            if (execState.IsDone())
            {
                remotePackages = list;
                return true;
            }

            remotePackages = null;
            return false;
        }
        
        private static void TryLoad()
        {
            kState.Compute(pair =>
            {
                if (!pair.Item1.CanStart()) return pair;
                FetchRemotePackages().ContinueWith(t => {
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

        private static async Task<Dictionary<string, RemoteThirdPartyPackages>> FetchRemotePackages()
        {
            var response = await new GetRequest(Configuration.k3rdRegistryFileLink).Execute();
            return await response.SuccessObj<Dictionary<string, RemoteThirdPartyPackages>>();
        }
	}
}