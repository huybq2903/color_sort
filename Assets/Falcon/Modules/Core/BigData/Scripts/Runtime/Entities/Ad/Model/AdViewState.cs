/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Ảnh chụp bộ tham số của lần xem ad hiện tại — bản SAO để game đọc; sửa vào đây không đụng
    /// tới state của SDK (SDK là nguồn chân lý duy nhất của vòng đời).
    /// </summary>
    [Serializable]
    public class AdViewSnapshot
    {
        public string adViewId;
        public AdType type;
        public string adWhere;
        public string adWhen;
        public string adMediation;

        /// <summary>Đã bao lâu kể từ lúc xin ad (ms); null nếu view tự sinh lúc xoay (không đi từ request nào).</summary>
        public int? sinceRequestMs;

        /// <summary>Đã bao lâu kể từ lúc ad lên hình (ms); null nếu chưa qua mốc show.</summary>
        public int? sinceShownMs;
    }

    /// <summary>
    /// Vòng đời MỘT LẦN XEM AD (xem EntityLifecycle-Design.md §4f): request → show → impression →
    /// close. Một <c>adViewId</c> duy nhất cho cả 4 mốc, sinh lúc request và xoay khi có request
    /// mới của cùng format — HOẶC khi show/impression chạm view đã có impression (mediation quên
    /// request đợt mới thì SDK tự xoay, khuôn play_turn_id — xem <see cref="StampImpression"/>;
    /// một id không bao giờ được tính tiền hai lần với non-banner). Request không bao giờ có
    /// impression mang id đó chính là view FILL-FAIL — không cần event fail riêng, sự vắng mặt
    /// đã là tín hiệu.
    /// <br/>Slot theo <see cref="AdType"/> vì mediation chỉ giữ MỘT ad object và MỘT load in-flight
    /// cho mỗi format (load → loaded/failed → show → closed → load lại); các format chạy song song.
    /// Nhờ vậy id tự xoay đúng lúc mà không cần ai gọi "đóng" tường minh.
    /// <br/>⚠ GIỚI HẠN ĐÃ XÁC NHẬN với loader (audit 12/08 mục 5): game chạy NHIỀU ad unit cùng
    /// format song song sẽ tráo id giữa hai unit (request B đè slot khi view A chưa xong → A thành
    /// fill-fail giả). Loader đã khai luật đọc "phễu fill per-view chỉ tin với game 1-unit/format";
    /// fix thật cần key theo adUnitId — chờ demand, vì mediation nội bộ hiện đúng 1 unit/format.
    /// <br/>Ngoài id, slot còn giữ bộ tham số bất biến của view (chỗ chiếu, ngữ cảnh, mediation) để
    /// các mốc sau khỏi nhập lại — cùng luật cache với entity level (§3).
    /// <br/>Mọi số đo thời lượng ở đây tính từ wall-clock CÓ CHỦ ĐÍCH (luật 3 của
    /// <c>MonotonicClock</c>): vòng đời một view VẮT QUA pause của chính game — trên Android, chiếu
    /// inter/rewarded tự nó là một lần pause — mà đồng hồ đơn điệu thì đóng băng khi máy ngủ, dùng
    /// nó là đếm thiếu. Nhiễu chỉnh-giờ chấp nhận được: số per-instance, clamp ≥ 0 sẵn, server
    /// đối chiếu được bằng hiệu timestamp hai event. ĐỪNG "sửa" sang monotonic.
    /// <br/>Pure class — không IO/DI/time, caller truyền mốc thời gian vào. KHÔNG tự lo
    /// thread-safety: <see cref="AdRequestService"/> khoá giúp, vì bên ghi (callback mediation) và
    /// bên đọc (pipeline decor lúc dựng log impression) không đảm bảo cùng thread.
    /// </summary>
    public class AdViewState
    {
        private class AdView
        {
            public string Id;

            /// <summary>0 = view TỰ SINH lúc xoay, không đi từ request nào — mọi số đo tính từ mốc request phải null thay vì bịa.</summary>
            public long RequestedAtMillis;
            public long ShownAtMillis;
            public string Where;
            public string When;
            public string Mediation;
            public bool Clicked;

            /// <summary>View đã bị impression TIÊU THỤ — mốc show/impression kế tiếp chạm vào là dấu hiệu thiếu request mới, phải xoay id (án dup 5% loader báo 03/09).</summary>
            public bool ImpressionStamped;
        }

        private readonly Dictionary<AdType, AdView> _viewByType = new();

        /// <summary>
        /// Mở vòng đời một lần xem ad: sinh <see cref="AAdLog.adViewId"/> và giữ vào slot
        /// của format. View cũ cùng format (nếu có) coi như kết thúc — nó hoặc đã xem xong rồi
        /// (mediation load lại ngay sau khi đóng ad), hoặc chưa từng lên hình (fill-fail).
        /// </summary>
        /// <returns>adViewId vừa sinh — caller gắn lên log (id là của SDK nên không nằm trong param).</returns>
        public string Open(AdViewParam param, long nowMillis)
        {
            var id = Guid.NewGuid().ToString();
            _viewByType[param.type] = new AdView
            {
                Id = id,
                RequestedAtMillis = nowMillis,
                Where = param.adWhere,
                When = param.adWhen,
                Mediation = param.adMediation
            };
            return id;
        }

        /// <summary>
        /// Id của lần xem ad hiện tại cho format này — hàm ĐỌC thuần, không tiêu thụ, không xoay.
        /// Mốc impression của non-banner đi qua <see cref="StampImpression"/> (có tiêu thụ),
        /// không đọc qua đây.
        /// <br/>Trả null nếu format này chưa từng đi qua request (vd đường load nằm ngoài SDK).
        /// <br/>Ca banner tự refresh trong SDK: các impression sau dùng CÙNG id với request đã tạo
        /// ra banner đó — đúng ngữ nghĩa "một banner instance đẻ N impression", bản tin cụm mang
        /// <c>impressionCount</c> nên server đọc id này là id INSTANCE, không phải id lần chiếu.
        /// </summary>
        public string CurrentId(AdType type)
        {
            return _viewByType.GetValueOrDefault(type)?.Id;
        }

        /// <summary>
        /// Đóng dấu impression lên view hiện tại — mốc TIÊU THỤ duy nhất của vòng đời (non-banner).
        /// Impression ĐẦU dùng id của request (phễu fill nguyên vẹn). Impression THỨ HAI trở đi
        /// trên cùng view = mediation thiếu OnRequested cho đợt mới → SDK TỰ XOAY id (khuôn
        /// play_turn_id, chốt owner 03/09: người dùng không mở lượt mới thì tự sinh — thà mất mối
        /// nối request còn hơn một id bị tính tiền N lần, đúng án dup 5% loader báo). View xoay
        /// giữ context (chỗ chiếu/ngữ cảnh/mediation là dữ liệu thật của format), nhưng
        /// fill-latency null (không đi từ request nào — không bịa) và cờ click đời trước không lây.
        /// <br/>Banner KHÔNG BAO GIỜ xoay: id banner là id instance theo hợp đồng — N impression
        /// chung id là đúng thiết kế, xoay là phá ngữ nghĩa cụm.
        /// </summary>
        /// <returns>(adViewId, fillLatencyMs, rotated) — id null nếu format chưa từng có request; caller cảnh báo khi rotated.</returns>
        public (string adViewId, int? fillLatencyMs, bool rotated) StampImpression(AdType type, long nowMillis)
        {
            if (!_viewByType.TryGetValue(type, out var view)) return (null, null, false);

            if (view.ImpressionStamped && type != AdType.Banner)
            {
                RotateConsumedView(view);
                view.ImpressionStamped = true;
                return (view.Id, null, true);
            }

            view.ImpressionStamped = true;
            return (view.Id, MillisSince(view.RequestedAtMillis, nowMillis), false);
        }

        /// <summary>
        /// Xoay view đã tiêu thụ thành view tự sinh cho lần hiển thị mới: id mới, mọi mốc thời
        /// gian về 0 (số đo từ request/show của đời trước không thuộc về đời này), cờ click sạch.
        /// Context giữ nguyên — nó là thuộc tính của format đang chạy, không phải của một lần chiếu.
        /// </summary>
        private static void RotateConsumedView(AdView view)
        {
            view.Id = Guid.NewGuid().ToString();
            view.RequestedAtMillis = 0;
            view.ShownAtMillis = 0;
            view.Clicked = false;
            view.ImpressionStamped = false;
        }

        /// <summary>Số ms từ một mốc wall-clock — null nếu mốc không tồn tại (view tự sinh), không bịa 0.</summary>
        private static int? MillisSince(long originMillis, long nowMillis)
        {
            if (originMillis <= 0) return null;
            return (int)Math.Max(0, nowMillis - originMillis);
        }

        /// <summary>
        /// Ghi tham số cho lần xem ad hiện tại của format này — chỉ đối số khác rỗng mới ghi đè.
        /// Preload thì lúc xin ad chưa biết sẽ chiếu ở đâu / trong ngữ cảnh nào: biết lúc nào ghi
        /// lúc đó, các mốc sau (show / impression / close) tự mang theo.
        /// </summary>
        /// <returns>false nếu format này chưa từng đi qua request (không có view nào để ghi).</returns>
        public bool SetContext(AdType type, string adWhere = null, string adWhen = null, string adMediation = null)
        {
            if (!_viewByType.TryGetValue(type, out var view)) return false;
            if (!string.IsNullOrEmpty(adWhere)) view.Where = adWhere;
            if (!string.IsNullOrEmpty(adWhen)) view.When = adWhen;
            if (!string.IsNullOrEmpty(adMediation)) view.Mediation = adMediation;
            return true;
        }

        /// <summary>
        /// Ảnh chụp lần xem ad hiện tại của format này (null nếu chưa từng có request) — bản sao,
        /// game sửa vào đó không ảnh hưởng state.
        /// </summary>
        public AdViewSnapshot TakeSnapshot(AdType type, long nowMillis)
        {
            if (!_viewByType.TryGetValue(type, out var view)) return null;
            return new AdViewSnapshot
            {
                adViewId = view.Id,
                type = type,
                adWhere = view.Where,
                adWhen = view.When,
                adMediation = view.Mediation,
                sinceRequestMs = MillisSince(view.RequestedAtMillis, nowMillis),
                sinceShownMs = MillisSince(view.ShownAtMillis, nowMillis)
            };
        }

        /// <summary>
        /// Vị trí chiếu đã biết của lần xem ad hiện tại (null nếu chưa ai ghi) — dùng điền cho log
        /// impression do mediation bắn.
        /// </summary>
        public string CurrentPlacement(AdType type)
        {
            return _viewByType.GetValueOrDefault(type)?.Where;
        }

        /// <summary>Ngữ cảnh kích hoạt đã biết của lần xem ad hiện tại (null nếu chưa ai ghi).</summary>
        public string CurrentContext(AdType type)
        {
            return _viewByType.GetValueOrDefault(type)?.When;
        }

        /// <summary>
        /// Đã bao lâu kể từ lúc xin ad cho format này (ms) — dùng làm <c>fillLatencyMs</c> trên log
        /// impression (§D2). Null nếu impression không đi từ request nào của SDK (không bịa 0).
        /// </summary>
        public int? MillisSinceRequest(AdType type, long nowMillis)
        {
            if (!_viewByType.TryGetValue(type, out var view)) return null;
            return MillisSince(view.RequestedAtMillis, nowMillis);
        }

        /// <summary>
        /// Đánh dấu mốc gọi hiển thị và đồng bộ ngữ cảnh vào param.
        /// <br/>Show chạm view ĐÃ CÓ impression (non-banner) = lần hiển thị MỚI mà mediation quên
        /// OnRequested → xoay id ngay tại đây (cùng án với <see cref="StampImpression"/> — xoay ở
        /// show thì cả bộ show/impression/close của lần chiếu mới đi chung id mới, không nửa nạc).
        /// <br/>Thời gian chờ để null nếu lần show này không đi từ request nào của SDK (không bịa 0).
        /// </summary>
        /// <returns>(adViewId, requestToShowMs, rotated) — id null nếu format chưa từng có request; caller cảnh báo khi rotated.</returns>
        public (string adViewId, int? requestToShowMs, bool rotated) MarkShown(AdShowParam param, long nowMillis)
        {
            if (!_viewByType.TryGetValue(param.type, out var view)) return (null, null, false);

            var rotated = view.ImpressionStamped && param.type != AdType.Banner;
            if (rotated) RotateConsumedView(view);

            view.ShownAtMillis = nowMillis;
            ApplyContext(param, view);
            return (view.Id, MillisSince(view.RequestedAtMillis, nowMillis), rotated);
        }

        /// <summary>
        /// Đánh dấu người chơi bấm vào lần xem ad hiện tại của format này. Cờ nằm trong view nên
        /// request mới tự sạch; bấm nhiều lần vẫn là một cờ.
        /// </summary>
        /// <returns>false nếu format này chưa có view nào (click không thuộc về ai — caller cảnh báo).</returns>
        public bool MarkClicked(AdType type)
        {
            if (!_viewByType.TryGetValue(type, out var view)) return false;
            view.Clicked = true;
            return true;
        }

        /// <summary>
        /// Đánh dấu mốc đóng và đồng bộ ngữ cảnh vào param.
        /// <br/>Thời lượng để null nếu chưa từng qua mốc show.
        /// <br/><c>hasClick</c>: caller tự nhập thì giá trị đó thắng; bỏ trống thì lấy CẢ HAI CHIỀU
        /// từ cờ <see cref="MarkClicked"/> — chưa thấy click thì gửi <c>false</c> TƯỜNG MINH (sửa
        /// 23/09 theo đo của loader: 0 dòng nào mang false, nên bên đọc số phải tự COALESCE và
        /// không phân biệt được "không bấm" với "không biết"). Mediation đã nối OnClicked ở mọi
        /// luồng nên cờ này là số đo thật, không phải suy bừa.
        /// </summary>
        /// <returns>(adViewId, shownDurationSec) — thời lượng null nếu chưa từng qua mốc show.</returns>
        public (string adViewId, int? shownDurationSec) MarkClosed(AdCloseParam param, long nowMillis)
        {
            if (!_viewByType.TryGetValue(param.type, out var view)) return (null, null);

            ApplyContext(param, view);
            param.hasClick ??= view.Clicked;
            return (view.Id,
                view.ShownAtMillis > 0 ? (int?)Math.Max(0, (nowMillis - view.ShownAtMillis) / 1000) : null);
        }

        /// <summary>
        /// Đồng bộ chỗ chiếu / ngữ cảnh / mediation giữa param và cache — HAI CHIỀU: mốc nào bỏ
        /// trống thì lấy từ cache, mốc nào tự nhập thì giá trị đó thắng VÀ cập nhật cache cho các
        /// mốc sau (preload không biết chỗ chiếu, lúc show mới biết — tin mới nhất là tin đúng nhất).
        /// <br/>Public vì log impression đi bằng <c>FAdLog</c> chứ không qua họ AdViewParam, nên
        /// nó phải gọi từ decor — nhưng LUẬT chỉ có một bản, ở đây.
        /// </summary>
        public void ApplyContext(AdViewParam param, AdType type)
        {
            if (_viewByType.TryGetValue(type, out var view)) ApplyContext(param, view);
        }

        private static void ApplyContext(AdViewParam param, AdView view)
        {
            param.adWhere = Merge(param.adWhere, ref view.Where);
            param.adWhen = Merge(param.adWhen, ref view.When);
            param.adMediation = Merge(param.adMediation, ref view.Mediation);
        }

        /// <summary>
        /// "Unknown" tính là TRỐNG: <c>AdParam.CorrectValues</c> điền sẵn giá trị đó cho mốc
        /// impression trước khi decor chạy, không coi là trống thì cache sẽ bị ghi đè bằng
        /// "Unknown" và mất luôn chỗ chiếu mà game đã báo lúc request.
        /// </summary>
        private static string Merge(string value, ref string cached)
        {
            if (string.IsNullOrEmpty(value) || value == FParam.UNKNOWN) return cached ?? value;
            cached = value;
            return value;
        }
    }
}
