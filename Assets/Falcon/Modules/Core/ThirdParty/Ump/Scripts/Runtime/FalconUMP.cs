/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System;
using System.Collections;
using Falcon.Helpers.EventBus;
#if GOOGLE_MOBILE_ADS_ENABLE
using GoogleMobileAds.Common;
using Falcon.Modules.Core.SaveLoad.Runtime;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif
using UnityEngine;
#if UNITY_IOS
using System;
using Falcon.Modules.Core.RemoteConfig;
using System.Collections;
using Falcon.Helpers.EventBus;
#if UNITY_ADS_IOS_SUPPORT_ENABLE
using Unity.Advertisement.IosSupport;
#endif
#endif

namespace Falcon.Modules.Core.ThirdParty.Ump.Runtime
{
    public class FalconUMP : AutoSingleton<FalconUMP>
    {
        public const string FALCON_UMP_IS_IN_EUROPE = "falcon_ump_is_in_europe";
        public const string FALCON_UMP_HAS_CONSENT = "falcon_ump_has_consent";
        public const string FALCON_UMP_COMPLETE = "falcon.modules.core.thirdparty.ump.complete";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            _ = Instance;
        }

        protected override void Awake()
        {
            base.Awake();
            StartCoroutine(InitUmp());
        }

        private IEnumerator InitUmp()
        {
#if GOOGLE_MOBILE_ADS_ENABLE
            // Set MỘT LẦN, trước mọi Update, và KHÔNG đổi nữa.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAdsEventExecutor.Initialize();
            yield return null;

            bool handled = false;            // chống callback nổ 2 lần / nổ muộn
            bool fired = false;
            FormError err = null;

            ConsentInformation.Update(new ConsentRequestParameters(), e =>
            {
                if (handled) return;         // callback zombie tới muộn -> bỏ qua
                handled = true;
                fired = true;
                err = e;
            });

            // Timeout chỉ để KHÔNG chặn UX vô hạn — KHÔNG gọi Update lần nữa.
            float timeOut = 5f;
            while (!fired && (timeOut -= Time.deltaTime) > 0f) yield return null;

            if (!fired)
            {
                handled = true;              // "đóng" callback: nếu native nổ muộn cũng bị lambda bỏ qua
                Debug.LogError("[UMP] Update timeout.");
                OnShowPopupATT();
                yield break;
            }

            if (err != null)
            {
                Debug.LogError($"FalconUMP > Update error {err.ErrorCode}: {err.Message}");
                OnShowPopupATT();
                yield break;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                OnShowPopupATT();
                if (formError != null)
                {
                    Debug.LogError($"FalconUMP > LoadAndShow error {formError.ErrorCode}: {formError.Message}");
                    return;
                }
                var consent = ConsentInformation.ConsentStatus;
                SaveLoadHandler.Save(FALCON_UMP_IS_IN_EUROPE, consent == ConsentStatus.Required);
                SaveLoadHandler.Save(FALCON_UMP_HAS_CONSENT, consent == ConsentStatus.Obtained);
            });
#endif
            yield break;
        }

        private void OnShowPopupATT()
        {
#if UNITY_IOS
            string cmpString = PlayerPrefs.GetString("IABTCF_AddtlConsent");
            bool showAtt = false;
            if (!string.IsNullOrEmpty(cmpString))
            {
                string[] cmpSlices = cmpString.Split("~");
                if (cmpSlices.Length >= 2)
                {
                    string version = cmpSlices[0];
                    if (version == "2" || version == "1")
                    {
                        if (cmpSlices[1].Contains("2878"))
                        {
                            showAtt = true;
                        }
                    }
                }
            }
            else
            {
                showAtt = true;
            }
#if UNITY_ADS_IOS_SUPPORT_ENABLE
            if (showAtt)
            {
                //show popup ATT
                if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                    ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    ATTrackingStatusBinding.RequestAuthorizationTracking();
                }
            }
            else
            {
                string keyCheckAttSecondOpen = "check_att";
                if (PlayerPrefs.HasKey(keyCheckAttSecondOpen))
                {
                    //show popup ATT
                    var versionFromRemote = FConfigController.Instance.Config<UmpConfig>().versionShowATTInSecondOpen;
                    int compareVersion = -1;
                    if (string.IsNullOrEmpty(versionFromRemote))
                    {
                        compareVersion = -1;
                    }
                    else
                    {
                        compareVersion = CompareVersion(Application.version, versionFromRemote);
                    }

                    if (compareVersion >= 0)
                    {
                        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                            ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                        {
                            ATTrackingStatusBinding.RequestAuthorizationTracking();
                            showAtt = true;
                        }
                    }
                }
                else
                {
                    PlayerPrefs.SetInt(keyCheckAttSecondOpen, 1);
                }
            }

            StartCoroutine(WaitUntilDetermined(showAtt));
#else
            Debug.LogError("Chưa cài package Unity.Advertisement.IosSupport.");
#endif
#elif UNITY_ANDROID
            StartCoroutine(WaitEof());

            IEnumerator WaitEof()
            {
                yield return new WaitForEndOfFrame();
                Debug.Log("init UMP complete");
                GameEvent<bool>.Emit(FALCON_UMP_COMPLETE);
            }

#endif
        }

#if UNITY_IOS
        private int CompareVersion(string v1, string v2)
        {
            try
            {
                var parts1 = v1.Split('.');
                var parts2 = v2.Split('.');

                int maxLength = Math.Max(parts1.Length, parts2.Length);

                for (int i = 0; i < maxLength; i++)
                {
                    int p1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                    int p2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

                    if (p1 > p2) return 1;
                    if (p1 < p2) return -1;
                }

                return 0;
            }
            catch
            {
                return -1;
            }
        }

#endif

#if UNITY_IOS && UNITY_ADS_IOS_SUPPORT_ENABLE
        IEnumerator WaitUntilDetermined(bool showAtt)
        {
#if UNITY_EDITOR
            yield return null;
#else
            if (showAtt)
            {
                yield return new WaitUntil(() =>
                    ATTrackingStatusBinding.GetAuthorizationTrackingStatus() !=
                    ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED);
            }

#endif
            GameEvent<bool>.Emit(FALCON_UMP_COMPLETE);
            Debug.Log("init UMP complete");
        }

#endif
    }
}