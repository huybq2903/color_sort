/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-07
     */


namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System.Collections.Generic;

	[System.Serializable]
	public class RemoteThirdPartyPackage
	{
		public string fileName;
		public string displayName;
		public string version;
		public string description;
	}
	
	[System.Serializable]
	public class RemoteThirdPartyPackages
	{
		public List<RemoteThirdPartyPackage> packages = new ();
		public string                        url;
	}
}