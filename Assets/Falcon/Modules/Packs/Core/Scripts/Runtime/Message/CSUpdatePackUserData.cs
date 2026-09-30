/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-10
 */

using Falcon.Modules.Core.Network;
using Newtonsoft.Json;

namespace Falcon.Modules.Packs.Core.Runtime
{
    [FAMessage("cs_update_pack_user_data")]
    public class CSUpdatePackUserData<D> : CSMessage where D : ABasePackUserData
    {
        /// <summary>
        /// Dữ liệu người dùng mới nhất của nhóm pack này.
        /// </summary>
        public D data;
    }
}