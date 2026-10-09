using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Falcon.Shared.BaseLevelEditor
{
    /// <summary>Một đề xuất của trợ lý; args là JSON gốc, state: 0 chờ, 1 đã áp dụng, 2 bỏ qua.</summary>
    public class AgentProposal
    {
        public string title, detail, kind, args, note; // note: hệ quả do tool tính (vd cát lệch)
        public int state;
    }

    public class AgentMessage
    {
        public string role; // "user" hoặc "assistant"
        public string text;
        public List<AgentProposal> proposals;
    }

    /// <summary>Một hội thoại = một phiên Claude Code (id là mã phiên); nội dung chat lưu kèm để mở lại xem.</summary>
    public class AgentChat
    {
        public string id = Guid.NewGuid().ToString();
        public string name = "Hội thoại mới";
        public long lastTicks = DateTime.UtcNow.Ticks;
        public bool titled, renamed; // titled: Haiku đã đặt tên; renamed: người dùng tự đặt, không ghi đè
        public bool started; // đã gửi ít nhất một lượt thành công: lượt sau dùng --resume
        public List<AgentMessage> messages = new();

        [JsonIgnore] public bool running, stopRequested;
        [JsonIgnore] public Process process;

        public string When()
        {
            var span = DateTime.UtcNow - new DateTime(lastTicks, DateTimeKind.Utc);
            if (span.TotalMinutes < 1) return "vừa xong";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} giờ trước";
            return $"{(int)span.TotalDays} ngày trước";
        }
    }

    /// <summary>Danh sách hội thoại dùng chung cho mọi level, lưu một file trong thư mục dữ liệu người dùng.</summary>
    public static class AgentChats
    {
        private static string Dir => Path.Combine(Application.persistentDataPath, "Agent");
        private static string FilePath => Path.Combine(Dir, "chats.json");

        public static string WorkDir
        {
            get
            {
                var d = Path.Combine(Dir, "work"); // chạy claude ở thư mục trống để không nạp CLAUDE.md hay cấu hình của dự án khác
                Directory.CreateDirectory(d);
                return d;
            }
        }

        public static List<AgentChat> Load()
        {
            try
            {
                if (File.Exists(FilePath)) return JsonConvert.DeserializeObject<List<AgentChat>>(File.ReadAllText(FilePath)) ?? new List<AgentChat>();
            }
            catch (Exception e) when (e is IOException or JsonException)
            {
                UnityEngine.Debug.LogWarning("Không đọc được danh sách hội thoại trợ lý: " + e.Message);
            }
            return new List<AgentChat>();
        }

        public static void Save(List<AgentChat> chats)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(chats, Formatting.Indented));
            }
            catch (IOException e)
            {
                UnityEngine.Debug.LogWarning("Không lưu được danh sách hội thoại trợ lý: " + e.Message);
            }
        }
    }
}
