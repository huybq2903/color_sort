/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Newtonsoft.Json;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class DataWrapper
    {
        public string data;
        public long clientSendTime;
        public string @event;
        public string packageName;
        public string platform;

        [Preserve]
        [JsonConstructor]
        public DataWrapper(string data, long clientSendTime, string @event, string packageName, string platform)
        {
            AnalyticLogger.Instance.Info($"Sending {@event}: {data}");
            this.data = data;
            this.clientSendTime = clientSendTime;
            this.@event = @event;
            this.packageName = packageName;
            this.platform = platform;
        }

        public DataWrapper(Dictionary<string, object> data, long clientSendTime, string @event, string packageName,
            string platform) : this(data.ToJson(), clientSendTime, @event, packageName, platform)
        {
        }

        public DataWrapper(IDataLog log, string packageName, string platform)
            : this(log.ToDictionary(), log.CreatedTime, log.Event, packageName, platform)
        {
        }
    }
}