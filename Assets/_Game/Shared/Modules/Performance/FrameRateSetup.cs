using UnityEngine;

namespace Falcon.Shared.Performance
{
    public static class FrameRateSetup
    {
        /// <summary>Trần fps — cao hơn chỉ tốn pin và nhiệt, mà nóng lên là throttle rồi tụt frame.</summary>
        private const int MaxFps = 90;

        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;

            var hz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = hz > 0 ? RateFor(hz) : 60;

            Application.backgroundLoadingPriority = ThreadPriority.Low;
        }

        /// <summary>Swappy chỉ present ở bội số vsync nên trần phải rơi đúng hz/n: 144Hz ra 72, không phải 90.</summary>
        private static int RateFor(int hz)
        {
            var n = (hz + MaxFps - 1) / MaxFps; // chia lấy trần, số nguyên để không dính sai số float
            return hz / Mathf.Max(1, n);
        }
    }
}
