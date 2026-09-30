/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    public class ServerPlayerAdRepository : IFPlayerAdRepository
    {
        public double? AdLtv
        {
            get
            {
                var adInfos = GameData4AdInfo.Instance.adTypeToInfo;
                return adInfos == null ? 0 : adInfos.Values.Sum(info => info.ltv);
            }
        }

        public int AdCountOf(AdType adType)
        {
            var adInfos = GameData4AdInfo.Instance.adTypeToInfo;
            if (adInfos == null) return 0;
            return adInfos.TryGetValue(adType, out var value) ? value.count : 0;
        }

        public (int typeWatched, double? adLtv) NewAdWatched(AdType adType, double? adRev)
        {
            //Thông tin iap được lấy từ module mediation, nên có cách update riêng và không chấp nhận việc update bên ngoài
            return (AdCountOf(adType), AdLtv);
        }
    }

    public class ReserveAdRepositoryDisabler : ISingletonShellSourceDisabler
    {
        public int Priority => 0;

        public IEnumerable<Type> DisablingSources
        {
            get
            {
                yield return typeof(ReservePlayerAdRepository);
            }
        }
    }
}