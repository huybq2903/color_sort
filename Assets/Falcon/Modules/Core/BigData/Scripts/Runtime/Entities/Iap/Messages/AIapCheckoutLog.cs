/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-11
 */

using System;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Gốc chung của ba mốc trong PHỄU CHECKOUT: mở lượt mua → thất bại / mua xong (§D4).
    /// <br/>Giữ <see cref="purchaseAttemptId"/> ở đây thay vì khai trong param: id do SDK sinh, game
    /// không set — cùng lý do với <see cref="AAdLog.adViewId"/>.
    /// </summary>
    [Serializable]
    public abstract class AIapCheckoutLog : AFalconLog
    {
        /// <summary>
        /// Id của lượt mua này — SDK sinh lúc mở billing flow rồi đóng dấu lên cả nhánh thất bại
        /// lẫn nhánh mua xong (khớp theo productId), nhờ vậy server ghép phễu chính xác và đo được
        /// time-to-complete.
        /// <br/>Vắng mặt khi mốc này không đi từ một lượt mua nào của SDK — vd app bị kill giữa
        /// dialog rồi callback mới về ở phiên sau (state không persist qua process, §A3.4).
        /// </summary>
        [FKey(RemoveIfNull = true)] public string purchaseAttemptId;
    }
}
