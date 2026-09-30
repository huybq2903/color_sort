    /*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-28
     */



namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System.Collections.Generic;

	[System.Serializable]
	public class RemoteCentralCorePackage
	{
		public string fileName;
		public string displayName;
		public string version;
		public string date;
	}
	
	[System.Serializable]
	public class RemoteCentralCorePackageCollection
	{
		public List<RemoteCentralCorePackage> packages = new();
	}
}