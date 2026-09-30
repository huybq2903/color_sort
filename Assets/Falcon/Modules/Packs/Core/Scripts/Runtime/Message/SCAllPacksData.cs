/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-10
 */

using System.Collections.Generic;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Thông điệp phản hồi từ server chứa toàn bộ dữ liệu các nhóm pack (bao gồm config và userData).
    /// Được sử dụng khi user đăng nhập xong để đồng bộ dữ liệu về client.
    /// </summary>
    [FAMessage("sc_all_packs_data")]
    public class SCAllPacksData : SCMessage
    {
        public List<PacksInfo> packs;

        /// <summary>
        /// Khi nhận dữ liệu từ server, gọi SyncFromServer cho từng WrapperPack tương ứng.
        /// Đồng bộ tất cả wrapper pack đã đăng ký với cấu hình và dữ liệu người dùng nhận được.
        /// </summary>
        public override void OnData()
        {
            foreach (var packInfo in packs)
            {
                if (packInfo == null) continue;
                var typeWrapper = PacksManager.GetWrapperType(packInfo.config.idGroup);
                var wrapper = PacksManager.GetOrCreate(typeWrapper);
                var syncMethod = wrapper.GetType().GetMethod("SyncFromServer");
                syncMethod?.Invoke(wrapper, new object[] { packInfo.config, packInfo.userData });
            }
        }
    }
}