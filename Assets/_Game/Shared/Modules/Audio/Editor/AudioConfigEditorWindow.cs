/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-06
 */

using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Falcon.Shared.Audio.Editor
{
    /// Quản lý audio key: đưa clip vào Addressable group Audio với address = key.
    public class AudioConfigEditorWindow : OdinEditorWindow
    {
        [MenuItem("Tools/Audio Config Editor")]
        private static void Open() => GetWindow<AudioConfigEditorWindow>("Audio Config").Show();

        [LabelText("Kéo AudioClip vào đây")]
        [OnValueChanged(nameof(AbsorbDropped), IncludeChildren = true)]
        public List<AudioClip> dropZone = new();

        [InfoBox("0 clip = key đặt trước (vào enum, không vào group). " +
                 "1 clip = address trùng key. Nhiều clip = biến thể, address thành key_0, key_1... " +
                 "và PlaySFX bốc ngẫu nhiên một cái.")]
        [TableList(AlwaysExpanded = true, DrawScrollView = true)]
        public List<Row> rows = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        protected override void OnDisable()
        {
            AudioPreview.Stop();
            base.OnDisable();
        }

        [Button(ButtonSizes.Medium), HorizontalGroup("Actions")]
        private void Refresh()
        {
            rows = AudioConfigTools.Read();
            dropZone.Clear();
        }

        [Button(ButtonSizes.Medium), HorizontalGroup("Actions")]
        private void Apply()
        {
            var errors = AudioConfigTools.Validate(rows);
            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog("Apply thất bại", string.Join("\n", errors), "OK");
                return;
            }

            AudioConfigTools.Apply(rows);

            var clips = rows.Sum(r => r.Clips.Count(c => c));
            var variants = rows.Count(r => r.Clips.Count(c => c) > 1);
            var pending = rows.Count(r => r.Clips.All(c => !c));
            Refresh();

            EditorUtility.DisplayDialog("Apply",
                $"{rows.Count} key, {clips} clip vào group Audio." +
                $"\nKey nhiều biến thể: {variants}.\nKey đặt trước: {pending}.", "OK");
        }

        [Button(ButtonSizes.Medium), HorizontalGroup("Actions")]
        private void GenEnum()
        {
            var errors = AudioConfigTools.Validate(rows);
            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog("Gen Enum thất bại", string.Join("\n", errors), "OK");
                return;
            }

            // Đọc lại từ nguồn đã lưu, không từ bảng: enum chỉ chứa cái đã Apply.
            var keys = AudioConfigTools.Read().Select(r => (r.Key, Count: r.Clips.Count(c => c))).ToList();
            AudioConfigTools.GenerateEnum(keys);

            EditorUtility.DisplayDialog("Gen Enum",
                $"Đã sinh {keys.Count} member vào\n{AudioConfigTools.EnumPath}", "OK");
        }

        // Mỗi clip kéo vào thành một key riêng. Muốn ghép biến thể thì kéo clip vào cột Clips của hàng có sẵn.
        private void AbsorbDropped()
        {
            foreach (var clip in dropZone.Where(c => c))
            {
                if (rows.Any(r => r.Clips.Contains(clip))) continue;
                rows.Add(new Row { Key = AudioConfigTools.ToPascal(clip.name), Clips = { clip } });
            }

            dropZone.Clear();
        }
    }
}
