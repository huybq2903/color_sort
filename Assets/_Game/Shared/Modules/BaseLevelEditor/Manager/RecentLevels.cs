using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Falcon.Shared.BaseLevelEditor
{
    /// <summary>Danh sách level mở gần đây (JSON + thumbnail PNG), lưu cố định ở LocalAppData, không phụ thuộc tên app.</summary>
    public static class RecentLevels
    {
        [Serializable]
        public class Entry
        {
            public string path;
            public long opened; // UTC ticks lần mở/lưu gần nhất
            public string thumb; // tên file png trong thư mục thumbs, rỗng = chưa có preview
        }

        [Serializable]
        private class Store
        {
            public List<Entry> items = new();
        }

        public const int Max = 10;

        /// <summary>Tăng mỗi khi danh sách hoặc thumbnail đổi, UI dùng để làm mới cache texture.</summary>
        public static int Version { get; private set; }

        private static Store _store;

        private static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FalconLevelEditor");
        private static string ListFile => Path.Combine(Root, "recent.json");
        private static string ThumbDir => Path.Combine(Root, "thumbs");

        public static IReadOnlyList<Entry> Items
        {
            get
            {
                Load();
                return _store.items;
            }
        }

        public static string ThumbPath(Entry e) => string.IsNullOrEmpty(e.thumb) ? null : Path.Combine(ThumbDir, e.thumb);

        public static bool Exists(Entry e) => File.Exists(e.path);

        /// <summary>Đưa level lên đầu danh sách; có thumb thì ghi đè preview.</summary>
        public static void Touch(string path, Texture2D thumb = null)
        {
            if (string.IsNullOrEmpty(path)) return;
            Load();
            path = Path.GetFullPath(path);
            var e = _store.items.Find(i => SamePath(i.path, path)) ?? new Entry { path = path };
            _store.items.Remove(e);
            e.opened = DateTime.UtcNow.Ticks;
            if (thumb) WriteThumb(e, thumb);
            _store.items.Insert(0, e);
            while (_store.items.Count > Max)
            {
                DeleteThumb(_store.items[^1]);
                _store.items.RemoveAt(_store.items.Count - 1);
            }
            Save();
        }

        public static void Remove(Entry e)
        {
            Load();
            if (!_store.items.Remove(e)) return;
            DeleteThumb(e);
            Save();
        }

        public static void Clear()
        {
            Load();
            foreach (var e in _store.items) DeleteThumb(e);
            _store.items.Clear();
            Save();
        }

        /// <summary>Đổi tên file thật (giữ đuôi); trả false kèm lý do nếu tên sai hoặc đã tồn tại.</summary>
        public static bool Rename(Entry e, string newName, out string error)
        {
            error = null;
            newName = newName?.Trim();
            if (string.IsNullOrEmpty(newName) || newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = "Tên không hợp lệ";
                return false;
            }

            var target = Path.Combine(Path.GetDirectoryName(e.path) ?? string.Empty, newName + Path.GetExtension(e.path));
            if (SamePath(target, e.path)) return true;
            if (File.Exists(target))
            {
                error = $"Đã có file {Path.GetFileName(target)}";
                return false;
            }

            try
            {
                File.Move(e.path, target);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            _store.items.RemoveAll(i => i != e && SamePath(i.path, target)); // bỏ mục cũ trùng đường dẫn mới
            e.path = target;
            Save();
            return true;
        }

        /// <summary>Chuyển file vào Thùng rác (Windows x64) hoặc xoá thẳng ở nền tảng khác, rồi bỏ khỏi danh sách.</summary>
        public static bool DeleteFile(Entry e, out string error)
        {
            error = null;
            try
            {
                if (File.Exists(e.path)) MoveToRecycleBin(e.path);
                if (File.Exists(e.path)) throw new IOException("Không xoá được file");
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            Remove(e);
            return true;
        }

        public static string TimeAgo(long ticks)
        {
            var span = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 2) return "Hôm qua";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
            return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy");
        }

        private static bool SamePath(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static void Load()
        {
            if (_store != null) return;
            _store = new Store();
            try
            {
                if (File.Exists(ListFile)) _store = JsonUtility.FromJson<Store>(File.ReadAllText(ListFile)) ?? new Store();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"RecentLevels: không đọc được danh sách ({ex.Message})");
            }
            _store.items ??= new List<Entry>();
        }

        private static void Save()
        {
            Version++;
            try
            {
                Directory.CreateDirectory(Root);
                File.WriteAllText(ListFile, JsonUtility.ToJson(_store));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"RecentLevels: không lưu được danh sách ({ex.Message})");
            }
        }

        private static void WriteThumb(Entry e, Texture2D thumb)
        {
            try
            {
                e.thumb ??= Guid.NewGuid().ToString("N") + ".png";
                Directory.CreateDirectory(ThumbDir);
                File.WriteAllBytes(ThumbPath(e), thumb.EncodeToPNG());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"RecentLevels: không lưu được thumbnail ({ex.Message})");
            }
        }

        private static void DeleteThumb(Entry e)
        {
            try
            {
                var p = ThumbPath(e);
                if (p != null && File.Exists(p)) File.Delete(p);
            }
            catch
            {
                // thumbnail mồ côi không ảnh hưởng gì
            }
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ShFileOp
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public int fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref ShFileOp op);
#endif

        private static void MoveToRecycleBin(string path)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (IntPtr.Size == 8)
            {
                // FO_DELETE + FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI
                var op = new ShFileOp { wFunc = 3, pFrom = path + '\0', fFlags = 0x40 | 0x10 | 0x4 | 0x400 };
                if (SHFileOperation(ref op) != 0) throw new IOException("Không chuyển được file vào Thùng rác");
                return;
            }
#endif
            File.Delete(path);
        }
    }
}
