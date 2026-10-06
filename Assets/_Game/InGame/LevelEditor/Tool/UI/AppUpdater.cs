using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Falcon.Shared.BaseLevelEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Tự cập nhật bản exe từ GitHub Releases: so tag mới nhất với Application.version, tải zip, thoát và giải nén đè.</summary>
    public class AppUpdater
    {
        private const string Repo = "huybq2903/color_sort";
        private const string Api = "https://api.github.com/repos/" + Repo + "/releases/latest";

        [Serializable] private class Asset { public string name; public string browser_download_url; }
        [Serializable] private class Release { public string tag_name; public Asset[] assets; }

        private string _zipUrl;
        private bool _busy;

        /// <summary>Tag bản mới đã phát hiện, null nếu chưa có.</summary>
        public string Tag { get; private set; }

        /// <summary>Hỏi GitHub bản mới nhất; có bản mới hơn thì gọi onFound(tag). Lỗi mạng/chưa có release thì im lặng.</summary>
        public IEnumerator Check(Action<string> onFound)
        {
            using var req = UnityWebRequest.Get(Api);
            req.SetRequestHeader("User-Agent", "ColorSortEditor"); // GitHub API bắt buộc có User-Agent
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;

            var rel = JsonUtility.FromJson<Release>(req.downloadHandler.text);
            if (rel?.assets == null || !IsNewer(rel.tag_name)) yield break;
            foreach (var a in rel.assets)
                if (a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) _zipUrl = a.browser_download_url;
            if (_zipUrl == null) yield break;
            Tag = rel.tag_name;
            onFound(Tag);
        }

        /// <summary>Tải zip, chạy script PowerShell đợi exe thoát rồi giải nén đè và mở lại, sau đó thoát app.</summary>
        public IEnumerator Apply()
        {
            if (_busy) yield break;
            _busy = true;
            var zip = Path.Combine(Path.GetTempPath(), "colorsort_update.zip");
            using (var req = UnityWebRequest.Get(_zipUrl))
            {
                req.downloadHandler = new DownloadHandlerFile(zip);
                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    LevelEditorMainUI.Log($"Đang tải bản cập nhật {(int)(req.downloadProgress * 100)}%");
                    yield return null;
                }
                if (req.result != UnityWebRequest.Result.Success)
                {
                    _busy = false;
                    LevelEditorMainUI.Warn("Tải bản cập nhật thất bại: " + req.error);
                    yield break;
                }
            }

            var ps1 = Path.Combine(Path.GetTempPath(), "colorsort_update.ps1");
            File.WriteAllText(ps1,
                "param($p,$zip,$dir,$exe)\n" +
                "Wait-Process -Id $p -ErrorAction SilentlyContinue\n" +
                "Expand-Archive -Force -LiteralPath $zip -DestinationPath $dir\n" +
                "$new = Join-Path $dir 'MosaicEditor.exe'\n" + // bản mới đổi tên exe, mở đúng file mới thay vì exe cũ
                "if (Test-Path $new) { $exe = $new }\n" +
                "Start-Process $exe\n");
            var self = Process.GetCurrentProcess();
            var dir = Path.GetDirectoryName(Application.dataPath);
            Process.Start(new ProcessStartInfo("powershell",
                $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{ps1}\" {self.Id} \"{zip}\" \"{dir}\" \"{self.MainModule.FileName}\"")
            { CreateNoWindow = true, UseShellExecute = false });
            Application.Quit();
        }

        private static bool IsNewer(string tag) =>
            Version.TryParse(tag.TrimStart('v', 'V'), out var latest)
            && Version.TryParse(Application.version, out var current)
            && latest > current;
    }
}
