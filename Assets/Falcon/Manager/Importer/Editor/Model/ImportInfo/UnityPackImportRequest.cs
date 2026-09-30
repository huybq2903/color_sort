/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using System;
using UnityEngine.Scripting;

namespace Falcon.Manager.Importer
{
    [Serializable]
    public class UnityPackImportRequest : IImportRequest
    {
        public string name;
        public string displayName;
        public string latestVersion;

        [Preserve]
        public UnityPackImportRequest()
        {
        }

        public UnityPackImportRequest(UnityPackageInfo unityPackageInfo)
        {
            name = unityPackageInfo.Name;
            displayName = unityPackageInfo.DisplayName;
            latestVersion = unityPackageInfo.LatestVersion;
        }
    }
}