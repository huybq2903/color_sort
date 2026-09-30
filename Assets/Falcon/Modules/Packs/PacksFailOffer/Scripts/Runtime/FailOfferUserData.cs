/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-30
 */

using System.Collections.Generic;
using Falcon.Modules.Packs.Core.Runtime;

namespace Falcon.Modules.Packs.PacksFailOffer.Runtime
{
    /// <summary>
    /// Data của user với fail offer
    /// </summary>
    public class FailOfferUserData : ABasePackUserData
    {
        /// <summary>
        /// key: idPack, value: số lần đã mua gói
        /// </summary>
        public Dictionary<string, int> timeBuy;
        /// <summary>
        /// key: idPack, value: mốc thời điểm gói xuất hiện lại
        /// </summary>
        public Dictionary<string, long> coolDown;
        /// <summary>
        /// key: idPack, value: mốc thời điểm gói bị ẩn đi
        /// </summary>
        public Dictionary<string, long> timeEnd;
    }
}