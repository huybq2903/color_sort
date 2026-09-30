/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-13
 */

using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    /// <summary>
    /// Gathers read-only project information via editor APIs for the General tab.
    /// Performs no GUI work. Reads are safe to call from OnGUI: the build-target
    /// lookups are guarded and RenderPipeline null-guards its render-pipeline lookup,
    /// so a getter returns "—" rather than throwing into the surrounding layout.
    /// </summary>
    internal static class ProjectInfoService
    {
        public static string ProductName => PlayerSettings.productName;
        public static string CompanyName => PlayerSettings.companyName;
        public static string BundleVersion => PlayerSettings.bundleVersion;
        public static string UnityVersion => Application.unityVersion;
        public static string ActiveBuildTarget => EditorUserBuildSettings.activeBuildTarget.ToString();

        public static string AndroidBundleVersionCode => PlayerSettings.Android.bundleVersionCode.ToString();
        public static string AndroidMinSdk => PlayerSettings.Android.minSdkVersion.ToString();
        public static string AndroidTargetSdk => PlayerSettings.Android.targetSdkVersion.ToString();
        public static string IosBuildNumber => PlayerSettings.iOS.buildNumber;

        public static string PackageId
        {
            get
            {
                try
                {
                    var named = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                    return PlayerSettings.GetApplicationIdentifier(named);
                }
                catch (System.ArgumentException)
                {
                    return "—";
                }
            }
        }

        public static string ScriptingBackend
        {
            get
            {
                try
                {
                    var named = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                    return PlayerSettings.GetScriptingBackend(named).ToString();
                }
                catch (System.ArgumentException)
                {
                    return "—";
                }
            }
        }

        public static string RenderPipeline
        {
            get
            {
                var rp = GraphicsSettings.currentRenderPipeline != null
                    ? GraphicsSettings.currentRenderPipeline
                    : GraphicsSettings.defaultRenderPipeline;
                if (rp == null) return "Built-in";
                var typeName = rp.GetType().Name;
                if (typeName.Contains("Universal")) return "URP";
                if (typeName.Contains("HD")) return "HDRP";
                return typeName;
            }
        }
    }
}
