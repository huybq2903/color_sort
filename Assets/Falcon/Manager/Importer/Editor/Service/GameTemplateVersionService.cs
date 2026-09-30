/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-04-20
 */

using System;
using Falcon.Manager.Shared;

namespace Falcon.Manager.Importer
{
    public enum TemplateState
    {
        NotInstalled,
        Installed,
        NeedsUpdate
    }

    public class GameTemplateVersionService
    {
        public TemplateState GetState(GameTemplatePackage remote, InstalledTemplateInfo local)
        {
            if (remote == null || string.IsNullOrEmpty(remote.version))
            {
                return TemplateState.NotInstalled;
            }

            if (local == null || string.IsNullOrEmpty(local.version))
            {
                return TemplateState.NotInstalled;
            }

            int comparison = CompareSemanticVersions(local.version, remote.version);

            if (comparison < 0)
                return TemplateState.NeedsUpdate;

            return TemplateState.Installed;
        }

        private int CompareSemanticVersions(string v1, string v2)
        {
            try
            {
                var version1 = ParseVersion(v1);
                var version2 = ParseVersion(v2);

                if (version1.major != version2.major)
                    return version1.major.CompareTo(version2.major);

                if (version1.minor != version2.minor)
                    return version1.minor.CompareTo(version2.minor);

                return version1.patch.CompareTo(version2.patch);
            }
            catch
            {
                return -1;
            }
        }

        private (int major, int minor, int patch) ParseVersion(string version)
        {
            var parts = version.Split('.');
            if (parts.Length != 3)
                throw new FormatException($"Invalid version format: {version}");

            return (
                int.Parse(parts[0]),
                int.Parse(parts[1]),
                int.Parse(parts[2])
            );
        }
    }
}
