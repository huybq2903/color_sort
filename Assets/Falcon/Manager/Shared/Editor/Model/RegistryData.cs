/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Shared
{
    [Serializable]
    public class VersionInfo
    {
        public string url;
        public string date;
    }

    [Serializable]
    public class RegistryEntry
    {
        public string displayName;
        public string author;
        public string authorEmail;
        public string group;
        public string latest;
        public string description;
        
        public Dictionary<string, string>      dependencies = new();
        public Dictionary<string, VersionInfo> versions     = new Dictionary<string, VersionInfo>();
    }
} 