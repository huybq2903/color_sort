/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-12
 */

using System;
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Sổ tên CỘT HỢP ĐỒNG trên wire — để chặn game đặt key trùng qua kênh mở
    /// (<c>Common.Set</c>, <c>Label.*</c>), theo luật §H1 "tra hợp đồng trước khi đặt tên".
    /// <br/>Vì sao phải chặn: key game tự đặt được merge bằng <c>PutIfAbsent</c>, nên key trùng cột
    /// bị cột thật ĐÈ trên những event có cột đó và chỉ xuất hiện trên phần còn lại — dữ liệu nửa
    /// có nửa không, im lặng, tệ hơn không có. Ca dễ dính nhất là tên phổ biến: game xây feature
    /// streak rồi đặt <c>"win_streak"</c> — trùng ngay cột thống kê của SDK.
    /// <br/>Gom bằng reflection từ chính các lớp payload thay vì chép tay — chép tay thì field mới
    /// thêm vào hợp đồng không ai nhớ cập nhật sổ. So khớp theo dạng CHUẨN HOÁ (bỏ <c>_</c>, thường
    /// hoá) nên <c>win_streak</c>/<c>winStreak</c>/<c>WIN_STREAK</c> đều bị bắt.
    /// <br/>Giới hạn biết trước: key nhét qua <c>extraMeta</c> (Dictionary thô trên param) không đi
    /// qua sổ này — đường đó merge ở hot path, soi từng key mỗi log là trả giá cho lỗi hiếm.
    /// </summary>
    public static class FWireReservedKeys
    {
        private static readonly Lazy<HashSet<string>> kKeys = new(Collect);

        public static bool IsReserved(string key)
        {
            return !string.IsNullOrEmpty(key) && kKeys.Value.Contains(Normalize(key));
        }

        private static string Normalize(string key)
        {
            return key.Replace("_", string.Empty).ToLowerInvariant();
        }

        private static HashSet<string> Collect()
        {
            var keys = new HashSet<string>();
            foreach (var type in typeof(FParam).Assembly.GetTypes())
            {
                if (!typeof(FParam).IsAssignableFrom(type) && !typeof(PlainLog).IsAssignableFrom(type)) continue;

                // Lấy CẢ field FKey(Ignore) (logValid, decorated...): chúng không lên wire nhưng
                // cũng chẳng ai thiệt gì khi mấy cái tên nội bộ đó bị cấm — đổi lại khỏi phải soi
                // attribute của Devkit ở đây.
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (field.Name != "param")
                        keys.Add(Normalize(field.Name));
            }

            return keys;
        }
    }
}
