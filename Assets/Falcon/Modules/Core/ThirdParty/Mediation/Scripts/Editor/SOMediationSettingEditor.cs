/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Editor
{
    [CustomEditor(typeof(SOMediationSetting))]
    public class SOMediationSettingEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            SirenixEditorGUI.HorizontalLineSeparator();

            var settings = (SOMediationSetting)target;

            if (GUILayout.Button("Save"))
            {
                ValidateEvent(settings);
                ProcessInstallAdapter(settings);
            }
        }

//ironsource
        private static List<string> adaptersIrs = new List<string>()
        {
            "AppLovin", "Fyber", "AdMob", "InMobi", "Liftoff", "Meta", "Mintegral", "Pangle",
            "Smaato", "UnityAds", "BidMachine", "Moloco", "Yandex"
        };

//max
        private static string unityPluginVersion = "";
        private static int cnt = 0;

        private static bool isLoading = false;

        private static List<string> adapters = new List<string>()
        {
            "BidMachine", "BigoAds", "ByteDance", "Facebook", "Fyber", "Google", "GoogleAdManager", "InMobi",
            "IronSource", "Mintegral", "Moloco", "Smaato", "UnityAds", "Verve", "Vungle", "Yandex"
        };

        private static List<(string path, string label)> filesToLabel = new();

        private async void ProcessInstallAdapter(SOMediationSetting settings)
        {
            #region IronSource

            if (settings.installAdapterIrs)
            {
                var type = Type.GetType("LevelPlayDependenciesManager, Unity.LevelPlay.Editor");
                if (type == null)
                {
                    Debug.LogError("LevelPlay chưa được import.");
                    return;
                }

                var window = CreateInstance(type);
                var onEnable = type.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
                onEnable?.Invoke(window, null);
                var managerField =
                    type.GetField("m_LevelPlayNetworkManager", BindingFlags.NonPublic | BindingFlags.Instance);
                var networkManager = managerField?.GetValue(window);
                var adaptersProperty = networkManager?.GetType().GetProperty("Adapters");
                var adapters = adaptersProperty?.GetValue(networkManager) as IDictionary;
                var downloadMethod = type.GetMethod("DownloadAdapterNetworkAction",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (adapters == null) return;
                var cnt = 0;
                Debug.Log("Processing download adapter for LevelPlay");
                foreach (DictionaryEntry entry in adapters)
                {
                    var key = entry.Key;
                    var adapter = entry.Value;

                    if (downloadMethod == null) continue;

                    if (adaptersIrs.Contains(key.ToString()))
                    {
                        var taskObj = downloadMethod.Invoke(window, new object[] { adapter });
                        if (taskObj is Task task)
                        {
                            cnt++;
                            Debug.Log("processing : " + cnt + "/" + adaptersIrs.Count);
                            await task;
                        }
                    }
                }

                if (cnt == adaptersIrs.Count)
                {
                    Debug.Log("Done");
                }
            }

            #endregion

            if (settings.installAdapterMax)
            {
#if MAX_ENABLE
                var type = Type.GetType("MaxSdk, MaxSdk.Scripts");
                // Tìm property "Version"
                if (type != null)
                {
                    var property = type.GetProperty("Version", BindingFlags.Public | BindingFlags.Static);

                    // Lấy giá trị property
                    if (property != null)
                    {
                        var value = property.GetValue(null, null);
                        unityPluginVersion = value?.ToString();
                    }
                }

                if (!string.IsNullOrWhiteSpace(unityPluginVersion))
                {
                    if (isLoading)
                    {
                        Debug.LogWarning("Downloading adapter, please wait....");
                    }
                    else
                    {
                        isLoading = true;
                        try
                        {
                            Debug.Log("Processing download adapter for MAX version " + unityPluginVersion);
                            var url =
                                "https://github.com/AppLovin/AppLovin-MAX-Unity-Plugin/releases/tag/release_" +
                                unityPluginVersion.Replace(".", "_");
                            GetContentFromUrl(url, s =>
                            {
                                var links = ExtractReleaseLinks(s);
                                ClearFolderMediation();
                                foreach (var link in links)
                                {
                                    var version = link["version"];
                                    var platform = link["platform"];
                                    cnt = 0;
                                    for (var i = 0; i < adapters.Count; i++)
                                    {
                                        var href = "https://raw.githubusercontent.com/AppLovin/AppLovin-MAX-SDK-" +
                                                   platform +
                                                   "/refs/tags/release_" + version + "/" + adapters[i] +
                                                   "/CHANGELOG.md";
                                        var i1 = i;
                                        GetContentFromUrl(href, s1 =>
                                        {
                                            cnt++;
                                            var match = Regex.Match(s1, @"^##\s+(\d+(?:\.\d+){2,5})",
                                                RegexOptions.Multiline);
                                            if (match.Success)
                                            {
                                                if (platform.Contains("Android"))
                                                {
                                                    var versionAdapter = match.Groups[1].Value;
                                                    CopyFileWithOverwrite(adapters[i1], versionAdapter, "");
                                                }
                                                else if (platform.Contains("iOS"))
                                                {
                                                    var versionAdapter = match.Groups[1].Value;
                                                    CopyFileWithOverwrite(adapters[i1], "", versionAdapter);
                                                }
                                            }
                                            else
                                            {
                                                Debug.LogError(adapters[i1] + " error ");
                                            }

                                            Debug.Log("processing : " + cnt + "/" + adapters.Count * 2);

                                            if (cnt == adapters.Count * 2)
                                            {
                                                AssetDatabase.Refresh();
                                                foreach (var (path, label) in filesToLabel)
                                                {
                                                    var asset = AssetDatabase.LoadMainAssetAtPath(path);
                                                    if (asset != null)
                                                    {
                                                        AssetDatabase.SetLabels(asset, new[] { label });
                                                    }
                                                    else
                                                    {
                                                        Debug.LogError($"Không load được asset tại: {path}");
                                                    }
                                                }

                                                isLoading = false;
                                                Debug.Log("Done");
                                            }
                                        });
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Lỗi: {ex.Message}");
                        }
                    }
                }
#else
                    Debug.LogError("MaxSdk chưa được import!");
#endif
            }

            if (settings.installAdapterGma)
            {
            }
        }

        private static void ClearFolderMediation()
        {
            var folderPath = "Assets/MaxSdk/Mediation";

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.DeleteAsset(folderPath);
                AssetDatabase.Refresh();
            }
        }

        private static void CopyFileWithOverwrite(string adapter, string androidVersion, string iOSVersion)
        {
            var script = MonoScript.FromScriptableObject(CreateInstance<SOMediationSettingEditor>());

            // Lấy đường dẫn tương đối trong Assets
            var scriptPath = AssetDatabase.GetAssetPath(script);

            var folderPath = Path.GetDirectoryName(scriptPath);

            var fileSourceName = "Dependencies.txt";
            var fileDestName = "Dependencies.xml";
            var sourcePath = Path.Combine(folderPath, "Max/Mediation/" + adapter + "/Editor", fileSourceName);
            var destinationPath = Path.Combine("Assets/MaxSdk", "Mediation/" + adapter + "/Editor", fileDestName);

            // Kiểm tra file nguồn
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"File không tồn tại tại: {sourcePath}");
                return;
            }

            // Đảm bảo thư mục đích tồn tại
            var destFolder = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            // Đọc nội dung file A
            var content = File.ReadAllText(sourcePath);
            if (!string.IsNullOrWhiteSpace(androidVersion))
                content = content.Replace("{androidVersion}", androidVersion);
            if (!string.IsNullOrWhiteSpace(iOSVersion))
                content = content.Replace("{iOSVersion}", iOSVersion);
            content = content.Replace("falconDep", "dependencies");

            if (!File.Exists(destinationPath))
            {
                // Nếu file chưa tồn tại → ghi nội dung từ A
                File.WriteAllText(destinationPath, content);
            }
            else
            {
                // Nếu đã có → đọc và sửa nội dung file B
                var contentB = File.ReadAllText(destinationPath);
                contentB = contentB.Replace("{androidVersion}", androidVersion);
                contentB = contentB.Replace("{iOSVersion}", iOSVersion);

                // Ghi lại nội dung đã sửa
                File.WriteAllText(destinationPath, contentB);
            }

            var newLabel = "al_max_export_path-MaxSdk/Mediation/" + adapter + "/Editor/Dependencies.xml";
            filesToLabel.Add((destinationPath, newLabel));
        }

        private static List<Dictionary<string, string>> ExtractReleaseLinks(string html)
        {
            var results = new List<Dictionary<string, string>>();

            string pattern =
                @"<a[^>]+href\s*=\s*""(https:\/\/github\.com\/AppLovin\/AppLovin-MAX-SDK-(Android|iOS)\/releases\/tag\/release_([0-9_]+))""";

            foreach (Match match in Regex.Matches(html, pattern))
            {
                if (match.Success && match.Groups.Count >= 4)
                {
                    var url = match.Groups[1].Value;
                    var platform = match.Groups[2].Value; // "Android" hoặc "iOS"
                    var version = match.Groups[3].Value; // "13_2_0"

                    results.Add(new Dictionary<string, string>
                    {
                        { "platform", platform },
                        { "version", version },
                        { "url", url }
                    });
                }
            }

            return results;
        }

        private static void GetContentFromUrl(string url, Action<string> onComplete)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SendWebRequest();

            void Wait()
            {
                if (!request.isDone) return;

                EditorApplication.update -= Wait;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(request.downloadHandler.text);
                }
                else
                {
                    onComplete?.Invoke(null);
                    Debug.LogError("UnityWebRequest error: " + request.error);
                }
            }

            EditorApplication.update += Wait;
        }

        private void ValidateEvent(SOMediationSetting settings)
        {
            var type = Type.GetType("LevelPlayMediationSettingsInspector, Unity.LevelPlay.Editor");

            #region iron source

            if (type != null)
            {
                var propertyInfo = type.GetProperty("LevelPlayMediationSettings",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo != null)
                {
                    var asset1 = propertyInfo.GetValue(null) as ScriptableObject;
                    if (asset1 != null)
                    {
                        if (settings.ironSourceAppKeyAndroid != string.Empty)
                        {
                            var fieldInfo = asset1.GetType().GetField("AndroidAppKey");
                            if (fieldInfo != null) fieldInfo.SetValue(asset1, settings.ironSourceAppKeyAndroid);
                            fieldInfo = asset1.GetType().GetField("DeclareAD_IDPermission");
                            if (fieldInfo != null)
                                fieldInfo.SetValue(asset1, settings.ironSourceAppKeyAndroid != string.Empty);
                        }

                        if (settings.ironSourceAppKeyIOS != string.Empty)
                        {
                            var fieldInfo = asset1.GetType().GetField("IOSAppKey");
                            if (fieldInfo != null) fieldInfo.SetValue(asset1, settings.ironSourceAppKeyIOS);
                            fieldInfo = asset1.GetType().GetField("AddIronsourceSkadnetworkID");
                            if (fieldInfo != null)
                                fieldInfo.SetValue(asset1, settings.ironSourceAppKeyIOS != string.Empty);
                        }

                        EditorUtility.SetDirty(asset1);
                    }
                }
            }


            type = Type.GetType("LevelPlayMediationNetworkSettingsInspector, Unity.LevelPlay.Editor");
            if (type != null)
            {
                var propertyInfo = type.GetProperty("LevelPlayMediationNetworkSettings",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo != null)
                {
                    var asset1 = propertyInfo.GetValue(null) as ScriptableObject;
                    if (asset1 != null)
                    {
                        if (settings.admobAppIdAndroid != string.Empty)
                        {
                            var fieldInfo = asset1.GetType().GetField("AdmobAndroidAppId");
                            if (fieldInfo != null) fieldInfo.SetValue(asset1, settings.admobAppIdAndroid);
                            fieldInfo = asset1.GetType().GetField("EnableAdmob");
                            if (fieldInfo != null)
                                fieldInfo.SetValue(asset1, settings.admobAppIdAndroid != string.Empty);
                        }

                        if (settings.admobAppIdIOS != string.Empty)
                        {
                            var fieldInfo = asset1.GetType().GetField("AdmobIOSAppId");
                            if (fieldInfo != null) fieldInfo.SetValue(asset1, settings.admobAppIdIOS);
                            fieldInfo = asset1.GetType().GetField("EnableAdmob");
                            if (fieldInfo != null)
                                fieldInfo.SetValue(asset1, settings.admobAppIdIOS != string.Empty);
                        }

                        EditorUtility.SetDirty(asset1);
                    }
                }
            }

            //for network manager settings
            const string SETTINGS_ASSET_PATH = "Assets/LevelPlay/Editor/NetworkManagerSettings.asset";
            var settingsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SETTINGS_ASSET_PATH);
            type = Type.GetType("Unity.Services.LevelPlay.Editor.NetworkManagerSettings, Unity.LevelPlay.Editor");
            if (type != null)
            {
                if (settingsAsset == null)
                {
                    // Tạo mới nếu chưa có asset
                    var newAsset = CreateInstance(type);
                    AssetDatabase.CreateAsset(newAsset, SETTINGS_ASSET_PATH);
                    settingsAsset = newAsset;
                    Debug.Log("Đã tạo mới NetworkManagerSettings.asset.");
                }

                var field = type.GetField("AddNetworksSkadnetworkID",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) field.SetValue(settingsAsset, true);
                EditorUtility.SetDirty(settingsAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            #endregion

            #region MAX

            type = Type.GetType("AppLovinSettings, MaxSdk.Scripts.IntegrationManager.Editor");
            if (type != null)
            {
                ScriptableObject so = CreateInstance(type);
                var fieldInfo = so.GetType().GetProperty("SdkKey");
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(so, settings.appLovinSdkKey);
                }

                fieldInfo = so.GetType().GetProperty("AdMobAndroidAppId");
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(so, settings.admobAppIdAndroid);
                }

                fieldInfo = so.GetType().GetProperty("AdMobIosAppId");
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(so, settings.admobAppIdIOS);
                }

                fieldInfo = so.GetType().GetProperty("QualityServiceEnabled");
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(so, true);
                }
            }

            #endregion

            #region GMA

            type = Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            if (type == null) return;
            var func = type.GetMethod("LoadInstance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (func == null) return;
            var asset = func.Invoke(null, null) as ScriptableObject;

            if (asset != null)
            {
                var fieldInfo = asset.GetType()
                    .GetField("adMobAndroidAppId", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null) fieldInfo.SetValue(asset, settings.admobAppIdAndroid);
                fieldInfo = asset.GetType()
                    .GetField("adMobIOSAppId", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null) fieldInfo.SetValue(asset, settings.admobAppIdIOS);
                fieldInfo = asset.GetType()
                    .GetField("delayAppMeasurementInit", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null) fieldInfo.SetValue(asset, true);
                fieldInfo = asset.GetType()
                    .GetField("optimizeInitialization", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null) fieldInfo.SetValue(asset, true);
                fieldInfo = asset.GetType()
                    .GetField("optimizeAdLoading", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null) fieldInfo.SetValue(asset, true);
                string content = "This uses device info for more personalized ads and content";
                fieldInfo = asset.GetType()
                    .GetField("userTrackingUsageDescription", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null) fieldInfo.SetValue(asset, content);
            }

            #endregion

            EditorUtility.SetDirty(asset);
        }
    }
}