/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-12
 */

using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// Đồng hồ ĐƠN ĐIỆU dùng chung — thước đo khoảng thời gian không co giãn theo giờ máy.
    ///
    /// <para><b>Chọn đồng hồ nào — ba luật, đừng đảo:</b></para>
    /// <list type="number">
    /// <item><b>MỐC</b> (retention, first login, timestamp bản tin) → wall-clock
    /// (<see cref="ITimeRepository"/>): mốc cần LỊCH THẬT, và server đối chiếu được.</item>
    /// <item><b>KHOẢNG không vắt qua suspend</b> (stretch chơi foreground, thời gian khởi động)
    /// → đồng hồ này: user chỉnh giờ máy giữa chừng không đẻ ra số âm hay số khổng lồ. Đây là chỗ
    /// duy nhất chỉnh-giờ gây hại thật — counter TÍCH LUỸ có động cơ cheat (skip-timer) và sai là
    /// sai vĩnh viễn.</item>
    /// <item><b>KHOẢNG phải vắt qua suspend</b> (thời lượng nằm nền, thời lượng xem ad — ad overlay
    /// tự nó pause game) → BẮT BUỘC wall-clock: đồng hồ đơn điệu ĐÓNG BĂNG khi máy ngủ sâu
    /// (CLOCK_MONOTONIC/mach_absolute_time không đếm suspend), dùng nó là đếm THIẾU. Chấp nhận
    /// nhiễu chỉnh-giờ ở nhóm này — chúng là số đo per-instance, tự lành ở lần sau, đã clamp ≥ 0,
    /// và server đối chiếu được bằng hiệu timestamp giữa hai event.</item>
    /// </list>
    ///
    /// <para>Static chứ không inject: dành cho SERVICE. Model thuần vẫn theo nếp cũ — nhận
    /// <c>nowMillis</c> từ caller, test tự bơm số.</para>
    /// </summary>
    public static class MonotonicClock
    {
        private static readonly Stopwatch kClock = Stopwatch.StartNew();

        /// <summary>Thời gian trôi từ lúc đồng hồ dựng (lần đầu có ai đụng vào class này).</summary>
        public static TimeSpan Elapsed => kClock.Elapsed;

        /// <inheritdoc cref="Elapsed"/>
        public static long ElapsedMillis => kClock.ElapsedMilliseconds;
    }
}
