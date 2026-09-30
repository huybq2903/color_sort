/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-17
 */

using Falcon.Modules.Core.ThirdParty.Mmp.Runtime;
using Falcon.Modules.Core.ThirdParty.Ump.Runtime;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.AndroidReferrer.Runtime
{
    public class FalconAndroidReferrer : AutoSingleton<FalconAndroidReferrer>
    {
        
        protected override void Awake()
        {
            base.Awake();
            PlayInstallReferrer.GetInstallReferrerInfo(installReferrerDetails =>
            {
                // check for error
                if (installReferrerDetails.Error != null)
                {
                    Debug.LogError("Error occurred!");
                    if (installReferrerDetails.Error.Exception != null)
                    {
                        Debug.LogError("Exception message: " + installReferrerDetails.Error.Exception.Message);
                    }

                    Debug.LogError("Response code: " + installReferrerDetails.Error.ResponseCode);
                    return;
                }

#if UNITY_ANDROID && !UNITY_EDITOR
                GameData4AndroidReferrer.Instance.installReferrer = installReferrerDetails.InstallReferrer;
                Debug.Log("InstallReferrer : " + installReferrerDetails.InstallReferrer);
                if (installReferrerDetails.ReferrerClickTimestampSeconds != null)
                {
                    GameData4AndroidReferrer.Instance.referrerClickTimestampSeconds =
                        installReferrerDetails.ReferrerClickTimestampSeconds.Value;
                    Debug.Log("ReferrerClickTimestampSeconds : " +
                              installReferrerDetails.ReferrerClickTimestampSeconds.Value);
                }

                if (installReferrerDetails.InstallBeginTimestampSeconds != null)
                {
                    GameData4AndroidReferrer.Instance.installBeginTimestampSeconds =
                        installReferrerDetails.InstallBeginTimestampSeconds.Value;
                    Debug.Log("InstallBeginTimestampSeconds : " +
                              installReferrerDetails.InstallBeginTimestampSeconds.Value);
                }

                if (installReferrerDetails.ReferrerClickTimestampServerSeconds != null)
                {
                    GameData4AndroidReferrer.Instance.referrerClickTimestampServerSeconds =
                        installReferrerDetails.ReferrerClickTimestampServerSeconds.Value;
                    Debug.Log("ReferrerClickTimestampServerSeconds : " +
                              installReferrerDetails.ReferrerClickTimestampServerSeconds.Value);
                }

                if (installReferrerDetails.InstallBeginTimestampServerSeconds != null)
                {
                    GameData4AndroidReferrer.Instance.installBeginTimestampServerSeconds =
                        installReferrerDetails.InstallBeginTimestampServerSeconds.Value;
                    Debug.Log("InstallBeginTimestampServerSeconds : " +
                              installReferrerDetails.InstallBeginTimestampServerSeconds.Value);
                }

                GameData4AndroidReferrer.Instance.installVersion = installReferrerDetails.InstallVersion;
                Debug.Log("InstallVersion : " + installReferrerDetails.InstallVersion);

                if (installReferrerDetails.GooglePlayInstant != null)
                {
                    GameData4AndroidReferrer.Instance.googlePlayInstant =
                        installReferrerDetails.GooglePlayInstant.Value;
                    Debug.Log("GooglePlayInstant : " + installReferrerDetails.GooglePlayInstant.Value);
                }

                GameData4AndroidReferrer.Instance.UpdateToServer();
#endif
            });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            _ = Instance;
        }
    }
}