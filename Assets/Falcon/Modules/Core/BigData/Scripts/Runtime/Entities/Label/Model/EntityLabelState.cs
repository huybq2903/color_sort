/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-11
 */

using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>Kết quả một lần khai nhãn VẬT.</summary>
    public enum EntityLabelSet
    {
        Ok,

        /// <summary>Key rỗng — không có gì để khai.</summary>
        BlankKey,

        /// <summary>
        /// Bundle vượt trần byte. Trần đếm BYTE chứ không đếm key: 12 nhãn số ngắn hay 3 nhãn chữ
        /// dài đều được — đếm key thì phạt oan nhãn số, đúng lúc nhãn vừa được mở cho giá trị số.
        /// </summary>
        BundleTooBig
    }

    /// <summary>
    /// Nhãn của VẬT (§D11, viết lại lần 3 ngày 2026-08-11): <b>không bắn event riêng</b> — game
    /// khai vào đây, rồi SDK-core đóng dấu cả bundle lên MỌI level event như đang đóng
    /// <c>playTurnId</c>.
    /// <br/>Vì sao bỏ đường bắn-một-lần: bridge có grain theo NGÀY, mà bắn một lần trong đời cài
    /// đặt thì ngày người chơi quay lại màn cũ sẽ không có bản tin nhãn nào — hộp ngày đó rỗng,
    /// trừ khi server nuôi một bảng state cỡ tỷ dòng chỉ để forward-fill. Stamp thì ngày nào chơi
    /// là ngày đó có nhãn tươi, và ván vắt qua nửa đêm UTC cũng tự lành vì heartbeat của ngày sau
    /// cũng mang bundle.
    /// <br/>Trạng thái thuần — phần lưu xuống đĩa nằm ở <see cref="EntityLabelService"/>.
    /// </summary>
    public class EntityLabelState
    {
        /// <summary>
        /// Trần cho MỘT bundle sau khi serialize. Bundle này đi kèm mọi level event — stream dày
        /// nhất hệ — nên đây là van bảo vệ WIRE, khác van ≤20 key/≤64 ký tự vốn bảo vệ namespace
        /// của nhãn user.
        /// </summary>
        public const int MAX_BUNDLE_BYTES = 256;

        /// <summary>
        /// Phần trần dành riêng cho nhãn ĐÃ ĐĂNG KÝ (<see cref="FLevelLabelKey"/>): nhãn tự do của
        /// game chỉ được xài tới <c>MAX_BUNDLE_BYTES - RESERVED_REGISTERED_BYTES</c>.
        /// <br/>Chỉ áp cho loại vật CÓ nhãn đăng ký (hiện là level). Vật không có thì trừ chỗ là
        /// bóp oan phần của game.
        /// <br/>Không giữ chỗ thì thứ tự gọi quyết định ai sống: game khai nhãn lúc load màn (đầy
        /// bundle) rồi mới OnLevelStart → <c>moves_limit</c> bị đẩy ra, mà nó vừa mới bị bỏ khỏi
        /// đường flat nên mất luôn, không còn chỗ nào để lấy lại.
        /// </summary>
        public const int RESERVED_REGISTERED_BYTES = 48;

        private readonly Dictionary<string, Dictionary<string, object>> _byEntity = new();

        /// <summary>
        /// Khai/sửa một nhãn. <paramref name="value"/> = null nghĩa là GỠ — ở mô hình stamp, gỡ
        /// đơn giản là thôi kèm key đó trong bundle từ lúc này (không có "bản tin gỡ").
        /// </summary>
        /// <param name="bundleBytes">Cỡ bundle sau khi áp thay đổi — để caller báo số cụ thể.</param>
        /// <param name="budgetBytes">
        /// Trần áp cho lần khai này. Nhãn do SDK cấp tên (<see cref="FLevelLabelKey"/>) được xài
        /// trọn <see cref="MAX_BUNDLE_BYTES"/>; nhãn tự do của game bị chặn sớm hơn để luôn còn chỗ
        /// cho nhóm kia. Ai trừ bao nhiêu là việc của <see cref="EntityLabelService"/> — state không
        /// biết loại vật nào có nhãn đăng ký.
        /// </param>
        public EntityLabelSet Set(string entityId, string key, object value, out int bundleBytes,
            int budgetBytes = MAX_BUNDLE_BYTES)
        {
            bundleBytes = 0;
            if (string.IsNullOrEmpty(key)) return EntityLabelSet.BlankKey;

            if (!_byEntity.TryGetValue(entityId, out var bundle))
                _byEntity[entityId] = bundle = new Dictionary<string, object>();

            var had = bundle.TryGetValue(key, out var old);
            if (value == null) bundle.Remove(key);
            else bundle[key] = value;

            bundleBytes = MeasureBytes(bundle);
            if (bundleBytes <= budgetBytes) return EntityLabelSet.Ok;

            // Vượt trần thì trả nguyên trạng: thà thiếu nhãn mới còn hơn để cả bundle bị server
            // vứt, kéo theo mấy nhãn cũ vốn đang chạy tốt
            if (had) bundle[key] = old;
            else bundle.Remove(key);
            return EntityLabelSet.BundleTooBig;
        }

        /// <summary>
        /// Bundle để đóng dấu lên level event của màn này; null nếu màn chưa có nhãn nào (§H4:
        /// không có thì vắng mặt, không gửi map rỗng).
        /// </summary>
        public Dictionary<string, object> BundleFor(string entityId)
        {
            if (!_byEntity.TryGetValue(entityId, out var bundle) || bundle.Count == 0) return null;
            return new Dictionary<string, object>(bundle);
        }

        /// <summary>Nạp lại nhãn đã lưu từ phiên trước.</summary>
        public void Restore(string entityId, Dictionary<string, object> bundle)
        {
            if (bundle == null || bundle.Count == 0) return;
            _byEntity[entityId] = bundle;
        }

        public static int MeasureBytes(Dictionary<string, object> bundle)
        {
            return bundle == null || bundle.Count == 0
                ? 0
                : System.Text.Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(bundle));
        }
    }
}
