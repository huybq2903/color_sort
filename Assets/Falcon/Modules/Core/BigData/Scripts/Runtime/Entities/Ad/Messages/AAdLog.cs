/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-11
 */

using System;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Gốc chung của cả bốn mốc trong vòng đời MỘT LẦN XEM AD: request → show → impression → close.
    /// <br/>Giữ <see cref="adViewId"/> ở đây thay vì khai bốn lần: id là thứ SDK sinh (game không
    /// set), và bài học <c>adWhen</c> hôm trước cho thấy field của cả họ mà khai nhiều nơi thì
    /// thêm/sửa sẽ quên một chỗ.
    /// </summary>
    [Serializable]
    public abstract class AAdLog : AFalconLog
    {
        /// <summary>
        /// Id của lần xem ad này — SDK sinh lúc request rồi đóng dấu lên cả bốn mốc, nhờ vậy server
        /// dựng lại được trọn vòng đời và thấy được view FILL-FAIL (request không có impression nào
        /// mang id đó).
        /// <br/>Vắng mặt khi mốc này không đi từ request nào của SDK (vd đường load nằm ngoài SDK,
        /// hoặc banner tự refresh bên trong mediation).
        /// </summary>
        [FKey(RemoveIfNull = true)] public string adViewId;
    }
}
