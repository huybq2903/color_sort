/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using System.Collections.Generic;
using Falcon.Manager.Shared;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public readonly struct RemoteFalconPackage
    {
        public string Name          { get; }
        public string DisplayName   { get; }
        public string Author        { get; }
        public string AuthorEmail   { get; }
        public string Group         { get; }
        public string LatestVersion { get; }
        public string Description   { get; }

        public Dictionary<string, VersionInfo> Versions { get; }
        public Dictionary<string, string> Dependencies { get; }

        public RemoteFalconPackage(string name, RegistryEntry entry)
        {
            Name          = name;
            DisplayName   = entry.displayName;
            Author        = entry.author;
            AuthorEmail   = entry.authorEmail;
            Group         = entry.group;
            LatestVersion = entry.latest;
            Versions      = entry.versions;
            Description   = entry.description;
            Dependencies  = entry.dependencies ?? new Dictionary<string, string>();
        }

        public VersionInfo LatestInfo => Versions[LatestVersion];
    }
}