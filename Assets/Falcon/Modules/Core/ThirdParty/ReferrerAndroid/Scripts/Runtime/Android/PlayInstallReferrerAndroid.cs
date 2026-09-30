/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-17
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.AndroidReferrer.Runtime
{
#if UNITY_ANDROID
    public class PlayInstallReferrerAndroid
    {
        
        private const int INSTALL_REFERRER_TIMEOUT_MS = 30000;
        
        private static InstallReferrerStateListener installReferrerStateProxy;
        private static AndroidJavaObject ajoInstallReferrerClient;
        private static Dictionary<int, string> installReferrerResponseCodes;

        // Unity main thread context + watchdog
        private static SynchronizationContext unityContext;
        private static int completed; // 0 = pending, 1 = finished
        private static Timer watchdogTimer;

        public static void GetInstallReferrerInfo(Action<PlayInstallReferrerDetails> callback)
        {
            // Capture Unity main thread context (call from Awake/Start).
            unityContext = SynchronizationContext.Current;
            Interlocked.Exchange(ref completed, 0);

            // Fail-fast timeout to avoid waiting forever if Play Store binder is stuck.
            watchdogTimer?.Dispose();
            watchdogTimer = new Timer(_ =>
            {
                if (Interlocked.Exchange(ref completed, 1) == 1) return;

                var err = new PlayInstallReferrerError(
                    -1,
                    new TimeoutException("InstallReferrer timeout (binder call too slow)"));

                PostToUnity(() => callback(new PlayInstallReferrerDetails(err)));
                SafeEndConnection();
            }, null, INSTALL_REFERRER_TIMEOUT_MS, Timeout.Infinite);

            ajoInstallReferrerClient = GetInstallReferrerClient();
            if (ajoInstallReferrerClient == null)
            {
                Debug.LogError("Unable to obtain InstallReferrerClient instance");
                return;
            }

            installReferrerStateProxy = new InstallReferrerStateListener(callback);
            ajoInstallReferrerClient.Call("startConnection", installReferrerStateProxy);
        }

        private static void PostToUnity(Action action)
        {
            var ctx = unityContext;
            if (ctx != null) ctx.Post(_ => action(), null);
            else action();
        }

        private static void SafeEndConnection()
        {
            try { ajoInstallReferrerClient?.Call("endConnection"); }
            catch { /* ignored */ }

            watchdogTimer?.Dispose();
            watchdogTimer = null;
        }

        private static AndroidJavaObject GetInstallReferrerClient()
        {
            if (ajoInstallReferrerClient == null)
            {
                AndroidJavaObject ajoCurrentActivity =
                    new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                        .GetStatic<AndroidJavaObject>("currentActivity");

                AndroidJavaClass ajcInstallReferrerClient =
                    new AndroidJavaClass("com.android.installreferrer.api.InstallReferrerClient");

                ajoInstallReferrerClient = ajcInstallReferrerClient
                    .CallStatic<AndroidJavaObject>("newBuilder", ajoCurrentActivity)
                    .Call<AndroidJavaObject>("build");
            }

            if (installReferrerResponseCodes == null)
            {
                installReferrerResponseCodes = new Dictionary<int, string>();

                var responseClass = new AndroidJavaClass(
                    "com.android.installreferrer.api.InstallReferrerClient$InstallReferrerResponse");

                installReferrerResponseCodes.Add(responseClass.GetStatic<int>("OK"), "OK");
                installReferrerResponseCodes.Add(responseClass.GetStatic<int>("FEATURE_NOT_SUPPORTED"), "FEATURE_NOT_SUPPORTED");
                installReferrerResponseCodes.Add(responseClass.GetStatic<int>("SERVICE_UNAVAILABLE"), "SERVICE_UNAVAILABLE");
                installReferrerResponseCodes.Add(responseClass.GetStatic<int>("DEVELOPER_ERROR"), "DEVELOPER_ERROR");
                installReferrerResponseCodes.Add(responseClass.GetStatic<int>("SERVICE_DISCONNECTED"), "SERVICE_DISCONNECTED");
            }

            return ajoInstallReferrerClient;
        }

        private class InstallReferrerStateListener : AndroidJavaProxy
        {
            private readonly Action<PlayInstallReferrerDetails> callback;

            public InstallReferrerStateListener(Action<PlayInstallReferrerDetails> pCallback)
                : base("com.android.installreferrer.api.InstallReferrerStateListener")
            {
                callback = pCallback;
            }

            public void onInstallReferrerSetupFinished(int responseCode)
            {
                try
                {
                    int okCode = installReferrerResponseCodes.FirstOrDefault(x => x.Value == "OK").Key;

                    if (responseCode == okCode)
                    {
                        Debug.Log("InstallReferrerResponse.OK status code received");

                        // IMPORTANT: getInstallReferrer() is a blocking Binder call.
                        // Do NOT execute it on main thread (ANR risk). Run on thread-pool (Task).
                        Task.Run(() =>
                        {
                            AndroidJNI.AttachCurrentThread();
                            try
                            {
                                AndroidJavaObject ajoReferrerDetails =
                                    ajoInstallReferrerClient.Call<AndroidJavaObject>("getInstallReferrer");

                                if (ajoReferrerDetails == null)
                                    throw new Exception("getInstallReferrer returned null AndroidJavaObject!");

                                string installReferrer = ajoReferrerDetails.Call<string>("getInstallReferrer");
                                long installBeginTimestampSeconds = ajoReferrerDetails.Call<long>("getInstallBeginTimestampSeconds");
                                long referrerClickTimestampSeconds = ajoReferrerDetails.Call<long>("getReferrerClickTimestampSeconds");
                                long installBeginTimestampServerSeconds = ajoReferrerDetails.Call<long>("getInstallBeginTimestampServerSeconds");
                                long referrerClickTimestampServerSeconds = ajoReferrerDetails.Call<long>("getReferrerClickTimestampServerSeconds");
                                string installVersion = ajoReferrerDetails.Call<string>("getInstallVersion");
                                bool googlePlayInstant = ajoReferrerDetails.Call<bool>("getGooglePlayInstantParam");

                                var details = new PlayInstallReferrerDetails(
                                    installReferrer,
                                    referrerClickTimestampSeconds,
                                    installBeginTimestampSeconds,
                                    referrerClickTimestampServerSeconds,
                                    installBeginTimestampServerSeconds,
                                    installVersion,
                                    googlePlayInstant
                                );

                                if (Interlocked.Exchange(ref completed, 1) == 1) return;

                                PostToUnity(() => callback(details));
                            }
                            catch (Exception e)
                            {
                                if (Interlocked.Exchange(ref completed, 1) == 1) return;

                                Debug.LogError("Exception: " + e);
                                var err = new PlayInstallReferrerError(responseCode, e);

                                PostToUnity(() => callback(new PlayInstallReferrerDetails(err)));
                            }
                            finally
                            {
                                SafeEndConnection();
                                AndroidJNI.DetachCurrentThread();
                            }
                        });

                        return;
                    }

                    // Non-OK response: return error immediately (main thread is fine here).
                    if (Interlocked.Exchange(ref completed, 1) == 1) return;

                    if (responseCode == installReferrerResponseCodes.FirstOrDefault(x => x.Value == "FEATURE_NOT_SUPPORTED").Key)
                        Debug.LogError("InstallReferrerResponse.FEATURE_NOT_SUPPORTED status code received");
                    else if (responseCode == installReferrerResponseCodes.FirstOrDefault(x => x.Value == "SERVICE_UNAVAILABLE").Key)
                        Debug.LogError("InstallReferrerResponse.SERVICE_UNAVAILABLE status code received");
                    else if (responseCode == installReferrerResponseCodes.FirstOrDefault(x => x.Value == "DEVELOPER_ERROR").Key)
                        Debug.LogError("InstallReferrerResponse.DEVELOPER_ERROR status code received");
                    else if (responseCode == installReferrerResponseCodes.FirstOrDefault(x => x.Value == "SERVICE_DISCONNECTED").Key)
                        Debug.LogError("InstallReferrerResponse.SERVICE_DISCONNECTED status code received");
                    else
                        Debug.LogError("InstallReferrerResponse.Unknown status code received: " + responseCode);

                    var error = new PlayInstallReferrerError(responseCode, null);
                    PostToUnity(() => callback(new PlayInstallReferrerDetails(error)));
                    SafeEndConnection();
                }
                catch (Exception e)
                {
                    if (Interlocked.Exchange(ref completed, 1) == 1) return;

                    Debug.LogError("Exception: " + e);
                    var err = new PlayInstallReferrerError(responseCode, e);
                    PostToUnity(() => callback(new PlayInstallReferrerDetails(err)));
                    SafeEndConnection();
                }
            }

            public void onInstallReferrerServiceDisconnected()
            {
                Debug.Log("onInstallReferrerServiceDisconnected invoked");
            }
        }
    }
#endif
}
