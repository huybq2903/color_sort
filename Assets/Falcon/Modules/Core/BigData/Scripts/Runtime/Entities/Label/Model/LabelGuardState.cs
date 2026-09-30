/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>Kết quả soi một lần gán nhãn.</summary>
    public enum LabelCheck
    {
        Ok,

        /// <summary>Key rỗng — không có gì để gán.</summary>
        BlankKey,

        /// <summary>
        /// Trùng tên một CỘT HỢP ĐỒNG (§H1). Nhãn user được snapshot đứng cạnh cột thật trên mọi
        /// dòng event — hai số cùng tên khác nghĩa là analyst đọc nhầm chắc chắn.
        /// </summary>
        ReservedKey,

        /// <summary>Value chữ dài quá <see cref="LabelGuardState.MAX_VALUE_LENGTH"/>.</summary>
        ValueTooLong,

        /// <summary>
        /// Chạm trần số key nhãn NGƯỜI CHƠI. Nhãn user được server merge vào profile rồi snapshot
        /// lên mọi event sau đó, nên mỗi key là một cột phình trên toàn bộ dữ liệu.
        /// </summary>
        TooManyUserKeys
    }

    /// <summary>
    /// Van chống nhãn bậy, đặt ở client cho khớp van mà loader enforce phía server (§D11):
    /// vượt trần thì server DROP + DQ đếm, nên chặn sớm ở đây vừa đỡ băng thông vừa báo được cho
    /// dev ngay lúc chạy thay vì để họ đi tìm dữ liệu mất tích.
    /// <br/>KHÔNG còn hàng rào vocab đóng / cardinality như bản trước: hợp đồng đã bỏ
    /// label-registry (2026-08-10) và nới cho nhãn gánh cả "tham số động" — một tham số kiểu số
    /// thì đương nhiên có vô số giá trị, chặn theo cardinality là chặn đúng công dụng mới.
    /// </summary>
    public class LabelGuardState
    {
        /// <summary>Van của loader: quá số key này trên một user thì server DROP.</summary>
        public const int MAX_USER_LABEL_KEYS = 20;

        /// <summary>Van của loader: value dạng chữ dài hơn ngần này thì server DROP.</summary>
        public const int MAX_VALUE_LENGTH = 64;

        private readonly HashSet<string> _userKeys = new();

        /// <summary>Bản sao tập key nhãn user đang đếm — để service đem đi persist.</summary>
        public string[] UserKeys
        {
            get
            {
                var copy = new string[_userKeys.Count];
                _userKeys.CopyTo(copy);
                return copy;
            }
        }

        /// <summary>
        /// Nạp lại tập key từ phiên trước. PHẢI có persist: trần 20 key của server đếm theo ĐỜI
        /// user chứ không theo phiên — van chỉ đếm phiên này thì phiên sau khai 12 key mới vẫn
        /// được cho qua trong khi server đã đầy, drop + DQ đúng cái van sinh ra để chặn.
        /// </summary>
        public void Restore(IEnumerable<string> keys)
        {
            if (keys == null) return;
            foreach (var key in keys)
                if (!string.IsNullOrEmpty(key))
                    _userKeys.Add(key);
        }

        public LabelCheck Check(string key, object value, bool userScope)
        {
            if (string.IsNullOrEmpty(key)) return LabelCheck.BlankKey;
            if (FWireReservedKeys.IsReserved(key)) return LabelCheck.ReservedKey;
            if (value is string text && text.Length > MAX_VALUE_LENGTH) return LabelCheck.ValueTooLong;

            // null = lệnh GỠ nhãn: luôn cho qua, kể cả khi đã chạm trần (gỡ là cách hạ số key xuống)
            if (userScope && value != null && !_userKeys.Contains(key) && _userKeys.Count >= MAX_USER_LABEL_KEYS)
                return LabelCheck.TooManyUserKeys;

            if (!userScope) return LabelCheck.Ok;
            if (value == null) _userKeys.Remove(key);
            else _userKeys.Add(key);
            return LabelCheck.Ok;
        }
    }
}
