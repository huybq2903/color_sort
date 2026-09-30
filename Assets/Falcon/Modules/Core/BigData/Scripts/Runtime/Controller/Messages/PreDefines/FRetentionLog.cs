/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using Falcon.Helpers.Devkit;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class FRetentionLog : AFalconLog
    {
        public string localDate;

        [Preserve]
        public FRetentionLog()
        {
        }

        public FRetentionLog(DateTime localDate)
        {
            LogParams(localDate);
            this.localDate = MyTime.DateToString(localDate);
        }

        public override string Event => "f_sdk_retention_data";
    }
}