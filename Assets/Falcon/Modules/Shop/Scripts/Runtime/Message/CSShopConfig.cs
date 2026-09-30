/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-13
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// Gửi yêu cầu lấy config từ server.
    /// </summary>
    [FAMessage("cs_shop_config")]
    public class CSShopConfig : CSMessage
    {
        
    }
}