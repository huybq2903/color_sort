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
    /// CS để lấy tất cả event
    /// </summary>
    [FAMessage("cs_get_events")]
    public class CSGetEvents : CSMessage
    {
        
    }
    
    /// <summary>
    /// CS gửi đi khi win level, server sẽ trả về những event được mở khóa ở level này mà trước đó chưa mở khóa
    /// </summary>
    [FAMessage("cs_get_events_level")]
    public class CSGetEventsByLevel : CSMessage
    {
        public int level;
    }
    
    /// <summary>
    /// CS để lấy event theo key
    /// </summary>
    [FAMessage("cs_get_events_key")]
    public class CSGetEventByKey : CSMessage
    {
        public string key;
    }
}