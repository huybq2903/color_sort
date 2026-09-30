/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using System.Linq;
using Newtonsoft.Json;

namespace Falcon.Modules.CDN
{
    [Serializable]
    public readonly struct CdnItem : IEquatable<CdnItem>
    {
        public readonly string fileName;
        public readonly string[] folderSegments;

        public CdnItem(string fileName, string[] folderSegments = null)
        {
            this.fileName = fileName;
            this.folderSegments = folderSegments ?? Array.Empty<string>();
        }

        [JsonIgnore] public string[] FolderSegments => folderSegments ?? Array.Empty<string>();

        public bool Equals(CdnItem other)
        {
            return string.Equals(fileName, other.fileName, StringComparison.Ordinal) &&
                   FolderSegments.SequenceEqual(other.FolderSegments);
        }

        public override bool Equals(object obj)
        {
            return obj is CdnItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // Use ordinal hash for cross-platform stability (string.GetHashCode is randomized on mobile)
                var hash = fileName != null ? StringComparer.Ordinal.GetHashCode(fileName) : 0;
                return FolderSegments.Aggregate(hash, (current, seg) =>
                    (current * 397) ^ (seg != null ? StringComparer.Ordinal.GetHashCode(seg) : 0));
            }
        }

        public static bool operator ==(CdnItem left, CdnItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CdnItem left, CdnItem right)
        {
            return !left.Equals(right);
        }
    }
}