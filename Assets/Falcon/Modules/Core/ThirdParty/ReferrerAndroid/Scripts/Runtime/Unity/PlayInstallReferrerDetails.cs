/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-17
 */

namespace Falcon.Modules.Core.ThirdParty.AndroidReferrer.Runtime
{
    public class PlayInstallReferrerDetails
    {
        public string InstallReferrer { get; }
        public long? ReferrerClickTimestampSeconds { get; }
        public long? InstallBeginTimestampSeconds { get; }
        public long? ReferrerClickTimestampServerSeconds { get; }
        public long? InstallBeginTimestampServerSeconds { get; }
        public string InstallVersion { get; }
        public bool? GooglePlayInstant { get; }

        public PlayInstallReferrerError Error { get; }

        internal PlayInstallReferrerDetails(
            string installReferrer,
            long referrerClickTimestampSeconds,
            long installBeginTimestampSeconds,
            long referrerClickTimestampServerSeconds,
            long installBeginTimestampServerSeconds,
            string installVersion,
            bool googlePlayInstant)
        {
            InstallReferrer = installReferrer;
            ReferrerClickTimestampSeconds = referrerClickTimestampSeconds;
            InstallBeginTimestampSeconds = installBeginTimestampSeconds;
            ReferrerClickTimestampServerSeconds = referrerClickTimestampServerSeconds;
            InstallBeginTimestampServerSeconds = installBeginTimestampServerSeconds;
            InstallVersion = installVersion;
            GooglePlayInstant = googlePlayInstant;
        }

        internal PlayInstallReferrerDetails(PlayInstallReferrerError error)
        {
            Error = error;
        }
    }
}