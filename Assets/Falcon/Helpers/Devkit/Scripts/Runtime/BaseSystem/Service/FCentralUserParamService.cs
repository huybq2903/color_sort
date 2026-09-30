/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class FCentralUserParamService : MySingleton<FCentralUserParamService>
    {
        private readonly IFAppInfoRepository _appInfoRepository;
        private readonly IFDeviceInfoRepository _deviceInfoRepository;
        private readonly FPlayerInfoService _infoService;

        public FCentralUserParamService(IFDeviceInfoRepository deviceInfoRepository,
            IFAppInfoRepository appInfoRepository, FPlayerInfoService playerInfoService)
        {
            _deviceInfoRepository = deviceInfoRepository;
            _appInfoRepository = appInfoRepository;
            _infoService = playerInfoService;
        }

        public Dictionary<string, object> GetUserParams()
        {
            var dictionary = new Dictionary<string, object>()
                //general
                .Put(ParamKey.General.ACCOUNT_ID, _infoService.General.AccountID)
                .Put(ParamKey.General.LEVEL, _infoService.General.MaxPassedLevel)
                .Put(ParamKey.General.MAX_PASSED_LEVEL, _infoService.General.MaxPassedLevel)
                .Put(ParamKey.General.INSTALL_VERSION, _infoService.General.InstallVersion)
                .PutIfNotNull(ParamKey.General.ADVERTISING_ID, _infoService.General.AdvertisingID)
                //session
                .Put(ParamKey.Session.INSTALL_DAY, MyTime.DateToString(_infoService.Session.FirstLoginDateUtc()))
                .Put(ParamKey.Session.INSTALL_DAY_LOCAL, MyTime.DateToString(_infoService.Session.FirstLoginDateLocal))
                .Put(ParamKey.Session.ACTIVE_DAYS, _infoService.Session.ActiveDays)
                .Put(ParamKey.Session.SESSION_ID, _infoService.Session.SessionId)
                .Put(ParamKey.Session.SESSION_UID, _infoService.Session.SessionUid)
                .Put(ParamKey.Session.FIRST_LOGIN, _infoService.Session.FirstLogInMillis)
                .Put(ParamKey.Session.TOTAL_PLAY_TIME, (long)_infoService.Session.TotalPlayTime.TotalSeconds)
                .Put(ParamKey.Session.RETENTION_DAY, _infoService.Session.Retention)
                //ad
                .PutIfNotNull(ParamKey.Ad.AD_LTV, _infoService.Ad.AdLtv)
                .Put(ParamKey.Ad.INTER_AD_COUNT, _infoService.Ad.AdCountOf(AdType.Interstitial))
                .Put(ParamKey.Ad.REWARD_AD_COUNT, _infoService.Ad.AdCountOf(AdType.Reward))
                //iap
                .PutIfNotNull(ParamKey.Iap.FIRST_IN_APP_LV, _infoService.Iap.FirstInAppLv)
                .PutIfNotNull(ParamKey.Iap.FIRST_IN_APP_DATE_STR, _infoService.Iap.FirstInAppDateStr)
                .PutIfNotNull(ParamKey.Iap.FIRST_IN_APP_PRODUCT, _infoService.Iap.FirstInAppProduct)
                .PutIfNotNull(ParamKey.Iap.LAST_IN_APP_LV, _infoService.Iap.LastInAppLv)
                .PutIfNotNull(ParamKey.Iap.LAST_IN_APP_DATE_STR, _infoService.Iap.LastInAppDateStr)
                .PutIfNotNull(ParamKey.Iap.LAST_IN_APP_PRODUCT, _infoService.Iap.LastInAppProduct)
                .Put(ParamKey.Iap.IN_APP_COUNT, _infoService.Iap.InAppCount)
                //app
                .Put(ParamKey.App.PLATFORM, _appInfoRepository.Platform)
                .Put(ParamKey.App.APP_VERSION, _appInfoRepository.AppVersion)
                //device
                .Put(ParamKey.Device.DEVICE_ID, _deviceInfoRepository.DeviceId)
                .Put(ParamKey.Device.DEVICE_OS, _deviceInfoRepository.OperatingSystem)
                .Put(ParamKey.Device.DEVICE_NAME, _deviceInfoRepository.DeviceName)
                .Put(ParamKey.Device.DEVICE_MODEL, _deviceInfoRepository.DeviceModel)
                .Put(ParamKey.Device.SCREEN_WIDTH, _deviceInfoRepository.ScreenWidth)
                .Put(ParamKey.Device.SCREEN_HEIGHT, _deviceInfoRepository.ScreenHeight)
                .Put(ParamKey.Device.SCREEN_DPI, _deviceInfoRepository.ScreenDpi)
                .Put(ParamKey.Device.DEVICE_GPU, _deviceInfoRepository.GpuName)
                .Put(ParamKey.Device.DEVICE_CPU, _deviceInfoRepository.CpuType)
                .Put(ParamKey.Device.DEVICE_RAM, _deviceInfoRepository.Ram)
                .Put(ParamKey.Device.DEVICE_GPU_RAM, _deviceInfoRepository.GpuRam)
                .Put(ParamKey.Device.DEVICE_CPU_COUNT, _deviceInfoRepository.CpuCount)
                .Put(ParamKey.Device.DEVICE_CPU_FREQUENCY, _deviceInfoRepository.CpuFrequency)
                .Put(ParamKey.Device.LANGUAGE, _deviceInfoRepository.Language)
                .Put(ParamKey.Device.IDFV, _deviceInfoRepository.Idfv);

            var inAppData = _infoService.Iap.InAppLtv;
            if (inAppData != null)
                dictionary
                    .Put(ParamKey.Iap.IN_APP_MAX, inAppData.max)
                    .Put(ParamKey.Iap.IN_APP_TOTAL, inAppData.total)
                    .Put(ParamKey.Iap.IN_APP_CURRENCY, inAppData.isoCurrencyCode);
            foreach (var (key, value) in _infoService.Custom.GetInfo()) dictionary.PutIfAbsent(key, value);
            return dictionary;
        }

        public static class ParamKey
        {
            public static class General
            {
                public const string ACCOUNT_ID = "accountId";
                public const string LEVEL = "level";
                public const string MAX_PASSED_LEVEL = "maxPassedLevel";
                public const string INSTALL_VERSION = "installVersion";
                public const string ADVERTISING_ID = "advertisingId";
            }

            public static class Session
            {
                public const string ACTIVE_DAYS = "activeDays";
                public const string SESSION_ID = "sessionId";
                public const string SESSION_UID = "sessionUid";
                public const string FIRST_LOGIN = "firstLogin";
                public const string TOTAL_PLAY_TIME = "totalPlayTime";
                public const string RETENTION_DAY = "retentionDay";
                public const string INSTALL_DAY = "installDay";
                public const string INSTALL_DAY_LOCAL = "installDayLocal";
            }

            public static class Ad
            {
                public const string AD_LTV = "adLtv";
                public const string INTER_AD_COUNT = "interAdCount";
                public const string REWARD_AD_COUNT = "rewardAdCount";
            }

            public static class Iap
            {
                public const string FIRST_IN_APP_LV = "firstInAppLv";
                public const string FIRST_IN_APP_DATE_STR = "firstInAppDateStr";
                public const string FIRST_IN_APP_PRODUCT = "firstInAppProduct";
                
                public const string LAST_IN_APP_LV = "lastInAppLv";
                public const string LAST_IN_APP_DATE_STR = "lastInAppDateStr";
                public const string LAST_IN_APP_PRODUCT = "lastInAppProduct";
                
                public const string IN_APP_COUNT = "inAppCount";
                public const string IN_APP_MAX = "inAppMax";
                public const string IN_APP_TOTAL = "inAppTotal";
                public const string IN_APP_CURRENCY = "inAppCurrency";
            }

            public static class App
            {
                public const string PLATFORM = "platform";
                public const string APP_VERSION = "appVersion";
            }

            public static class Device
            {
                public const string DEVICE_OS = "deviceOs";
                public const string DEVICE_MODEL = "deviceModel";
                public const string SCREEN_WIDTH = "screenWidth";
                public const string SCREEN_HEIGHT = "screenHeight";
                public const string SCREEN_DPI = "screenDpi";
                public const string DEVICE_GPU = "deviceGpu";
                public const string DEVICE_CPU = "deviceCpu";
                public const string LANGUAGE = "language";
                public const string IDFV = "idFv";
                public const string DEVICE_ID = "deviceId";
                public const string DEVICE_NAME = "deviceName";
                public const string DEVICE_RAM = "deviceRam";
                public const string DEVICE_GPU_RAM = "deviceGpuRam";
                public const string DEVICE_CPU_COUNT = "deviceCpuCount";
                public const string DEVICE_CPU_FREQUENCY = "deviceCpuFrequency";
            }
        }
    }
}