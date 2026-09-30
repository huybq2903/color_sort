/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Falcon.Manager.Shared;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{

    [SuppressMessage("ReSharper", "CommentTypo")]
    public sealed class FalconPackageInfo : IPackageInfo, IEquatable<FalconPackageInfo>
    {
        public string                     Name                  { get; }
        public string                     DisplayName           { get; }
        public string                     Author                { get; }
        public string                     AuthorEmail            { get; }
        public string                     Group                 { get; }
        public bool                       Installed             { get; }
        public string                     InstalledVersion      { get; }
        public string                     InstalledPath         { get; }
        public string                     LatestVersion         { get; }
        public VersionInfo                LatestVersionInfo     { get; }
        public Dictionary<string, string> InstalledDependencies { get; }
        public Dictionary<string, string> RemoteDependencies    { get; }
        public string                     Description           { get; }

        /// <summary>
        /// Xây thông tin tổng hợp chỉ với gói cài ở local khi remote không rõ thông tin, khả năng do gói này không còn được hỗ trợ nữa
        /// </summary>
        /// <param name="localFalconPackage"> thông tin gói cài ở local</param>
        public FalconPackageInfo(LocalFalconPackage localFalconPackage)
        {
            Name                  = localFalconPackage.name;
            DisplayName           = localFalconPackage.displayName ?? localFalconPackage.name;
            Author                = localFalconPackage.author.name;
            AuthorEmail           = localFalconPackage.author.email;
            Group                 = null;
            Installed             = true;
            InstalledVersion      = localFalconPackage.version;
            InstalledPath         = localFalconPackage.packagePath;
            LatestVersion         = null;
            LatestVersionInfo     = null;
            InstalledDependencies = localFalconPackage.dependencies ?? new Dictionary<string, string>();
            RemoteDependencies    = new Dictionary<string, string>();
            Description           = localFalconPackage.description;
        }
        
        /// <summary>
        /// Xây thông tin tổng hợp chỉ với gói cài ở remote khi local không rõ thông tin, khả năng do gói này chưa được cài
        /// </summary>
        /// <param name="remoteFalconPackage"> thông tin gói cài ở remote</param>
        public FalconPackageInfo(RemoteFalconPackage remoteFalconPackage)
        {
            Name                  = remoteFalconPackage.Name;
            DisplayName           = remoteFalconPackage.DisplayName ?? remoteFalconPackage.Name;
            Author                = remoteFalconPackage.Author ?? "";
            AuthorEmail           = remoteFalconPackage.AuthorEmail ?? "";
            Group                 = remoteFalconPackage.Group;
            Installed             = false;
            InstalledVersion      = null;
            InstalledPath         = null;
            LatestVersion         = remoteFalconPackage.LatestVersion;
            LatestVersionInfo     = remoteFalconPackage.LatestInfo;
            InstalledDependencies = new Dictionary<string, string>();
            RemoteDependencies    = remoteFalconPackage.Dependencies ?? new Dictionary<string, string>();
            Description           = remoteFalconPackage.Description;
        }

        /// <summary>
        /// Xây thông tin tổng hợp
        /// </summary>
        /// <param name="remoteFalconPackage"> thông tin gói cài ở remote</param>
        /// <param name="localFalconPackage"> thông tin gói cài ở local</param>
        public FalconPackageInfo(RemoteFalconPackage remoteFalconPackage, LocalFalconPackage localFalconPackage)
        {
            Name        = remoteFalconPackage.Name;
            DisplayName = remoteFalconPackage.DisplayName ?? localFalconPackage.displayName ?? remoteFalconPackage.Name;

            if (string.IsNullOrEmpty(localFalconPackage.author.name) || localFalconPackage.author.name.Equals("author"))
            {
                Author = remoteFalconPackage.Author;
            }
            else
            {
                Author = localFalconPackage.author.name;
            }

            AuthorEmail           = remoteFalconPackage.AuthorEmail ?? localFalconPackage.author.email;
            Group                 = remoteFalconPackage.Group;
            Installed             = true;
            InstalledVersion      = localFalconPackage.version;
            InstalledPath         = localFalconPackage.packagePath;
            LatestVersion         = remoteFalconPackage.LatestVersion;
            LatestVersionInfo     = remoteFalconPackage.LatestInfo;
            InstalledDependencies = localFalconPackage.dependencies ?? new Dictionary<string, string>();
            RemoteDependencies    = remoteFalconPackage.Dependencies ?? new Dictionary<string, string>();
            Description           = remoteFalconPackage.Description ?? localFalconPackage.description;
        }

        public bool Equals(FalconPackageInfo other)
        {
            if (other == null)
            {
                return false;
            }
            return Name == other.Name && DisplayName == other.DisplayName && Installed == other.Installed && InstalledVersion == other.InstalledVersion && InstalledPath == other.InstalledPath && LatestVersion == other.LatestVersion && Equals(LatestVersionInfo, other.LatestVersionInfo) && Equals(InstalledDependencies, other.InstalledDependencies) && Equals(RemoteDependencies, other.RemoteDependencies) && Description == other.Description;
        }

        public bool Equals(IPackageInfo other)
        {
            return other is FalconPackageInfo falconPackage && Equals(falconPackage);
        }

        public override bool Equals(object obj)
        {
            return obj is FalconPackageInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Name);
            hashCode.Add(DisplayName);
            hashCode.Add(Author);
            hashCode.Add(AuthorEmail);
            hashCode.Add(Group);
            hashCode.Add(Installed);
            hashCode.Add(InstalledVersion);
            hashCode.Add(InstalledPath);
            hashCode.Add(LatestVersion);
            hashCode.Add(LatestVersionInfo);
            hashCode.Add(InstalledDependencies);
            hashCode.Add(RemoteDependencies);
            hashCode.Add(Description);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(FalconPackageInfo left, FalconPackageInfo right)
        {
            if(left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(FalconPackageInfo left, FalconPackageInfo right)
        {
            if(left is null) return right is null;
            return !left.Equals(right);
        }
    }
}