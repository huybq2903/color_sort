/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-25
 */
using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using UnityEngine;

namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class IapOfferParam : FParam
    {
        /// <summary>
        ///     Id định danh cho offer trong hệ thống offers
        ///     Ví dụ: black_friday_2024_gem_pack, new_user_starter_30off_v2
        /// </summary>
        public string offerId;

        /// <summary>
        ///     Danh sách Product ID đang được offer
        /// </summary>
        [FKey(RemoveIfNull = true)] public string[] offerProductId;

        /// <summary>
        ///     Phân loại offer (BattlePass, GemBundle, StarterPack, LimitedTime...)
        /// </summary>
        public string offerCategory;
        public OfferTriggerType triggerType;

        /// <summary>
        ///     Kiểu layout hiển thị (Discount, Progression, ChooseOne, Carousel...)
        /// </summary>
        public string offerLayout;

        /// <summary>
        ///     Kiểu BỀ MẶT hiển thị (popup chặn màn / card nằm trong UI khác / kệ shop).
        ///     Bắt buộc phải tách kiểu thì chỉ số mới có nghĩa — xem <see cref="OfferSurfaceType"/>.
        ///     Không biết thì để null, đừng đoán.
        /// </summary>
        [FKey(RemoveIfNull = true)] public OfferSurfaceType? offerSurfaceType;

        /// <summary>
        ///     Tỷ lệ giảm giá TIÊU BIỂU của cả offer (dạng thập phân: 0.3 = giảm 30%, 0 = không giảm).
        ///     <br/>Có trước <see cref="offerDiscounts"/> và đang có dữ liệu sống nên hợp đồng
        ///     grandfather; với offer nhiều sản phẩm khác mức giảm thì đây là con số HEADLINE, còn
        ///     chi tiết nằm ở map. SDK KHÔNG tự suy con số này từ map — suy hộ là bịa cái mà game
        ///     mới biết (headline hiển thị lên UI là mức nào).
        /// </summary>
        public float discountRate;

        /// <summary>
        ///     Vị trí xuất hiện trong Game (Home, Shop, ResultScreen...)
        /// </summary>
        public string placementScene;

        /// <summary>
        ///     Điều kiện/Thời điểm trigger (OnLogin, LevelFailed, LevelCleared...)
        /// </summary>
        public string triggerEvent;

        /// <summary>
        ///     Trạng thái người dùng có tương tác bấm vào offer hay không
        /// </summary>
        public bool isClicked;

        /// <summary>
        ///     Level hiện tại của user khi offer xuất hiện
        /// </summary>
        [FKey(RemoveIfNull = true)] public int? currentLevel;

        /// <summary>
        ///     Giá theo TỪNG SẢN PHẨM, tiền tệ ĐỊA PHƯƠNG: <c>{productId → giá}</c>. Lấy từ
        ///     store/config, KHÔNG tự quy đổi USD — server convert theo
        ///     <see cref="offerCurrencyCode"/> qua đúng đường tỷ giá của IAP.
        ///     <br/>Là MAP chứ không phải một số vì một offer bán được nhiều sản phẩm với giá khác
        ///     nhau; offer đơn sản phẩm thì map một entry. Khoá phải là productId có trong
        ///     <see cref="offerProductId"/>, không thì server không join được với event mua.
        ///     <br/>Theo §H4: không biết thì để null (vắng mặt khỏi payload), đừng điền bừa.
        /// </summary>
        [FKey(RemoveIfNull = true)] public Dictionary<string, double> offerPrices;

        /// <summary>
        ///     Mức giảm theo TỪNG SẢN PHẨM: <c>{productId → tỷ lệ 0..1}</c> — 0.3 = giảm 30%.
        ///     <br/>Cùng lý do map như <see cref="offerPrices"/>: bundle "gem 100 giảm 30%, gem 500
        ///     giảm 50%" là ca thường, một số vô ích ở đó.
        ///     <br/>Đây là fact của LẦN HIỂN THỊ chứ không phải của thiết kế (giá/mức giảm đổi theo
        ///     đợt sale, và giá cá nhân hoá cũng đi lọt qua đúng hình dạng này) — nên nó ở đây chứ
        ///     KHÔNG ở bundle nhãn thiết kế <c>offerLabels</c>.
        /// </summary>
        [FKey(RemoveIfNull = true)] public Dictionary<string, float> offerDiscounts;

        /// <summary>
        ///     Mã tiền tệ ISO dùng chung cho <see cref="offerPrices"/> (vd "VND", "USD" — lấy từ
        ///     store). Vẫn là MỘT giá trị: cả kệ offer bán cùng một loại tiền.
        /// </summary>
        [FKey(RemoveIfNull = true)] public string offerCurrencyCode;

        /// <summary>
        ///     Offer này có dẫn tới mua thành công không — SDK-core tự điền khi có giao dịch khớp
        ///     <see cref="offerProductId"/> trong lúc offer hiển thị. Null = không kết luận (game
        ///     không khai products, hoặc bản tin chốt lúc app pause).
        ///     <br/>⚠ CỜ NÀY LÀ PROXY ĐỌC NHANH, KHÔNG PHẢI NGUỒN CHÂN LÝ — đúng chữ của hợp đồng
        ///     §D3. Hai lý do: (1) dialog billing tự nó là một lần pause, nên dòng impression luôn
        ///     bị chốt TRƯỚC khi biết kết quả — mua thật thì cờ trên dòng là null chứ không phải
        ///     true; (2) mua trong cửa sổ ân hạn sau khi offer đóng thì dòng đã đi với false.
        ///     <br/>Conversion tính bằng JOIN: log mua mang <c>offerImpressionId</c> (§C) — tồn tại
        ///     bản ghi mua cùng vé nghĩa là có chuyển đổi, bất kể cờ này nói gì.
        /// </summary>
        [FKey(RemoveIfNull = true)] public bool? isPurchased;

        /// <summary>
        ///     Key-value tuỳ ý của game cho LẦN HIỂN THỊ này — flatten vào payload, server lưu
        ///     <c>event_extra_props</c>; cột hợp đồng thắng khi trùng key. Nhãn của BẢN THIẾT KẾ
        ///     offer (đúng cho mọi lần hiện) thì dùng <c>Label.IapOffer(offerId, ...)</c> chứ không
        ///     nhét đây.
        /// </summary>
        [FKey(RemoveIfNull = true)] public Dictionary<string, object> extraMeta;

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }

        public override void CorrectValues()
        {
            offerId = CheckNonBlank(offerId, nameof(offerId));
            offerCategory = CheckNonBlank(offerCategory, nameof(offerCategory));
            offerLayout = CheckNonBlank(offerLayout, nameof(offerLayout));
            discountRate = CheckNumberNonNegative(discountRate, nameof(discountRate));

            placementScene = CheckNonBlank(placementScene, nameof(placementScene));
            triggerEvent = CheckNonBlank(triggerEvent, nameof(triggerEvent));

            currentLevel = CheckNumberNonNegative(currentLevel, nameof(currentLevel));

            // Map rỗng phải VẮNG MẶT khỏi payload chứ không gửi "{}" (§H4) — RemoveIfNull chỉ
            // bắt null, nên hạ rỗng về null ở đây.
            if (offerPrices is { Count: 0 }) offerPrices = null;
            if (offerDiscounts is { Count: 0 }) offerDiscounts = null;

            CheckProductMapKeys(offerPrices?.Keys, nameof(offerPrices));
            CheckProductMapKeys(offerDiscounts?.Keys, nameof(offerDiscounts));
        }

        /// <summary>
        /// Khoá của map giá/giảm phải nằm trong <see cref="offerProductId"/> — lệch một ký tự là
        /// server không join được với event mua, mà lệch thì im lặng chứ không hỏng gì để thấy.
        /// Chỉ CẢNH BÁO lúc dev, không sửa/xoá: dữ liệu game khai vẫn gửi, để bên phân tích tự thấy.
        /// </summary>
        private void CheckProductMapKeys(IEnumerable<string> keys, string fieldName)
        {
            if (keys == null || offerProductId is not { Length: > 0 }) return;

            foreach (var key in keys)
                if (Array.IndexOf(offerProductId, key) < 0)
                    Debug.LogError(
                        $"Dwh Log invalid field: {fieldName} có khoá '{key}' không nằm trong " +
                        "offerProductId — server sẽ không join được entry này với event mua");
        }
    }
}