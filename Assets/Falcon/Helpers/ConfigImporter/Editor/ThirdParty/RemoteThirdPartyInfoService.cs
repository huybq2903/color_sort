/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-07
     */

namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System.Collections.Generic;
	using Falcon.Helpers.Devkit;

	public static class RemoteThirdPartyInfoService
	{
		private static readonly AtomicRef<Dictionary<string, RemoteThirdPartyPackages>> kAtomic = new(null);
		
		public static bool TryGetInfos(out Dictionary<string, RemoteThirdPartyPackages> packageInfos)
		{
			packageInfos = kAtomic.Compute(dict =>
			{
				if(dict != null) return dict;
				if (!RemoteThirdPartyPackageRepo.TryGetRemotePackages(out var remote))
				{
					return null;
				}

				return remote;
				
			});
			return packageInfos != null;
		}
	}
}