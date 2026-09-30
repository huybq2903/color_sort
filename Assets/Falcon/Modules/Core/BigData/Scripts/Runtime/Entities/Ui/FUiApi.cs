/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-09-05
 */

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Vocab hành vi exposure UI — <b>thêm giá trị = SỬA HỢP ĐỒNG</b> (§H2).
    /// </summary>
    public static class FUiAction
    {
        public const string Impression = "impression";
        public const string Click = "click";
    }

    /// <summary>
    /// Entity EXPOSURE UI — <c>FalconBigDataController.Ui</c>: đếm icon/popup/banner event hiện ra
    /// và được bấm, phục vụ metric "tổng impression + CTR theo ngày" của live-op.
    /// <br/>Van gộp là BẮT BUỘC (chốt owner 05/09): per-lần-hiện không bao giờ lên wire — SDK gộp
    /// theo (surface × action), flush lúc app pause thành bản tin <c>f_sdk_ui_impression</c> mang
    /// <c>count</c>; CTR ngày phía server = sum(click)/sum(impression), client chỉ NÉN không tính.
    /// <br/>KHÁC bước funnel <c>open</c>: open là MỐC phễu (một dòng/chu kỳ — "đã xem hay chưa"),
    /// còn đây là MÁY ĐẾM ("hiện bao nhiêu lần") — hai câu hỏi, hai grain, đừng dùng lẫn.
    /// </summary>
    public class FUiApi
    {
        private readonly UiExposureClusterService _clusterService;

        internal FUiApi(UiExposureClusterService clusterService)
        {
            _clusterService = clusterService;
        }

        /// <summary>
        /// Một bề mặt UI vừa THẬT SỰ hiện ra với người chơi (icon event xuất hiện trên home,
        /// popup bật lên...) — không phải mỗi frame, không phải lúc nằm ngoài màn hình.
        /// Cứ gọi mỗi lần, volume SDK lo.
        /// </summary>
        /// <param name="surfaceId">Bề mặt nào — vocab của game/event, đặt tên ổn định ("race_event_entry", "race_event_popup").</param>
        /// <param name="extraMeta">Ngữ cảnh BẤT BIẾN của surface trong stretch (vd race_id) — cụm giữ bản của LẦN ĐẦU, thứ đổi theo từng lần hiện sẽ bị rơi.</param>
        public void OnImpression(string surfaceId, Dictionary<string, object> extraMeta = null)
        {
            _clusterService.Record(surfaceId, FUiAction.Impression, extraMeta);
        }

        /// <summary>Người chơi bấm vào bề mặt đó — cùng <paramref name="surfaceId"/> với lần OnImpression tương ứng thì CTR mới ghép được.</summary>
        public void OnClicked(string surfaceId, Dictionary<string, object> extraMeta = null)
        {
            _clusterService.Record(surfaceId, FUiAction.Click, extraMeta);
        }
    }
}
