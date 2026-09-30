/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-03-03
     */


namespace Falcon.Manager.Importer
{
	using System.Collections.Generic;

	[System.Serializable]
	internal class SimplePackage
	{
		public string fileName;
		public string displayName;
		public string version;
		public string date;
	}
	
	[System.Serializable]
	internal class SimplePackageCollection
	{
		public List<SimplePackage> packages = new();
	}
}