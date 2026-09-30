/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-10
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Đổi trạng thái quyền/consent (§D8) — MỘT EVENT CHO MỖI LOẠI QUYỀN, tên event lấy từ
    /// <see cref="PermissionType"/> nên không thể gõ sai.
    /// <br/>KHÔNG dùng lại <c>property_data</c>: log đó là di sản thời hệ sơ khai dùng sinh biểu đồ
    /// động (dấu vết còn nguyên ở field <c>priority</c> = "thứ tự step"), mượn nó là trộn ngữ nghĩa
    /// với dữ liệu biểu đồ đời cũ.
    /// </summary>
    [Serializable]
    public class FPermissionLog : AFalconLog
    {
        public PermissionType type;
        public PermissionParam param;

        [Preserve]
        public FPermissionLog()
        {
        }

        public FPermissionLog(PermissionType type, PermissionParam param)
        {
            this.type = type;
            this.param = param;
            AnalyticLogger.Instance.Info($"{Event}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override string Event => type.ToEventId();

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Remove(nameof(param));
            result.Remove(nameof(type));
            foreach (var (key, value) in param.ToDictionary())
                result.PutIfAbsentAndNotNull(key, value);
            return result;
        }
    }
}
