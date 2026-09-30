/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-04
     */

using UnityEngine;

namespace Falcon.Modules.Core.SystemInformation.Runtime
{
	using System;
	using System.Globalization;
	using System.IO;
    using System.Text;
    using Falcon.Helpers.EventBus;
	using Falcon.Helpers.Security;
    using Falcon.Modules.Core.SaveLoad.Runtime;

    /// <summary>
	/// Query needed information about devices spec: Model, OS, RAM, etc. Furthermore, about App itself: Version, Libs, etc...
	/// Using <see cref="Encryption"/> to hash ids if no information was found.
	/// NOTES: Before using <see cref="Device.AdvertisingID"/>, function <see cref="Device.GetAdvertisingID"/> must be called after ATT consent.
	/// </summary>
	public static class SystemInformation
    {
	    public static class Device
	    {
		    public static readonly string DeviceModel;
		    public static readonly string OperatingSystem;
		    public static readonly string SystemLanguage;
		    public static readonly string SystemLanguageISO;
		    public static readonly string Resolution;
		    public static readonly int    DeviceMemory;
		    public static readonly int    GraphicsMemorySize;
		    public static readonly string Platform;

		    public static string DeviceUUID    { get; private set; }
		    public static string AdvertisingID { get; private set; }

		    private static readonly string kUniqueIDKey = "UniqueIdentifier";
		    
#if UNITY_EDITOR
		    public static readonly string kCustomDeviceUUIDKey = "SysInfo.DeviceUUID.Custom";
#endif

		    static Device()
		    {
#if UNITY_EDITOR
			    DeviceModel        = "editor";
			    OperatingSystem    = "operatingSystem";
			    SystemLanguage     = "VN";
			    SystemLanguageISO  = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
			    Resolution         = "1920x1080";
			    DeviceMemory       = 1024;
			    GraphicsMemorySize = 2048;
			    Platform           = "editor";
#else
			    DeviceModel        = SystemInfo.deviceModel;
			    OperatingSystem    = SystemInfo.operatingSystem;
			    SystemLanguage     = Application.systemLanguage.ToString();
			    SystemLanguageISO  = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
				Resolution         = Screen.currentResolution.width.ToString() + "x" + Screen.currentResolution.height.ToString();
				DeviceMemory       = SystemInfo.systemMemorySize;
				GraphicsMemorySize = SystemInfo.graphicsMemorySize;
				Platform           = Application.platform.ToString();
			    
#if UNITY_ANDROID
			    Platform = "android";
#elif UNITY_IOS
				Platform = "ios";
#endif
#endif
			    
			    AdvertisingID = "---advertisingId-not-initialized---";
			    GetDeviceUUID();
			    
#if UNITY_ANDROID
			    GetAdvertisingID();
#endif
			    
			    GameEvent<bool>.Register(Const.UMP_COMPLETE_EVENT, b =>
			    {
				    GetAdvertisingID();
			    }, null);
		    }

		    public static void Initialize() { }

		    /// <summary>
		    /// Must be called when ATT complete to get right value
		    /// </summary>
		    public static void GetAdvertisingID()
		    {
#if UNITY_EDITOR
			    AdvertisingID = "advertisingId-editor";
#elif UNITY_ANDROID
			    try
			    {
				    AndroidJavaClass  up              = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				    AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
				    AndroidJavaClass  client          = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
				    AndroidJavaObject adInfo          = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);
				    AdvertisingID = adInfo.Call<string>("getId").ToString();
			    }
			    catch (Exception)
			    {
				    //
			    }
#elif UNITY_IOS
			    Application.RequestAdvertisingIdentifierAsync(
				    (string advertisingId, bool trackingEnabled, string error) =>
				    {
					    AdvertisingID = advertisingId;
				    }
			    );
#endif
		    }

		    private static void GetDeviceUUID()
		    {
			    string deviceUUID = string.Empty;
#if UNITY_EDITOR
				DeviceUUID = SystemInfo.deviceUniqueIdentifier + "-editor";
				var custom = UnityEditor.EditorPrefs.GetString(kCustomDeviceUUIDKey, string.Empty);
				if (!string.IsNullOrEmpty(custom))
				{
					DeviceUUID = custom;
				}
#elif UNITY_ANDROID
                try
                {
                    AndroidJavaClass  up              = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
                    AndroidJavaClass  secure          = new AndroidJavaClass("android.provider.Settings$Secure");
                    
                    deviceUUID = secure.CallStatic<string>("getString", contentResolver, "android_id");

                    if (!IsDeviceUUIDValid(deviceUUID))
                    {
                        AndroidJavaClass  client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
                        AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);
                        deviceUUID = adInfo.Call<string>("getId");
                    }
                    
                    if (!IsDeviceUUIDValid(deviceUUID))
                    {
	                    deviceUUID = CreateUniqueString();
                    }

                    if (!IsDeviceUUIDValid(deviceUUID))
                    {
	                    deviceUUID = SystemInfo.deviceUniqueIdentifier;
                    }

                }
                catch (Exception e)
                {
	                if (!IsDeviceUUIDValid(deviceUUID))
	                {
		                deviceUUID = CreateUniqueString();
	                }
	                
                    if (!IsDeviceUUIDValid(deviceUUID))
                    {
	                    deviceUUID = SystemInfo.deviceUniqueIdentifier;
                    }
                }
                
				DeviceUUID = deviceUUID;
#elif UNITY_WEBGL
                if (!PlayerPrefs.HasKey(kUniqueIDKey))
                    PlayerPrefs.SetString(kUniqueIDKey, System.Guid.NewGuid().ToString());
                DeviceUUID = PlayerPrefs.GetString(kUniqueIDKey);
#elif UNITY_IOS
                string key = "falcon_" + Application.identifier + "_uuid";
                deviceUUID = UUIDiOS.GetKeyChainValue(key);

                if (!IsDeviceUUIDValid(deviceUUID))
                {
	                deviceUUID = Guid.NewGuid().ToString();
                    UUIDiOS.SaveKeyChainValue(key, deviceUUID); 
                }

                if (!IsDeviceUUIDValid(deviceUUID))
                {
	                deviceUUID = UnityEngine.iOS.Device.vendorIdentifier;
                }
                
                DeviceUUID = deviceUUID;
#else
                DeviceUUID = SystemInfo.deviceUniqueIdentifier;
#endif
		    }

		    private static bool IsDeviceUUIDValid(string deviceId)
		    {
			    if (string.IsNullOrEmpty(deviceId) || deviceId.ToLower().Equals("unknown") || deviceId.Length < 4)
			    {
				    return false;
			    }
			    
			    return true;
		    }
		    
		    private static string CreateUniqueString()
		    {
			    string deviceInfo = SystemInfo.deviceModel + SystemInfo.deviceType + SystemInfo.graphicsDeviceName + SystemInfo.graphicsDeviceType;
			    return Encryption.SHA1Hash(deviceInfo);
		    }
	    }

	    public static class App
	    {
		    public static readonly string Version;
		    public static readonly int    VersionInt;
		    public static readonly string PackageName;
		    public static readonly string InstallVendor;
		    
		    public static int    NumberLibFiles   { get; private set; }
		    public static long   TotalLibFileSize { get; private set; }
		    public static string LibFileNameList  { get; private set; }
		    public static string LibFolder        { get; private set; }
		    public static string LibMD5           { get; private set; }
            
            private static readonly string kAppLibInfoSaveKey = "SystInfo.App.Lib";

		    static App()
		    {
			    Version       = Application.version;
			    VersionInt    = GetVerInt();
			    PackageName   = Application.identifier;
			    InstallVendor = Application.installerName;
			    
			    //GetInfo();
		    }
		    
		    public static void Initialize() { }
		    
		    private static int GetVerInt()
		    {
			    var versionString = Application.version;
			    var parts         = versionString.Split('.');

			    int major = 0;
			    int minor = 0;
			    int patch = 0;

			    if (parts.Length > 0) int.TryParse(parts[0], out major);
			    if (parts.Length > 1) int.TryParse(parts[1], out minor);
			    if (parts.Length > 2) int.TryParse(parts[2], out patch);

			    return major * 1000000 + minor * 1000 + patch;
		    }

		    private static void GetInfo()
		    {
#if UNITY_EDITOR
			    NumberLibFiles   = 0;
			    TotalLibFileSize = 0;
			    LibFileNameList  = string.Empty;
			    LibFolder        = string.Empty;
			    LibMD5           = string.Empty;
#elif UNITY_ANDROID

                if (SaveLoadHandler.ExistsKey(kAppLibInfoSaveKey))
                {
                    var save = SaveLoadHandler.Load<AppLibInfoSave>(kAppLibInfoSaveKey);
                    if (save != null)
                    {
                        NumberLibFiles   = save.numberLibFiles;
                        TotalLibFileSize = save.totalLibFileSize;
                        LibFileNameList  = save.libFileNameList;
                        LibFolder        = save.libFolder;
                        LibMD5           = save.libMD5;
                    }
                    else
                    {
                        GetAppLibInfo();
                    }
                }
                else
                {
                    GetAppLibInfo();
                }
#else
			    NumberLibFiles   = 0;
			    TotalLibFileSize = 0;
			    LibFileNameList  = string.Empty;
			    LibFolder        = string.Empty;
			    LibMD5           = string.Empty;
#endif
		    }

            private static void GetAppLibInfo()
            {
                GetAndroidAppLibInfo();
                SaveAppLibInfo();
            }

            private static void GetAndroidAppLibInfo()
            {
#if UNITY_ANDROID
                try
		        {
                    var player          = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		            var activity        = player.GetStatic<AndroidJavaObject>("currentActivity");
		            var applicationInfo = activity.Call<AndroidJavaObject>("getApplicationInfo");
		            var nativeLibPath   = applicationInfo.Get<string>("nativeLibraryDir");
		            
		            
		            var lastFolderName = Path.GetFileName(nativeLibPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

		            string[] fileList = Directory.GetFiles(nativeLibPath);
                    Array.Sort(fileList, StringComparer.Ordinal);
		            long   totalFileSize = 0;
                    
                    // Efficiently build concatenation of per-file MD5 strings
                    var perFileHashesConcat = new StringBuilder(lastFolderName);

                    // Build name list without Replace chains
                    var names = new string[fileList.Length];

                    for (int i = 0; i < fileList.Length; i++)
                    {
                        var path = fileList[i];

                        // file size
                        var fi = new FileInfo(path);
                        totalFileSize += fi.Length;

                        perFileHashesConcat.Append(Encryption.MD5File(path));

                        // Faster & safer than replacing substrings
                        names[i] = Path.GetFileNameWithoutExtension(path);
                    }

                    var comboString = string.Join(",", names);

		            NumberLibFiles   = fileList.Length;
		            TotalLibFileSize = totalFileSize;
		            LibFileNameList  = comboString;
		            LibFolder        = lastFolderName;
		            LibMD5           = Encryption.MD5(perFileHashesConcat + comboString + totalFileSize);
		        }
		        catch
		        {
			        NumberLibFiles   = 0;
			        TotalLibFileSize = 0;
			        LibFileNameList  = string.Empty;
			        LibFolder        = string.Empty;
			        LibMD5           = string.Empty;
		        }
#endif
            }

            private static void SaveAppLibInfo()
            {
                var libSave = new AppLibInfoSave()
                {
                    numberLibFiles   = NumberLibFiles,
                    totalLibFileSize = TotalLibFileSize,
                    libFileNameList  = LibFileNameList,
                    libFolder        = LibFolder,
                    libMD5           = LibMD5,
                };
                    
                SaveLoadHandler.Save<AppLibInfoSave>(kAppLibInfoSaveKey, libSave);
            }
	    }

	    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
	    private static void Run()
	    {
		    Device.Initialize();
		    App.Initialize();
		    
		    Debug.Log("SystemInformation.Run()");
	    }
    }
}
