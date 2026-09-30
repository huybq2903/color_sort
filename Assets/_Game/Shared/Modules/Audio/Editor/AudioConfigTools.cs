/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-06
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Falcon.Shared.Audio.Editor
{
    /// Một key trong bảng. 0 clip = key đặt trước, 1 clip = thường, nhiều clip = biến thể.
    [Serializable]
    public class Row
    {
        public string Key;

        [ListDrawerSettings(DefaultExpandedState = true)]
        public List<AudioClip> Clips = new();

        [Button("▶", ButtonSizes.Small), TableColumnWidth(30, false)]
        private void Play()
        {
            var usable = Clips.Where(c => c).ToList();
            if (usable.Count == 0) return;
            AudioPreview.Toggle(usable[UnityEngine.Random.Range(0, usable.Count)]);
        }
    }

    public static class AudioConfigTools
    {
        public const string GroupName = "Audio";
        public const string EnumPath = "Assets/_Game/Shared/Modules/Audio/Scripts/SoundEnum.cs";
        public const string KeyCountsPath = "Assets/_Game/Shared/Modules/Audio/Editor/AudioKeys.txt";

        private static readonly Regex IdentifierPattern = new(@"^[A-Za-z_][A-Za-z0-9_]*$");

        private static readonly HashSet<string> CSharpKeywords = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while"
        };

        /// Address của clip thứ i: 1 clip thì trùng key, nhiều clip thì key_0, key_1...
        public static string AddressOf(string key, int index, int total)
            => total > 1 ? $"{key}_{index}" : key;

        /// Có "_" thì tách rồi PascalCase từng phần, không có thì giữ nguyên.
        public static string ToPascal(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;
            if (!fileName.Contains("_")) return fileName;

            var sb = new StringBuilder();
            foreach (var part in fileName.Split('_'))
            {
                if (part.Length == 0) continue;
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1) sb.Append(part.Substring(1).ToLowerInvariant());
            }

            return sb.ToString();
        }

        /// Trả danh sách lỗi, rỗng nghĩa là hợp lệ.
        public static List<string> Validate(List<Row> rows)
        {
            var errors = new List<string>();

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var line = i + 1;

                // Clips rỗng là hợp lệ: đó là key đặt trước, chưa có file.
                if (string.IsNullOrWhiteSpace(row.Key))
                {
                    errors.Add($"Dòng {line}: Key rỗng.");
                    continue;
                }

                if (!IdentifierPattern.IsMatch(row.Key))
                    errors.Add($"Dòng {line}: Key '{row.Key}' không phải identifier C# hợp lệ.");
                else if (CSharpKeywords.Contains(row.Key))
                    errors.Add($"Dòng {line}: Key '{row.Key}' trùng từ khoá C#.");

                if (row.Clips.Any(c => !c))
                    errors.Add($"Dòng {line} ({row.Key}): có ô clip để trống trong danh sách, xoá ô đó đi.");
            }

            var duplicateKeys = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Key))
                .GroupBy(r => r.Key, StringComparer.Ordinal)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicateKeys)
                errors.Add($"Key '{group.Key}' bị trùng ở {group.Count()} dòng.");

            // Một asset chỉ giữ được một entry Addressables, nên không thể nằm ở 2 key.
            var duplicateClips = rows
                .SelectMany(r => r.Clips.Where(c => c).Select(c => (clip: c, r.Key)))
                .GroupBy(x => x.clip)
                .Where(g => g.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count() > 1);

            foreach (var group in duplicateClips)
                errors.Add($"Clip '{group.Key.name}' dùng ở nhiều key: {string.Join(", ", group.Select(x => x.Key).Distinct())}.");

            return errors;
        }

        /// Đọc group Audio + file đếm thành bảng, sắp theo Key.
        public static List<Row> Read()
        {
            var byAddress = new Dictionary<string, AudioClip>(StringComparer.Ordinal);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings)
            {
                var group = settings.FindGroup(GroupName);
                if (group)
                {
                    foreach (var entry in group.entries)
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.AssetPath);
                        if (clip) byAddress[entry.address] = clip;
                    }
                }
            }

            var rows = new List<Row>();

            // Key nhiều clip / key đặt trước phải đọc từ file: không suy được từ tên address,
            // vì bgm_chapter_1..4 là 4 key riêng chứ không phải 4 biến thể của bgm_chapter.
            foreach (var (key, count) in ReadKeyCounts())
            {
                var row = new Row { Key = key };
                for (var i = 0; i < count; i++)
                {
                    var address = AddressOf(key, i, count);
                    if (!byAddress.TryGetValue(address, out var clip)) continue;
                    row.Clips.Add(clip);
                    byAddress.Remove(address);
                }

                rows.Add(row);
            }

            // Còn lại là key một clip, address chính là key.
            foreach (var pair in byAddress)
                rows.Add(new Row { Key = pair.Key, Clips = { pair.Value } });

            return rows.OrderBy(r => r.Key, StringComparer.Ordinal).ToList();
        }

        /// Ghi clip vào group với address theo key + hậu tố, ghi số clip của key lệch 1 ra file.
        public static void Apply(List<Row> rows)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (!settings)
                throw new InvalidOperationException("Không tìm thấy AddressableAssetSettings.");

            var group = settings.FindGroup(GroupName);
            if (!group)
                throw new InvalidOperationException($"Không tìm thấy Addressable group '{GroupName}'.");

            var keepGuids = new HashSet<string>();

            foreach (var row in rows)
            {
                var clips = row.Clips.Where(c => c).ToList();
                for (var i = 0; i < clips.Count; i++)
                {
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clips[i]));
                    if (string.IsNullOrEmpty(guid)) continue;

                    var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                    entry.SetAddress(AddressOf(row.Key, i, clips.Count));
                    keepGuids.Add(guid);
                }
            }

            // Gom trước rồi mới xoá, không sửa collection đang duyệt.
            var stale = group.entries.Where(e => !keepGuids.Contains(e.guid)).Select(e => e.guid).ToList();
            foreach (var guid in stale)
                settings.RemoveAssetEntry(guid);

            WriteKeyCounts(rows.Select(r => (r.Key, r.Clips.Count(c => c))));

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
        }

        /// Chỉ ghi key có số clip khác 1; key một clip suy được từ group nên không cần lưu.
        public static List<(string Key, int Count)> ReadKeyCounts()
        {
            var result = new List<(string, int)>();
            if (!File.Exists(KeyCountsPath)) return result;

            foreach (var line in File.ReadAllLines(KeyCountsPath))
            {
                var parts = line.Split('=');
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                if (key.Length == 0 || !int.TryParse(parts[1].Trim(), out var count)) continue;
                result.Add((key, Mathf.Max(0, count)));
            }

            return result;
        }

        public static void WriteKeyCounts(IEnumerable<(string Key, int Count)> entries)
        {
            var lines = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Key) && e.Count != 1)
                .GroupBy(e => e.Key.Trim(), StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => $"{g.Key}={g.First().Count}");

            File.WriteAllLines(KeyCountsPath, lines);
            AssetDatabase.ImportAsset(KeyCountsPath);
        }

        /// Sinh SoundEnum.cs + bảng đếm biến thể, sắp alphabet để (int)key ổn định.
        public static void GenerateEnum(IEnumerable<(string Key, int Count)> entries)
        {
            var ordered = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Key))
                .GroupBy(e => e.Key, StringComparer.Ordinal)
                .Select(g => (Key: g.Key, Count: g.First().Count))
                .OrderBy(e => e.Key, StringComparer.Ordinal)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated> Sinh bởi Audio Config Editor. Đừng sửa tay.");
            sb.AppendLine("// Đừng gán giá trị cho member: AudioManager tra tên bằng (int)key.");
            sb.AppendLine();
            sb.AppendLine("namespace Falcon.Shared.Audio");
            sb.AppendLine("{");
            sb.AppendLine("    public enum SoundEnum");
            sb.AppendLine("    {");
            foreach (var entry in ordered)
                sb.AppendLine($"        {entry.Key},");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /// Số clip của mỗi key, index theo (int)SoundEnum. >1 nghĩa là address có hậu tố _0.._n.");
            sb.AppendLine("    public static class SoundEnumVariants");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static readonly int[] Counts = {{ {string.Join(", ", ordered.Select(e => e.Count))} }};");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(EnumPath, sb.ToString());
            AssetDatabase.ImportAsset(EnumPath);
        }

        private static int ExpectErrorCount(string label, List<Row> rows, int expected)
        {
            var actual = Validate(rows).Count;
            if (actual == expected) return 0;
            Debug.LogError($"Validate({label}) trả {actual} lỗi, mong đợi {expected}");
            return 1;
        }
    }
}
