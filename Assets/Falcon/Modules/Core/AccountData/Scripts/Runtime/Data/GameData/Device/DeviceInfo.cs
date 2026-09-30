/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-13

namespace Falcon.Modules.Core.AccountData 
{
    using global::Falcon.Modules.Core.SystemInformation.Runtime;
    [FGameDataType("device_info")]
    public class DeviceInfo : FGameData<DeviceInfo>
    {
        public string deviceModel = SystemInformation.Device.DeviceModel;
        public string operatingSystem = SystemInformation.Device.OperatingSystem;
        public string systemLanguage = SystemInformation.Device.SystemLanguage;
        public string systemLanguageISO = SystemInformation.Device.SystemLanguageISO;
        public string resolution = SystemInformation.Device.Resolution;
        public int deviceMemory = SystemInformation.Device.DeviceMemory;
        public int graphicsMemorySize = SystemInformation.Device.GraphicsMemorySize;
        public string platform = SystemInformation.Device.Platform ;

        public override void PostConstructor()
        {
            deviceModel = SystemInformation.Device.DeviceModel;
            operatingSystem = SystemInformation.Device.OperatingSystem;
            systemLanguage = SystemInformation.Device.SystemLanguage;
            systemLanguageISO = SystemInformation.Device.SystemLanguageISO;
            resolution = SystemInformation.Device.Resolution;
            deviceMemory = SystemInformation.Device.DeviceMemory;
            graphicsMemorySize = SystemInformation.Device.GraphicsMemorySize;
            platform = SystemInformation.Device.Platform;
        }
    }
}