/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-06
 */

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Falcon.Shared.Audio.Editor
{
    /// Nghe thử AudioClip ngay trong Editor, không cần vào Play mode.
    // ponytail: UnityEditor.AudioUtil là API internal, gọi qua reflection. Unity đổi tên method
    // thì nút Play im lặng kèm warning chứ không ném lỗi. Chưa có API public thay thế.
    public static class AudioPreview
    {
        private static MethodInfo _playMethod;
        private static MethodInfo _stopMethod;
        private static bool _resolved;
        private static AudioClip _playing;

        /// Đang phát clip này thì dừng, không thì phát nó.
        public static void Toggle(AudioClip clip)
        {
            if (!clip) return;

            Resolve();

            if (_playing == clip)
            {
                Stop();
                return;
            }

            Stop();
            if (_playMethod == null)
            {
                Debug.LogWarning("[AudioPreview] Không tìm thấy UnityEditor.AudioUtil.PlayPreviewClip.");
                return;
            }

            _playMethod.Invoke(null, new object[] { clip, 0, false });
            _playing = clip;
        }

        public static void Stop()
        {
            if (!_playing) return;
            _stopMethod?.Invoke(null, null);
            _playing = null;
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            var type = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (type == null) return;

            _playMethod = type.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            _stopMethod = type.GetMethod("StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);
        }
    }
}
