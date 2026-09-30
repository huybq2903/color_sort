/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using Falcon.Modules.Core.RemoteConfig;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class AnalyticConfig : IFalconConfig
    {
        public bool fCoreAnalyticShouldNotSendLogToServer;
        public bool fCoreAnalyticTesting;
        public string fCoreAnalyticTestingSingleUrl = "https://dwhapi-v2.data4game.com/fake/event-log-v2";
        public string fCoreAnalyticTestingBatchUrl = "https://dwhapi-v2.data4game.com/batch/fake/event-log-v2";
        public float fBatchLogSendSec = 15;

        /// <summary>
        /// Quay lại từ background mà nền ngắn hơn ngưỡng này (giây) thì KHÔNG log app_open —
        /// lọc nhiễu kiểu thoát ra 2 giây copy OTP / nghe điện thoại. Đặt 0 để log mọi lần quay lại.
        /// </summary>
        public int fAppOpenMinBackgroundSec = 30;

        /// <summary>
        ///     Sau khi offer đóng, giao dịch khớp sản phẩm của offer đó trong bao nhiêu giây nữa
        ///     thì vẫn được quy về offer (luồng bấm mua → popup đóng → callback giao dịch về muộn).
        ///     Đặt 0 để tắt cửa sổ ân hạn.
        /// </summary>
        public int fOfferAttributionGraceSec = 120;
    }
}