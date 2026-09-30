/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-13
 */

using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public partial class ModuleSettingsWindow : EditorWindow
    {
        private const string URL_APPSFLYER = "https://github.com/AppsFlyerSDK/appsflyer-unity-plugin/releases";
        private const string URL_FIREBASE  = "https://github.com/firebase/firebase-unity-sdk/releases";
        private const string URL_MAX       = "https://github.com/AppLovin/AppLovin-MAX-Unity-Plugin/releases";

        private Vector2 _generalScroll;

        private static GUIStyle _sectionHeaderStyle;
        private static GUIStyle _infoLabelStyle;
        private static GUIStyle _infoValueStyle;

        private static GUIStyle SectionHeaderStyle =>
            _sectionHeaderStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };

        private static GUIStyle InfoLabelStyle =>
            _infoLabelStyle ??= new GUIStyle(EditorStyles.label) { fontSize = 13, fontStyle = FontStyle.Bold };

        private static GUIStyle InfoValueStyle =>
            _infoValueStyle ??= new GUIStyle(EditorStyles.label) { fontSize = 13 };

        private void RenderGeneralTab()
        {
            _generalScroll = EditorGUILayout.BeginScrollView(_generalScroll);

            RenderProjectInfo();

            GUILayout.Space(10);

            RenderQuickActions();

            EditorGUILayout.EndScrollView();
        }

        private void RenderQuickActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Quick Actions", SectionHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Player Settings", GUILayout.Height(24)))
                SettingsService.OpenProjectSettings("Project/Player");
            if (GUILayout.Button("Open Build Settings", GUILayout.Height(24)))
                OpenBuildSettings();
            if (GUILayout.Button("AppLovin Integration Manager", GUILayout.Height(24)))
                ExecuteMenu("AppLovin/Integration Manager");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("3rd-party SDK Releases", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("AppsFlyer Unity", GUILayout.Height(22)))
                Application.OpenURL(URL_APPSFLYER);
            if (GUILayout.Button("Firebase Unity", GUILayout.Height(22)))
                Application.OpenURL(URL_FIREBASE);
            if (GUILayout.Button("AppLovin MAX Unity", GUILayout.Height(22)))
                Application.OpenURL(URL_MAX);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void ExecuteMenu(string menuPath)
        {
            if (!EditorApplication.ExecuteMenuItem(menuPath))
                Debug.LogWarning($"[ModuleSettings] Menu item not found: {menuPath}");
        }

        private static void OpenBuildSettings()
        {
            // Unity 6 renamed "Build Settings" to "Build Profiles"; try the new menu
            // first, then fall back to the pre-6 paths. ExecuteMenuItem returns false
            // (no exception) for a path that doesn't exist on the running version.
            string[] candidates =
            {
                "File/Build Profiles...",
                "File/Build Profiles",
                "File/Build Settings...",
                "File/Build Settings",
            };

            foreach (var path in candidates)
            {
                if (EditorApplication.ExecuteMenuItem(path))
                    return;
            }

            Debug.LogWarning("[ModuleSettings] Could not open Build Settings/Profiles window.");
        }

        private void RenderProjectInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Project Info", SectionHeaderStyle);
            GUILayout.Space(2);

            InfoRow("Product Name", ProjectInfoService.ProductName, false);
            InfoRow("Company Name", ProjectInfoService.CompanyName, false);
            InfoRow("Package ID", ProjectInfoService.PackageId, true);
            InfoRow("Version", ProjectInfoService.BundleVersion, true);

            GUILayout.Space(4);
            InfoRow("Android Version Code", ProjectInfoService.AndroidBundleVersionCode, true);
            InfoRow("iOS Build Number", ProjectInfoService.IosBuildNumber, true);

            GUILayout.Space(4);
            InfoRow("Active Platform", ProjectInfoService.ActiveBuildTarget, false);
            InfoRow("Unity Version", ProjectInfoService.UnityVersion, false);
            InfoRow("Scripting Backend", ProjectInfoService.ScriptingBackend, false);
            InfoRow("Android Min SDK", ProjectInfoService.AndroidMinSdk, false);
            InfoRow("Android Target SDK", ProjectInfoService.AndroidTargetSdk, false);

            GUILayout.Space(4);
            InfoRow("Render Pipeline", ProjectInfoService.RenderPipeline, false);

            EditorGUILayout.EndVertical();
        }

        private static void InfoRow(string label, string value, bool copyable)
        {
            var display = value ?? "";
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, InfoLabelStyle, GUILayout.Width(180));
            // Read-only: SelectableLabel is not editable; a plain label style (no text-field
            // box) makes it read as text while still allowing select-to-copy.
            EditorGUILayout.SelectableLabel(display, InfoValueStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
            if (copyable && GUILayout.Button("Copy", GUILayout.Width(52), GUILayout.Height(20)))
                EditorGUIUtility.systemCopyBuffer = display;
            EditorGUILayout.EndHorizontal();
        }
    }
}
