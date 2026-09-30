/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-20
 */

using System.Collections.Generic;
using Falcon.Modules.Core.AccountData;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    [FGameDataType("iap_data")]
    public class FInAppData : FGameData<FInAppData>
    {
        /// <summary>
        /// Lưu trữ data các giá local
        /// </summary>
        public Dictionary<string, LocalizedData> localizeData = new();
        /// <summary>
        /// Lần giao dịch đầu và cuối
        /// </summary>
        public RecordData firstRecord, lastRecord;
        
        /// <summary>
        /// Dữ liệu về các localize giá trong quá trình giao dịch
        /// </summary>
        public class LocalizedData
        {
            /// <summary>
            /// Số giao dịch đã thành công
            /// </summary>
            public int count;
            /// <summary>
            /// Mã localize giá
            /// </summary>
            public string isoCurrencyCode;
            /// <summary>
            /// Lượng giao dịch cao nhất ở giá local này
            /// </summary>
            public decimal max;
            /// <summary>
            /// Tổng lượng giao dịch của giá local này
            /// </summary>
            public decimal total;
        }

        /// <summary>
        /// Thông tin lưu được khi xảy ra 1 giao dịch
        /// </summary>
        public class RecordData
        {
            /// <summary>
            /// Giao dịch khi đang ở level cao nhất đã vượt qua
            /// </summary>
            public int level;
            /// <summary>
            /// Thời điểm giao dịch theo UTC
            /// </summary>
            public long timestamp; //milliseconds
            /// <summary>
            /// Tên produt id giao dịch
            /// </summary>
            public string productId;
        }
    }
}