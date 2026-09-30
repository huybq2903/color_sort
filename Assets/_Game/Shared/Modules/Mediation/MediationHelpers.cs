/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-13
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.InAppPurchase.Runtime;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using Falcon.Modules.Level.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Shared.Common;
using UnityEngine;
using UnityEngine.Purchasing;
using Falcon.Modules.Core.UI.Runtime;

namespace Falcon.Shared.Mediation
{
    public class MediationHelpers
    {
        private int _intersCount;

        private int levelInitMediationConfig => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "levelInitMediation");
        private int levelStartBannerConfig => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "levelStartBanner");
        private int levelStartIntersConfig => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "levelStartInters");
        private int timeToOpenRemoveAdsAfterIntersConfig => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "timeToOpenRemoveAdsAfterInters");
        private int timeShowInterAgainConfig => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "timeShowInterAgain");
        private int timeShowInterAfterPayConfig => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "timeShowInterAfterPay");

        private bool _isBannerShowing;
        private bool _isAvailableInters = true;
        private int _interSession = 0;
        private int _interInDayCache = -1;

        public static MediationHelpers Instance = new();

        public int _interInDay
        {
            get
            {
                if (_interInDayCache < 0)
                    _interInDayCache = PlayerPrefs.GetInt("MediationHelpers__interInDay", 0);
                return _interInDayCache;
            }
            set
            {
                _interInDayCache = value;
                PlayerPrefs.SetInt("MediationHelpers__interInDay", value);
            }
        }
        private int countryTier = 1;
        private CancellationTokenSource _cts;
        private void DoTimeAvailableInters(float timeCountDown)
        {
            _isAvailableInters = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            CoTimeAvailableInters(timeCountDown);
        }

        private async void CoTimeAvailableInters(float countDown)
        {
            _isAvailableInters = false;
            try
            {
                await UniTask.WaitForSeconds(countDown, cancellationToken: _cts.Token);
                _isAvailableInters = true;
            }
            catch (OperationCanceledException) { } // DoTimeAvailableInters gọi lại -> huỷ cái cũ, không phải lỗi
        }

        private void OnPurchaseSuccess(Product product)
        {
            DoTimeAvailableInters(timeShowInterAfterPayConfig);
        }

        public void ShowIntersAndRecommendRemoveAds(string placementId, Action onSuccess)
        {
            ShowInterstitial(placementId, RecommendRemoveAds, onSuccess);
            return;

            void RecommendRemoveAds()
            {
                if (_intersCount % timeToOpenRemoveAdsAfterIntersConfig == 1 && !GameData4RemoveAds.Instance.removeAds)
                {
                    GameEvent<Transform>.Register(Const.EVENT_CLOSE_POPUP, OnClosePopupRemoveAds, null);
                    GameEvent.Register(GameKeys.PURCHASE_CLOSE_SUCCESS, OnClosePopupPurchaseSuccess, null);
                    GameEvent<string>.Emit(Const.EVENT_OPEN_POPUP_NAME, "UIPopup_RemoveAds");
                    return;
                }

                onSuccess?.Invoke();
            }

            void OnClosePopupRemoveAds(Transform popupTrans)
            {
                if (popupTrans.name != "UIPopup_RemoveAds") return;

                onSuccess?.Invoke();
                GameEvent<Transform>.Unregister(Const.EVENT_CLOSE_POPUP, OnClosePopupRemoveAds, null);
                GameEvent.Unregister(GameKeys.PURCHASE_CLOSE_SUCCESS, OnClosePopupPurchaseSuccess, null);
            }

            void OnClosePopupPurchaseSuccess()
            {
                onSuccess?.Invoke();
                GameEvent.Unregister(GameKeys.PURCHASE_CLOSE_SUCCESS, OnClosePopupPurchaseSuccess, null);
                GameEvent<Transform>.Unregister(Const.EVENT_CLOSE_POPUP, OnClosePopupRemoveAds, null);
            }
        }

        public void ShowInterstitial(string placementId, Action onSuccess, Action onFail = null)
        {
            var status = MediationManager.Instance.GetStatusAdsFromPlacement(placementId, false);
            onFail ??= onSuccess;
            var cooldownByLevel = intersCooldownByLevelAndTiers[countryTier];
            var _interInDayTemp = _interInDay;
            foreach (var kvp in cooldownByLevel)
            {
                if (LevelData.Instance.level >= kvp.MinLevel)
                {
                    _isAvailableInters = _isAvailableInters && _interSession < kvp.limitSession && _interInDayTemp < 12;
                    break;
                }
            }

            if (status.statusShowAds != StatusShowAds.Active || !_isAvailableInters ||
                LevelData.Instance.level < levelStartIntersConfig)
            {
                onFail?.Invoke();
                return;
            }

            _intersCount++;
            _interSession++;
            _interInDay++;

            DoTimeAvailableInters(timeShowInterAgain);
            MediationManager.Instance.ShowInterstitial(placementId, onSuccess, onFail, true);
        }

        public void ShowRewardedVideo(string placementId, Action onSuccess = null, Action onFail = null)
        {
            DoTimeAvailableInters(timeShowInterAgain);
            MediationManager.Instance.ShowRewardedVideo(placementId, onSuccess, onFail ?? ShowNotAdsAvailable);
        }

        private void ShowNotAdsAvailable()
        {
            GameEvent<string>.Emit(GameKeys.TOAST_OPEN_LOCALIZE, "no_ads");
        }

        public bool IsHideBanner()
        {
            return GameData4RemoveAds.Instance.removeAds || levelStartBannerConfig > LevelData.Instance.level;
        }

        // Khác IsHideBanner (chỉ nói config có cho phép): cờ này là banner đã load xong và hiện thật.
        public bool IsBannerShowing => _isBannerShowing;

        private void OnBannerShown() => _isBannerShowing = true;

        private void OnBannerHidden() => _isBannerShowing = false;

        public void Initialize()
        {
            IAPManager.onPurchaseSuccess += OnPurchaseSuccess;
            countryTier = GetCountryTier();
            MediationManager.Instance.onBannerShow += OnBannerShown;
            MediationManager.Instance.onBannerHide += OnBannerHidden;
            GameEvent.Register(GameKeys.LOAD_SCENE_LEVEL_COMPLETE, ShowBanner, null);
            GameEvent.Register(GameKeys.BACK_TO_HOME, BackToHome, null);
            GameEvent<int>.Register(GameKeys.ON_WIN_LEVEL, OnWinLevel, null);
            GameRequest<bool>.Register("is_hide_banner", IsHideBanner);
            InitMediation(LevelData.Instance.level);
            RewardAdsData.Instance.Initialized();
        }

        private void OnWinLevel(int level) => InitMediation(level + 1);

        private static void InitMediation(int currentLevel)
        {
            if (currentLevel < Instance.levelInitMediationConfig) return;
            MediationManager.Instance.InitMediation();
        }

        private static void ShowBanner()
        {
            if (Instance.levelStartBannerConfig <= LevelData.Instance.level && !Application.isEditor)
            {
                MediationManager.Instance.ShowBanner();
            }
        }

        // Banner chỉ hiện trong GameScene -> về Home là ẩn
        private static void BackToHome() => MediationManager.Instance.HideBanner();

        #region Country Context

        private string _countryCode = null;

        public string CountryCode
        {
            get
            {
                if (_countryCode != null)
                    return _countryCode;

                try
                {
                    _countryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;
                    return _countryCode;
                }
                catch (Exception)
                {
                    // Ignored
                }

                return "US";
            }
        }

        public int GetCountryTier(string countryISOCode = null)
        {
            if (string.IsNullOrEmpty(countryISOCode))
                countryISOCode = CountryCode;

            if (_tierByCountries.TryGetValue(countryISOCode, out var tier))
                return tier;

            return 4;
        }

        private readonly Dictionary<string, int> _tierByCountries = new Dictionary<string, int>
        {
            { "US", 1 }, { "DE", 1 }, { "FR", 1 }, { "KR", 1 }, { "JP", 1 }, { "UK", 1 }, { "SE", 1 }, { "CA", 1 },
            { "TW", 1 }, { "AT", 1 },
            { "AU", 1 }, { "CH", 1 }, { "DK", 1 }, { "NO", 1 }, { "NZ", 1 }, { "IT", 2 }, { "PL", 2 }, { "ES", 2 },
            { "AE", 2 }, { "NL", 2 },
            { "SA", 2 }, { "BE", 2 }, { "ZA", 2 }, { "IE", 2 }, { "SG", 2 }, { "FI", 2 }, { "HK", 2 }, { "RU", 2 },
            { "IS", 2 }, { "PT", 2 },
            { "UA", 2 }, { "KW", 2 }, { "MO", 2 }, { "QA", 2 }, { "LU", 2 }, { "CN", 2 }, { "IL", 3 }, { "BR", 3 },
            { "CL", 3 }, { "Name", 3 },
            { "MX", 3 }, { "VN", 3 }, { "CZ", 3 }, { "TR", 3 }, { "NP", 3 }, { "PR", 3 }, { "TH", 3 }, { "PH", 3 },
            { "LB", 3 }, { "IN", 3 },
            { "GR", 3 }, { "MN", 3 }, { "LA", 3 }, { "RO", 3 }, { "HU", 3 }, { "KE", 3 }, { "KH", 3 }, { "OM", 3 },
            { "LT", 3 }, { "GH", 3 },
            { "CY", 3 }, { "NA", 3 }, { "BH", 3 }, { "SI", 3 }, { "EE", 3 }, { "BS", 3 }, { "MT", 3 }, { "PK", 4 },
            { "EG", 4 }, { "DZ", 4 },
            { "AR", 4 }, { "IQ", 4 }, { "VE", 4 }, { "MY", 4 }, { "EC", 4 }, { "CO", 4 }, { "PS", 4 }, { "SY", 4 },
            { "LY", 4 }, { "LV", 4 },
            { "MA", 4 }, { "MM", 4 }, { "TN", 4 }, { "YE", 4 }, { "BY", 4 }, { "UZ", 4 }, { "JO", 4 }, { "CU", 4 },
            { "AL", 4 }, { "SN", 4 },
            { "RS", 4 }, { "DO", 4 }, { "UY", 4 }, { "KG", 4 }, { "KZ", 4 }, { "AZ", 4 }, { "BO", 4 }, { "AF", 4 },
            { "CR", 4 }, { "NI", 4 },
            { "SO", 4 }, { "MK", 4 }, { "TJ", 4 }, { "GE", 4 }, { "PY", 4 }, { "HN", 4 }, { "SK", 4 }, { "PE", 4 },
            { "GT", 4 }, { "SV", 4 },
            { "AM", 4 }, { "LK", 4 }, { "ET", 4 }, { "PA", 4 }, { "HT", 4 }, { "BA", 4 }, { "SD", 4 }, { "RE", 4 },
            { "MD", 4 }, { "TT", 4 },
            { "BG", 4 }, { "CI", 4 }, { "MR", 4 }, { "GA", 4 }, { "MU", 4 }, { "CG", 4 }, { "HR", 4 }, { "AO", 4 },
            { "ZW", 4 }, { "JM", 4 },
            { "SR", 4 }, { "IR", 4 }, { "BF", 4 }, { "CD", 4 }, { "ML", 4 }, { "BN", 4 }, { "ME", 4 }, { "AD", 4 },
            { "GY", 4 }, { "BW", 4 },
            { "GM", 4 }, { "BT", 4 }, { "GF", 4 }, { "PG", 4 }, { "ZM", 4 }, { "CM", 4 }, { "BJ", 4 }, { "NC", 4 },
            { "MQ", 4 }, { "MV", 4 },
            { "VU", 4 }, { "SL", 4 }, { "YT", 4 }, { "XK", 4 }, { "PF", 4 }, { "TL", 4 }, { "UG", 4 }, { "MZ", 4 },
            { "TG", 4 }, { "BB", 4 },
            { "DJ", 4 }, { "LC", 4 }, { "VC", 4 }, { "FJ", 4 }, { "CV", 4 }, { "BZ", 4 }, { "LR", 4 }, { "SC", 4 },
            { "TZ", 4 }, { "RW", 4 },
            { "NG", 4 }, { "OTHER", 4 }, { "GP", 4 }, { "NE", 4 }, { "MW", 4 }, { "MG", 4 }, { "WS", 4 }, { "GD", 4 },
            { "DM", 4 }, { "LS", 4 },
            { "KN", 4 }, { "AW", 4 }, { "GN", 4 }, { "AG", 4 }, { "TO", 4 }, { "LI", 4 }, { "TD", 4 }, { "FO", 4 },
            { "GW", 4 }, { "SZ", 4 },
            { "AS", 4 }, { "KM", 4 }, { "KY", 4 }, { "CK", 4 }, { "BD", 4 }, { "WF", 4 }, { "MH", 4 }, { "GL", 4 },
            { "PW", 4 }, { "BM", 4 },
            { "SX", 4 }, { "VI", 4 }, { "SJ", 4 }, { "CW", 4 }, { "BI", 4 }, { "TM", 4 }, { "AN", 4 }, { "BQ", 4 },
            { "GQ", 4 }, { "EH", 4 },
            { "SB", 4 }, { "MS", 4 }, { "FM", 4 }, { "TC", 4 }, { "ST", 4 }, { "VG", 4 }, { "AI", 4 }, { "GI", 4 },
            { "MF", 4 }, { "SM", 4 },
            { "MC", 4 }, { "NR", 4 }, { "PM", 4 }, { "AX", 4 }, { "ZZ", 4 }, { "IM", 4 }, { "GG", 4 }, { "JE", 4 },
            { "ER", 4 }, { "BL", 4 },
            { "AQ", 4 }, { "BV", 4 }, { "CC", 4 }, { "CF", 4 }, { "CX", 4 }, { "EU", 4 }, { "FK", 4 }, { "GS", 4 },
            { "HM", 4 }, { "IO", 4 },
            { "KI", 4 }, { "KP", 4 }, { "NF", 4 }, { "NU", 4 }, { "PN", 4 }, { "SH", 4 }, { "SS", 4 }, { "TF", 4 },
            { "TK", 4 }, { "TV", 4 },
            { "UM", 4 }, { "VA", 4 }, { "GU", 4 }, { "MP", 4 }
        };


        #endregion

        #region Ads Context

        private readonly IReadOnlyDictionary<int, IReadOnlyList<(int MinLevel, int Cooldown, int limitSession)>>
            intersCooldownByLevelAndTiers =
                new Dictionary<int, IReadOnlyList<(int MinLevel, int Cooldown, int limitSession)>>
                {
                {
                    1, new List<(int MinLevel, int Cooldown, int limitSession)>
                    {
                        (MinLevel: 30, Cooldown: 150, limitSession : 4),
                        (MinLevel: 1, Cooldown: 180, limitSession : 4),
                        (MinLevel: 1, Cooldown: 180, limitSession : 4)
                    }
                },
                {
                    2, new List<(int MinLevel, int Cooldown, int limitSession)>
                    {
                        (MinLevel: 30, Cooldown: 150, limitSession : 4),
                        (MinLevel: 1, Cooldown: 180, limitSession : 4),
                        (MinLevel: 1, Cooldown: 180, limitSession : 4)
                    }
                },
                {
                    3, new List<(int MinLevel, int Cooldown, int limitSession)>
                    {
                        (MinLevel: 30, Cooldown: 150, limitSession : 3),
                        (MinLevel: 1, Cooldown: 180 ,limitSession : 3),
                        (MinLevel: 1, Cooldown: 180 ,limitSession : 3)
                    }
                },
                {
                    4, new List<(int MinLevel, int Cooldown, int limitSession)>
                    {
                        (MinLevel: 30, Cooldown: 150, limitSession : 3),
                        (MinLevel: 1, Cooldown: 180, limitSession : 3),
                        (MinLevel: 1, Cooldown: 180, limitSession : 3)
                    }
                }
                };

        private readonly IReadOnlyList<(int MinVIPPoint, int Cooldown)> intersCooldownByVIPPoint =
            new List<(int, int)>
            {
            (20, 300),
            (10, 270),
            (5, 240),
            (1, 240),
            };


        public int timeShowInterAgain
        {
            get
            {
                if (IAPManager.Ltv > 0)
                    return timeShowInterAgainPay;

                var countryTier = GetCountryTier();
                var cooldownByLevel = intersCooldownByLevelAndTiers[countryTier];

                foreach (var kvp in cooldownByLevel)
                {
                    if (LevelData.Instance.level >= kvp.MinLevel)
                    {
                        return kvp.Cooldown;
                    }
                }

                return timeShowInterAgainConfig;
            }
        }

        public int timeShowInterAgainPay
        {
            get
            {
                foreach (var kvp in intersCooldownByVIPPoint)
                {
                    if (IAPManager.Ltv >= kvp.MinVIPPoint)
                    {
                        return kvp.Cooldown;
                    }
                }

                return timeShowInterAgainConfig;
            }
        }
        #endregion
    }

    public static class PlacementAds
    {
        public const string REVIVE = "revive";
        public const string ADD_LIVE = "add_live";
        public const string WIN_GAME = "win_game";
        public const string QUIT = "quit_game";
        public const string RETRY = "retry_game";
    }
}
