/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Shared
{
    [Serializable]
    public class LocalFalconPackage
    {
        public string name;
        public string displayName;
        public string version;
        public string unity;
        public string description;
        
        public LocalFalconPackageAuthor author = new();

        [JsonIgnore] public List<string> asmdefReferences = new();

        [JsonIgnore] public string packagePath; // Store package path

        public Dictionary<string, string> dependencies = new();

        public LocalFalconPackage() { }

        public LocalFalconPackage(AuthRegistryEntry registry)
        {
            name             = registry.name;
            displayName      = registry.displayName;
            version          = registry.version;
            unity            = "6000.0";
            description      = registry.description;
            author           = registry.author ?? new();
            dependencies     = registry.dependencies ?? new();
            asmdefReferences = registry.asmdefReferences ?? new();
            packagePath      = registry.packagePath;
        }
    }

    [Serializable]
    public class LocalFalconPackageAuthor
    {
        public string name  = "author";
        public string email = "author@falcongames.com";
    }
}