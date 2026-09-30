/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-09
 */

namespace Falcon.Shared.BaseEvents
{
    public abstract class ABaseEventUserData
    {
        public int id;
        public int cacheId;
        public int sequence;
        public int code;
        public int day;
        public int state; //dùng để quản lý trạng thái của event (mỗi event sẽ định nghĩa riêng)
        public long endTime;
        public string type; //chỉ dùng để gửi lên server (không dc xóa)
    }
}