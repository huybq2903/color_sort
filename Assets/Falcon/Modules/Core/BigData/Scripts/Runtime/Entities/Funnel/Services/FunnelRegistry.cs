/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System.Collections.Concurrent;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Khai hình dạng của từng funnel (§D9). Lý do phải có: luật hợp lệ của funnel trong SDK
    /// vốn được viết cho ĐÚNG một hình dạng — <c>ftue</c>: một lần trên đời, priority tăng đều
    /// không hở. Mọi funnel feature (<c>battle_pass</c> nhận thưởng tier 3 hai bước
    /// milestone+claim, <c>daily_quest</c> join lại mỗi ngày, người chơi mua vượt tier) đều vi
    /// phạm luật đó — không khai hình dạng thì log bị chính SDK đánh trượt.
    /// <br/>Funnel CHƯA đăng ký chạy nguyên luật cũ (không đổi hành vi game đang chạy).
    /// </summary>
    public class FunnelRegistry : MySingleton<FunnelRegistry>
    {
        private readonly ConcurrentDictionary<string, FunnelShape> _shapes = new();

        public FunnelRegistry()
        {
            // Vocab hợp đồng: đăng ký sẵn để dev game dùng hằng số là chạy đúng, không phải khai
            // lại. 4 vocab Repeatable là DI SẢN (shape nghỉ hưu 05/09) — game sống đang chạy trên
            // luật này, đổi shape là đổi luật lọc trên data sống.
#pragma warning disable 618
            _shapes[FFunnelName.Ftue] = FunnelShape.OnceOrdered;
            _shapes[FFunnelName.BattlePass] = FunnelShape.Repeatable;
            _shapes[FFunnelName.DailyQuest] = FunnelShape.Repeatable;
            _shapes[FFunnelName.PiggyBank] = FunnelShape.Repeatable;
            _shapes[FFunnelName.LuckyWheel] = FunnelShape.Repeatable;
#pragma warning restore 618
        }

        /// <summary>
        /// Khai hình dạng cho funnel riêng của game. Gọi trước lần log đầu tiên của funnel đó
        /// (thường lúc khởi động). Khai lại thì giá trị sau thắng.
        /// </summary>
        public void Register(string funnelName, FunnelShape shape)
        {
            if (string.IsNullOrEmpty(funnelName)) return;

#pragma warning disable 618
            if (shape == FunnelShape.Repeatable && !_shapes.ContainsKey(funnelName))
#pragma warning restore 618
                AnalyticLogger.Instance.Warning(
                    $"Funnel '{funnelName}' đăng ký shape Repeatable — shape này NGHỈ HƯU 05/09: " +
                    "mốc tiến độ (kể cả bước lặp kiểu mở-UI) dùng OncePerCycle, đếm sự kiện " +
                    "(impression/CTR) dùng event exposure riêng chứ không đi funnel.");

            _shapes[funnelName] = shape;
        }

        /// <summary>
        /// Hình dạng hiệu lực của một funnel. Tên chưa đăng ký: <c>event_*</c> (game event/season)
        /// mặc định <see cref="FunnelShape.OncePerCycle"/> (đổi 05/09 khi Repeatable nghỉ hưu —
        /// lưới quên-Register giờ còn LỌC ĐÚNG luôn vì khuôn bắt mọi bước mang cycleId; bước lỡ
        /// thiếu cycleId thì van tự hạ xuống pass-through + warning, tức ca xấu nhất của lưới mới
        /// bằng ca thường của lưới cũ); còn lại giữ nguyên luật cũ
        /// (<see cref="FunnelShape.OnceOrdered"/>) để game đang chạy không đổi hành vi.
        /// <br/>KHÔNG persist <c>_shapes</c> — đây là KHAI BÁO từ code (game Register lại mỗi lần
        /// khởi động, như provider của common param), persist là đẻ nguồn chân lý thứ hai có thể
        /// cãi nhau với code sau khi game update. Cái phải sống qua restart là STATE lọc trùng —
        /// nó nằm trong dataPool của <see cref="FunnelLogService"/>.
        /// </summary>
        public FunnelShape ShapeOf(string funnelName)
        {
            if (string.IsNullOrEmpty(funnelName)) return FunnelShape.OnceOrdered;
            if (_shapes.TryGetValue(funnelName, out var shape)) return shape;
            return funnelName.StartsWith(FFunnelName.EventPrefix)
                ? FunnelShape.OncePerCycle
                : FunnelShape.OnceOrdered;
        }

        /// <summary>Funnel này có phải hình dạng LẶP TỰ DO không (giữ cho call site cũ).</summary>
        public bool IsRepeatable(string funnelName)
        {
#pragma warning disable 618
            return ShapeOf(funnelName) == FunnelShape.Repeatable;
#pragma warning restore 618
        }
    }
}
