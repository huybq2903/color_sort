/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-07
 */

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// THAM SỐ DÙNG CHUNG — <c>FalconBigDataController.Common</c>: khai một lần, đi theo MỌI log.
    /// <code>
    /// FalconBigDataController.Common.Set("guildId", guild.Id);                 // giá trị cố định
    /// FalconBigDataController.Common.Set("gold", () => wallet.Gold);           // đọc tươi mỗi log
    /// FalconBigDataController.Common.Set("cohort", "abc", persist: true);      // sống qua restart
    /// </code>
    /// Đường class <c>IFCustomInfoRepository</c> vẫn còn cho tham số của cả một module (có ctor
    /// bắt sự kiện, có state) — kho này là cho vài key lẻ mà viết hẳn một file thì không đáng.
    /// <br/>⚠ Đây là chỗ ĐẮT NHẤT để thêm field: một key nhân với mọi log của mọi user. Tham số
    /// chỉ có nghĩa với một loại khoảnh khắc thì để trên param của log đó. Và key vẫn phải đăng ký
    /// với loader — key vô danh server không nhặt (§H1).
    /// </summary>
    public class FCommonApi
    {
        private readonly CommonParamRepository _repository;

        internal FCommonApi(CommonParamRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Khai tham số dùng chung với giá trị CỐ ĐỊNH (tới khi set lại). Giá trị null = xoá key.
        /// </summary>
        /// <param name="persist">
        /// Giữ giá trị qua lần khởi động sau (mặc định KHÔNG). Bật thì key có mặt ngay từ log ĐẦU
        /// phiên — quãng game còn chưa chạy tới chỗ set; đổi lại, giá trị của phiên trước mà đã sai
        /// thì sai ngay từ log đầu. Chỉ bật cho thứ bền thật (guildId, cohort onboarding…).
        /// </param>
        public void Set(string key, object value, bool persist = false)
        {
            _repository.Set(key, value, persist);
        }

        /// <summary>
        /// Khai tham số dùng chung ĐỌC TƯƠI — hàm được gọi mỗi lần dựng log nên giá trị luôn mới
        /// nhất, game khỏi phải nhớ set lại. Hàm ném exception thì SDK nuốt + cảnh báo, các key
        /// khác vẫn gửi bình thường.
        /// <br/>Bản này không có <c>persist</c>: provider là code, không lưu xuống đĩa được — mà
        /// cũng không cần, game khai lại nó mỗi lần khởi động ở đúng chỗ khai lần đầu.
        /// </summary>
        public void Set(string key, Func<object> provider)
        {
            _repository.Set(key, provider);
        }

        /// <summary>Khai một lúc nhiều tham số cố định.</summary>
        public void SetAll(IEnumerable<KeyValuePair<string, object>> values, bool persist = false)
        {
            _repository.SetAll(values, persist);
        }

        /// <summary>Thôi gửi key này từ log kế tiếp.</summary>
        public void Remove(string key)
        {
            _repository.Remove(key);
        }

        /// <summary>Key này đang được khai không.</summary>
        public bool Has(string key)
        {
            return _repository.Has(key);
        }

        /// <summary>Danh sách key đang khai — để soi lúc debug.</summary>
        public IReadOnlyCollection<string> Keys => _repository.Keys;
    }
}
