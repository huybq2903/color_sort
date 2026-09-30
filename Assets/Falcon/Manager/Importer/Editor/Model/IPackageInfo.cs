/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */
using System;
// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public interface IPackageInfo : IEquatable<IPackageInfo>
    {
        public string Name {get;}
        public string DisplayName {get;}
        public bool Installed {get;}
        public string InstalledVersion {get;}
        public string LatestVersion {get;}
        public string Description {get;}
    }

    public static class PackageInfoExtensions
    {
        
        private const string VERSION_DELIMITER = ".";
        public static bool IsInstallLatestVersion(this IPackageInfo info) => info.IsInstallAtLeastVersion(info.LatestVersion);
        public static bool IsInstallAtLeastVersion(this IPackageInfo info, string version)
        {
            if (!info.Installed) return false;
            var requiredSplits = version.Split(VERSION_DELIMITER);
            var installedSplits = info.InstalledVersion.Split(VERSION_DELIMITER);
            for (var i = 0; i < Math.Max(requiredSplits.Length, installedSplits.Length); i++)
            {
                if(requiredSplits.Length < i +1) return true;
                if(installedSplits.Length < i +1) return false;

                var compareTo = int.Parse(installedSplits[i]).CompareTo(int.Parse(requiredSplits[i]));
                if (compareTo != 0)
                {
                    return compareTo > 0;
                }
            }
            return true;
        }
    }
}