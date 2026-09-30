/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LevelStatus
    {
        Fail, Pass, Skip, Start, HeartBeat
    }

    public static class LevelStatusExtensions
    {
        public static bool IsPass(this LevelStatus levelStatus) => levelStatus == LevelStatus.Pass;
        public static bool IsFail(this LevelStatus levelStatus) => levelStatus == LevelStatus.Fail;
        public static bool IsStart(this LevelStatus levelStatus) => levelStatus == LevelStatus.Start;
    }
}