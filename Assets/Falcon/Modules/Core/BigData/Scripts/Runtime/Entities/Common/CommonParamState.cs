/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-07
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>Kết quả một lần khai tham số dùng chung.</summary>
    public enum CommonParamSet
    {
        /// <summary>Key rỗng / provider null — bỏ qua.</summary>
        Ignored,

        /// <summary>
        /// Trùng key tham số trung tâm của SDK. Lúc merge, tham số trung tâm luôn thắng
        /// (<c>PutIfAbsent</c>) nên key này sẽ vô hình — từ chối ngay để dev không ngồi đoán.
        /// </summary>
        CentralKeyRejected,

        /// <summary>
        /// Trùng tên một CỘT HỢP ĐỒNG (§H1, so theo dạng chuẩn hoá nên <c>win_streak</c> cũng bắt
        /// được <c>winStreak</c>). Cho qua thì key bị cột thật đè trên event có cột đó và chỉ xuất
        /// hiện trên phần còn lại — nửa có nửa không, tệ hơn không có.
        /// </summary>
        WireKeyRejected,

        Added
    }

    /// <summary>
    /// Bộ tham số dùng chung đi theo MỌI log — trạng thái thuần (không IO/DI/logger), phần lưu
    /// xuống đĩa nằm ở <see cref="CommonParamRepository"/>.
    /// </summary>
    public class CommonParamState
    {
        /// <summary>
        /// Quá số key này thì đáng cảnh báo: mỗi key ở đây nhân với MỌI log của MỌI user — chỗ này
        /// là nơi đắt nhất để thêm field. Ngưỡng là kinh nghiệm, không phải luật, nên chỉ cảnh báo.
        /// </summary>
        public const int WARN_KEY_COUNT = 15;

        private static readonly Lazy<HashSet<string>> kCentralKeys = new(CollectCentralKeys);

        private readonly ConcurrentDictionary<string, Func<object>> _providers = new();
        private readonly ConcurrentDictionary<string, object> _persisted = new();

        public int Count => _providers.Count;

        /// <summary>Các key đang khai — để soi lúc debug.</summary>
        public IReadOnlyCollection<string> Keys => _providers.Keys.ToArray();

        /// <summary>Các key + giá trị được đánh dấu sống qua restart (bản sao để đem đi lưu).</summary>
        public Dictionary<string, object> PersistedValues => new(_persisted);

        public bool Has(string key)
        {
            return !string.IsNullOrEmpty(key) && _providers.ContainsKey(key);
        }

        /// <summary>Khai tham số ĐỌC TƯƠI — hàm được gọi mỗi lần dựng log. Không bao giờ persist được (nó là code).</summary>
        public CommonParamSet Set(string key, Func<object> provider)
        {
            if (string.IsNullOrEmpty(key) || provider == null) return CommonParamSet.Ignored;
            if (kCentralKeys.Value.Contains(key)) return CommonParamSet.CentralKeyRejected;
            if (FWireReservedKeys.IsReserved(key)) return CommonParamSet.WireKeyRejected;

            _providers[key] = provider;
            _persisted.TryRemove(key, out _);
            return CommonParamSet.Added;
        }

        /// <summary>
        /// Khai tham số giá trị CỐ ĐỊNH. <paramref name="persist"/> = giữ qua lần khởi động sau.
        /// </summary>
        public CommonParamSet Set(string key, object value, bool persist)
        {
            if (value == null)
            {
                Remove(key);
                return CommonParamSet.Ignored;
            }

            if (string.IsNullOrEmpty(key)) return CommonParamSet.Ignored;
            if (kCentralKeys.Value.Contains(key)) return CommonParamSet.CentralKeyRejected;
            if (FWireReservedKeys.IsReserved(key)) return CommonParamSet.WireKeyRejected;

            _providers[key] = () => value;
            if (persist) _persisted[key] = value;
            else _persisted.TryRemove(key, out _);
            return CommonParamSet.Added;
        }

        /// <summary>Thôi gửi key này (kể cả bản đã persist).</summary>
        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _providers.TryRemove(key, out _);
            _persisted.TryRemove(key, out _);
        }

        /// <summary>
        /// Nạp lại các key đã lưu từ phiên trước. KHÔNG đè key game đã set trong phiên này —
        /// giá trị mới luôn đúng hơn giá trị của phiên trước.
        /// </summary>
        public void Restore(IEnumerable<KeyValuePair<string, object>> saved)
        {
            if (saved == null) return;
            foreach (var (key, value) in saved)
            {
                if (value == null || string.IsNullOrEmpty(key)) continue;
                if (kCentralKeys.Value.Contains(key)) continue;
                // Key đã persist từ bản SDK cũ có thể ĐỤNG cột mà bản mới vừa thêm vào hợp đồng —
                // bỏ qua thay vì nạp về trạng thái nửa-có-nửa-không.
                if (FWireReservedKeys.IsReserved(key)) continue;
                if (_providers.ContainsKey(key)) continue;

                _providers[key] = () => value;
                _persisted[key] = value;
            }
        }

        /// <summary>
        /// Dựng bộ tham số cho một log. Provider ném exception thì bỏ QUA ĐÚNG KEY ĐÓ — một key
        /// hỏng không được phép làm chết cả đường log; caller nhận key lỗi qua
        /// <paramref name="onError"/> để cảnh báo.
        /// </summary>
        public Dictionary<string, object> BuildInfo(Action<string, Exception> onError = null)
        {
            var result = new Dictionary<string, object>();
            foreach (var (key, provider) in _providers)
                try
                {
                    var value = provider();
                    if (value != null) result[key] = value;
                }
                catch (Exception e)
                {
                    onError?.Invoke(key, e);
                }

            return result;
        }

        /// <summary>
        /// Gom tên các tham số trung tâm từ <see cref="FCentralUserParamService.ParamKey"/> bằng
        /// reflection thay vì chép tay: chép tay thì mai kia Devkit thêm key, danh sách ở đây
        /// không ai nhớ cập nhật.
        /// </summary>
        private static HashSet<string> CollectCentralKeys()
        {
            var keys = new HashSet<string>();
            foreach (var group in typeof(FCentralUserParamService.ParamKey).GetNestedTypes())
            foreach (var field in group.GetFields(BindingFlags.Public | BindingFlags.Static))
                if (field.IsLiteral && field.GetRawConstantValue() is string value)
                    keys.Add(value);
            return keys;
        }
    }
}
