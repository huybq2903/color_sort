/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public interface IDataLog
    {
        [JsonIgnore]
        long CreatedTime { get; }
        [JsonIgnore]
        string Event { get; }
        
        Dictionary<string, object> ToDictionary();
    }
}