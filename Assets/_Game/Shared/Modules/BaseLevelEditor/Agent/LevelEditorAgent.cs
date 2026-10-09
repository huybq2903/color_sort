using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Imui.Controls;
using Imui.Core;
using Imui.IO.Touch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFB;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falcon.Shared.BaseLevelEditor
{
    /// <summary>Trợ lý trong editor: cửa sổ nổi chat với Claude Code đã đăng nhập trên máy; mỗi hội thoại là một phiên claude -p riêng, chạy nền.</summary>
    public partial class LevelEditorAgent : MonoBehaviour, IEditorManager
    {
        private const float MinW = 420f, MinH = 380f, GripSize = 18f;
        private const string InstallCommand = "npm install -g @anthropic-ai/claude-code";
        private const string GuideUrl = "https://claude.com/product/claude-code";
        private const string ProposalFormat =
            "Khi có thay đổi nên làm trên level, cuối câu trả lời thêm đúng một khối mã bắt đầu bằng ```proposals chứa một mảng JSON (kết thúc bằng ```). " +
            "Mỗi phần tử có: title (ngắn), detail (một câu, nhắc mảnh theo số thứ tự #n như bản tóm tắt), kind và các trường theo kind. Không có đề xuất thì không viết khối này.";
        private static readonly Regex ProposalBlock = new(@"```proposals\s*(.*?)```", RegexOptions.Singleline);
        private static readonly Color32 Good = new(134, 239, 172, 255);
        private static readonly Color32 Dim = new(148, 163, 184, 255), Soft = new(226, 232, 240, 255), Mine = new(125, 211, 252, 255);

        public IAgentHost Host { get; set; }

        private bool _open;
        private ClaudeStatus _status = ClaudeStatus.Checking;
        private string _exe;
        private Task<ClaudeStatus> _check;
        private List<AgentChat> _chats = new();
        private int _cur;
        private string _input = "";
        private bool _stick; // vừa có chữ mới: cuộn xuống cuối
        private float _w, _h; // kích thước cửa sổ (0 = chưa đặt, lấy theo màn hình): góc phải dưới cố định, đổi bằng góc trái trên
        private bool _resizing;
        private readonly ConcurrentQueue<Action> _main = new();

        private AgentChat Current => _chats[Mathf.Clamp(_cur, 0, _chats.Count - 1)];

        public void Initialized()
        {
            _w = PlayerPrefs.GetFloat("Agent.W", 0f);
            _h = PlayerPrefs.GetFloat("Agent.H", 0f);
            _chats = AgentChats.Load();
            _chats.Sort((a, b) => b.lastTicks.CompareTo(a.lastTicks));
            if (_chats.Count == 0) _chats.Add(new AgentChat());
        }

        /// <summary>Mở hoặc đóng cửa sổ; mỗi lần mở kiểm tra lại cài đặt và đăng nhập.</summary>
        public void Toggle()
        {
            _open = !_open;
            if (_open) Recheck();
        }

        private void Recheck()
        {
            _exe = ClaudeCli.Find();
            _status = ClaudeStatus.Checking;
            _check = ClaudeCli.CheckAsync(_exe);
        }

        private void OnDestroy()
        {
            foreach (var c in _chats)
                if (c.running && c.process != null) ClaudeCli.Kill(c.process);
        }

        private void Update()
        {
            while (_main.TryDequeue(out var action)) action();
            if (_check != null && _check.IsCompleted)
            {
                _status = _check.IsFaulted ? ClaudeStatus.NotLoggedIn : _check.Result;
                _check = null;
            }
        }

        // ---- cửa sổ ----

        public void DrawWindow(ImGui gui, float rightW)
        {
            if (!_open) return;
            var screen = gui.Canvas.ScreenSize;
            if (_w < MinW) _w = Mathf.Max(MinW, screen.x * 0.24f); // lần đầu hoặc giá trị lưu quá nhỏ
            if (_h < MinH) _h = Mathf.Max(MinH, screen.y * 0.62f);
            var right = screen.x - rightW - 24f; // góc phải dưới nằm cố định ở góc dưới phải vùng tranh
            var bottom = 10f + gui.GetRowHeight() + 14f;
            HandleResize(gui, screen, right, bottom);
            var rect = new ImRect(right - _w, bottom, _w, _h);
            var oldWin = gui.Style.Window;
            var oldEdit = gui.Style.TextEdit;
            PushStyle(gui);
            try
            {
                if (!gui.BeginWindow("Trợ lý", ref _open, rect, ImWindowFlag.NoMovingAndResizing | ImWindowFlag.NoTitleBar)) return;
                try
                {
                    DrawGrip(gui, rect);
                    DrawHeader(gui, _status == ClaudeStatus.Ready);
                    switch (_status)
                    {
                        case ClaudeStatus.Checking: gui.Text("Đang kiểm tra Claude Code…", Dim); break;
                        case ClaudeStatus.NotInstalled: DrawNotInstalled(gui); break;
                        case ClaudeStatus.NotLoggedIn: DrawNotLoggedIn(gui, false); break;
                        case ClaudeStatus.Expired: DrawNotLoggedIn(gui, true); break;
                        default: DrawChat(gui); break;
                    }
                }
                finally
                {
                    gui.EndWindow(); // luôn đóng window để Imui không lệch stack Begin/End
                }
            }
            finally
            {
                gui.Style.Window = oldWin;
                gui.Style.TextEdit = oldEdit;
            }
        }

        // Kéo góc trái trên để đổi kích thước; đọc chuột thẳng từ Input System vì Imui không báo chuột ngoài window của nó
        private void HandleResize(ImGui gui, Vector2 screen, float right, float bottom)
        {
            var m = Mouse.current;
            if (m == null) return;
            var mp = m.position.ReadValue() * (screen.x / Mathf.Max(1f, Screen.width));
            var grip = new ImRect(right - _w - 2f, bottom + _h - GripSize + 2f, GripSize, GripSize);
            if (m.leftButton.wasPressedThisFrame && grip.Contains(mp)) _resizing = true;
            if (!_resizing) return;
            if (m.leftButton.isPressed)
            {
                var maxW = Mathf.Max(MinW, right - 20f);
                var maxH = Mathf.Max(MinH, screen.y - bottom - gui.GetRowHeight() * 3f - 20f);
                _w = Mathf.Clamp(right - mp.x, MinW, maxW);
                _h = Mathf.Clamp(mp.y - bottom, MinH, maxH);
            }
            else
            {
                _resizing = false;
                PlayerPrefs.SetFloat("Agent.W", _w);
                PlayerPrefs.SetFloat("Agent.H", _h);
            }
        }

        // Hai gạch nhỏ ở góc trái trên cửa sổ báo chỗ kéo
        private void DrawGrip(ImGui gui, ImRect rect)
        {
            var c = _resizing ? Soft : Dim;
            gui.Canvas.Rect(new ImRect(rect.X + 7f, rect.Y + rect.H - 8f, 10f, 2f), c);
            gui.Canvas.Rect(new ImRect(rect.X + 7f, rect.Y + rect.H - 18f, 2f, 10f), c);
        }

        private void DrawNotInstalled(ImGui gui)
        {
            gui.Text("Không tìm thấy Claude Code", Soft);
            gui.Text("Trợ lý cần Claude Code cài trên máy. Cài xong thì bấm Kiểm tra lại.", Dim, true);
            gui.Text(InstallCommand, new Color32(167, 243, 208, 255), true);
            if (gui.Button("Sao chép lệnh cài")) GUIUtility.systemCopyBuffer = InstallCommand;
            if (gui.Button("Kiểm tra lại")) Recheck();
            if (gui.Button("Mở trang hướng dẫn cài")) Application.OpenURL(GuideUrl);
            gui.Text("Đã cài nhưng ở chỗ khác?", Dim);
            if (gui.Button("Chọn đường dẫn claude…"))
            {
                var picked = StandaloneFileBrowser.OpenFilePanel("Chọn claude", "", new[] { new ExtensionFilter("claude", "exe", "cmd") }, false);
                if (picked is { Length: > 0 } && !string.IsNullOrEmpty(picked[0]))
                {
                    ClaudeCli.SetCustomPath(picked[0]);
                    Recheck();
                }
            }
        }

        private void DrawNotLoggedIn(ImGui gui, bool expired)
        {
            gui.Text(expired ? "Đăng nhập Claude Code đã hết hạn" : "Chưa đăng nhập Claude Code", Soft);
            gui.Text(expired
                ? "Claude Code trên máy này có tài khoản nhưng phiên đã hết hạn (khác với đăng nhập của app khác). Bấm Đăng nhập lại, trình duyệt sẽ mở để xác nhận."
                : "Trợ lý dùng Claude Code đã đăng nhập trên máy này. Bấm Đăng nhập, trình duyệt sẽ mở để xác nhận.", Dim, true);
            if (gui.Button(expired ? "Đăng nhập lại Claude" : "Đăng nhập Claude")) ClaudeCli.StartLogin(_exe);
            if (gui.Button("Kiểm tra lại")) Recheck();
        }

        // ---- hội thoại ----

        private void NewChat()
        {
            _chats.Insert(0, new AgentChat());
            _cur = 0;
            AgentChats.Save(_chats);
        }

        private void DeleteChat(AgentChat chat)
        {
            if (chat.running) Stop(chat);
            var wasCurrent = chat == Current;
            _chats.Remove(chat);
            if (_chats.Count == 0) _chats.Add(new AgentChat());
            if (wasCurrent) _cur = 0;
            else _cur = Mathf.Clamp(_cur, 0, _chats.Count - 1);
            AgentChats.Save(_chats);
        }

        private void Stop(AgentChat chat)
        {
            chat.stopRequested = true;
            if (chat.process != null) ClaudeCli.Kill(chat.process);
        }

        private void Send(string text)
        {
            var chat = Current;
            if (chat.running || _exe == null || string.IsNullOrWhiteSpace(text)) return;
            text = text.Trim();
            chat.messages.Add(new AgentMessage { role = "user", text = text });
            var reply = new AgentMessage { role = "assistant", text = "" };
            chat.messages.Add(reply);
            if (chat.messages.Count == 2) chat.name = text.Replace('\n', ' ').Substring(0, Mathf.Min(36, text.Length));
            chat.running = true;
            chat.stopRequested = false;
            chat.lastTicks = DateTime.UtcNow.Ticks;
            _stick = true;

            var prompt = Host != null ? $"{Host.BuildContext()}\n\n---\nCâu hỏi của người dùng:\n{text}" : text;
            var workDir = AgentChats.WorkDir; // phải lấy ở luồng chính
            var sysFile = Path.Combine(workDir, "system.txt");
            File.WriteAllText(sysFile, Host != null ? $"{Host.SystemPrompt}\n\n{ProposalFormat}\n{Host.ProposalGuide}" : "Trả lời ngắn gọn bằng tiếng Việt, gọi người dùng là \"bạn\".");
            var exe = _exe;
            var resume = chat.started;
            var model = PlayerPrefs.GetString("Agent.Model", ClaudeCli.DefaultModel);
            Task.Run(() => RunTurn(chat, reply, prompt, exe, sysFile, workDir, resume, model));
        }

        // Chạy một lượt claude -p ở luồng nền: gửi prompt qua stdin, đọc từng dòng stream-json, đẩy chữ về luồng chính
        private void RunTurn(AgentChat chat, AgentMessage reply, string prompt, string exe, string sysFile, string workDir, bool resume, string model)
        {
            string error = "", resultText = null;
            var isError = false;
            var stalled = false;
            var code = -1;
            try
            {
                var args = new List<string>
                {
                    "-p", "--output-format", "stream-json", "--verbose", "--include-partial-messages",
                    "--tools", "", "--strict-mcp-config", "--disable-slash-commands",
                    "--setting-sources", "project", "--model", model, // bỏ hook, plugin, cấu hình cá nhân và model nặng: bớt vài giây khởi động
                    "--system-prompt-file", sysFile,
                    resume ? "--resume" : "--session-id", chat.id,
                };
                var log = Path.Combine(workDir, "last-run.log");
                var t0 = DateTime.UtcNow;
                void Log(string m) { try { File.AppendAllText(log, $"{(DateTime.UtcNow - t0).TotalSeconds:0.0}s {m}\n"); } catch (IOException) { } }
                File.WriteAllText(log, "");
                Log("start: " + string.Join(" ", args));
                using var p = System.Diagnostics.Process.Start(ClaudeCli.Build(exe, args, workDir));
                chat.process = p;
                var lastOut = DateTime.UtcNow;
                var watchdog = Task.Run(async () => // không có dòng nào trong 90 giây: coi như treo, tắt tiến trình
                {
                    while (!p.HasExited) { await Task.Delay(2000); if ((DateTime.UtcNow - lastOut).TotalSeconds > 90) { Log("stalled, killing"); stalled = true; ClaudeCli.Kill(p); break; } }
                });
                p.StandardInput.Write(prompt);
                p.StandardInput.Close();
                var errTask = p.StandardError.ReadToEndAsync();
                var gotDelta = false;
                string line;
                while ((line = p.StandardOutput.ReadLine()) != null)
                {
                    lastOut = DateTime.UtcNow;
                    Log(line.Length > 160 ? line.Substring(0, 160) : line);
                    JObject o;
                    try { o = JObject.Parse(line); }
                    catch (JsonException) { continue; }
                    switch (o.Value<string>("type"))
                    {
                        case "stream_event":
                            var d = o["event"]?["delta"];
                            if (d?.Value<string>("type") == "text_delta")
                            {
                                var t = d.Value<string>("text");
                                gotDelta = true;
                                _main.Enqueue(() => { reply.text += t; _stick = true; });
                            }
                            break;
                        case "assistant":
                            if (!gotDelta)
                            {
                                var whole = string.Concat(((o["message"]?["content"] as JArray) ?? new JArray()).Where(b => b.Value<string>("type") == "text").Select(b => b.Value<string>("text")));
                                if (whole.Length > 0) _main.Enqueue(() => { if (string.IsNullOrEmpty(reply.text)) reply.text = whole; _stick = true; });
                            }
                            break;
                        case "result":
                            resultText = o.Value<string>("result");
                            isError = o.Value<bool?>("is_error") == true;
                            break;
                    }
                }
                p.WaitForExit();
                code = p.ExitCode;
                error = errTask.Result;
                Log($"exit {code} stderr: {error}");
                if (stalled) error = "Claude không phản hồi sau 90 giây";
            }
            catch (Exception e)
            {
                error = e.Message;
                UnityEngine.Debug.LogException(e);
            }
            _main.Enqueue(() => Finish(chat, reply, code, isError, resultText, error));
        }

        private void Finish(AgentChat chat, AgentMessage reply, int code, bool isError, string resultText, string error)
        {
            chat.running = false;
            chat.process = null;
            chat.lastTicks = DateTime.UtcNow.Ticks;
            var ok = code == 0 && !isError;
            if (ok) chat.started = true;
            else if (!chat.started) chat.id = Guid.NewGuid().ToString(); // lượt đầu hỏng: phiên có thể đã tạo dở, lượt sau dùng mã mới
            if (string.IsNullOrEmpty(reply.text))
                reply.text = chat.stopRequested ? "(đã dừng)" : ok ? (resultText ?? "") : $"Lỗi: {(string.IsNullOrWhiteSpace(resultText) ? error.Trim() : resultText)}";
            if (ok) ExtractProposals(reply);
            if (ok && !chat.titled && !chat.renamed && chat.messages.Count <= 2) StartTitle(chat);
            _stick = true;
            AgentChats.Save(_chats);
        }

        // Haiku đặt tên ngắn cho hội thoại sau lượt đầu; lỗi thì giữ tên tạm
        private void StartTitle(AgentChat chat)
        {
            chat.titled = true;
            if (_exe == null) return;
            var answer = chat.messages[1].text ?? "";
            var prompt = $"Câu hỏi: {chat.messages[0].text}\nTrả lời: {(answer.Length > 300 ? answer.Substring(0, 300) : answer)}\n\nĐặt tên cho hội thoại này: tối đa 5 từ tiếng Việt, không dấu câu, chỉ in ra tên.";
            var exe = _exe;
            var workDir = AgentChats.WorkDir;
            Task.Run(() =>
            {
                string name = null;
                try
                {
                    var args = new[] { "-p", "--tools", "", "--strict-mcp-config", "--disable-slash-commands", "--no-session-persistence", "--setting-sources", "project", "--model", "haiku", "--system-prompt", "Chỉ trả lời đúng tên được yêu cầu, không giải thích." };
                    using var p = System.Diagnostics.Process.Start(ClaudeCli.Build(exe, args, workDir));
                    p.StandardInput.Write(prompt);
                    p.StandardInput.Close();
                    var outTask = p.StandardOutput.ReadToEndAsync();
                    if (!p.WaitForExit(30000)) { ClaudeCli.Kill(p); return; }
                    if (p.ExitCode == 0) name = CleanTitle(outTask.Result);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Đặt tên hội thoại lỗi: " + e.Message);
                }
                if (!string.IsNullOrEmpty(name)) _main.Enqueue(() => { if (!chat.renamed) { chat.name = name; AgentChats.Save(_chats); } });
            });
        }

        private static string CleanTitle(string raw)
        {
            var line = raw.Trim().Split('\n')[0].Trim().Trim('"', '\'', '“', '”', '*', '.', ' ');
            return line.Length > 40 ? line.Substring(0, 40).TrimEnd() : line;
        }

        // ---- đề xuất ----

        // Bỏ khối proposals (kể cả khi đang soạn dở) khỏi chữ hiển thị
        private static string Visible(string text)
        {
            var s = ProposalBlock.Replace(text, "");
            var i = s.IndexOf("```proposals", StringComparison.Ordinal);
            if (i >= 0) s = s.Substring(0, i).TrimEnd() + "\n(đang soạn đề xuất…)";
            return s.TrimEnd();
        }

        // Lấy mảng JSON trong khối proposals thành các đề xuất của tin nhắn; JSON hỏng thì chỉ giữ chữ
        private void ExtractProposals(AgentMessage reply)
        {
            var m = ProposalBlock.Match(reply.text ?? "");
            if (!m.Success) return;
            try
            {
                var list = new List<AgentProposal>();
                foreach (var o in JArray.Parse(m.Groups[1].Value).OfType<JObject>())
                    list.Add(new AgentProposal { title = o.Value<string>("title") ?? "Đề xuất", detail = o.Value<string>("detail") ?? "", kind = o.Value<string>("kind") ?? "", args = o.ToString(Formatting.None) });
                var bad = new List<string>();
                reply.proposals = Host == null ? list : list.Where(p => { var e = Host.Validate(p, out var note); p.note = note; if (e != null) bad.Add($"{p.title}: {e}"); return e == null; }).ToList();
                reply.text = ProposalBlock.Replace(reply.text, "").TrimEnd();
                if (bad.Count > 0) reply.text += $"\n\n(Đã bỏ {bad.Count} đề xuất không hợp lệ: {string.Join("; ", bad)})";
            }
            catch (JsonException) { }
        }

        private bool ApplyOne(AgentProposal p)
        {
            if (Host == null) return false;
            if (!Host.Apply(p, out var error))
            {
                LevelEditorMainUI.Warn(string.IsNullOrEmpty(error) ? "Không áp dụng được đề xuất" : error);
                return false;
            }
            p.state = 1;
            AgentChats.Save(_chats);
            return true;
        }

        // Áp mọi đề xuất còn chờ trong một nhóm lệnh: một lần Ctrl+Z hoàn tác tất cả
        private void ApplyAll(AgentMessage m)
        {
            var invoker = LevelEditorManager.Get<LevelEditorCommandInvoker>();
            invoker?.BeginGroup();
            try
            {
                foreach (var p in m.proposals.Where(x => x.state == 0).ToList()) ApplyOne(p);
            }
            finally
            {
                invoker?.EndGroup();
            }
        }
    }
}
