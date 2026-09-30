/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Vocab <see cref="ResourceParam.resourceWhen"/> — <b>VÌ SAO</b> tài nguyên này phát sinh.
    /// Phía server field này đáp vào <c>event_when</c>.
    /// <br/>Cùng khuôn <see cref="AdViewParam.adWhen"/> và <c>InAppParam.iapWhen</c> vốn có: trong
    /// SDK này <c>*When</c> nghĩa là <b>ngữ cảnh KÍCH HOẠT</b> chứ không phải mốc đồng hồ —
    /// <c>adWhen = "level_fail"</c> là "ad này nổ VÌ thua màn", không phải "ad nổ lúc 20:03".
    /// <br/>Tên feature trùng khít <see cref="FFunnelName"/> tương ứng để join được hai mặt của
    /// cùng một tính năng: tiến độ (funnel) ↔ ví thưởng (resource).
    /// <br/><b>Thêm giá trị = SỬA HỢP ĐỒNG</b> (§H2 cấm string gõ tay).
    ///
    /// <para><b>Phân biệt với hai thứ dễ nhầm:</b></para>
    /// <list type="bullet">
    /// <item><see cref="ResourceParam.resourceWhere"/> = màn/panel nào — <b>vị trí</b>, vocab riêng
    /// của từng game, SDK không khai hằng số.</item>
    /// <item><c>playTurnId</c> / <c>adViewId</c> mà SDK đóng tự động = <b>ĐỒNG THỜI</b>, không phải
    /// nguyên nhân. Mua gói trong shop giữa lúc đang chơi màn thì log vẫn mang playTurnId, nhưng
    /// tài nguyên đó KHÔNG do màn sinh ra. Muốn nói nguyên nhân thì nói ở đây.</item>
    /// </list>
    /// </summary>
    public static class FResourceWhen
    {
        /// <summary>Đổi/mua trong kệ shop.</summary>
        public const string Shop = "shop";

        /// <summary>
        /// Vế nhận của giao dịch đổi bằng TIỀN THẬT — DỜI từ itemType sang đây theo phán quyết
        /// loader vòng 18/08 (nó tả NGUYÊN NHÂN phát sinh, không phải loại vật phẩm; class
        /// FResourceItemType đã xoá). Loader đã sửa công thức buy/receive đọc 2 era
        /// (item_type='iap' cho data cũ, event_when='iap' từ nay) — client chỉ việc gửi đúng
        /// chỗ mới, không migration gì.
        /// </summary>
        public const string Iap = "iap";

        public const string BattlePass = "battle_pass";
        public const string DailyQuest = "daily_quest";
        public const string PiggyBank = "piggy_bank";
        public const string LuckyWheel = "lucky_wheel";

        /// <summary>Quà đăng nhập theo ngày.</summary>
        public const string DailyBonus = "daily_bonus";

        /// <summary>Thưởng phát ra VÌ hoàn thành level.</summary>
        public const string LevelReward = "level_reward";

        /// <summary>Thưởng/quà không thuộc nguyên nhân nào ở trên — catch-all.</summary>
        public const string Reward = "reward";

        /// <inheritdoc cref="Reward"/>
        public const string Gift = "gift";

        /// <summary>
        /// Tiền tố cho liveops per-game: <c>FResourceWhen.EventPrefix + "halloween_2026"</c>.
        /// Cùng khuôn <see cref="FFunnelName.EventPrefix"/> — phần sau tiền tố là vocab MỞ của
        /// từng game nên không khai hằng số được.
        /// </summary>
        public const string EventPrefix = "event_";
    }
}
