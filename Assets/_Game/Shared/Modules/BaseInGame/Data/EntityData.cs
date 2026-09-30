/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-16
 */

namespace Falcon.Shared.BaseInGame
{
    /// <summary>
    /// Base class cho các entity, là dữ liệu tĩnh của entity trong Level
    /// </summary>
    public abstract class EntityData
    {
        public string id; 
    }
    
    /// <summary>
    /// Base class cho các entity, là dữ liệu runtime của entity trong Level
    /// </summary>
    public abstract class EntityRuntime
    {
        public string id; 
    }
    
    /// <summary>
    /// Nếu entity không có dữ liệu tĩnh, sẽ dùng NullData
    /// </summary>
    public class NullData : EntityData {}
    
    /// <summary>
    /// Nếu entity không có dữ liệu runtime, sẽ dùng NullRuntime
    /// </summary>
    public class NullRuntime : EntityRuntime {}
}