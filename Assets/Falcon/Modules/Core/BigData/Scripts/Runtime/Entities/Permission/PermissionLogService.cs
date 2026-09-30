/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Gửi trạng thái quyền/consent (§D8) — mỗi loại quyền một event riêng.
    /// <br/>Trạng thái thì chỉ đáng gửi khi ĐỔI, nên service nhớ giá trị đã gửi và bỏ qua lần báo
    /// trùng: module gọi được ở bất cứ đâu (mỗi lần khởi động, mỗi lần user đổi trong setting)
    /// mà không phình volume. "Trạng thái hiện tại" phía server = giá trị mới nhất của chuỗi event.
    /// </summary>
    public class PermissionLogService : MySingleton<PermissionLogService>
    {
        private const string LAST_SENT_PREFIX = "Analytic_Permission_";

        private readonly IDataPool _dataPool;
        private readonly LogScheduleService _logScheduleService;

        public PermissionLogService(IDataPool dataPool, LogScheduleService logScheduleService)
        {
            _dataPool = dataPool;
            _logScheduleService = logScheduleService;
        }

        /// <summary>
        /// Báo trạng thái hiện tại của một quyền. Chỉ gửi log khi khác giá trị đã gửi lần trước
        /// (lần đầu trên máy thì luôn gửi).
        /// </summary>
        /// <returns>true nếu có log được gửi.</returns>
        public bool ReportPermission(
            PermissionType type, string value, int? currentLevel = null,
            Dictionary<string, object> extraMeta = null)
        {
            if (string.IsNullOrEmpty(value)) return false;

            var poolKey = LAST_SENT_PREFIX + type;
            // Extras không có chuyến xe riêng: giá trị không ĐỔI thì không có log, extras cũng
            // không đi — đúng nghĩa "metadata của LẦN ĐỔI trạng thái"
            if (_dataPool.GetOrDefault<string>(poolKey, null) == value) return false;

            // Enqueue TRƯỚC rồi mới ghi mốc "đã gửi": ngược lại thì Enqueue ném/máy chết giữa
            // chừng là giá trị coi như đã gửi và VĨNH VIỄN không gửi lại (chỉ bắn khi đổi).
            // Thứ tự này xấu nhất là thừa một log trùng — server đọc last-value, không sao.
            _logScheduleService.Enqueue(new FPermissionLog(type, new PermissionParam
            {
                permissionValue = value,
                currentLevel = currentLevel,
                extraMeta = extraMeta
            }));
            _dataPool.Compute<string>(poolKey, _ => value);
            return true;
        }
    }
}
