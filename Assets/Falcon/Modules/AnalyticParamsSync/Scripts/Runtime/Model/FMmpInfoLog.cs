/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-20
 */
using Falcon.Modules.Core.BigData;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    public class FMmpInfoLog : AFalconLog
    {
        [Preserve]
        public FMmpInfoLog()
        {
        }

        public override string Event => "f_sdk_mmp_info_data";
    }
}