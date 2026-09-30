/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Collections.Concurrent;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class ReservePlayerAdRepository : IFPlayerAdRepository
    {
        private const string kAnalyticDataPrefix = "Analytic_SDK_Data_";
        private const string kAdLtvKey = kAnalyticDataPrefix + "Ad_Ltv";
        private readonly BasicPoolData<SealedBox<double>> _adLtv;

        private readonly ConcurrentDictionary<AdType, BasicPoolData<int>> _cache = new();
        private readonly IDataPool _dataPool;

        public ReservePlayerAdRepository(IDataPool dataPool)
        {
            _dataPool = dataPool;
            _adLtv = new BasicPoolData<SealedBox<double>>(dataPool, kAdLtvKey, SealedBox.Empty<double>());
        }

        public double? AdLtv
        {
            get => _adLtv.Value.AsNullable();
            set => _adLtv.Value = SealedBox.OfNullable(value);
        }

        public int AdCountOf(AdType adType)
        {
            return _cache.Compute(adType, val => val ?? new BasicPoolData<int>(_dataPool, KeyOf(adType), 0)).Value;
        }

        public (int typeWatched, double? adLtv) NewAdWatched(AdType adType, double? adRev)
        {
            var watched = IncrementAdCount(adType);
            if (!adRev.HasValue) return (watched, AdLtv);
            var sealedBox = _adLtv.Compute(d =>
                !d.TryGetValue(out var adLtv)
                    ? new SealedBox<double>(adRev.Value)
                    : SealedBox.Of(adLtv + adRev.Value));
            return (watched, sealedBox.AsNullable());
        }

        public void SetAdCountOf(AdType adType, int value)
        {
            DataOf(adType).Value = value;
        }

        public int IncrementAdCount(AdType adType)
        {
            return DataOf(adType).Compute(c => c + 1);
        }

        private BasicPoolData<int> DataOf(AdType adType)
        {
            return _cache.Compute(adType,
                val => val ?? new BasicPoolData<int>(_dataPool, KeyOf(adType), 0));
        }

        private static string KeyOf(AdType adType)
        {
            return kAnalyticDataPrefix + adType + "_Ad_Count";
        }
    }
}