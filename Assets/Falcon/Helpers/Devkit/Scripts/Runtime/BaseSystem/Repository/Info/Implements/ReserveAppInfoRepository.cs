/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    [NoLazy]
    public class ReserveAppInfoRepository : IFAppInfoRepository
    {
        public string PackageName { get; } = Application.identifier.ToLower();
        public string GameName { get; } = Application.productName.ToLower();
#if UNITY_IOS
        public string Platform { get; } = "ios";
#elif UNITY_ANDROID
        public string Platform { get; } = "android";
#else
        public string Platform { get; } = Application.platform.ToString().ToLower();
#endif
        public string AppVersion { get; } = Application.version.ToLower();
    }
}