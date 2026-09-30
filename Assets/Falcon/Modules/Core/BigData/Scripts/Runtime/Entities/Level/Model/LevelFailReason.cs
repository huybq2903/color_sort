/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-30
 */

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Lý do thua level — vocab theo hợp đồng sdk-contract-vnext §D
    /// (giá trị wire là snake_case qua EnumMember, dev không gõ string tay).
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LevelFailReason
    {
        [EnumMember(Value = "out_of_moves")] OutOfMoves,
        [EnumMember(Value = "out_of_time")] OutOfTime,
        [EnumMember(Value = "died")] Died,
        [EnumMember(Value = "quit")] Quit
    }
}
