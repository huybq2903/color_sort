/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-04
     */


using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Falcon.Modules.Core.SystemInformation.Runtime;
using System.Globalization;

namespace Falcon.Modules.Core.SystemInformation.Tests
{
    using System.Linq;

    public class SystemInfoPlayModeTests
    {
        [SetUp]
        public void Setup()
        {
            // Ensure the static constructors are called if they haven't been already.
            // Forcing a call to the Initialize methods which are empty but will trigger static constructor if needed.
            // However, the RuntimeInitializeOnLoadMethod should have already triggered them.
            // We can access a property to ensure initialization.
            var platform = Falcon.Modules.Core.SystemInformation.Runtime.SystemInformation.Device.Platform;
            var version = Falcon.Modules.Core.SystemInformation.Runtime.SystemInformation.App.Version;
        }

        [Test]
        public void Device_Properties_ReturnEditorDefaults()
        {
            Assert.AreEqual("editor", Runtime.SystemInformation.Device.DeviceModel, "DeviceModel mismatch");
            Assert.AreEqual("operatingSystem", Runtime.SystemInformation.Device.OperatingSystem, "OperatingSystem mismatch");
            Assert.AreEqual("VN", Runtime.SystemInformation.Device.SystemLanguage, "SystemLanguage mismatch");
            Assert.AreEqual(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Runtime.SystemInformation.Device.SystemLanguageISO, "SystemLanguageISO mismatch");
            Assert.AreEqual("1920x1080", Runtime.SystemInformation.Device.Resolution, "Resolution mismatch");
            Assert.AreEqual(1024, Runtime.SystemInformation.Device.DeviceMemory, "DeviceMemory mismatch");
            Assert.AreEqual(2048, Runtime.SystemInformation.Device.GraphicsMemorySize, "GraphicsMemorySize mismatch");
            Assert.AreEqual("editor", Runtime.SystemInformation.Device.Platform, "Platform mismatch");
            
            string expectedEditorUUID = UnityEngine.SystemInfo.deviceUniqueIdentifier + "-editor";
            Assert.AreEqual(expectedEditorUUID, Runtime.SystemInformation.Device.DeviceUUID, "DeviceUUID mismatch");
        }

        [UnityTest]
        public IEnumerator Device_AdvertisingID_ReturnsEditorDefault_AfterAccessing()
        {
            // AdvertisingID is fetched, let's check initial value (can be editor default or empty if GetAdvertisingID wasn't called)
            // In editor, it's set in static constructor of Device.
            // However, GetAdvertisingID() sets it directly.
            Runtime.SystemInformation.Device.GetAdvertisingID();
            yield return null; // Wait a frame in case of any async operations (though not expected for editor)
            Assert.AreEqual("advertisingId-editor", Runtime.SystemInformation.Device.AdvertisingID, "AdvertisingID mismatch");
            yield break;
        }

        [Test]
        public void App_Properties_ReturnExpectedValuesInEditor()
        {
            Assert.AreEqual(Application.version, Runtime.SystemInformation.App.Version, "App Version mismatch");
            Assert.AreEqual(Application.identifier, Runtime.SystemInformation.App.PackageName, "PackageName mismatch");
            Assert.AreEqual(Application.installerName, Runtime.SystemInformation.App.InstallVendor, "InstallVendor mismatch");

            string ver = Application.version.Replace(".", "");
            int expectedVerInt = 0;
            if (!int.TryParse(ver, out expectedVerInt))
            {
                expectedVerInt = 0;
            }
            Assert.AreEqual(expectedVerInt, Runtime.SystemInformation.App.VersionInt, "VersionInt mismatch");

            // Android specific properties should have editor defaults
            Assert.AreEqual(0, Runtime.SystemInformation.App.NumberLibFiles, "NumberLibFiles mismatch for Editor");
            Assert.AreEqual(0L, Runtime.SystemInformation.App.TotalLibFileSize, "TotalLibFileSize mismatch for Editor");
            Assert.AreEqual(string.Empty, Runtime.SystemInformation.App.LibFileNameList, "LibFileNameList mismatch for Editor");
            Assert.AreEqual(string.Empty, Runtime.SystemInformation.App.LibFolder, "LibFolder mismatch for Editor");
            Assert.AreEqual(string.Empty, Runtime.SystemInformation.App.LibMD5, "LibMD5 mismatch for Editor");
        }

        [Test]
        public void App_GetVerInt_HandlesDifferentVersionFormats()
        {
            // This tests the private GetVerInt logic indirectly via the public VersionInt property
            // by checking its initialization based on Application.version
            string originalVersion = Application.version;
            
            // Test case 1: Standard "1.2.3"
            // Mocking Application.version is not straightforward in tests, 
            // so we rely on the static constructor's behavior with current Application.version
            // and the specific test for VersionInt in App_Properties_ReturnExpectedValuesInEditor
            // For a direct test of GetVerInt, the class structure would need to change or use reflection.
            
            // Example test, assuming Application.version is "1.0.0"
            if (Application.version == "1.0.0")
            {
                Assert.AreEqual(100, Runtime.SystemInformation.App.VersionInt, "VersionInt for 1.0.0");
            }
            else if (Application.version == "0.5.12")
            {
                 Assert.AreEqual(0512, Runtime.SystemInformation.App.VersionInt, "VersionInt for 0.5.12");
            }
            // If Application.version is something like "2023.1.0f1", this logic will be tested based on that.
            string ver = Application.version.Replace(".", "");
            int expectedVerInt = 0;
            if (int.TryParse(ver, out var parsedVer))
            {
                expectedVerInt = parsedVer;
            } else {
                // Handle cases like "202310f1" - TryParse would fail for non-numeric parts
                // The current implementation of GetVerInt simply replaces "." and parses.
                // It doesn't handle non-numeric parts like 'f1', so int.TryParse will return 0 for such cases.
                string numericPart = new string(ver.TakeWhile(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(numericPart)) int.TryParse(numericPart, out expectedVerInt);
                else expectedVerInt = 0;

                // The actual GetVerInt just does ver.Replace(".",""); int.TryParse(ver, out verInt);
                // So if Application.version = "1.0.f1", ver becomes "10f1", TryParse fails, verInt = 0.
                // If Application.version = "1.b.c", ver becomes "1bc", TryParse fails, verInt = 0.
                // We'll test against the logic as implemented in the source code.
                string tempVer = Application.version.Replace(".", "");
                if (!int.TryParse(tempVer, out expectedVerInt))
                {
                    expectedVerInt = 0;
                }
            }
            Assert.AreEqual(expectedVerInt, Runtime.SystemInformation.App.VersionInt, "VersionInt general case");
        }

        [Test]
        public void Device_IsDeviceUUIDValid_LogicCheck()
        {
            // This tests the private IsDeviceUUIDValid logic indirectly.
            // We know the editor UUID is SystemInfo.deviceUniqueIdentifier + "-editor"
            // This should be considered valid by the logic.
            // We can't call IsDeviceUUIDValid directly, but we can check DeviceUUID is not empty.
            Assert.IsNotEmpty(Runtime.SystemInformation.Device.DeviceUUID, "Editor DeviceUUID should be valid and not empty.");
        }
    }
} 