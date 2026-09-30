/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-30
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Stamp playTurnId lên MỌI log (ad/iap/resource/funnel...) khi có turn đang MỞ —
    /// hợp đồng §C: "state có thì stamp, không phân biệt format, zero logic riêng".
    /// Đi qua cơ chế IFCustomInfoRepository sẵn có của Devkit: giá trị được đọc tươi mỗi log,
    /// inject bằng PutIfAbsent nên log tự set playTurnId (level log) luôn thắng.
    /// Sau terminal (pha ENDED) trả rỗng — ad giữa 2 màn tự nhiên không có id.
    /// <br/>⚠ Id này nói <b>ĐỒNG THỜI</b>, KHÔNG nói nguyên nhân: mua gói trong shop giữa lúc đang
    /// chơi màn thì log vẫn mang playTurnId, nhưng giao dịch đó không do màn sinh ra. Nguyên nhân
    /// là việc của field <c>*When</c> trên chính bản tin (<see cref="FResourceWhen"/>,
    /// <c>adWhen</c>, <c>iapWhen</c>). Trộn hai thứ là đọc ra "màn này đẻ ra doanh thu" từ một
    /// giao dịch chỉ tình cờ xảy ra trong màn.
    /// </summary>
    public class TurnCustomInfoRepository : IFCustomInfoRepository
    {
        /// <summary>Định danh của MỘT lượt chơi — không dán lên bản tin tổng hợp (xem PlainLog.IsSpanRecord).</summary>
        public const string PLAY_TURN_ID_KEY = "playTurnId";
        private readonly LevelTurnService _levelTurnService;

        public TurnCustomInfoRepository(LevelTurnService levelTurnService)
        {
            _levelTurnService = levelTurnService;
        }

        public Dictionary<string, object> GetInfo()
        {
            var playTurnId = _levelTurnService.OpenPlayTurnId;
            return playTurnId == null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object> { [PLAY_TURN_ID_KEY] = playTurnId };
        }
    }
}
