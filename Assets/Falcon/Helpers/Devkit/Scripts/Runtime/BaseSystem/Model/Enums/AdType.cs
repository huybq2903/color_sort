/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AdType
    {
        Banner,
        Interstitial,
        Reward,
        AppOpen,
        Native,
        CollapsibleBanner,
        MREC
    }
}