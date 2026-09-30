/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-10
 */

using System;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.BigData;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.ThirdParty.Mmp.Runtime
{
    [Serializable]
    public class FMmpWarningLog : AFalconLog
    {
        [FKey(Name = nameof(deviceIdIsNull) + "$")]
        public bool deviceIdIsNull;

        [Preserve]
        public FMmpWarningLog()
        {
        }

        public FMmpWarningLog(bool deviceIdIsNull)
        {
            LogParams(deviceIdIsNull);
            this.deviceIdIsNull = deviceIdIsNull;
        }

        public override string Event => "f_sdk_mmp_warning_data";
    }
}