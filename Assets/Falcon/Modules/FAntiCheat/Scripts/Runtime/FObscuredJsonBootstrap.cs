/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-08-08


namespace Falcon.Modules.FAntiCheat.Scripts.Runtime
{
    using Newtonsoft.Json;
    using UnityEngine;

    public static class ObscuredJsonBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Init()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                // Nếu bạn muốn field null bị bỏ qua thì bật cái này:
                // NullValueHandling = NullValueHandling.Ignore,
                Converters =
                {
                    new ObscuredIntJsonConverter(),
                    new ObscuredLongJsonConverter(),
                    new ObscuredUIntJsonConverter(),
                    new ObscuredFloatJsonConverter(),
                    new ObscuredDoubleJsonConverter(),
                    new ObscuredBoolJsonConverter(),
                    new ObscuredStringJsonConverter(),
                }
            };

            Debug.Log("[ObscuredJsonBootstrap] Newtonsoft.Json DefaultSettings registered.");
        }
    }
}