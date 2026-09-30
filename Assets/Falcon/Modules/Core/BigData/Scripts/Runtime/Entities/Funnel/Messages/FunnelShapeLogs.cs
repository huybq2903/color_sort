/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-10
 */

using System;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Funnel MỘT LẦN, ĐÚNG THỨ TỰ (<see cref="FunnelShape.OnceOrdered"/>) — khuôn của <c>ftue</c>:
    /// mỗi mốc chỉ được đi một lần trên đời máy và priority phải tăng đều không hở.
    /// <br/>Bước sai luật bị <b>chặn</b> (kế thừa cơ chế veto của <see cref="FFilteredFunnelLog"/>),
    /// vì đây chính là thứ người dùng chọn class này để mua: SDK lọc trùng hộ.
    /// </summary>
    [Serializable]
    public class OnceOrderedFunnelLog : FFilteredFunnelLog
    {
        [Preserve]
        public OnceOrderedFunnelLog()
        {
        }

        public OnceOrderedFunnelLog(FunnelParam param) : base(param)
        {
        }
    }

    /// <summary>
    /// Funnel MỘT LẦN MỖI CHU KỲ (<see cref="FunnelShape.OncePerCycle"/>) — battle pass lọc trùng
    /// theo mùa: trong một <c>cycleId</c>, mỗi cặp (action, priority) chỉ đi một lần; sang chu kỳ
    /// mới tự sạch. Bước trùng bị <b>chặn</b> (kế thừa veto của <see cref="FFilteredFunnelLog"/>).
    /// Không có luật thứ tự — mua vượt tier là hợp lệ.
    /// </summary>
    [Serializable]
    public class OncePerCycleFunnelLog : FFilteredFunnelLog
    {
        [Preserve]
        public OncePerCycleFunnelLog()
        {
        }

        public OncePerCycleFunnelLog(FunnelParam param) : base(param)
        {
        }
    }

    /// <summary>
    /// Funnel LẶP LẠI (<see cref="FunnelShape.Repeatable"/>) — battle pass, daily quest, event mùa:
    /// vào lại mùa mới, nhảy cóc tier, nhận thưởng nhiều mốc đều HỢP LỆ.
    /// <br/>Không có veto, vì ở đây không có khái niệm "trùng": lọc là mất tier-up thật.
    /// Việc duy nhất còn lại là đóng <c>funnelDay</c> theo lần <c>join</c> gần nhất.
    /// <br/>Cố tình KHÔNG kế thừa <see cref="FFilteredFunnelLog"/>: class đó bán lời hứa "lọc hộ",
    /// mà ở hình dạng này lời hứa đó vừa vô nghĩa vừa gây hiểu nhầm.
    /// </summary>
    [Serializable]
    [Obsolete("Shape Repeatable nghỉ hưu 05/09 — chỉ còn cho vocab di sản. Mốc tiến độ dùng " +
              "OncePerCycleFunnelLog; đếm sự kiện dùng event exposure riêng, không đi funnel.")]
    public class RepeatableFunnelLog : AFunnelLog
    {
        [Preserve]
        public RepeatableFunnelLog()
        {
        }

        public RepeatableFunnelLog(FunnelParam param) : base(param)
        {
            // Check ở đây không để veto mà để đóng funnelDay — bản repeatable luôn hợp lệ
            FunnelLogService.Instance.Check(this);
        }
    }
}
