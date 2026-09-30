/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */


using System;
using Falcon.Manager.Shared;
using UnityEngine.Scripting;

namespace Falcon.Manager.Importer
{
    [Serializable]
    public class FPackImportRequest : IImportRequest
    {
        public string name;
        public string displayName;
        public string latestVersion;
        public VersionInfo latestInfo;
        public string installedPath;

        [Preserve]
        public FPackImportRequest()
        {
        }

        public FPackImportRequest(FalconPackageInfo package)
        {
            name = package.Name;
            displayName = package.DisplayName;
            latestVersion = package.LatestVersion;
            latestInfo = package.LatestVersionInfo;
            installedPath = package.InstalledPath;
        }
    }
}