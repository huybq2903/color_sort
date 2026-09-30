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
    /// Vocab <c>placement</c> của hợp đồng §D9 — "CỬA THU TIỀN nào" — dùng cho
    /// <see cref="IapPurchaseAttemptParam.where"/> trên cả ba mốc phễu checkout (mở / thất bại /
    /// mua xong). Phía server field này đáp vào <c>event_where</c>.
    /// <br/>Bảng giá trị do loader ký 2026-08-12. <b>Thêm giá trị = SỬA HỢP ĐỒNG</b>, không phải
    /// việc client tự quyết (§H2 cấm string gõ tay): mỗi game một kiểu thì cross-game mù.
    /// </summary>
    public static class FIapPlacement
    {
        /// <summary>Mua trong màn hình battle pass — trùng tên với funnel và itemType tương ứng để join được.</summary>
        public const string BattlePass = "battle_pass";

        /// <inheritdoc cref="BattlePass"/>
        public const string PiggyBank = "piggy_bank";

        /// <summary>Kệ shop của một liveops event.</summary>
        public const string EventShop = "event_shop";

        /// <summary>Gói khởi đầu chào người mới.</summary>
        public const string StarterPack = "starter_pack";

        /// <summary>Gói gỡ quảng cáo.</summary>
        public const string RemoveAds = "remove_ads";

        /// <summary>Kệ shop thường.</summary>
        public const string Shop = "shop";

        /// <summary>Gói bán chuỗi Endless Treasure (popup + icon Lobby). Chờ loader ký.</summary>
        public const string EndlessTreasure = "endless_treasure";
    }
}
