/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-17
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.ThirdParty.Ump.Runtime;
#if ADJUST_ENABLE
using AdjustSdk;
#endif
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Runtime
{
    public class FalconAdjustService : AutoSingleton<FalconAdjustService>
    {
        private const string _FALCON_MMP_STARTED = "falcon_mmp_started";
        private const string _K_FALCON_ADJUST_CONVERSION_DATA = "falcon_analytics_adjust_conversion_data";
        private LazyVal<IDataPool> DataPool = new(MySingletonService.Instance<IDataPool>);
        private BasicPoolData<IDictionary<string, JToken>> _conversionData;

        private SOMmpSetting _setting;
        private string _adjustID;
        private string _trackerToken;
        private string _network;
        private string _campaign;
        private string _adGroup;
        private string _creative;
        private string _costType;
        private double _costAmount;

        private string _costCurrency;

#if ADJUST_ENABLE
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            _ = Instance;
        }
#endif
        protected override void Awake()
        {
            base.Awake();
            GameEvent<bool>.Register(FalconUMP.FALCON_UMP_COMPLETE, Init, null);
        }

        private void Init(bool value)
        {
            _setting = Resources.Load<SOMmpSetting>("SOMmpSetting");
            if (_setting == null)
            {
                Debug.LogError(
                    "SOMmpSetting doesn't exists. Go to 'Falcon/Modules/ThirdParty/Mmp Settings' to config.");
            }
            else
            {
                if (_setting.mmp == MmpType.Adjust)
                {
#if ADJUST_ENABLE
                    UpdateConversionData();
                    var config = new AdjustConfig(_setting.appToken, AdjustEnvironment.Production, true)
                    {
                        LogLevel = AdjustLogLevel.Suppress,
                        IsSendingInBackgroundEnabled = true,
                        AttributionChangedDelegate = OnAttributionChanged,
                        SessionSuccessDelegate = OnSessionSuccess,
                        SessionFailureDelegate = OnSessionFailed
                    };
                    Adjust.InitSdk(config);
                    Adjust.GetAdid((s) => { AdjustID = s; });
                    Debug.Log("Init adjust");
#endif
                }
            }
        }
#if ADJUST_ENABLE
        private void OnSessionFailed(AdjustSessionFailure obj)
        {
            Debug.LogError("Adjust OnSessionFailed : " + obj);
        }

        private void OnSessionSuccess(AdjustSessionSuccess obj)
        {
            Debug.Log("On onConversionDataSuccess");
            GameEvent.Emit(_FALCON_MMP_STARTED);
        }
#endif
        private string AdjustID
        {
            get => _adjustID;
            set
            {
                _adjustID = value;
                GameData4Adjust.Instance.adjustID = value;
                GameData4Adjust.Instance.Save();
            }
        }

        private string TrackerToken
        {
            get => _trackerToken;
            set
            {
                _trackerToken = value;
                GameData4Adjust.Instance.trackerToken = value;
            }
        }

        private string Network
        {
            get => _network;
            set
            {
                _network = value;
                GameData4Adjust.Instance.network = value;
            }
        }

        private string Campaign
        {
            get => _campaign;
            set
            {
                _campaign = value;
                GameData4Adjust.Instance.campaign = value;
            }
        }

        private string AdGroup
        {
            get => _adGroup;
            set
            {
                _adGroup = value;
                GameData4Adjust.Instance.adGroup = value;
            }
        }

        private string Creative
        {
            get => _creative;
            set
            {
                _creative = value;
                GameData4Adjust.Instance.creative = value;
            }
        }

        private string CostType
        {
            get => _costType;
            set
            {
                _costType = value;
                GameData4Adjust.Instance.costType = value;
            }
        }

        private double CostAmount
        {
            get => _costAmount;
            set
            {
                _costAmount = value;
                GameData4Adjust.Instance.costAmount = value.ToString();
            }
        }

        private string CostCurrency
        {
            get => _costCurrency;
            set
            {
                _costCurrency = value;
                GameData4Adjust.Instance.costCurrency = value;
            }
        }

        private Dictionary<string, object> Info { get; set; } = new();
#if ADJUST_ENABLE
        private void OnAttributionChanged(AdjustAttribution data)
        {
            var aae = new AdjustAttributionExtends(data);
            var json = aae.ToJson();
            ConversionData.Value = json.JsonToObj<Dictionary<string, JToken>>();
            Debug.Log("Conversion Data Success : " + json);
            UpdateConversionData();
        }

        private void UpdateConversionData()
        {
            var jsonObject = ConversionData.Value;
            if (jsonObject.TryGetString(AdjustUtils.KeyTrackerToken, out var trackerToken))
            {
                TrackerToken = trackerToken;
            }

            if (jsonObject.TryGetString(AdjustUtils.KeyNetwork, out var network))
            {
                Network = network;
            }

            if (jsonObject.TryGetString(AdjustUtils.KeyCampaign, out var campaign))
            {
                Campaign = campaign;
            }

            if (jsonObject.TryGetString(AdjustUtils.KeyAdgroup, out var adGroup))
            {
                AdGroup = adGroup;
            }

            if (jsonObject.TryGetString(AdjustUtils.KeyCreative, out var creative))
            {
                Creative = creative;
            }

            if (jsonObject.TryGetString(AdjustUtils.KeyCostType, out var costType))
            {
                CostType = costType;
            }

            if (jsonObject.TryGetDouble(AdjustUtils.KeyCostAmount, out var costAmount))
            {
                CostAmount = costAmount;
            }

            if (jsonObject.TryGetString(AdjustUtils.KeyCostCurrency, out var costCurrency))
            {
                CostCurrency = costCurrency;
            }

            GameData4Adjust.Instance.Save();
            GameData4Adjust.Instance.UpdateToServer();
        }
#endif
        public void LogEvent(string eventName)
        {
            var eventToken = "";
            switch (eventName)
            {
                case FalconMmpLog.MMP_REWARDED_DISPLAYED:
                    eventToken = _setting.rewardedDisplayedToken;
                    break;
                case FalconMmpLog.MMP_REWARDED_SHOW:
                    eventToken = _setting.rewardedShowToken;
                    break;
                case FalconMmpLog.MMP_INTERS_DISPLAYED:
                    eventToken = _setting.interstitialDisplayedToken;
                    break;
                case FalconMmpLog.MMP_INTERS_SHOW:
                    eventToken = _setting.interstitialShowToken;
                    break;
            }
#if ADJUST_ENABLE
            var adjustEvent = new AdjustEvent(eventToken);
            Adjust.TrackEvent(adjustEvent);
#endif
        }

        public void LogRevenue(string network, double value, string instance = null, string placement = null)
        {
            var source = "admob_sdk";
#if IRONSOURCE_ENABLE
            source = "ironsource_sdk";
#elif MAX_ENABLE
            source = "applovin_max_sdk";
#endif
#if ADJUST_ENABLE
            var adjustAdRevenue = new AdjustAdRevenue(source);
            adjustAdRevenue.SetRevenue(value, "USD");
            adjustAdRevenue.AdRevenueNetwork = network;
            adjustAdRevenue.AdRevenueUnit = instance;
            adjustAdRevenue.AdRevenuePlacement = placement;
            Adjust.TrackAdRevenue(adjustAdRevenue);
#endif
        }

        public void LogIAP(string currencyCode, string contentId, string purchasePrice)
        {
#if ADJUST_ENABLE
            var eventToken = _setting.inAppPurchaseToken;
            var adjustEvent = new AdjustEvent(eventToken);
            adjustEvent.SetRevenue(double.Parse(purchasePrice), currencyCode);
            adjustEvent.ProductId = contentId;
            Adjust.TrackEvent(adjustEvent);
#endif
        }

        internal BasicPoolData<IDictionary<string, JToken>> ConversionData => _conversionData ??=
            new BasicPoolData<IDictionary<string, JToken>>(DataPool.Value,
                _K_FALCON_ADJUST_CONVERSION_DATA, new Dictionary<string, JToken>());
    }
    
    public class AdjustInfoService : IFCustomInfoRepository
    {
        public Dictionary<string, object> GetInfo()
        {
            Dictionary<string, object> result = new();
            var dictionary = FalconAdjustService.Instance.ConversionData.Value;
            foreach (var (key, value) in dictionary)
            {
                result["adjust_" + key] = value.Value<object>();
            }

            return result;
        }
    }
}