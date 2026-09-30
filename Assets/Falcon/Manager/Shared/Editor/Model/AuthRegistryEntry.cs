/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-31
     */


namespace Falcon.Manager.Shared
{
	using System.Collections.Generic;
	using Newtonsoft.Json;

	[System.Serializable]
	public class AuthRegistryEntry
	{
		public string name;
		public string displayName;
		public string version;
		public string latest;
		public string description;
		
		public LocalFalconPackageAuthor author = new();
		
		[JsonIgnore] public List<string> asmdefReferences = new();

		[JsonIgnore] public string packagePath; // Store package path
        
		[JsonIgnore] public bool packageExists;
		
		public Dictionary<string, string>      dependencies = new();
		public Dictionary<string, VersionInfo> versions     = new Dictionary<string, VersionInfo>();
	}
}