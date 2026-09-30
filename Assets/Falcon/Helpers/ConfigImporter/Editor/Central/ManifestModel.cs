/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-02-25
     */

namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public class ManifestModel
	{
		public Dictionary<string, string> dependencies     = new();
		public List<ScopedRegistry>       scopedRegistries = new();
	}

	[Serializable]
	public class ScopedRegistry
	{
		public string       name;
		public string       url;
		public List<string> scopes = new();
		public bool         overrideBuiltIns;
	}
}
