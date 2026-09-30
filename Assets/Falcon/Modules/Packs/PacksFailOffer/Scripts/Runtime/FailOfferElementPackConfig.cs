/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-30
 */

using Falcon.Modules.Packs.Core.Runtime;

namespace Falcon.Modules.Packs.PacksFailOffer.Runtime
{
    /// <summary>
    /// Config của 1 pack fail offer
    /// </summary>
    public class FailOfferElementPackConfig : ABaseElementPackConfig
    {
        /// <summary>
        /// level mở khóa
        /// </summary>
        public int levelUnlock;
        /// <summary>
        /// Giới hạn số lần mua, -1 nếu k giới hạn
        /// </summary>
        public int limitBuy;
        /// <summary>
        /// pack này sẽ xuất hiện trong bao lâu
        /// </summary>
        public float durationShow;
        /// <summary>
        /// thời gian cho lần xuất hiện tiếp theo của pack
        /// </summary>
        public float coolDown;
        /// <summary>
        /// Nội dung đi kèm
        /// </summary>
        public string tag;
        /// <summary>
        /// Tên hiển thị của pack
        /// </summary>
        public string name;
    }
}