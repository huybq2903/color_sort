/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-17
 */

using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Tên CŨ của <see cref="AdLoadResultParam"/> — đổi tên 18/08 vì tên gốc gây hiểu nhầm
    /// "không multicall thì không gọi" (event là kết quả load generic, multicall hay không là
    /// chuyện config). Ruột nằm hết ở base; class rỗng này chỉ giữ cho code đã viết theo
    /// 1.3.3/1.3.4 compile tiếp.
    /// <br/><c>floor</c> là SỐ ở CẢ cửa cũ này (quyết owner 21/08): bản 1.3.6 từng có shim
    /// property string tự parse, rút ngay ở 1.3.7 vì nó che field số của base — ép người dùng
    /// tên cũ nhập string trong khi kiểu thật đã là số. Code cũ gán <c>floor = "1.2"</c> gãy
    /// compile chủ động (warning obsolete của class dẫn đường) — sửa là bỏ dấu nháy.
    /// </summary>
    [Serializable]
    [Obsolete("Đổi tên thành AdLoadResultParam (event load-result là generic, không riêng gì " +
              "multi-floor). Class cũ vẫn chạy y hệt — đổi tên khi tiện. Lưu ý floor giờ là " +
              "double? — gán chuỗi thì bỏ dấu nháy.")]
    public class AdMultiFloorParam : AdLoadResultParam
    {
    }
}
