/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public class FunnelLogService : MySingleton<FunnelLogService>
    {
        private const string JOIN_DAY_SUFFIX = "_joinDay";

        private readonly IDataPool _dataPool;
        private readonly ITimeRepository _timeRepository;
        private readonly FunnelRegistry _funnelRegistry;
        private readonly LogScheduleService _logScheduleService;

        public FunnelLogService(
            IDataPool dataPool, ITimeRepository timeRepository,
            FunnelRegistry funnelRegistry, LogScheduleService logScheduleService)
        {
            _dataPool = dataPool;
            _timeRepository = timeRepository;
            _funnelRegistry = funnelRegistry;
            _logScheduleService = logScheduleService;
        }

        /// <summary>
        /// Game báo một bước funnel (§D6 + §D9). Service CHỈ nhận param — nén knob phẳng là việc
        /// của <see cref="FFunnelApi"/>. Dùng hằng số <see cref="FFunnelName"/> /
        /// <see cref="FFunnelAction"/> cho các funnel đã có trong hợp đồng.
        /// </summary>
        public void LogStep(FunnelParam param)
        {
            if (param == null) return;
            // API công khai nhận string tự do — phải soi như đường ctor cũ vẫn soi
            param.CorrectValues();

            // Hình dạng funnel quyết định LOẠI LOG: mỗi loại mang đúng luật của mình, thay vì một
            // class chung rồi rẽ nhánh ngầm bên trong (đọc call site là biết luật nào đang áp)
#pragma warning disable 618 // Repeatable nghỉ hưu nhưng vocab di sản còn chạy — plumbing phải giữ
            _logScheduleService.Enqueue(_funnelRegistry.ShapeOf(param.funnelName) switch
            {
                FunnelShape.Repeatable => new RepeatableFunnelLog(param),
                FunnelShape.OncePerCycle => new OncePerCycleFunnelLog(param),
                _ => (AFunnelLog)new OnceOrderedFunnelLog(param)
            });
#pragma warning restore 618
        }

        // ---- Đọc sổ tiến độ (cùng sổ mà van lọc trùng dùng) ----

        /// <summary>
        /// Mốc CAO NHẤT đã đi của funnel <see cref="FunnelShape.OnceOrdered"/> — null nếu chưa
        /// bước nào. Dò được vì luật shape này ép priority liền mạch từ 0 không hở.
        /// <br/>⚠ Sổ LOCAL đời máy (đúng cái van lọc trùng đọc/ghi) — không phải chân lý
        /// cross-device.
        /// </summary>
        public int? CurrentOrderedPriority(string funnelName)
        {
            var priority = 0;
            while (_dataPool.HasKey(funnelName + priority)) priority++;
            return priority > 0 ? priority - 1 : null;
        }

        /// <summary>Funnel OnceOrdered đã đi qua mốc này chưa (sổ local đời máy).</summary>
        public bool HasPassedOrdered(string funnelName, int priority)
        {
            return _dataPool.HasKey(funnelName + priority);
        }

        /// <summary>
        /// Funnel <see cref="FunnelShape.OncePerCycle"/> đã đi bước (action, priority) trong chu kỳ
        /// này chưa (sổ local đời máy). Key gồm cả ACTION — cùng bài học với van lọc.
        /// </summary>
        public bool HasPassedInCycle(string funnelName, string cycleId, string action, int priority)
        {
            return _dataPool.HasKey(CycleStepKey(funnelName, cycleId, action, priority));
        }

        /// <summary>
        /// Ngày <see cref="FFunnelAction.Join"/> GẦN NHẤT của funnel (giờ local) — cùng mốc mà
        /// <c>funnelDay</c> trên log dùng. Null nếu chưa từng join (game mới chỉ log milestone).
        /// </summary>
        public DateTime? LastJoinDayLocal(string funnelName)
        {
            var key = funnelName + JOIN_DAY_SUFFIX;
            if (!_dataPool.HasKey(key)) return null;
            return _dataPool.GetOrDefault(key, default(DateTime)).ToLocalTime();
        }

        /// <summary>
        ///     Decor the log with necessary params (does not contain params in BaseFalconLog)
        /// </summary>
        /// <param name="funnelLog"></param>
        /// <returns></returns>
        public DecorInfo Check(AFunnelLog funnelLog)
        {
            var logParams = funnelLog.param;

            // Loại log nói lên hình dạng (đường OnStep dựng đúng loại); registry chỉ dùng cho log
            // dựng bằng tay (đường cũ, class không nói được gì).
#pragma warning disable 618 // plumbing di sản — xem LogStep
            var shape = funnelLog switch
            {
                RepeatableFunnelLog => FunnelShape.Repeatable,
                OncePerCycleFunnelLog => FunnelShape.OncePerCycle,
                _ => _funnelRegistry.ShapeOf(logParams.funnelName)
            };
#pragma warning restore 618

            // Tự thú shape ĐANG ÁP lên wire — đóng cả cho log sắp bị veto (nó bị vứt, không sao);
            // xem doc AFunnelLog.funnelShape về vai DQ của field này.
            funnelLog.funnelShape = shape.ToWireName();

            // Funnel lặp tự do (daily quest, event mùa): mốc trùng/nhảy cóc/vào lại mùa mới đều
            // HỢP LỆ — luật một-lần-đúng-thứ-tự dưới kia chỉ đúng cho ftue.
#pragma warning disable 618 // nhánh di sản
            if (shape == FunnelShape.Repeatable)
#pragma warning restore 618
            {
                // Class hứa "lọc trùng hộ" mà funnel lại là loại lặp — lời hứa đó thành vô hiệu
                // một cách IM LẶNG, đúng loại hồi quy khó truy nhất
                if (funnelLog is FFilteredFunnelLog)
                    AnalyticLogger.Instance.Warning(
                        $"Funnel '{logParams.funnelName}' khai là Repeatable nhưng đang log bằng " +
                        "FFilteredFunnelLog — class đó lọc trùng hộ bạn, mà ở hình dạng lặp lại thì " +
                        "KHÔNG lọc gì cả. Dùng RepeatableFunnelLog (hoặc Funnel.OnStep) cho khỏi hiểu nhầm.");

                return CheckRepeatable(funnelLog);
            }

            if (shape == FunnelShape.OncePerCycle) return CheckOncePerCycle(funnelLog);

            var valid = true;
            var error = string.Empty;
            if (logParams.priority != 0 && !_dataPool.HasKey(logParams.funnelName + (logParams.priority - 1)))
            {
                error =
                    $"Dwh Log invalid logic : Funnel {logParams.funnelName} not created in order in this device instance";
                valid = false;
            }

            if (!valid)
            {
                return new DecorInfo(false, error);
            }

            _dataPool.Compute<DateTime>(logParams.funnelName + logParams.priority, value =>
            {
                if (!value.HasValue) return _timeRepository.UtcNow();
                error =
                    $"Dwh Log invalid logic : This device already joined the funnel {logParams.funnelName} of the priority {logParams.priority}";
                valid = false;
                return value;
            });
            
            var day = _dataPool.GetOrDefault(logParams.funnelName + 0, _timeRepository.UtcNow()).ToLocalTime();
            funnelLog.funnelDay = MyTime.DateToString(day);
            return new DecorInfo(valid, error);
        }

        /// <summary>
        /// Funnel lặp lại: không có gì để đánh trượt (mọi thứ tự đều hợp lệ), chỉ còn việc đóng
        /// <c>funnelDay</c>.
        /// </summary>
        private DecorInfo CheckRepeatable(AFunnelLog funnelLog)
        {
            StampJoinDay(funnelLog);
            return new DecorInfo(true, string.Empty);
        }

        /// <summary>
        /// Một lần mỗi CHU KỲ: trong một <c>cycleId</c>, mỗi cặp (action, priority) chỉ đi một lần
        /// — sang chu kỳ mới tự sạch. Không có luật thứ tự (mua vượt tier hợp lệ). Key lọc gồm cả
        /// ACTION: <c>milestone(3)</c> rồi <c>claim(3)</c> là hai bước thật cùng priority, lọc
        /// theo priority trần là tái tạo cái bug làm luật ftue gãy với battle pass.
        /// <br/>Thiếu <c>cycleId</c> thì KHÔNG lọc (cảnh báo + xử như Repeatable) — chặn dữ liệu
        /// thật vì thiếu tham số còn tệ hơn để lọt trùng.
        /// </summary>
        private DecorInfo CheckOncePerCycle(AFunnelLog funnelLog)
        {
            var logParams = funnelLog.param;
            if (string.IsNullOrEmpty(logParams.cycleId))
            {
                AnalyticLogger.Instance.Warning(
                    $"Funnel '{logParams.funnelName}' khai OncePerCycle nhưng bước này không mang " +
                    "cycleId — không có ranh giới chu kỳ thì không lọc được, bước đi qua KHÔNG lọc. " +
                    "Truyền cycleId vào OnStep (vd \"season_5\").");
                return CheckRepeatable(funnelLog);
            }

            var valid = true;
            var error = string.Empty;
            _dataPool.Compute<DateTime>(
                CycleStepKey(logParams.funnelName, logParams.cycleId, logParams.action, logParams.priority),
                value =>
                {
                    if (!value.HasValue) return _timeRepository.UtcNow();
                    valid = false;
                    error = "Dwh Log invalid logic : Funnel " + logParams.funnelName +
                            $" already logged '{logParams.action}' at priority {logParams.priority}" +
                            $" in cycle '{logParams.cycleId}'";
                    return value;
                });

            StampJoinDay(funnelLog);
            return new DecorInfo(valid, error);
        }

        /// <summary>
        /// Key state lọc trùng của một bước trong chu kỳ — public để test ghim được bài học
        /// "key phải gồm cả action".
        /// </summary>
        public static string CycleStepKey(string funnelName, string cycleId, string action, int priority)
        {
            return funnelName + "_" + cycleId + "_" + action + "_" + priority;
        }

        /// <summary>
        /// Đóng <c>funnelDay</c> — server derive "vào được mấy ngày" từ nó. Mốc tính là lần
        /// <see cref="FFunnelAction.Join"/> GẦN NHẤT, không phải lần đầu đời máy: battle pass mùa 5
        /// mà mang ngày join mùa 1 thì con số "được mấy ngày" thành vô nghĩa. Chưa từng join
        /// (game chỉ log milestone) thì lấy hôm nay — không bịa cohort.
        /// </summary>
        private void StampJoinDay(AFunnelLog funnelLog)
        {
            var key = funnelLog.param.funnelName + JOIN_DAY_SUFFIX;
            var now = _timeRepository.UtcNow();
            if (funnelLog.param.action == FFunnelAction.Join)
                _dataPool.Compute<DateTime>(key, _ => now);

            funnelLog.funnelDay = MyTime.DateToString(_dataPool.GetOrDefault(key, now).ToLocalTime());
        }
    }
}