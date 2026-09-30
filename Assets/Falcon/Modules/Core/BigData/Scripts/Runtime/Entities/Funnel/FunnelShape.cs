/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Hình dạng của một funnel — quyết định luật hợp lệ áp lên nó (xem <see cref="FunnelRegistry"/>).
    /// </summary>
    public enum FunnelShape
    {
        /// <summary>
        /// MỘT LẦN trên đời máy, các bước phải đi đúng thứ tự priority 0,1,2... (vd <c>ftue</c>).
        /// Lặp lại một bước hoặc nhảy cóc = lỗi logic của game, SDK báo lỗi.
        /// </summary>
        OnceOrdered,

        /// <summary>
        /// [NGHỈ HƯU 05/09 — chốt owner] Không lọc gì cả. Giữ cho 4 vocab di sản đang sống
        /// (<c>battle_pass</c>/<c>daily_quest</c>/<c>piggy_bank</c>/<c>lucky_wheel</c> — đổi shape
        /// của funnel đang chạy là đổi luật lọc trên data sống) và làm tầng pass-through khi
        /// OncePerCycle thiếu cycleId. THIẾT KẾ MỚI KHÔNG DÙNG: mốc tiến độ → OncePerCycle
        /// (kể cả bước lặp "mở UI" — van nén một dòng/chu kỳ là đúng grain phễu); đếm sự kiện
        /// (impression/CTR per lần hiện) → event exposure riêng, không đi funnel (litmus
        /// TrackingGuide: funnel là MỐC, không phải máy đếm).
        /// </summary>
        [Obsolete("Nghỉ hưu 05/09: mốc tiến độ dùng OncePerCycle; đếm sự kiện dùng event " +
                  "exposure riêng (không đi funnel). Chỉ còn cho vocab di sản đang sống.")]
        Repeatable,

        /// <summary>
        /// Lặp lại theo CHU KỲ game tự khai (<c>cycleId</c> trên <c>OnStep</c>): trong MỘT chu kỳ,
        /// mỗi cặp (action, priority) chỉ được log một lần — sang chu kỳ mới tự sạch, khỏi reset.
        /// Đây là <see cref="OnceOrdered"/> nới ranh giới: "đời máy" thành "chu kỳ", và BỎ luật
        /// thứ tự (mua vượt tier là hợp lệ).
        /// <br/>Lọc theo cả ACTION vì <c>milestone(3)</c> rồi <c>claim(3)</c> là hai bước thật
        /// cùng priority — lọc theo priority trần là tái tạo đúng cái bug làm luật ftue gãy với
        /// battle pass.
        /// <br/>⚠ Đừng dùng cho funnel có bước lặp HỢP LỆ trong một chu kỳ (lucky_wheel quay N
        /// lần/ngày) — đó là đất của <see cref="Repeatable"/>.
        /// </summary>
        OncePerCycle
    }

    public static class FunnelShapeWire
    {
        /// <summary>
        /// Tên shape trên wire (<c>AFunnelLog.funnelShape</c> — SDK tự đóng trong decor). Vai DQ:
        /// đây là shape ĐANG ÁP tại client lúc log (đọc từ registry), không phải shape "đúng"
        /// theo thiết kế — catalog data team giữ bản chuẩn, hai bên lệch nhau là bắt được ngay ca
        /// quên Register (funnel rơi về luật ftue, trượt trắng) thay vì phải nội suy từ hành vi
        /// stream. <b>Đổi tên = SỬA HỢP ĐỒNG</b> (§H2).
        /// </summary>
        public static string ToWireName(this FunnelShape shape)
        {
#pragma warning disable 618 // vocab di sản còn trên wire
            return shape switch
            {
                FunnelShape.OnceOrdered => "once_ordered",
                FunnelShape.OncePerCycle => "once_per_cycle",
                FunnelShape.Repeatable => "repeatable",
                _ => "unknown"
            };
#pragma warning restore 618
        }
    }
}
