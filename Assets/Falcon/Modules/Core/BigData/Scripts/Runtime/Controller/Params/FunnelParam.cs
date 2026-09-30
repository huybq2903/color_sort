/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */
using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class FunnelParam : FParam
    {
        [NotNull] public string action = UNKNOWN;
        [NotNull] public string funnelName = UNKNOWN;
        public int priority;

        [FKey(RemoveIfNull = true)] public int? currentLevel;

        /// <summary>
        ///     Ranh giới CHU KỲ của funnel lặp — mùa ("season_5"), ngày ("2026-08-12")... Bắt buộc
        ///     để <see cref="FunnelShape.OncePerCycle"/> lọc được; các shape khác mang lên wire
        ///     làm ngữ cảnh (server tách mùa chính xác thay vì suy từ funnelDay), không ảnh hưởng
        ///     luật. Null = vắng mặt khỏi payload (§H4).
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull] public string cycleId;

        /// <summary>
        ///     Số thứ tự chu kỳ (mùa/giải thứ mấy) — trục SORT bằng số cho server, khỏi parse tên
        ///     <see cref="cycleId"/> ("season_9" &gt; "season_10" khi sort chuỗi — cùng họ bài học
        ///     floor). Null = vắng mặt (§H4).
        /// </summary>
        [FKey(RemoveIfNull = true)] public int? cycleIndex;

        /// <summary>
        ///     Mốc MỞ/ĐÓNG của chu kỳ — epoch millis UTC, lấy từ CONFIG event (đúng con số UI
        ///     countdown đang vẽ: game phải đúng nó để chạy nên log nó gần như không thể sai).
        ///     Có nó server tự tính countdown-tại-log = <c>cycleEndTs − ts</c>, mọi phép cắt nhóm
        ///     "join khi còn ≥4/1–4/&lt;1 ngày" đứng trên chính dòng log — hết án chờ bảng lịch
        ///     vận hành (dim-table Master League).
        ///     <br/>Chu kỳ ĐỘNG (extend giữa mùa): các bước sau mang giá trị mới — wire tự ghi
        ///     lịch sử đổi lịch; server đọc theo giá-trị-tại-thời-điểm-log, đừng assume bất biến
        ///     trong chu kỳ.
        /// </summary>
        [FKey(RemoveIfNull = true)] public long? cycleStartTs;

        /// <inheritdoc cref="cycleStartTs"/>
        [FKey(RemoveIfNull = true)] public long? cycleEndTs;

        /// <summary>
        ///     Key-value tuỳ ý cho bước này — flatten vào payload, server lưu
        ///     <c>event_extra_props</c>; cột hợp đồng thắng khi trùng key. Hoisted từ
        ///     <see cref="OldCodeSupportFunnelParam"/> khi Funnel.OnStep mở tham số extraMeta —
        ///     con KHÔNG khai lại (hai field cùng tên là FKeyService ghi đè nhau, bài học
        ///     OldCodeSupportLevelParam).
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> extraMeta;

        public override void CorrectValues()
        {
            action = CheckNonBlank(action, nameof(action));
            funnelName = CheckNonBlank(funnelName, nameof(funnelName));
            currentLevel = CheckNumberNonNegative(currentLevel, nameof(currentLevel));
            priority = CheckNumberNonNegative(priority, nameof(priority));
            cycleIndex = CheckNumberNonNegative(cycleIndex, nameof(cycleIndex));

            // Lịch ngược gần như chắc là bug config — CẢNH BÁO nhưng giữ nguyên giá trị (không
            // sửa hộ, không đoán): server thấy raw mới truy được nguồn, âm thầm null hoá là xoá
            // dấu vết của chính cái bug cần tìm.
            if (cycleStartTs.HasValue && cycleEndTs.HasValue && cycleEndTs.Value <= cycleStartTs.Value)
                AnalyticLogger.Instance.Warning(
                    $"FunnelParam '{funnelName}': cycleEndTs ({cycleEndTs}) <= cycleStartTs " +
                    $"({cycleStartTs}) — lịch chu kỳ ngược, soi lại config event.");
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }
    }

    [Serializable]
    public class OldCodeSupportFunnelParam : FunnelParam
    {
    }
}