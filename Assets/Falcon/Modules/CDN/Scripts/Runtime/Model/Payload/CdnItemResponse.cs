/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.CDN
{
    [Serializable]
    public class CdnItemResponse
    {
        public string[] folderSegments;
        public string fileName;
        public string extension;
        public string fileHash;
        public string url;
        public DateTime lastUpdatedUtc;

        [Preserve]
        public CdnItemResponse()
        {
        }

        public CdnItem CdnItem => new(fileName, folderSegments);
    }
}