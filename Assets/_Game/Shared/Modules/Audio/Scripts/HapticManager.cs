// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-06-11

using UnityEngine;
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
using CandyCoded.HapticFeedback;
#endif

namespace Falcon.Shared.Audio
{
    public static class HapticManager
    {
        private static bool _isOn;

        public static void SetOnOff(bool isOn)
        {
            _isOn = isOn;
        }

        // Máy có motor rung hay không. iPad không có motor, iPhone thì luôn có.
        public static bool HasVibrator
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return GetVibrator() != null;
#elif UNITY_IOS && !UNITY_EDITOR
                return SystemInfo.deviceModel.StartsWith("iPhone");
#else
                return false;
#endif
            }
        }

        public static void LightFeedback()
        {
            if (!_isOn) return;
#if UNITY_IOS && !UNITY_EDITOR
            HapticFeedback.LightFeedback();
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (CandyCodedUsable) HapticFeedback.LightFeedback();
            else AndroidVibrate(LightMs, 60);
#endif
        }

        public static void MediumFeedback()
        {
            if (!_isOn) return;
#if UNITY_IOS && !UNITY_EDITOR
            HapticFeedback.MediumFeedback();
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (CandyCodedUsable) HapticFeedback.MediumFeedback();
            else AndroidVibrate(MediumMs, 140);
#endif
        }

        public static void HeavyFeedback()
        {
            if (!_isOn) return;
#if UNITY_IOS && !UNITY_EDITOR
            HapticFeedback.HeavyFeedback();
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (CandyCodedUsable) HapticFeedback.HeavyFeedback();
            else AndroidVibrate(HeavyMs, 255);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Motor ERM bỏ qua amplitude nên chỉ còn thời lượng phân biệt 3 mức. Dưới ~25ms motor chưa quay lên
        // kịp, sờ không thấy gì. Chỉnh 3 số này nếu tay cảm thấy nhẹ/nặng quá.
        private const long LightMs = 40, MediumMs = 60, HeavyMs = 80;

        private static bool? _candyCodedUsable;
        private static AndroidJavaObject _vibrator;
        private static int _sdkInt = -1;
        private static bool _initFailed;

        // CandyCoded chạy qua View.performHapticFeedback: nhiều máy nuốt im (OEM tắt rung chạm, hoặc
        // haptic_feedback_intensity = 0), và AAR vứt luôn giá trị bool báo thành công. Nên tự probe 1 lần
        // bằng chính API đó để lấy return value, hỏng thì fallback gọi thẳng Vibrator.
        // ponytail: cache 1 lần/session, user đổi setting giữa chừng thì phải restart app mới nhận.
        private static bool CandyCodedUsable
        {
            get
            {
                _candyCodedUsable ??= ProbeHapticFeedback();
                return _candyCodedUsable.Value;
            }
        }

        private static bool ProbeHapticFeedback()
        {
            try
            {
                var activity = GetActivity();
                if (activity == null) return false;

                using var settings = new AndroidJavaClass("android.provider.Settings$System");
                using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");
                var intensity = settings.CallStatic<int>("getInt", resolver, "haptic_feedback_intensity", -1);
                var enabled = settings.CallStatic<int>("getInt", resolver, "haptic_feedback_enabled", 1);

                // Không dùng return value của performHapticFeedback: nó chỉ báo "đã gửi request", máy motor
                // ERM vẫn trả true rồi im. hasAmplitudeControl mới phân biệt được LRA (dựng được tick/click)
                // với ERM (chỉ rung on/off) → chỉ máy LRA mới tin CandyCoded.
                var usable = HasAmplitudeControl && enabled != 0 && intensity != 0;
                Debug.Log($"[Haptic] enabled={enabled} intensity={intensity} " +
                          $"hasVibrator={HasVibrator} amplitudeControl={HasAmplitudeControl} → candy={usable}");
                return usable;
            }
            catch (System.Exception e)
            {
                Debug.Log($"[Haptic] probe failed: {e.Message}");
                return false;
            }
        }

        // Motor không chỉnh được cường độ (ERM) thì amplitude bị bỏ qua, 3 mức chỉ khác nhau ở thời lượng.
        private static bool HasAmplitudeControl
        {
            get
            {
                var vib = GetVibrator();
                if (vib == null || _sdkInt < 26) return false;
                try { return vib.Call<bool>("hasAmplitudeControl"); }
                catch { return false; }
            }
        }

        // ms = thời gian rung, amplitude 1..255 = cường độ (chỉ dùng được từ API 26).
        private static void AndroidVibrate(long ms, int amplitude)
        {
            var vib = GetVibrator();
            if (vib == null) return;
            try
            {
                if (_sdkInt >= 26)
                {
                    using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
                    vib.Call("vibrate", effect);
                }
                else
                {
                    vib.Call("vibrate", ms);
                }
            }
            catch { /* máy không có motor rung / không hỗ trợ */ }
        }

        private static AndroidJavaObject GetActivity()
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return player.GetStatic<AndroidJavaObject>("currentActivity");
        }

        private static AndroidJavaObject GetVibrator()
        {
            if (_vibrator != null) return _vibrator;
            if (_initFailed) return null;
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                _sdkInt = version.GetStatic<int>("SDK_INT");

                var activity = GetActivity();

                // API 31+: lấy Vibrator qua VibratorManager (getSystemService("vibrator") đã deprecated).
                if (_sdkInt >= 31)
                {
                    using var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                    _vibrator = manager.Call<AndroidJavaObject>("getDefaultVibrator");
                }
                else
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                if (_vibrator == null || !_vibrator.Call<bool>("hasVibrator"))
                {
                    _vibrator = null;
                    _initFailed = true;
                    return null;
                }
                return _vibrator;
            }
            catch
            {
                _initFailed = true;
                return null;
            }
        }
#endif
    }
}
