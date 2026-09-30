/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Họ bản tin gán nhãn (§D11) — một cửa cho cả "nhãn" lẫn "tham số động": dev không phải phân
    /// biệt hai thứ đó (ranh giới ấy là việc của data team, bắt dev phân biệt thì kiểu gì cũng
    /// dùng sai).
    /// <br/>Định danh instance không phải nhập: <c>accountId</c>/<c>sessionUid</c> có trên mọi log,
    /// <c>playTurnId</c> do <see cref="TurnCustomInfoRepository"/> stamp khi lượt còn mở.
    /// </summary>
    public class LabelLogService : MySingleton<LabelLogService>
    {
        private const string PERSIST_USER_KEYS = "Analytic_UserLabelKeys";

        private readonly LabelGuardState _guard = new();
        private bool _guardRestored;

        // Nhãn user là khai báo "từ mốc này trở đi", không phải sự kiện — gán lại đúng giá trị cũ
        // KHÔNG mang thông tin gì mà vẫn tốn một bản tin trên stream đông nhất. Cache theo PHIÊN
        // chứ không persist: profile server có TTL 90 ngày LRU (param-taxonomy §b), user ngủ lâu
        // quay lại là server "chưa từng gặp" — gửi lại một lần mỗi phiên là cái giá đúng.
        private readonly Dictionary<string, object> _lastUserLabel = new();

        // _guard đếm key nhãn user bằng HashSet thường; game có thể dán nhãn từ callback (mua xong,
        // xem ad xong) chứ không riêng luồng UI.
        private readonly object _lock = new();
        private readonly IDataPool _dataPool;
        private readonly LevelTurnService _levelTurnService;
        private readonly AdViewCache _adViewCache;
        private readonly OfferImpressionCache _offerCache;
        private readonly PurchaseAttemptCache _purchaseCache;
        private readonly LogScheduleService _logScheduleService;

        public LabelLogService(
            LevelTurnService levelTurnService, AdViewCache adViewCache,
            OfferImpressionCache offerCache, PurchaseAttemptCache purchaseCache,
            LogScheduleService logScheduleService, IDataPool dataPool)
        {
            _dataPool = dataPool;
            _levelTurnService = levelTurnService;
            _adViewCache = adViewCache;
            _offerCache = offerCache;
            _purchaseCache = purchaseCache;
            _logScheduleService = logScheduleService;
        }

        /// <summary>
        /// Nhãn của NGƯỜI CHƠI — hiệu lực từ mốc này trở đi, quá khứ miễn nhiễm.
        /// <paramref name="value"/> = null nghĩa là GỠ nhãn.
        /// </summary>
        public void LabelUser(string labelKey, object value)
        {
            if (!Check(labelKey, value, userScope: true)) return;

            lock (_lock)
            {
                if (_lastUserLabel.TryGetValue(labelKey, out var last) && Equals(last, value)) return;
                _lastUserLabel[labelKey] = value;
            }

            _logScheduleService.Enqueue(new FUserLabelLog(NewParam(labelKey, value)));
        }

        /// <summary>Nhãn của PHIÊN đang mở.</summary>
        public void LabelSession(string labelKey, object value)
        {
            if (!Check(labelKey, value, userScope: false)) return;
            _logScheduleService.Enqueue(new FSessionLabelLog(NewParam(labelKey, value)));
        }

        /// <summary>
        /// Nhãn của MỘT LƯỢT CHƠI LEVEL đang mở. Không có lượt nào mở thì bỏ qua kèm cảnh báo —
        /// nhãn không có playTurnId thì server không biết dán vào đâu.
        /// </summary>
        public void LabelLevelPlayTurn(string labelKey, object value)
        {
            if (string.IsNullOrEmpty(_levelTurnService.OpenPlayTurnId))
            {
                AnalyticLogger.Instance.Warning(
                    $"Nhãn turn '{labelKey}' bỏ qua: không có lượt chơi nào đang mở. Nhãn instance " +
                    "chỉ dán được khi instance CÒN MỞ — SDK không giữ id của lượt đã đóng.");
                return;
            }

            if (!Check(labelKey, value, userScope: false)) return;
            _logScheduleService.Enqueue(new FTurnLabelLog(NewParam(labelKey, value)));
        }

        /// <summary>
        /// Nhãn của LẦN XEM AD đang mở của format này.
        /// <br/>⚠ Id đã chốt trong hợp đồng §D11 (bản 11/08), còn chờ loader mở mapping rule.
        /// </summary>
        public void LabelAdView(AdType type, string labelKey, object value)
        {
            var id = RequireOpen(labelKey, "lần xem ad " + type, _adViewCache.CurrentViewId(type));
            if (id == null || !Check(labelKey, value, userScope: false)) return;

            _logScheduleService.Enqueue(new FAdViewLabelLog(NewParam(labelKey, value)) { adViewId = id });
        }

        /// <summary>
        /// Nhãn của LẦN HIỂN THỊ OFFER đang mở.
        /// <inheritdoc cref="LabelAdView"/>
        /// </summary>
        public void LabelIapOffer(string labelKey, object value)
        {
            var id = RequireOpen(labelKey, "lần hiển thị offer", _offerCache.OpenImpressionId);
            if (id == null || !Check(labelKey, value, userScope: false)) return;

            _logScheduleService.Enqueue(new FIapOfferLabelLog(NewParam(labelKey, value)) { offerImpressionId = id });
        }

        /// <summary>
        /// Nhãn của LƯỢT MUA đang mở.
        /// <inheritdoc cref="LabelAdView"/>
        /// </summary>
        public void LabelIapPurchase(string labelKey, object value)
        {
            var id = RequireOpen(labelKey, "lượt mua", _purchaseCache.OpenAttemptId);
            if (id == null || !Check(labelKey, value, userScope: false)) return;

            _logScheduleService.Enqueue(new FIapPurchaseLabelLog(NewParam(labelKey, value)) { purchaseAttemptId = id });
        }

        /// <summary>
        /// Nhãn chỉ dán được cho instance CÒN MỞ — không có id thì server không biết dán vào đâu,
        /// mà gửi lên để server vứt thì chỉ tốn băng thông và làm bẩn DQ.
        /// </summary>
        private static string RequireOpen(string labelKey, string what, string id)
        {
            if (!string.IsNullOrEmpty(id)) return id;

            AnalyticLogger.Instance.Warning(
                $"Nhãn '{labelKey}' bỏ qua: không có {what} nào đang mở. Nhãn instance chỉ dán được " +
                "khi instance CÒN MỞ — SDK không giữ id của thứ đã đóng.");
            return null;
        }

        private static LabelParam NewParam(string labelKey, object value)
        {
            return new LabelParam { labelKey = labelKey, labelValue = value };
        }

        private bool Check(string labelKey, object value, bool userScope)
        {
            LabelCheck check;
            lock (_lock)
            {
                // Nạp tập key user của phiên trước — trần 20 key của server đếm theo ĐỜI user,
                // van quên qua restart là phiên sau lách được trần rồi bị server drop im lặng.
                if (!_guardRestored)
                {
                    _guardRestored = true;
                    _guard.Restore(_dataPool.GetOrDefault<string[]>(PERSIST_USER_KEYS, null));
                }

                check = _guard.Check(labelKey, value, userScope);
                // Gỡ nhãn (null) cũng đổi tập key nên persist theo, không riêng lần thêm
                if (check == LabelCheck.Ok && userScope)
                    _dataPool.Compute<string[]>(PERSIST_USER_KEYS, _ => _guard.UserKeys);
            }

            switch (check)
            {
                case LabelCheck.BlankKey:
                    AnalyticLogger.Instance.Warning("Nhãn bỏ qua: labelKey rỗng.");
                    return false;

                case LabelCheck.ReservedKey:
                    AnalyticLogger.Instance.Warning(
                        $"Nhãn '{labelKey}' bỏ qua: trùng tên cột hợp đồng trên wire (§H1). Nhãn " +
                        "đứng cạnh cột thật trong dữ liệu — hai số cùng tên khác nghĩa là analyst " +
                        "đọc nhầm chắc chắn. Đặt tên khác, vd feature streak của game thì là " +
                        "'streak_status' chứ đừng 'win_streak'.");
                    return false;

                case LabelCheck.ValueTooLong:
                    AnalyticLogger.Instance.Warning(
                        $"Nhãn '{labelKey}' bỏ qua: value chữ dài quá {LabelGuardState.MAX_VALUE_LENGTH} " +
                        "ký tự — loader DROP thẳng bản tin này. Nhãn để LỌC, đoạn văn dài thì để " +
                        "extraMeta của event tương ứng.");
                    return false;

                case LabelCheck.TooManyUserKeys:
                    AnalyticLogger.Instance.Warning(
                        $"Nhãn user '{labelKey}' bỏ qua: đã chạm trần {LabelGuardState.MAX_USER_LABEL_KEYS} " +
                        "key/user do loader enforce (vượt là DROP + DQ đếm). Nhãn user theo profile " +
                        "vào MỌI dòng event của user đó — gỡ bớt key cũ (gán null) trước khi thêm mới.");
                    return false;

                default:
                    return true;
            }
        }
    }
}
