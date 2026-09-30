/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

#if !UNITY_EDITOR && UNITY_ANDROID
using System.Security.Cryptography;
using System.Text;
#endif
using UnityEngine;
using System;

#if UNITY_IOS
using System.Globalization;
using Falcon;
using Newtonsoft.Json;
using UnityEngine.iOS;
#endif

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [NoLazy]
    public class ReserveDeviceInfoRepository : IFDeviceInfoRepository
    {
        private const string kDeviceIDKey = "FDeviceInfoRepo_Device_Id";

#if UNITY_IOS
        private readonly IDataPool _dataPool;
#endif
        private readonly DelegatePoolData<string> _deviceId;

        public string DeviceId => _deviceId.Value;
        public string DeviceName { get; } = SystemInfo.deviceName.ToLower();
        public string OperatingSystem { get; } = SystemInfo.operatingSystem.ToLower();
        public string DeviceModel { get; } = SystemInfo.deviceModel.ToLower();
        public int ScreenWidth { get; } = Screen.width;
        public int ScreenHeight { get; } = Screen.height;
        public float ScreenDpi { get; } = Screen.dpi;
        public string GpuName { get; } = SystemInfo.graphicsDeviceName.ToLower();
        public string CpuType { get; } = SystemInfo.processorType.ToLower();
        public string Language { get; } = Application.systemLanguage.ToString();


        // ReSharper disable once IdentifierTypo
        public string Idfv { get; }
        public int Ram { get; } = SystemInfo.systemMemorySize;
        public int GpuRam { get; }= SystemInfo.graphicsMemorySize;
        public int CpuCount { get; } = SystemInfo.processorCount;
        public int CpuFrequency { get; } = SystemInfo.processorFrequency;

        public ReserveDeviceInfoRepository(IDataPool dataPool)
        {
#if UNITY_IOS
            _dataPool = dataPool;
#endif
            _deviceId = DelegatePoolData<string>.Factory.GetDelegate(dataPool, kDeviceIDKey, GetDeviceId);
#if UNITY_IOS
            Idfv = Device.vendorIdentifier;
#elif UNITY_ANDROID
            Idfv = SystemInfo.deviceUniqueIdentifier;
#endif
        }

        private
#if !UNITY_IOS
            static
#endif
            string GetDeviceId()
        {
            // ReSharper disable once RedundantAssignment
            var deviceId = "";
#if UNITY_EDITOR
            deviceId = SystemInfo.deviceUniqueIdentifier + "-editor";
#elif UNITY_ANDROID
            try
            {
                var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
                var contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
                var secure = new AndroidJavaClass("android.provider.Settings$Secure");
                deviceId = secure.CallStatic<string>("getString", contentResolver, "android_id");


                if (string.IsNullOrEmpty(deviceId) || deviceId.ToLower().Equals("unknown") || deviceId.Length < 4)
                {
                    var client =
                        new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
                    var adInfo =
                        client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);
                    deviceId = adInfo.Call<string>("getId");
                }

                
                if (string.IsNullOrEmpty(deviceId) || deviceId.ToLower().Equals("unknown") || deviceId.Length < 4)
                    deviceId = SystemInfo.deviceUniqueIdentifier;
                
                if (string.IsNullOrEmpty(deviceId) || deviceId.ToLower().Equals("unknown") || deviceId.Length < 4)
                    deviceId = CreateUniqueString();
            }
            catch (Exception e)
            {
                if (string.IsNullOrEmpty(deviceId) || deviceId.ToLower().Equals("unknown") || deviceId.Length < 4)
                    deviceId = SystemInfo.deviceUniqueIdentifier;
                
                if (string.IsNullOrEmpty(deviceId) || deviceId.ToLower().Equals("unknown") || deviceId.Length < 4)
                    deviceId = CreateUniqueString();
            }

#elif UNITY_WEBGL
            deviceId = _dataPool.GetOrSet("UniqueIdentifier", Guid.NewGuid().ToString());
#elif UNITY_IOS
            var key = "falcon_" + Application.identifier + "_uuid";
            deviceId = UUIDiOS.GetKeyChainValue(key);

            if (string.IsNullOrEmpty(deviceId) || deviceId.ToLower().Equals("unknown") || deviceId.Length < 4)
            {
                deviceId = Guid.NewGuid().ToString();
                UUIDiOS.SaveKeyChainValue(key, deviceId);
            }

            if (string.IsNullOrEmpty(deviceId) || deviceId.ToLower().Equals("unknown") || deviceId.Length < 4)
            {
                deviceId = Device.vendorIdentifier;
            }

#else
            deviceId = SystemInfo.deviceUniqueIdentifier;
#endif

            return deviceId;
        }

#if !UNITY_EDITOR && UNITY_ANDROID
        private static string CreateUniqueString()
        {
            // Get device Info to create a String
            var deviceInfo = SystemInfo.deviceModel + SystemInfo.deviceType + SystemInfo.graphicsDeviceName +
                             SystemInfo.graphicsDeviceType;

            // encode that string to create a String which is unique
            using (var sha1 = SHA1.Create())
            {
                var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(deviceInfo));
                var stringBuilder = new StringBuilder();
                foreach (var b in hashBytes) stringBuilder.Append(b.ToString("x2"));
                return stringBuilder.ToString();
            }
        }
#endif
    }
}