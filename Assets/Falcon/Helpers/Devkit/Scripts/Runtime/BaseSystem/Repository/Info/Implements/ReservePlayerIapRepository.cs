/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class ReservePlayerIapRepository : IFPlayerIapRepository
    {
        private const string ANALYTIC_DATA_PREFIX = "Analytic_SDK_Data_";
        private const string IN_APP_LTV_KEY = ANALYTIC_DATA_PREFIX + "In_App_Ltv";
        private const string IN_APP_COUNT_KEY = ANALYTIC_DATA_PREFIX + "In_App_Count";
        private const string FIRST_IN_APP_DATE_KEY = ANALYTIC_DATA_PREFIX + "First_In_App_Day";
        private const string FIRST_IN_APP_LV_KEY = ANALYTIC_DATA_PREFIX + "First_In_App_Lv";
        private const string FIRST_IN_APP_PRODUCT_KEY = ANALYTIC_DATA_PREFIX + "First_In_App_Product";
        private const string LAST_IN_APP_DATE_KEY = ANALYTIC_DATA_PREFIX + "Last_In_App_Day";
        private const string LAST_IN_APP_LV_KEY = ANALYTIC_DATA_PREFIX + "Last_In_App_Lv";
        private const string LAST_IN_APP_PRODUCT_KEY = ANALYTIC_DATA_PREFIX + "Last_In_App_Product";
        private readonly ITimeRepository _timeRepository;
        
        private readonly BasicPoolData<SealedBox<DateTime>> _firstInAppDate;
        private readonly BasicPoolData<SealedBox<int>> _firstInAppLv;
        private readonly BasicPoolData<SealedBox<string>> _firstInAppProduct;
        private readonly BasicPoolData<SealedBox<DateTime>> _lastInAppDate;
        private readonly BasicPoolData<SealedBox<int>> _lastInAppLv;
        private readonly BasicPoolData<SealedBox<string>> _lastInAppProduct;
        private readonly BasicPoolData<int> _inAppCount;
        private readonly BasicPoolData<Dictionary<string, InAppData>> _inAppData;

        public ReservePlayerIapRepository(IDataPool dataPool, ITimeRepository timeRepository)
        {
            _timeRepository = timeRepository;
            _inAppCount = new BasicPoolData<int>(dataPool, IN_APP_COUNT_KEY, 0);
            _inAppData = new BasicPoolData<Dictionary<string, InAppData>>(dataPool, IN_APP_LTV_KEY,
                new Dictionary<string, InAppData>());
            
            _firstInAppDate =
                new BasicPoolData<SealedBox<DateTime>>(dataPool, FIRST_IN_APP_DATE_KEY, SealedBox.Empty<DateTime>());
            _firstInAppLv = new BasicPoolData<SealedBox<int>>(dataPool, FIRST_IN_APP_LV_KEY, SealedBox.Empty<int>());
            _firstInAppProduct = new BasicPoolData<SealedBox<string>>(dataPool, FIRST_IN_APP_PRODUCT_KEY, SealedBox.Empty<string>());
            
            _lastInAppDate =
                new BasicPoolData<SealedBox<DateTime>>(dataPool, LAST_IN_APP_DATE_KEY, SealedBox.Empty<DateTime>());
            _lastInAppLv = new BasicPoolData<SealedBox<int>>(dataPool, LAST_IN_APP_LV_KEY, SealedBox.Empty<int>());
            _lastInAppProduct = new BasicPoolData<SealedBox<string>>(dataPool, LAST_IN_APP_PRODUCT_KEY, SealedBox.Empty<string>());
        }

        public InAppData InAppLtv
        {
            get
            {
                InAppData inAppData = null;
                _inAppData.Compute(dict =>
                {
                    foreach (var value in dict.Values)
                    {
                        inAppData ??= value;
                        inAppData = value.count > inAppData.count ? value : inAppData;
                    }
                    return dict;
                });

                return inAppData;
            }
        }

        public int InAppCount
        {
            get => _inAppCount.Value;
            set => _inAppCount.Value = value;
        }

        public int? FirstInAppLv
        {
            get => _firstInAppLv.Value.AsNullable();
            set => _firstInAppLv.Value = SealedBox.OfNullable(value);
        }

        public DateTime? FirstInAppDate
        {
            get => _firstInAppDate.Value.AsNullable();
            set => _firstInAppDate.Value = SealedBox.OfNullable(value?.ToUniversalTime());
        }

        [CanBeNull]
        public string FirstInAppDateStr
        {
            get
            {
                var firstInAppDate = FirstInAppDate;
                return firstInAppDate.HasValue ? MyTime.DateToString(firstInAppDate.Value) : null;
            }
        }

        [CanBeNull]
        public string FirstInAppProduct
        {
            get => _firstInAppProduct.Value.TryGetValue(out var value)? value : null;
            set => _firstInAppProduct.Value = SealedBox.Of(value);
        }

        public int? LastInAppLv 
        {
            get => _lastInAppLv.Value.AsNullable();
            set => _lastInAppLv.Value = SealedBox.OfNullable(value);
        }
        public DateTime? LastInAppDate
        {
            get => _lastInAppDate.Value.AsNullable();
            set => _lastInAppDate.Value = SealedBox.OfNullable(value?.ToUniversalTime());
        }
        public string LastInAppDateStr 
        {
            get
            {
                var lastInAppDate = LastInAppDate;
                return lastInAppDate.HasValue ? MyTime.DateToString(lastInAppDate.Value) : null;
            }
        }
        
        [CanBeNull]
        public string LastInAppProduct
        {
            get => _lastInAppProduct.Value.TryGetValue(out var value)? value : null;
            set => _lastInAppProduct.Value = SealedBox.Of(value);
        }

        public void RecordNewIap(string iapProduct, decimal amount, string isoCountryCode, int maxPassedLevel)
        {
            _inAppData.Compute(dict =>
            {
                var copy = new Dictionary<string, InAppData>(dict);
                if (!copy.ContainsKey(isoCountryCode))
                    copy[isoCountryCode] = new InAppData(0, 0, 0, isoCountryCode);
                copy[isoCountryCode].Update(amount);
                return copy;
            });
            
            InAppCount++;
            LastInAppLv = maxPassedLevel;
            LastInAppProduct = iapProduct;
            LastInAppDate = _timeRepository.UtcNow();
            
            if (InAppCount != 1) return;
            FirstInAppLv = maxPassedLevel;
            FirstInAppProduct = iapProduct;
            FirstInAppDate = _timeRepository.UtcNow();
        }
    }
}