/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-17
 */

using System;

namespace Falcon.Modules.Core.ThirdParty.AndroidReferrer.Runtime
{
#if UNITY_EDITOR
    public class PlayInstallReferrerEditor
    {
        // public API
        public static void GetInstallReferrerInfo(Action<PlayInstallReferrerDetails> callback)
        {
            PlayInstallReferrerDetails installReferrerDetails = new PlayInstallReferrerDetails(
                "test-install-referrer",
                123456,
                123456,
                123456,
                123456,
                "1.2.3.4.5.6",
                false);
            callback(installReferrerDetails);
        }
    }
#endif
}