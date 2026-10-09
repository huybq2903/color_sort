using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Falcon.Shared.BaseLevelEditor
{
    public enum ClaudeStatus { Checking, NotInstalled, NotLoggedIn, Expired, Ready }

    /// <summary>Tìm và chạy Claude Code CLI trên máy: kiểm tra đã cài, đã đăng nhập chưa, mở đăng nhập, dựng lệnh chạy một lượt.</summary>
    public static class ClaudeCli
    {
        private const string PathPref = "Agent.ClaudePath";

        /// <summary>Đường dẫn claude: đường dẫn người dùng chọn tay, rồi PATH, rồi các chỗ cài thường gặp; null nếu không có (gọi ở luồng chính).</summary>
        public static string Find()
        {
            var saved = PlayerPrefs.GetString(PathPref, "");
            if (!string.IsNullOrEmpty(saved) && File.Exists(saved)) return saved;
            var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
            dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin"));
            dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm"));
            foreach (var dir in dirs)
            foreach (var name in new[] { "claude.exe", "claude.cmd" })
            {
                try
                {
                    var p = Path.Combine(dir.Trim('"'), name);
                    if (File.Exists(p)) return p;
                }
                catch (ArgumentException) { } // mục PATH sai định dạng
            }
            return null;
        }

        public static void SetCustomPath(string path) => PlayerPrefs.SetString(PathPref, path ?? "");

        /// <summary>Model mặc định cho trợ lý: nhanh, đủ cho việc đọc tóm tắt level và đề xuất; đổi bằng PlayerPrefs "Agent.Model".</summary>
        public const string DefaultModel = "sonnet";

        private const string TokenVar = "CLAUDE_CODE_OAUTH_TOKEN";

        // Token dài hạn (claude setup-token): lấy từ môi trường tiến trình, rồi biến môi trường người dùng và máy (Unity mở trước khi đặt biến thì không thấy)
        private static string Token() =>
            new[] { EnvironmentVariableTarget.Process, EnvironmentVariableTarget.User, EnvironmentVariableTarget.Machine }
                .Select(t => Environment.GetEnvironmentVariable(TokenVar, t)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        /// <summary>Dựng lệnh chạy ẩn có chuyển hướng đầu vào ra; claude.cmd (cài bằng npm) phải qua cmd.exe.</summary>
        public static ProcessStartInfo Build(string exe, IEnumerable<string> args, string workDir)
        {
            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = workDir,
            };
            var token = Token();
            if (token != null) psi.Environment[TokenVar] = token; // tiến trình con luôn thấy token dù Unity không có biến này
            if (exe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || exe.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            {
                psi.FileName = "cmd.exe";
                psi.ArgumentList.Add("/c");
                psi.ArgumentList.Add(exe);
            }
            else psi.FileName = exe;
            foreach (var a in args) psi.ArgumentList.Add(a);
            return psi;
        }

        /// <summary>Hỏi `claude auth status` (chạy nền, tối đa 20 giây).</summary>
        public static Task<ClaudeStatus> CheckAsync(string exe) => Task.Run(() =>
        {
            if (exe == null) return ClaudeStatus.NotInstalled;
            try
            {
                var dir = Path.GetTempPath();
                using var p = Process.Start(Build(exe, new[] { "auth", "status" }, dir));
                p.StandardInput.Close();
                var outTask = p.StandardOutput.ReadToEndAsync();
                if (!p.WaitForExit(20000)) { Kill(p); return ClaudeStatus.NotLoggedIn; }
                var text = outTask.Result;
                var start = text.IndexOf('{');
                if (start < 0) return ClaudeStatus.NotLoggedIn;
                var json = JObject.Parse(text.Substring(start));
                if (json.Value<bool?>("loggedIn") == true) return ClaudeStatus.Ready;
                if (Token() != null && ProbeWorks(exe)) return ClaudeStatus.Ready; // đăng nhập bằng token dài hạn: auth status không báo, phải thử gọi thật
                return IsExpired(exe) ? ClaudeStatus.Expired : ClaudeStatus.NotLoggedIn;
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or Newtonsoft.Json.JsonException)
            {
                return ClaudeStatus.NotInstalled;
            }
        });

        // Gọi thử một lượt rất ngắn không công cụ: xác nhận token chạy được (tốn một request nhỏ)
        private static bool ProbeWorks(string exe)
        {
            try
            {
                using var p = Process.Start(Build(exe, new[] { "-p", "--tools", "", "--strict-mcp-config", "--disable-slash-commands", "--no-session-persistence", "--setting-sources", "project", "--model", DefaultModel, "--output-format", "json" }, Path.GetTempPath()));
                p.StandardInput.Write("ok");
                p.StandardInput.Close();
                var outTask = p.StandardOutput.ReadToEndAsync();
                if (!p.WaitForExit(45000)) { Kill(p); return false; }
                var text = outTask.Result;
                var start = text.IndexOf('{');
                return p.ExitCode == 0 && start >= 0 && JObject.Parse(text.Substring(start)).Value<bool?>("is_error") != true;
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or Newtonsoft.Json.JsonException) { return false; }
        }

        // Đã có đăng nhập nhưng hết hạn: `claude auth status --text` ghi "Login: Expired"
        private static bool IsExpired(string exe)
        {
            try
            {
                using var p = Process.Start(Build(exe, new[] { "auth", "status", "--text" }, Path.GetTempPath()));
                p.StandardInput.Close();
                var outTask = p.StandardOutput.ReadToEndAsync();
                if (!p.WaitForExit(20000)) { Kill(p); return false; }
                return outTask.Result.IndexOf("Expired", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { return false; }
        }

        /// <summary>Mở `claude auth login` trong cửa sổ riêng: trình duyệt mở để xác nhận, cửa sổ này cho dán mã nếu cần.</summary>
        public static void StartLogin(string exe)
        {
            try { Process.Start(new ProcessStartInfo(exe, "auth login") { UseShellExecute = true }); }
            catch (System.ComponentModel.Win32Exception e) { UnityEngine.Debug.LogWarning("Không mở được claude auth login: " + e.Message); }
        }

        /// <summary>Tắt tiến trình và các tiến trình con của nó.</summary>
        public static void Kill(Process p)
        {
            try
            {
                if (p.HasExited) return;
                using var k = Process.Start(new ProcessStartInfo("taskkill", $"/PID {p.Id} /T /F") { CreateNoWindow = true, UseShellExecute = false });
                k?.WaitForExit(5000);
            }
            catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
        }
    }
}
