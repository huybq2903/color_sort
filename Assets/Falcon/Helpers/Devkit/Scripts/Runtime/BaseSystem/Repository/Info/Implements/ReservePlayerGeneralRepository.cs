/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
using UnityEngine.iOS;
#endif
using System;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class ReservePlayerGeneralRepository : IFPlayerGeneralRepository, IInit
    {
        private const string kAnalyticDataPrefix = "Analytic_SDK_Data_";

        private const string kAccountIDKey = kAnalyticDataPrefix + "Account_ID_Key";

        private const string kMaxLevelKey = kAnalyticDataPrefix + "Max_Level";
        private const string kInstallVersionKey = kAnalyticDataPrefix + "Install_Version";
        private const string kFalconAdvertisingID = "falcon_analytics_advertising_id";
        private readonly BasicPoolData<string> _accountId;
        private readonly BasicPoolData<SealedBox<string>> _advertisingId;
        private readonly BasicPoolData<string> _installVersion;
        private readonly DelegatePoolData<int> _maxPassedLevel;
        
        public ReservePlayerGeneralRepository(IDataPool dataPool, IFDeviceInfoRepository deviceInfoRepository, IFAppInfoRepository appInfoRepository)
        {
            _accountId = new BasicPoolData<string>(dataPool, kAccountIDKey, deviceInfoRepository.DeviceId);
            _maxPassedLevel = DelegatePoolData<int>.Factory.SetDelegate(dataPool, kMaxLevelKey, 0,
                (oldVal, newVal) => SealedBox.Of(Math.Max(oldVal, newVal)));
            _installVersion = new BasicPoolData<string>(dataPool, kInstallVersionKey, appInfoRepository.AppVersion);
            _advertisingId =
                new BasicPoolData<SealedBox<string>>(dataPool, kFalconAdvertisingID, SealedBox.Empty<string>());
        }

        public string AdvertisingID
        {
            get => _advertisingId.Value.TryGetValue(out var value) ? value : null;
            set => _advertisingId.Value = SealedBox.Of(value);
        }

        public string AccountID
        {
            get => _accountId.Value;
            set => _accountId.Value = value;
        }

        public int MaxPassedLevel
        {
            get => _maxPassedLevel.Value;
            set => _maxPassedLevel.Value = value;
        }

        public string InstallVersion
        {
            get => _installVersion.Value;
            set => _installVersion.Value = value;
        }

        public async Task Init(CancellationToken cancellationToken = default)
        {
            var installVersion = _installVersion.Value;
            BaseSystemLogger.Instance.Info($"Player Install Version: {installVersion}");
            try
            {
#if UNITY_EDITOR
                _advertisingId.Value = SealedBox.Of("advertisingId-editor");
                await Task.CompletedTask;
#elif UNITY_IOS
                if (!_advertisingId.Value.HasValue)
                {
                    string adId = null;
                    string exception = null;

                    if (!Application.RequestAdvertisingIdentifierAsync(
                            (advertisingId, _, error) =>
                            {
                                adId = advertisingId;
                                exception = error;
                            }
                        ))
                    {
                        BaseSystemLogger.Instance.Warning("Failed to get advertising ID since platform doesn't support");
                        return;
                    }

                    while (adId == null && exception == null)
                    {
                        await Task.Yield();
                    }
                
                    if (!string.IsNullOrEmpty(exception))
                    {
                        BaseSystemLogger.Instance.Error($"Failed to get advertising ID : {exception}");
                    }
                    else
                    {
                        _advertisingId.Value = SealedBox.Of(adId);
                    }
                }
#elif UNITY_ANDROID
                 AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                 AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
                 AndroidJavaClass client =
  new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
                 AndroidJavaObject adInfo =
  client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);
                 var adId = adInfo.Call<string>("getId");
                 _advertisingId.Value = SealedBox.Of(adId);
                 await Task.CompletedTask;
#endif
            }
            catch (Exception e)
            {
                BaseSystemLogger.Instance.Error(e);
            }
        }
    }
}