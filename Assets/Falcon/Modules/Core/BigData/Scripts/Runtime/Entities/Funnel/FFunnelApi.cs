/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-07
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// FUNNEL — <c>FalconBigDataController.Funnel</c>: tiến độ FTUE (§D6) và tiến độ feature
    /// (battle pass, daily quest, event mùa — §D9). Mọi feature engagement chung một hình dạng
    /// nên KHÔNG đẻ event mới cho từng feature.
    /// </summary>
    public class FFunnelApi
    {
        private readonly FunnelLogService _funnelLogService;
        private readonly FunnelRegistry _funnelRegistry;

        internal FFunnelApi(FunnelLogService funnelLogService, FunnelRegistry funnelRegistry)
        {
            _funnelLogService = funnelLogService;
            _funnelRegistry = funnelRegistry;
        }

        /// <summary>
        /// Khai hình dạng cho funnel RIÊNG của game (§D9) — gọi lúc khởi động, trước bước đầu tiên.
        /// <br/>Vì sao cần: luật hợp lệ mặc định của SDK viết cho funnel kiểu <c>ftue</c> (một lần
        /// trên đời, priority tăng đều không hở). Funnel lặp lại/nhảy cóc mốc mà không khai
        /// <see cref="FunnelShape.Repeatable"/> sẽ bị chính SDK đánh trượt.
        /// Các funnel trong <see cref="FFunnelName"/> đã đăng ký sẵn, không phải khai lại.
        /// </summary>
        public void Register(string funnelName, FunnelShape shape)
        {
            _funnelRegistry.Register(funnelName, shape);
        }

        /// <summary>
        /// Game báo một bước funnel — CỬA CHUẨN nhận param (bản knob phẳng là shorthand).
        /// Dùng hằng số <see cref="FFunnelName"/> + <see cref="FFunnelAction"/> cho các funnel đã
        /// có trong hợp đồng; tên tự chế thì phải đăng ký với loader trước, không thì server
        /// không nhặt. <c>param.funnelName</c>/<c>action</c> bắt buộc.
        /// </summary>
        public void OnStep(FunnelParam param)
        {
            _funnelLogService.LogStep(param);
        }

        /// <inheritdoc cref="OnStep(FunnelParam)"/>
        /// <param name="funnelName">Tên funnel, vd <see cref="FFunnelName.BattlePass"/>.</param>
        /// <param name="action">Bước, vd <see cref="FFunnelAction.Milestone"/>.</param>
        /// <param name="priority">Số thứ tự bước / số tier của mốc.</param>
        /// <param name="cycleId">
        /// Ranh giới chu kỳ ("season_5", "2026-08-12"...) — BẮT BUỘC với funnel khai
        /// <see cref="FunnelShape.OncePerCycle"/> (thiếu thì bước đó không được lọc); các shape
        /// khác truyền vào cũng tốt: server tách mùa chính xác thay vì suy từ funnelDay.
        /// </param>
        /// <param name="cycleIndex">Số thứ tự chu kỳ (mùa/giải thứ mấy) — trục sort bằng SỐ cho server, khỏi parse tên cycleId.</param>
        /// <param name="cycleStartTs">Mốc MỞ chu kỳ, epoch millis UTC, từ config event — xem doc field trên <see cref="FunnelParam"/>.</param>
        /// <param name="cycleEndTs">Mốc ĐÓNG chu kỳ, epoch millis UTC — server tự tính countdown-tại-log = cycleEndTs − ts, khỏi cần bảng lịch event.</param>
        /// <param name="extraMeta">Key-value tuỳ ý cho bước này — server lưu <c>event_extra_props</c>; cột hợp đồng thắng khi trùng key.</param>
        public void OnStep(
            string funnelName, string action, int priority = 0, int? currentLevel = null,
            string cycleId = null, int? cycleIndex = null,
            long? cycleStartTs = null, long? cycleEndTs = null,
            Dictionary<string, object> extraMeta = null)
        {
            _funnelLogService.LogStep(new FunnelParam
            {
                funnelName = funnelName,
                action = action,
                priority = priority,
                currentLevel = currentLevel,
                cycleId = cycleId,
                cycleIndex = cycleIndex,
                cycleStartTs = cycleStartTs,
                cycleEndTs = cycleEndTs,
                extraMeta = extraMeta
            });
        }

        // ---- Đọc tiến độ (sổ LOCAL đời máy — đúng cái van lọc trùng dùng) ----

        /// <summary>
        /// Mốc CAO NHẤT user đã đi của funnel <see cref="FunnelShape.OnceOrdered"/> (ftue...) —
        /// null nếu chưa bước nào. Chỉ shape này có sổ liền mạch để dò; hỏi funnel shape khác sẽ
        /// cảnh báo và trả null.
        /// <br/>⚠ Sổ local của THIẾT BỊ, không phải chân lý cross-device — dùng để rẽ nhánh
        /// hành vi client (skip tutorial đã qua...), đừng dùng làm nguồn tiến độ cần sync.
        /// </summary>
        public int? CurrentPriority(string funnelName)
        {
            if (_funnelRegistry.ShapeOf(funnelName) != FunnelShape.OnceOrdered)
            {
                AnalyticLogger.Instance.Warning(
                    $"CurrentPriority('{funnelName}') chỉ có nghĩa với funnel OnceOrdered (sổ " +
                    "liền mạch từ 0). Funnel lặp/theo chu kỳ không có 'mốc hiện tại' duy nhất — " +
                    "dùng HasPassed(funnelName, cycleId, action, priority) cho OncePerCycle.");
                return null;
            }

            return _funnelLogService.CurrentOrderedPriority(funnelName);
        }

        /// <summary>
        /// Funnel <see cref="FunnelShape.OnceOrdered"/> đã đi qua mốc này chưa (sổ local đời máy).
        /// </summary>
        public bool HasPassed(string funnelName, int priority)
        {
            return _funnelLogService.HasPassedOrdered(funnelName, priority);
        }

        /// <summary>
        /// Funnel <see cref="FunnelShape.OncePerCycle"/> đã đi bước (action, priority) trong chu
        /// kỳ này chưa — đúng câu mà van lọc trùng sẽ trả lời khi bạn OnStep bước đó.
        /// </summary>
        public bool HasPassed(string funnelName, string cycleId, string action, int priority)
        {
            return _funnelLogService.HasPassedInCycle(funnelName, cycleId, action, priority);
        }

        /// <summary>
        /// Ngày <see cref="FFunnelAction.Join"/> GẦN NHẤT của funnel (giờ local) — cùng mốc mà
        /// <c>funnelDay</c> trên log dùng. Null nếu chưa từng join.
        /// </summary>
        public DateTime? LastJoinDay(string funnelName)
        {
            return _funnelLogService.LastJoinDayLocal(funnelName);
        }
    }
}
