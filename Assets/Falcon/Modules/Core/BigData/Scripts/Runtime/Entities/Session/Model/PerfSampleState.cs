/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>Tóm tắt hiệu năng của một quãng chơi (§D7). Không có mẫu nào thì mọi field null.</summary>
    public readonly struct PerfSummary
    {
        public readonly float? AvgFps;
        public readonly int? FrameDropCount;
        public readonly int? MemoryWarningCount;

        public PerfSummary(float? avgFps, int? frameDropCount, int? memoryWarningCount)
        {
            AvgFps = avgFps;
            FrameDropCount = frameDropCount;
            MemoryWarningCount = memoryWarningCount;
        }
    }

    /// <summary>
    /// Cộng dồn số liệu hiệu năng của quãng chơi hiện tại (§D7). Lag-không-crash là mảng mù của
    /// Crashlytics (crash/ANR vẫn là sân của họ) mà giật máy yếu thì giết retention.
    /// <br/>Trạng thái thuần, không đụng Unity — <see cref="PerfSampleService"/> bơm số vào.
    /// </summary>
    public class PerfSampleState
    {
        /// <summary>
        /// Khung hình lâu hơn ngưỡng này = một cú tụt khung NẶNG (người chơi thấy được).
        /// 100ms ≈ dưới 10 fps tức thời, đủ nặng để không lẫn với dao động bình thường của
        /// máy 30fps lẫn 60fps — hợp đồng §D7 yêu cầu ngưỡng cố định phía client.
        /// </summary>
        public const float FRAME_DROP_SEC = 0.1f;

        /// <summary>
        /// Khung "lâu" hơn mức này không phải giật mà là app không thật sự render (đi ads,
        /// vừa quay lại từ background, load scene chặn luồng). Đếm vào là bịa số giật.
        /// </summary>
        public const float NOT_RENDERING_SEC = 2f;

        // Bên ghi là main thread (Update), bên đọc có thể là thread nền (tick log bảo hiểm) —
        // khoá không tranh chấp tốn vài chục nano, không đáng kể so với một frame
        private readonly object _lock = new();
        private int _frames;
        private float _elapsedSec;
        private int _frameDropCount;
        private int _memoryWarningCount;

        public void AddFrame(float deltaSec)
        {
            if (deltaSec <= 0 || deltaSec > NOT_RENDERING_SEC) return;
            lock (_lock)
            {
                _frames++;
                _elapsedSec += deltaSec;
                if (deltaSec > FRAME_DROP_SEC) _frameDropCount++;
            }
        }

        public void AddMemoryWarning()
        {
            lock (_lock) _memoryWarningCount++;
        }

        /// <summary>
        /// Lấy tóm tắt của quãng vừa rồi rồi reset — số liệu đi kèm đúng quãng mà bản tin
        /// session_data đang khai (server muốn gộp cả phiên thì cân theo sessionTime).
        /// </summary>
        public PerfSummary TakeAndReset()
        {
            lock (_lock)
            {
                var summary = _frames > 0 && _elapsedSec > 0
                    ? new PerfSummary(_frames / _elapsedSec, _frameDropCount, _memoryWarningCount)
                    : new PerfSummary(null, null, _memoryWarningCount > 0 ? _memoryWarningCount : (int?)null);

                _frames = 0;
                _elapsedSec = 0;
                _frameDropCount = 0;
                _memoryWarningCount = 0;
                return summary;
            }
        }
    }
}
