/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-10
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Một lần ĐỔI trạng thái quyền/consent (§D8). Cú đổi trạng thái chính là một khoảnh khắc —
    /// event đúng nghĩa đen; "trạng thái hiện tại" = giá trị mới nhất của chuỗi event này.
    /// <br/>Không có field "key": loại quyền nằm ở TÊN EVENT (xem <see cref="PermissionType"/>).
    /// </summary>
    [Serializable]
    public class PermissionParam : FParam
    {
        /// <summary>
        /// Giá trị trạng thái — dùng hằng số <see cref="FPermissionValue"/>.
        /// Server map vào <c>sub_event</c> (slot variant chính, có index — "user denied" lọc thẳng).
        /// </summary>
        [NotNull] public string permissionValue = UNKNOWN;

        [FKey(RemoveIfNull = true)] public int? currentLevel;

        /// <summary>
        /// Key-value tuỳ ý cho lần đổi trạng thái này — flatten vào payload, server lưu
        /// <c>event_extra_props</c>; cột hợp đồng thắng khi trùng key.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> extraMeta;

        public override void CorrectValues()
        {
            permissionValue = CheckNonBlank(permissionValue, nameof(permissionValue));
            currentLevel = CheckNumberNonNegative(currentLevel, nameof(currentLevel));
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }
    }
}
