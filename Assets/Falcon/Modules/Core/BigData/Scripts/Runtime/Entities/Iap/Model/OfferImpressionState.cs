/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Ảnh chụp offer đang hiển thị — bản SAO để game đọc; sửa vào đây không đụng tới state SDK.
    /// </summary>
    [Serializable]
    public class OfferSnapshot
    {
        public string offerImpressionId;
        public string offerId;
        public string[] offerProductId;
        public OfferSurfaceType? offerSurfaceType;
        public Dictionary<string, double> offerPrices;
        public Dictionary<string, float> offerDiscounts;
        public string offerCurrencyCode;
        public bool isClicked;
        public bool? isPurchased;

        /// <summary>Offer đã hiển thị được bao lâu (ms) tính tới lúc đọc.</summary>
        public int shownDurationMs;
    }

    /// <summary>Kết quả một cú bấm vào offer.</summary>
    public enum OfferClick
    {
        /// <summary>Không có offer nào đang mở — bỏ qua.</summary>
        NoOpenOffer,

        /// <summary>Ghi vào state; bản tin impression (chưa gửi) sẽ mang isClicked = true.</summary>
        Recorded,

        /// <summary>
        /// Ghi vào state, NHƯNG bản tin impression đã gửi từ lúc app pause với isClicked = false —
        /// cú bấm này không còn đường lên wire qua dòng impression, caller phải chở bằng kênh khác.
        /// </summary>
        RecordedAfterLogged,

        /// <summary>Bấm lặp — đã ghi từ trước, không có gì mới.</summary>
        AlreadyClicked
    }

    /// <summary>
    /// Một lần hiển thị offer đã CHỐT, sẵn sàng để log: param game khai + các số đo của SDK.
    /// Gói lại thành một cục vì state thôi ghi ngược vào param của caller (param = cái game khai).
    /// </summary>
    public class OfferImpressionRecord
    {
        public IapOfferParam param;
        public string offerImpressionId;

        /// <summary>Offer nằm trên màn hình bao lâu, tính bằng GIÂY.</summary>
        public long impressionDurationSec;
    }

    /// <summary>
    /// Vòng đời một lần hiển thị offer IAP (xem EntityLifecycle-Design.md §4d):
    /// hiện → (click / mua) → đóng, log bắn lúc ĐÓNG (span-record theo §G, không log per-frame).
    /// Id sinh lúc hiện để carry sang log mua — đo conversion offer→purchase thật (§C).
    /// <br/>Sau khi đóng, offer còn nhận attribution thêm một CỬA SỔ ÂN HẠN: luồng phổ biến là
    /// bấm mua → popup đóng/forward sang shop → callback giao dịch mới về, đóng state ngay lập tức
    /// là mất attribution của đúng những lượt mua có chuyển đổi.
    /// <br/>Pure class — không IO/DI/time, caller truyền mốc thời gian vào. Thread-safety do
    /// <see cref="OfferImpressionService"/> lo (bên ghi là UI, bên đọc là callback billing).
    /// </summary>
    public class OfferImpressionState
    {
        private IapOfferParam _param;
        private string _impressionId;
        private long _shownAtMillis;
        private bool _logged;

        private IapOfferParam _graceParam;
        private string _graceImpressionId;
        private long _graceUntilMillis;

        /// <summary>Id của offer ĐANG hiển thị (null nếu không có).</summary>
        public string OpenImpressionId => _impressionId;

        /// <summary>
        /// Gộp extras khai lúc ĐÓNG vào param của offer đang mở — tin mới thắng khi trùng key
        /// (cùng triết lý "tin mới nhất là tin đúng nhất" của ApplyContext bên ad).
        /// Không có offer đang mở (đã chốt lúc app pause / chưa từng mở) thì bỏ qua —
        /// payload đã đi thì không sửa được nữa.
        /// </summary>
        public void MergeExtraMeta(Dictionary<string, object> extraMeta)
        {
            if (_param == null || extraMeta == null || extraMeta.Count == 0) return;

            _param.extraMeta ??= new Dictionary<string, object>();
            foreach (var (key, value) in extraMeta) _param.extraMeta[key] = value;
        }

        /// <summary>
        /// Ảnh chụp offer đang hiển thị (null nếu không có) — bản sao của bộ tham số game đã nhập
        /// lúc mở, kèm thời lượng hiển thị tới lúc đọc. KHÔNG trả offer trong cửa sổ ân hạn: nó đã
        /// đóng rồi, trả ra sẽ khiến game tưởng còn đang hiển thị.
        /// </summary>
        public OfferSnapshot TakeSnapshot(long nowMillis)
        {
            if (_param == null) return null;
            return new OfferSnapshot
            {
                offerImpressionId = _impressionId,
                offerId = _param.offerId,
                offerProductId = _param.offerProductId == null
                    ? null
                    : (string[])_param.offerProductId.Clone(),
                offerSurfaceType = _param.offerSurfaceType,
                offerPrices = Copy(_param.offerPrices),
                offerDiscounts = Copy(_param.offerDiscounts),
                offerCurrencyCode = _param.offerCurrencyCode,
                isClicked = _param.isClicked,
                isPurchased = _param.isPurchased,
                shownDurationMs = (int)Math.Max(0, nowMillis - _shownAtMillis)
            };
        }

        /// <summary>Offer bắt đầu hiển thị: sinh id của lần hiển thị và giữ state.</summary>
        /// <returns>
        /// (id vừa sinh — caller gắn lên log; replaced = lần hiển thị TRƯỚC đó nếu nó chưa được log
        /// và caller phải log nó — offer surface cũ đã thực sự kết thúc, bỏ đi là mất hẳn một
        /// impression; null nếu không có gì cần log).
        /// </returns>
        public (string offerImpressionId, OfferImpressionRecord replaced) Open(
            IapOfferParam param, long nowMillis, long graceMillis)
        {
            var replaced = Close(nowMillis, graceMillis);
            _param = param;
            _impressionId = Guid.NewGuid().ToString();
            _shownAtMillis = nowMillis;
            _logged = false;
            return (_impressionId, replaced);
        }

        /// <summary>
        /// Người chơi bấm vào offer đang hiển thị. Trả về kết quả để caller biết cú bấm này còn
        /// đường lên wire qua dòng impression không (xem <see cref="OfferClick"/>) — chỉ báo MỘT
        /// lần cho lần chuyển false→true đầu tiên, bấm liên hồi không đẻ thêm tín hiệu.
        /// </summary>
        public (OfferClick result, string offerImpressionId) MarkClicked()
        {
            if (_param == null) return (OfferClick.NoOpenOffer, null);
            if (_param.isClicked) return (OfferClick.AlreadyClicked, _impressionId);

            _param.isClicked = true;
            return (_logged ? OfferClick.RecordedAfterLogged : OfferClick.Recorded, _impressionId);
        }

        /// <summary>
        /// Một giao dịch vừa xảy ra: quy về offer nếu productId khớp danh sách sản phẩm của offer.
        /// Ưu tiên offer ĐANG hiển thị, sau đó mới tới offer vừa đóng còn trong cửa sổ ân hạn
        /// (luồng offer → forward sang shop → mua: shop có thể lại là một offer surface khác).
        /// <br/>CHỈ khớp theo productId chứ KHÔNG "có offer mở thì gán": offer dạng card nằm lì
        /// trên UI sẽ ăn nhầm mọi giao dịch khác trong lúc nó hiển thị — claim không bằng chứng.
        /// </summary>
        /// <returns>offerImpressionId nếu quy được, null nếu không.</returns>
        public string TryAttribute(string productId, long nowMillis)
        {
            if (Matches(_param, productId))
            {
                _param.isPurchased = true;
                return _impressionId;
            }

            // "<" chứ không "<=": cửa sổ ân hạn 0 giây phải là KHÔNG có ân hạn, mà giao dịch về
            // đúng mili-giây offer đóng thì nowMillis == _graceUntilMillis.
            if (nowMillis < _graceUntilMillis && Matches(_graceParam, productId))
                // Log của offer này đã gửi rồi nên không set isPurchased nữa —
                // join theo offerImpressionId mới là nguồn chân lý.
                return _graceImpressionId;

            return null;
        }

        /// <summary>
        /// Offer đóng lại: chốt thời lượng hiển thị, chuyển sang cửa sổ ân hạn và trả bản ghi để
        /// log. Null nếu không có offer mở, hoặc impression này đã được log ở
        /// <see cref="TakePauseSnapshot"/> rồi (không log đôi).
        /// </summary>
        public OfferImpressionRecord Close(long nowMillis, long graceMillis)
        {
            if (_param == null) return null;

            var record = Finish(nowMillis, conclusive: true);
            var alreadyLogged = _logged;

            _graceParam = _param;
            _graceImpressionId = _impressionId;
            _graceUntilMillis = nowMillis + graceMillis;
            _param = null;
            _impressionId = null;

            return alreadyLogged ? null : record;
        }

        /// <summary>
        /// App rơi xuống background lúc offer đang mở: chốt và trả bản ghi để log ngay (chống mất
        /// impression nếu app bị kill), nhưng GIỮ state — app quay lại mà người chơi mua thì vẫn
        /// quy được giao dịch về offer này. Trả null nếu không có gì để log.
        /// </summary>
        public OfferImpressionRecord TakePauseSnapshot(long nowMillis)
        {
            if (_param == null || _logged) return null;

            _logged = true;
            // conclusive: false — impression chưa thực sự kết thúc, giao dịch có thể về ngay sau đó,
            // nên để isPurchased là null (chưa biết) thay vì khẳng định false rồi mâu thuẫn với log mua.
            return Finish(nowMillis, conclusive: false);
        }

        private static Dictionary<string, TValue> Copy<TValue>(Dictionary<string, TValue> source)
        {
            return source == null ? null : new Dictionary<string, TValue>(source);
        }

        private static bool Matches(IapOfferParam param, string productId)
        {
            return param?.offerProductId is { Length: > 0 } && param.offerProductId.Contains(productId);
        }

        private OfferImpressionRecord Finish(long nowMillis, bool conclusive)
        {
            // Chỉ kết luận "không mua" khi impression thực sự kết thúc VÀ có khai offerProductId
            // để đối chiếu; ngoài ra để null (không biết) thay vì khẳng định sai.
            if (conclusive && _param.offerProductId is { Length: > 0 }) _param.isPurchased ??= false;

            return new OfferImpressionRecord
            {
                param = _param,
                offerImpressionId = _impressionId,
                impressionDurationSec = Math.Max(0, nowMillis - _shownAtMillis) / 1000
            };
        }
    }
}
