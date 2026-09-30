/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// Defines the contract for a repository that provides read-only access to various device-specific information.
    /// This data is crucial for analytics, debugging, and understanding the environment in which the application is running.
    /// </summary>
    /// <remarks>
    /// As an <see cref="IMySingleton"/>, implementers of this interface should ensure a single, consistent source
    /// for device information across the application. All properties are read-only, reflecting static device characteristics.
    /// </remarks>
    public interface IFDeviceInfoRepository : IMySingleton
    {

        /// <summary>
        /// Gets the user-defined name of the device (e.g., "My iPhone").
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Gets the operating system name and version of the device (e.g., "Android OS 12", "iOS 16.5").
        /// </summary>
        string OperatingSystem { get; }

        /// <summary>
        /// Gets the model identifier of the device (e.g., "iPhone14,2", "Samsung SM-G998B").
        /// </summary>
        string DeviceModel { get; }

        /// <summary>
        /// Gets the width of the device's screen in pixels.
        /// </summary>
        int ScreenWidth { get; }

        /// <summary>
        /// Gets the height of the device's screen in pixels.
        /// </summary>
        int ScreenHeight { get; }

        /// <summary>
        /// Gets the screen density expressed as Dots Per Inch (DPI).
        /// </summary>
        float ScreenDpi { get; }

        /// <summary>
        /// Gets the name of the Graphics Processing Unit (GPU) used by the device.
        /// </summary>
        string GpuName { get; }

        /// <summary>
        /// Gets the name of the Central Processing Unit (CPU) used by the device.
        /// </summary>
        string CpuType { get; }

        /// <summary>
        /// Gets the primary language set on the device, typically as an ISO 639-1 code (e.g., "en", "es", "vi").
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets the Identifier For Vendor (IDFV) for iOS devices.
        /// This is a unique identifier for the app on a specific vendor's devices.
        /// </summary>
        string Idfv { get; }

        /// <summary>
        /// Gets a unique device identifier. This could be a persistent ID specific to the device.
        /// </summary>
        string DeviceId { get; }
        
        int Ram { get; }
        int GpuRam { get; }
        int CpuCount { get; }
        int CpuFrequency { get; }
    }
}