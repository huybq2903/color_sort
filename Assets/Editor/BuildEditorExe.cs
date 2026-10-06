using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Build bản exe Level Editor (EditorScene làm scene đầu), chạy bằng -executeMethod BuildEditorExe.Build.</summary>
public static class BuildEditorExe
{
    private const string OutDir = "Builds/ColorSortEditor";

    [MenuItem("Tools/Build Editor Exe")]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        var editor = scenes.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "EditorScene");
        if (editor == null) throw new System.Exception("EditorScene chưa có trong Build Settings");
        scenes.Remove(editor);
        scenes.Insert(0, editor); // scene đầu tiên là scene chạy khi mở exe

        AddressableAssetSettings.BuildPlayerContent(out var content); // content Addressables cho Windows, đi kèm bản build
        if (!string.IsNullOrEmpty(content.Error)) throw new System.Exception("Build Addressables lỗi: " + content.Error);

        if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = $"{OutDir}/ColorSortEditor.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });

        var ok = report.summary.result == BuildResult.Succeeded;
        Debug.Log($"Build {report.summary.result}: {report.summary.totalSize / 1048576} MB, {report.summary.totalErrors} lỗi");
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }
}
