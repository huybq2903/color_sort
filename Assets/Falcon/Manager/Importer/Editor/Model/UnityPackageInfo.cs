/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */
using System;
using JetBrains.Annotations;
using UnityEditor.PackageManager;
// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public sealed class UnityPackageInfo : IPackageInfo, IEquatable<UnityPackageInfo>
    {
        public string Name { get; }
        public string DisplayName { get; }
        public bool Installed { get; }
        public string InstalledVersion { get; }
        public string LatestVersion { get; }
        public string Description { get; }
        
        /// <summary>
        /// Xây thông tin tổng hợp
        /// </summary>
        /// <param name="remoteFalconPackage"> thông tin gói cài ở remote</param>
        /// <param name="localFalconPackage"> thông tin gói cài ở local</param>
        public UnityPackageInfo([NotNull] PackageInfo remoteFalconPackage, [CanBeNull] PackageInfo localFalconPackage)
        {
            Name = remoteFalconPackage.name;
            LatestVersion = remoteFalconPackage.versions.latestCompatible;
            if (localFalconPackage == null)
            {
                DisplayName = remoteFalconPackage.displayName ??  remoteFalconPackage.name;
                Installed = false;
                InstalledVersion = null;
                Description = remoteFalconPackage.description;
            }
            else
            {
                DisplayName = remoteFalconPackage.displayName ?? localFalconPackage.displayName ?? remoteFalconPackage.name;
                Installed = true;
                InstalledVersion = localFalconPackage.version;
                Description = remoteFalconPackage.description ?? localFalconPackage.description;
            }
        }
        
        public bool Equals(UnityPackageInfo other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && DisplayName == other.DisplayName && Installed == other.Installed && InstalledVersion == other.InstalledVersion && LatestVersion == other.LatestVersion && Description == other.Description;
        }

        public bool Equals(IPackageInfo other)
        {
            return other is UnityPackageInfo unityPackageInfo && Equals(unityPackageInfo);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UnityPackageInfo)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, DisplayName, Installed, InstalledVersion, LatestVersion, Description);
        }

        public static bool operator ==(UnityPackageInfo left, UnityPackageInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UnityPackageInfo left, UnityPackageInfo right)
        {
            return !Equals(left, right);
        }
    }
}