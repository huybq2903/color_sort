/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-20
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.AccountData;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    public class ExtraUserParamRepository : IFCustomInfoRepository
    {
        private readonly LazyVal<Dictionary<string, object>> info = new(() => new Dictionary<string, object>
        {
            { "serverAccountId", AccountManager.Instance.Code.ToString() }
        });

        public ExtraUserParamRepository()
        {
            AccountManager.Instance.OnLoginEvent += _ => { info.Reset(); };
        }

        public Dictionary<string, object> GetInfo()
        {
            return info.Value;
        }
    }
}