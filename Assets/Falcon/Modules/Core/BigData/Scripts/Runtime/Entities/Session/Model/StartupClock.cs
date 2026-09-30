/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using Falcon.Helpers.Devkit;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bấm giờ khởi động (§B4 <c>startupDurationMs</c>) — mốc bắt đầu là lúc code C# sống dậy
    /// (<see cref="RuntimeInitializeLoadType.SubsystemRegistration"/>), trước cả DI lẫn scene đầu.
    /// <br/>⚠ THIẾU phần native bootstrap (từ lúc OS tạo process tới lúc Unity runtime lên) — Unity
    /// không nhìn thấy khoảng đó. Vì vậy đây là **số tương đối để so giữa các bản/các máy**, KHÔNG
    /// phải cold-start tuyệt đối; đã chốt với loader như vậy.
    /// <br/>Đây cũng là NGOẠI LỆ duy nhất client được tự đo thời gian: khoảng này nằm trước khi hệ
    /// thống event sống nên server mù. Từ mốc app_open trở đi, mọi khoảng cách thời gian server tự
    /// trừ giữa các event — client không cộng hộ.
    /// <br/>Đồng hồ đơn điệu (<see cref="MonotonicClock"/>) chứ không phải wall-clock: chỉnh giờ
    /// máy giữa chừng không đẻ ra số âm hay số khổng lồ. Riêng file này chỉ giữ cái NEO — mốc
    /// SubsystemRegistration — còn thước dùng chung của cả hệ.
    /// </summary>
    public static class StartupClock
    {
        private static long? kStartMillis;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Start()
        {
            kStartMillis = MonotonicClock.ElapsedMillis;
        }

        /// <summary>
        /// Số ms từ lúc C# sống dậy tới bây giờ; null nếu chưa bấm giờ (editor test, domain reload
        /// tắt kiểu lạ) — không bịa 0.
        /// </summary>
        public static int? ElapsedMillis =>
            kStartMillis == null ? null : (int)(MonotonicClock.ElapsedMillis - kStartMillis.Value);
    }
}
