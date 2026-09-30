/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// GÁN NHÃN — <c>FalconBigDataController.Label</c> (§B3 + §D11). Hai TRỤC, đọc tên hàm là biết:
    /// <br/>· Trục LẦN (instance đang mở, mỗi hàm một event nhãn): <c>User</c> · <c>Session</c> ·
    /// <c>LevelPlayTurn</c> · <c>AdView(type)</c> · <c>IapOfferImpression</c> · <c>IapPurchase</c>.
    /// <br/>· Trục VẬT (bản thiết kế, không bắn event — đi ké bundle): <c>Level(id)</c> ·
    /// <c>IapOffer(offerId)</c>.
    /// <code>
    /// Label.User("vip_tier", "gold");            // theo người chơi, từ mốc này trở đi
    /// Label.User("is_whale", true);              // giữ nguyên kiểu bool, không ép về chữ
    /// Label.User("vip_tier", null);              // null = GỠ nhãn
    /// Label.LevelPlayTurn("turn_type", "tutorial");  // theo LƯỢT CHƠI đang mở
    /// Label.Level(42, "tier", "B");                  // theo BẢN THIẾT KẾ màn
    /// Label.IapOffer("bf_2026", "theme", "dark");    // theo BẢN THIẾT KẾ offer
    /// </code>
    /// <br/>Một cửa cho cả "nhãn" lẫn "tham số động" — dev không phải phân biệt hai thứ đó.
    /// Key TỰ DO, không phải đăng ký trước.
    /// <br/>⚠ Van do loader enforce, vượt là server DROP: ≤<see cref="LabelGuardState.MAX_USER_LABEL_KEYS"/>
    /// key trên một user, value chữ ≤<see cref="LabelGuardState.MAX_VALUE_LENGTH"/> ký tự. SDK chặn
    /// sớm ở client kèm cảnh báo để khỏi mất dữ liệu trong im lặng.
    /// </summary>
    public class FLabelApi
    {
        private readonly LabelLogService _labelLogService;
        private readonly EntityLabelService _entityLabelService;

        internal FLabelApi(LabelLogService labelLogService, EntityLabelService entityLabelService)
        {
            _labelLogService = labelLogService;
            _entityLabelService = entityLabelService;
        }

        /// <summary>
        /// Nhãn cho chính NGƯỜI CHƠI — vd "vip", "care_group", cohort onboarding.
        /// <br/>Hiệu lực **từ mốc gán trở đi**: server nạp vào profile rồi mọi event SAU đó tự mang
        /// nhãn, còn dữ liệu quá khứ không bị nhuộm ngược. Agg theo nhãn là một câu GROUP BY.
        /// <br/><paramref name="value"/> giữ nguyên kiểu (chữ/số/bool); <c>null</c> = GỠ nhãn.
        /// </summary>
        public void User(string labelKey, object value)
        {
            _labelLogService.LabelUser(labelKey, value);
        }

        /// <summary>Nhãn cho PHIÊN đang mở — vd "phiên test nội bộ".</summary>
        public void Session(string labelKey, object value)
        {
            _labelLogService.LabelSession(labelKey, value);
        }

        /// <summary>
        /// Nhãn cho MỘT LƯỢT CHƠI LEVEL đang mở — vd "ván tutorial", "ván có dùng revive".
        /// Chỉ dán được khi còn lượt mở; không có thì SDK cảnh báo và bỏ qua.
        /// <br/>Đây là nhãn của <b>một lần chơi</b>, không phải của màn. Thuộc tính của MÀN
        /// (thiết kế tay, chương mấy, tier độ khó) thì dùng <see cref="Level"/> — xem lý do ở đó.
        /// </summary>
        public void LevelPlayTurn(string labelKey, object value)
        {
            _labelLogService.LabelLevelPlayTurn(labelKey, value);
        }

        /// <summary>
        /// Nhãn cho LẦN XEM AD đang mở của format này — vd đánh dấu ad thuộc một thử nghiệm bidding.
        /// <br/>⚠ Event <c>f_sdk_ad_view_label</c> đã có trong hợp đồng §D11 (bản 11/08) nhưng còn
        /// chờ loader mở mapping rule.
        /// <br/>Cân nhắc trước khi dùng: một lần xem ad sống ~30 giây, nhãn thường thuộc về PHIÊN
        /// hoặc NGƯỜI CHƠI đúng hơn (cả nhóm ad của user đó cùng nằm trong thử nghiệm).
        /// </summary>
        public void AdView(AdType type, string labelKey, object value)
        {
            _labelLogService.LabelAdView(type, labelKey, value);
        }

        /// <summary>
        /// Nhãn cho LẦN HIỂN THỊ OFFER đang mở (impression) — nhãn của MỘT LẦN, không phải của
        /// bản thiết kế offer; cái đó là <see cref="IapOffer"/>.
        /// <inheritdoc cref="AdView"/>
        /// </summary>
        public void IapOfferImpression(string labelKey, object value)
        {
            _labelLogService.LabelIapOffer(labelKey, value);
        }

        /// <summary>
        /// Nhãn cho LƯỢT MUA đang mở.
        /// <inheritdoc cref="AdView"/>
        /// </summary>
        public void IapPurchase(string labelKey, object value)
        {
            _labelLogService.LabelIapPurchase(labelKey, value);
        }

        /// <summary>
        /// Nhãn cho BẢN THIẾT KẾ MÀN (khoá theo số màn) — vd "cụm khó", cờ tune tay, tier.
        /// Cặp VẬT/LẦN với <see cref="LevelPlayTurn"/>: cái kia tả một lần chơi, cái này tả thiết
        /// kế — đúng cho mọi lần chơi màn đó.
        /// <br/><b>KHÔNG bắn bản tin nào</b>: giá trị vào kho nhãn của SDK, rồi SDK-core đóng dấu
        /// cả bundle (<c>levelLabels</c>) lên MỌI level event của màn đó — y khuôn <c>playTurnId</c>
        /// đi ké mọi log. Gọi bao nhiêu lần cũng không tốn bản tin; gọi lúc load màn là hợp lý nhất.
        /// <br/><paramref name="value"/> = null là GỠ nhãn (thôi kèm key từ lúc này).
        /// <br/>⚠ Bundle đi kèm stream dày nhất hệ nên có trần
        /// <see cref="EntityLabelState.MAX_BUNDLE_BYTES"/>B sau serialize — vượt thì SDK cảnh báo
        /// kèm số cụ thể và GIỮ NGUYÊN bundle cũ (thà thiếu nhãn mới còn hơn để server vứt cả bundle).
        /// <br/><br/>Đừng gửi qua đây thứ đã có kênh riêng: <c>difficulty</c> (đã ở level event),
        /// <c>currentLevelId</c> (đổi tune = id mới). Nhãn để dành cho thứ CHƯA có chỗ.
        /// <br/>Riêng giới hạn lượt đi / thời gian của màn thì ĐÚNG là nhãn màn, nhưng có tên hợp
        /// đồng riêng (<see cref="FLevelLabelKey"/>) và có đường vào riêng —
        /// <c>Level.OnStart(movesLimit:, timeLimitSec:)</c> hoặc <c>Level.SetLevelConfig(...)</c>.
        /// Gõ tay tên khác là mất khả năng so chéo game.
        /// <br/><br/>Vì sao đáng dùng: đánh lại màn 20 lần thì nhãn lượt bắn 20 bản tin cho cùng
        /// một sự thật; nhãn màn đi ké, không thêm bản tin nào — và đáp xuống bridge/level_daily
        /// nên hỏi "màn tune tay ăn bao nhiêu doanh thu" là đọc thẳng, không phải gom ngược từ hộp turn.
        /// </summary>
        public void Level(int levelId, string labelKey, object value)
        {
            _entityLabelService.Set(LabelEntityKind.Level, levelId.ToString(), labelKey, value);
        }

        /// <summary>
        /// Nhãn cho BẢN THIẾT KẾ OFFER (khoá theo <c>offerId</c>, KHÔNG phải theo lần hiển thị) —
        /// vd theme mùa vụ, tier. Cặp VẬT/LẦN với <see cref="IapOfferImpression"/> y như
        /// <see cref="Level"/> cặp với <see cref="LevelPlayTurn"/>.
        /// <code>
        /// Label.IapOffer("black_friday_gem", "tier", "premium");
        /// </code>
        /// Bundle đi field <c>offerLabels</c> trên chính event offer — cùng cơ chế, cùng trần với
        /// <see cref="Level"/> (xem doc bên đó).
        /// <br/>⚠ <c>offerLabels</c> NGOÀI hợp đồng §D11 đợt 1 — chờ loader khai mapping.
        /// </summary>
        public void IapOffer(string offerId, string labelKey, object value)
        {
            _entityLabelService.Set(LabelEntityKind.Offer, offerId, labelKey, value);
        }
    }
}
