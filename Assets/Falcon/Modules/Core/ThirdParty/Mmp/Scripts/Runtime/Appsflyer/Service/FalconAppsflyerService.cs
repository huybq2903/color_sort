/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-21
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
#if APPSFLYER_ENABLE
using AppsFlyerSDK;
#endif
using Falcon.Modules.Core.ThirdParty.Ump.Runtime;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Runtime
{
    public class FalconAppsflyerService : AutoSingleton<FalconAppsflyerService>
#if APPSFLYER_ENABLE
        , IAppsFlyerConversionData
#endif
    {
        private const string _FALCON_APPSFLYER_CONVERSION_DATA = "falcon_analytics_appsflyer_conversion_data";
        private const string _FALCON_MMP_STARTED = "falcon_mmp_started";
        private const string _FALCON_MMP_LOGIN = "af_login";
        private LazyVal<IDataPool> DataPool = new(MySingletonService.Instance<IDataPool>);

        private LazyVal<IFDeviceInfoRepository> DeviceInfoRepository =
            new(MySingletonService.Instance<IFDeviceInfoRepository>);

        private BasicPoolData<IDictionary<string, JToken>> _conversionData;
        private SOMmpSetting _setting;
        private string _appsflyerID;
        private string _adGroupID;
        private string _origCost;
        private string _afCostCurrency;
        private bool _isFirstLaunch;
        private string _campaignID;
        private string _campaignName;
        private string _afCid;
        private string _mediaSource;
        private string _advertisingID;
        private string _afStatus;
        private double _costCentsUsd;
        private double _afCostValue;
        private string _afCostModel;
        private string _afAD;
        private bool _isRetargeting;
        private string _adgroup;

        public string AppsflyerID
        {
            get => _appsflyerID;
            set
            {
                _appsflyerID = value;
                GameData4Appsflyer.Instance.appsflyerID = value;
                GameData4Appsflyer.Instance.Save();
            }
        }

        private string AdGroupID
        {
            get => _adGroupID;
            set
            {
                _adGroupID = value;
                GameData4Appsflyer.Instance.adGroupID = value;
            }
        }

        private string OrigCost
        {
            get => _origCost;
            set
            {
                _origCost = value;
                GameData4Appsflyer.Instance.origCost = value;
            }
        }

        private string AfCostCurrency
        {
            get => _afCostCurrency;
            set
            {
                _afCostCurrency = value;
                GameData4Appsflyer.Instance.afCostCurrency = value;
            }
        }

        private bool IsFirstLaunch
        {
            get => _isFirstLaunch;
            set
            {
                _isFirstLaunch = value;
                GameData4Appsflyer.Instance.isFirstLaunch = value;
            }
        }

        private string CampaignID
        {
            get => _campaignID;
            set
            {
                _campaignID = value;
                GameData4Appsflyer.Instance.campaignID = value;
                GameEvent<string>.Emit("falcon.modules.thirdparty.appsflyer.campaign_id", value);
            }
        }

        private string CampaignName
        {
            get => _campaignName;
            set
            {
                _campaignName = value;
                GameData4Appsflyer.Instance.campaignName = value;
                GameEvent<string>.Emit("falcon.modules.thirdparty.appsflyer.campaign_name", value);
            }
        }

        private string AfCid
        {
            get => _afCid;
            set
            {
                _afCid = value;
                GameData4Appsflyer.Instance.afCid = value;
            }
        }

        private string MediaSource
        {
            get => _mediaSource;
            set
            {
                _mediaSource = value;
                GameData4Appsflyer.Instance.mediaSource = value;
            }
        }

        private string AdvertisingID
        {
            get => _advertisingID;
            set
            {
                _advertisingID = value;
                GameData4Appsflyer.Instance.advertisingID = value;
            }
        }

        private string AfStatus
        {
            get => _afStatus;
            set
            {
                _afStatus = value;
                GameData4Appsflyer.Instance.afStatus = value;
            }
        }

        private double CostCentsUsd
        {
            get => _costCentsUsd;
            set
            {
                _costCentsUsd = value;
                GameData4Appsflyer.Instance.costCentsUsd = value;
            }
        }

        private double AfCostValue
        {
            get => _afCostValue;
            set
            {
                _afCostValue = value;
                GameData4Appsflyer.Instance.afCostValue = value;
            }
        }

        private string AfCostModel
        {
            get => _afCostModel;
            set
            {
                _afCostModel = value;
                GameData4Appsflyer.Instance.afCostModel = value;
            }
        }

        private string AfAD
        {
            get => _afAD;
            set
            {
                _afAD = value;
                GameData4Appsflyer.Instance.afAD = value;
            }
        }

        private bool IsRetargeting
        {
            get => _isRetargeting;
            set
            {
                _isRetargeting = value;
                GameData4Appsflyer.Instance.isRetargeting = value;
            }
        }

        private string Adgroup
        {
            get => _adgroup;
            set
            {
                _adgroup = value;
                GameData4Appsflyer.Instance.adgroup = value;
            }
        }

#if APPSFLYER_ENABLE
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            _ = Instance;
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            Init();
            GameEvent<bool>.Register(FalconUMP.FALCON_UMP_COMPLETE, StartSDK, null);
            GameEvent<(string currentLevel, string timePlayed, int score)>.Register(
                FalconMmpLog.EVENT_BUS_LEVEL_COMPLETE, ActionComplete, null);
        }

        private void ActionComplete((string currentLevel, string timePlayed, int score) obj)
        {
            LogEvent(FalconMmpLog.MMP_LEVEL_ACHIEVED, new Dictionary<string, string>
            {
                { FalconMmpLog.MMP_LEVEL, obj.currentLevel },
                { FalconMmpLog.MMP_SCORE, obj.score.ToString() }
            });
        }

        private void Init()
        {
            UpdateConversionData();
            _setting = Resources.Load<SOMmpSetting>("SOMmpSetting");
            if (_setting == null)
            {
                Debug.LogError(
                    "SOMmpSetting doesn't exists. Go to 'Falcon/Modules/ThirdParty/Mmp Settings' to config.");
            }
            else
            {
                if (_setting.mmp == MmpType.Appsflyer)
                {
#if APPSFLYER_ENABLE
                    AppsFlyer.initSDK(_setting.devKey, _setting.appId, this);
                    if (string.IsNullOrEmpty(DeviceInfoRepository.Value.DeviceId))
                    {
                        new FMmpWarningLog(true).Send();
                    }

                    AppsFlyer.setCustomerUserId(DeviceInfoRepository.Value.DeviceId);
                    AppsFlyer.sendEvent(_FALCON_MMP_LOGIN, null);
#if UNITY_IOS && !UNITY_EDITOR
                    AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(60);
#endif
                    var isInEurope = SaveLoadHandler.Load(FalconUMP.FALCON_UMP_IS_IN_EUROPE, false);
                    var hasConsent = SaveLoadHandler.Load(FalconUMP.FALCON_UMP_HAS_CONSENT, true);
                    if (isInEurope)
                    {
                        var consent = new AppsFlyerConsent(true, hasConsent, hasConsent, hasConsent);
                        AppsFlyer.setConsentData(consent);
                    }
                    else
                    {
                        var consent = new AppsFlyerConsent(false);
                        AppsFlyer.setConsentData(consent);
                    }
#endif
                }
            }
        }

        private void StartSDK(bool value)
        {
#if APPSFLYER_ENABLE
            AppsFlyer.startSDK();
            AppsflyerID = AppsFlyer.getAppsFlyerId();
#endif
        }

        private void UpdateConversionData()
        {
            var jsonObject = ConversionData.Value;
            if (jsonObject.TryGetString("adgroup_id", out var adgroupId))
            {
                AdGroupID = adgroupId;
            }

            if (jsonObject.TryGetString("orig_cost", out var origCost))
            {
                OrigCost = origCost;
            }

            if (jsonObject.TryGetString("af_cost_currency", out var costCurrency))
            {
                AfCostCurrency = costCurrency;
            }

            if (jsonObject.TryGetBool("is_first_launch", out var isFirstLaunch))
            {
                IsFirstLaunch = isFirstLaunch;
            }

            if (jsonObject.TryGetString("campaign_id", out var campaignId))
            {
                CampaignID = campaignId;
            }

            if (jsonObject.TryGetString("campaign", out var campaignName))
            {
                CampaignName = campaignName;
            }

            if (jsonObject.TryGetString("af_c_id", out var afCId))
            {
                AfCid = afCId;
            }

            if (jsonObject.TryGetString("media_source", out var mediaSource))
            {
                MediaSource = mediaSource;
            }

            if (jsonObject.TryGetString("advertising_id", out var advertisingId))
            {
                AdvertisingID = advertisingId;
            }

            if (jsonObject.TryGetString("af_status", out var afStatus))
            {
                AfStatus = afStatus;
            }

            if (jsonObject.TryGetDouble("cost_cents_USD", out var costCentsUsd))
            {
                CostCentsUsd = costCentsUsd;
            }

            if (jsonObject.TryGetDouble("af_cost_value", out var costValue))
            {
                AfCostValue = costValue;
            }

            if (jsonObject.TryGetString("af_cost_model", out var afCostModel))
            {
                AfCostModel = afCostModel;
            }

            if (jsonObject.TryGetString("af_ad", out var afAd))
            {
                AfAD = afAd;
            }

            if (jsonObject.TryGetBool("is_retargeting", out var isRetargeting))
            {
                IsRetargeting = isRetargeting;
            }

            if (jsonObject.TryGetString("adgroup", out var adGroup))
            {
                Adgroup = adGroup;
            }

            GameData4Appsflyer.Instance.Save();
            GameData4Appsflyer.Instance.UpdateToServer();
        }

        #region AppsFlyerCallback

        public void onConversionDataSuccess(string conversionData)
        {
            Debug.Log("On onConversionDataSuccess");
            ConversionData.Value = JObject.Parse(conversionData);
            UpdateConversionData();
            GameEvent.Emit(_FALCON_MMP_STARTED);
        }

        public void onConversionDataFail(string error)
        {
            Debug.LogError("error onConversionDataFail : " + error);
            GameEvent.Emit(_FALCON_MMP_STARTED);
        }

        public void onAppOpenAttribution(string attributionData)
        {
        }

        public void onAppOpenAttributionFailure(string error)
        {
        }

        public void LogRevenueFsn(string adFormat, string adSource, string adUnitId, long valueMicros,
            string currencyCode, string placement)
        {
            double revenue = valueMicros / 1_000_000.0;

            var additionalParams = new Dictionary<string, string>
            {
                { AdRevenueScheme.AD_UNIT, adUnitId ?? "" },
                { AdRevenueScheme.AD_TYPE, adFormat ?? "" },
                { AdRevenueScheme.PLACEMENT, placement ?? "" }
            };

            var revenueData = new AFAdRevenueData(
                adSource ?? "admob",
                MediationNetwork.GoogleAdMob,
                currencyCode ?? "USD",
                revenue
            );

            AppsFlyer.logAdRevenue(revenueData, additionalParams);
        }

        public void LogRevenue(string network, string format, double value, string instance = null)
        {
            //log appslfyer
            var additionalParams = new Dictionary<string, string>();
            additionalParams.Add("ad_format", format);
            if (instance != null)
            {
                additionalParams.Add("ad_unit_name", instance);
            }
#if APPSFLYER_ENABLE
            var mediationNetwork = MediationNetwork.GoogleAdMob;
#if MAX_ENABLE
            mediationNetwork = MediationNetwork.ApplovinMax;
#endif
#if IRONSOURCE_ENABLE
            mediationNetwork = MediationNetwork.IronSource;
#endif
            var logRevenue =
                new AFAdRevenueData(network, mediationNetwork, "USD", value);
            AppsFlyer.logAdRevenue(logRevenue, additionalParams);
#endif
        }

        public void LogEvent(string eventName, Dictionary<string, string> dictionary = null)
        {
            if (dictionary != null && !dictionary.ContainsKey(FalconMmpLog.MMP_CUSTOMER_USER_ID))
            {
                dictionary.Add(FalconMmpLog.MMP_CUSTOMER_USER_ID, DeviceInfoRepository.Value.DeviceId);
            }
#if APPSFLYER_ENABLE
            AppsFlyer.sendEvent(eventName, dictionary);
#endif
        }

        #endregion

        internal BasicPoolData<IDictionary<string, JToken>> ConversionData => _conversionData ??=
            new BasicPoolData<IDictionary<string, JToken>>(DataPool.Value,
                _FALCON_APPSFLYER_CONVERSION_DATA, new Dictionary<string, JToken>());
    }

    public class AppsFlyerInfoService : IFCustomInfoRepository
    {
        public Dictionary<string, object> GetInfo()
        {
            Dictionary<string, object> result = new();
            var dictionary = FalconAppsflyerService.Instance.ConversionData.Value;
            foreach (var (key, value) in dictionary)
            {
                result["appsflyer_" + key] = value.Value<object>();
            }

            result["appsflyer_id"] = FalconAppsflyerService.Instance.AppsflyerID;

            return result;
        }
    }
}