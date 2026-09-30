/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */
// ReSharper disable once CheckNamespace

namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Phần CỘNG ĐƯỢC của một cụm impression banner. Cố tình không giữ adLtv: LTV là sổ cái do
    /// SDK (hoặc module Mediation) tự cộng, nhận từ ngoài vào là claim không bằng chứng (§H3).
    /// </summary>
    public struct BannerValue
    {
        public double AdRev { get; private set; }

        public int ImpressionCount { get; private set; }

        /// <summary>
        /// Id banner instance của cụm — CHỈ khác null khi mọi impression trong cụm cùng một
        /// instance. Cụm vắt qua ≥2 instance (banner bị huỷ rồi load lại giữa hai lần chốt, mà
        /// <see cref="BannerKey"/> không phân biệt) thì để trống: dán id của instance này lên
        /// impression của instance kia là bịa.
        /// <br/>Nhờ vậy dòng gộp vẫn đếm được VIEW THẬT ở ca phổ biến (banner refresh trong cùng
        /// một instance) mà không phải chẻ nhỏ cụm — chẻ theo id là mở lại đúng cái hố volume mà
        /// việc gộp sinh ra để lấp.
        /// </summary>
        public string AdViewId { get; private set; }

        private bool _hasImpression;
        private bool _mixedInstances;

        public BannerValue(double adRev, int impressionCount = 1)
        {
            AdRev = adRev;
            ImpressionCount = impressionCount;
            AdViewId = null;
            _hasImpression = impressionCount > 0;
            _mixedInstances = false;
        }

        public void Update(double adRev, string adViewId = null)
        {
            AdRev += adRev;
            ImpressionCount++;

            if (_mixedInstances) return;
            if (!_hasImpression)
            {
                _hasImpression = true;
                AdViewId = adViewId;
                return;
            }

            if (AdViewId == adViewId) return;
            _mixedInstances = true;
            AdViewId = null;
        }
    }
}
