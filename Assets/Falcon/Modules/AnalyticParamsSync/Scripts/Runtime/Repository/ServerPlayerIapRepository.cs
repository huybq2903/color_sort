/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-20
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.InAppPurchase.Runtime;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    public class ServerPlayerIapRepository : IFPlayerIapRepository
    {
        public InAppData InAppLtv
        {
            get
            {
                var dictionary = FInAppData.Instance.localizeData;
                if (dictionary == null) return null;

                InAppData result = null;
                foreach (var localizedData in dictionary.Values)
                {
                    result ??= Wrap(localizedData);
                    result = localizedData.count > result.count ? Wrap(localizedData) : result;
                }

                return result;
            }
        }

        public int InAppCount
        {
            get
            {
                var dictionary = FInAppData.Instance.localizeData;
                return dictionary == null ? 0 : dictionary.Values.Sum(data => data.count);
            }
        }

        public int? FirstInAppLv => FInAppData.Instance.firstRecord?.level;

        public DateTime? FirstInAppDate
        {
            get
            {
                var timestamp = FInAppData.Instance.firstRecord?.timestamp;
                return timestamp.HasValue ? DateTime.UnixEpoch.AddMilliseconds(timestamp.Value) : null;
            }
        }

        public string FirstInAppDateStr
        {
            get
            {
                var firstInAppDate = FirstInAppDate;
                return firstInAppDate.HasValue ? MyTime.DateToString(firstInAppDate.Value) : null;
            }
        }

        public string FirstInAppProduct => FInAppData.Instance.firstRecord?.productId;
        public int? LastInAppLv => FInAppData.Instance.lastRecord?.level;

        public DateTime? LastInAppDate
        {
            get
            {
                var timestamp = FInAppData.Instance.lastRecord?.timestamp;
                return timestamp.HasValue ? DateTime.UnixEpoch.AddMilliseconds(timestamp.Value) : null;
            }
        }

        public string LastInAppDateStr
        {
            get
            {
                var lastInAppDate = LastInAppDate;
                return lastInAppDate.HasValue ? MyTime.DateToString(lastInAppDate.Value) : null;
            }
        }

        public string LastInAppProduct => FInAppData.Instance.lastRecord?.productId;

        public void RecordNewIap(string iapProduct, decimal amount, string isoCountryCode, int maxPassedLevel)
        {
            //Thông tin iap được lấy từ module iap, nên có cách update riêng và không chấp nhận việc update bên ngoài
        }

        private static InAppData Wrap(FInAppData.LocalizedData localizedData)
        {
            return new InAppData(localizedData.total, localizedData.max, localizedData.count,
                localizedData.isoCurrencyCode);
        }
    }
    
    public class ReserveIapRepositoryDisabler : ISingletonShellSourceDisabler
    {
        public int Priority => 0;

        public IEnumerable<Type> DisablingSources
        {
            get
            {
                yield return typeof(ReservePlayerIapRepository);
            }
        }
    }
}