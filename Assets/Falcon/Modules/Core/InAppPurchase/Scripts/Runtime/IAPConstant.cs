/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Các hằng số và đường dẫn dùng chung cho module IAP.
    /// </summary>
    public class IAPConstant
    {
        public const string NAME_CONFIG = "SO_FCM_InAppPurchase_Config";
        public const string PATH_CONFIG = "Assets/FalconAssets/Modules/Core/InAppPurchase/Resources";

        public const string DEFAULT_PRICE = "0.01$";

        public const string EVENT_GET_LOCALIZED_PRICE = "falcon.modules.iap.get_localized_price";
        public const string EVENT_PURCHASE = "falcon.modules.iap.purchase";
        public const string EVENT_START_PURCHASE = "falcon.modules.iap.start_purchase";
        public const string EVENT_PURCHASE_SUCCESS = "falcon.modules.iap.purchase_success";
        public const string EVENT_PURCHASE_SUCCESS_USD = "falcon.modules.iap.purchase_success.usd";
        public const string EVENT_PURCHASE_FAIL = "falcon.modules.iap.purchase_fail";
        public const string EVENT_PURCHASE_TIME_OUT = "falcon.modules.iap.purchase_time_out";
        public const string EVENT_GET_LTV = "falcon.modules.iap.get_ltv";
    }

    public enum RestoreResult
    {
        SUCCESS,     // Khôi phục thành công ít nhất 1 món đồ
        NO_PURCHASE,  // Không có giao dịch để khôi phục
        FAILED       // Lỗi kết nối hoặc người dùng bấm Cancel
    }
}