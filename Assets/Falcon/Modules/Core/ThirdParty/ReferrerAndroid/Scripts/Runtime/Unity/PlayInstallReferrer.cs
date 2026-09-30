/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-17
 */

using System;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.AndroidReferrer.Runtime
{
    public class PlayInstallReferrer : MonoBehaviour
    {
        public static void GetInstallReferrerInfo(Action<PlayInstallReferrerDetails> callback)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            PlayInstallReferrerAndroid.GetInstallReferrerInfo(callback);
#elif UNITY_EDITOR
            PlayInstallReferrerEditor.GetInstallReferrerInfo(callback);
#else
            Debug.LogError("play-install-referrer plugin can only be used in Android apps.");
#endif
        }
    }
}
