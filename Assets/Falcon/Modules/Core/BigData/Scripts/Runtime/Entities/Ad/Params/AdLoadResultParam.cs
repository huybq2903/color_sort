/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-18
 */

using System;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Kết quả MỘT lần gọi load của MỘT ad unit — input cho <see cref="FAdApi.OnLoadResult"/>.
    /// KHÔNG lên wire trực tiếp: SDK gộp cụm (<see cref="AdLoadStatsService"/>) rồi bắn
    /// bản tin tổng <c>f_sdk_ad_load_success</c>/<c>f_sdk_ad_load_fail</c> — mediation cứ báo
    /// từng attempt, volume SDK lo.
    /// <br/>Game KHÔNG chạy multi-floor (máy yếu/3G tắt MultiCall, hoặc chỉ 1 unit) vẫn dùng
    /// param này — <see cref="floor"/> để null là xong; kết quả load của một unit đơn vẫn là
    /// kết quả load, và chính nhóm máy yếu/3G là nơi telemetry fill/timeout giá trị nhất.
    /// <br/>Thay thế MAdLog/f_sdk_mo_multiple_floor_adunit cũ (đã bị CHẶN trên server vì spam
    /// per-attempt). Bản mới cũng cắt các field double-coverage: revenue/displayed/paid đã có
    /// f_sdk_ads_data lo, display-fail đi <see cref="FAdApi.OnShowFailed"/>.
    /// </summary>
    [Serializable]
    public class AdLoadResultParam
    {
        public AdType adType;

        /// <summary>
        /// Load thành công hay thất bại — quyết định EVENT tại client (success/fail là hai id
        /// riêng nên field này không lên wire). Default <c>false</c>: quên set ở callback loaded
        /// là success bị đếm nhầm thành fail — set TƯỜNG MINH ở cả hai nhánh như doc mẫu.
        /// </summary>
        public bool success;

        [CanBeNull] public string adUnitId;

        /// <summary>
        /// BẬC giá sàn của unit — hệ số/nhãn thứ tự trong config (vd 1.5 / 3.0 / 5.0), MỘT nghĩa
        /// cho mọi mediation. Đây là thứ mediation nào cũng BIẾT THẬT, nên là trục chính để so
        /// bậc nào fill tốt hơn.
        /// <br/>⚠ ĐỪNG nhân bậc với doanh thu đo được rồi nhét vào <see cref="floor"/>: nền nhân
        /// là giá của ad vừa xem nên cùng một bậc ra số khác nhau theo máy/theo ngày, và đầu phiên
        /// ra 0 vì chưa có ad nào làm nền (đúng bài học của công thức cũ bên MAX, loader đo 22/09).
        /// <br/>Kiểu <c>double</c> chứ không float để "1.2" in lại đúng "1.2" (float box lên double
        /// là 1.2000000476… — bậc tự tách đôi trên wire). Parse từ config bằng
        /// <c>CultureInfo.InvariantCulture</c> — culture vi-VN đảo dấu thập phân, "1.2" thành 12.
        /// <br/><b>Unit KHÔNG đặt giá sàn thì để null</b> — key biến mất khỏi payload. ĐỪNG điền
        /// <c>0</c> cho gọn: vắng-key là thứ DUY NHẤT phân biệt "không đặt sàn" với "đặt sàn 0"
        /// (điều kiện loader đặt 23/09; chẩn đoán nhóm AdMob-không-đặt-floor đứng trên đúng nó).
        /// </summary>
        public double? tier;

        /// <summary>
        /// Giá sàn THẬT đã đặt cho unit — USD trên 1000 lượt hiển thị (eCPM).
        /// <br/>CHỈ mediation nào biết con số thật mới điền (AdMob tự đặt sàn từng slot nên biết);
        /// mediation không biết (MAX — giá sàn nằm trên dashboard, client chỉ thấy bậc) thì
        /// <b>để null</b>. Null ở đây nghĩa là "KHÔNG BIẾT", khác với null của <see cref="tier"/>
        /// nghĩa là "không đặt sàn".
        /// <br/>⚠ TUYỆT ĐỐI không suy ra bằng cách nhân <see cref="tier"/> với doanh thu đo được —
        /// đó là số bịa (xem ghi chú ở <see cref="tier"/>).
        /// </summary>
        public double? floor;

        /// <summary>Network thắng — chỉ biết khi <see cref="success"/>.</summary>
        [CanBeNull] public string networkName;

        /// <summary>
        /// Mediation của unit này ("Max" / "Admob"…). Loader 23/09 gọi đây là nút thắt: thiếu nó
        /// họ phải suy mediation bằng prefix của <c>adUnitId</c>, chỉ tách được AdMob với "phần
        /// còn lại" — mà đọc <see cref="floor"/> thì BẮT BUỘC group theo mediation.
        /// </summary>
        [CanBeNull] public string adMediation;

        /// <summary>Error message của lần fail này — cụm giữ bản CUỐI, không làm khoá gộp.</summary>
        [CanBeNull] public string errorMess;

        /// <summary>
        /// Thời gian load của CHÍNH attempt này (ms) — cụm cộng dồn thành <c>totalLoadingMs</c>
        /// (cũng <c>long</c>), server chia <c>count</c> ra thời gian load trung bình. MAX đo hộ:
        /// <c>adInfo.LatencyMillis</c> (loaded) / <c>errorInfo.WaterfallInfo?.LatencyMillis</c>
        /// (fail) — hai cái đó là <c>long</c> nên gán thẳng, khỏi cast; không có số thì để 0 —
        /// cộng dồn 0 không làm bẩn tổng.
        /// </summary>
        public long loadingMs;
    }
}
