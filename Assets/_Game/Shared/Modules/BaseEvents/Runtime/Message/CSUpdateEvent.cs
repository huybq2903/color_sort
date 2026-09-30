/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-10
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Shared.BaseEvents
{
    /// <summary>
    /// Update data của event
    /// </summary>
    /// <typeparam name="D"></typeparam>
    [FAMessage("cs_update_event_data")]
    public class CSUpdateEventData<D> : CSMessage where D : ABaseEventUserData
    {
        public D data;

        public CSUpdateEventData(D data, string type)
        {
            this.data = data;
            this.data.type = type;
        }
    }
}