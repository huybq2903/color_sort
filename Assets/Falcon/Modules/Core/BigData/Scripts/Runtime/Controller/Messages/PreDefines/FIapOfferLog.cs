/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-04-02
 */
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using UnityEngine.Scripting;
// ReSharper disable once CheckNamespace

namespace Falcon.Modules.Core.BigData
{
    public class FIapOfferLog : AFalconLog
    {
        public IapOfferParam param;

        /// <summary>
        /// Id của LẦN HIỂN THỊ này — SDK sinh lúc offer hiện, rồi đóng dấu lên log mua phát sinh
        /// từ offer này để đo conversion thật (§C).
        /// </summary>
        [FKey(RemoveIfNull = true)] public string offerImpressionId;

        /// <summary>
        /// Nhãn của BẢN THIẾT KẾ offer (khoá <c>offerId</c>) — SDK đóng dấu từ kho nhãn, game khai
        /// qua <c>Label.IapOffer(offerId, ...)</c>. Đúng cho MỌI lần offer đó
        /// hiện, khác <c>f_sdk_iap_offer_label</c> vốn dán cho một lần hiển thị.
        /// <br/>⚠ Field NGOÀI hợp đồng §D11 — chờ loader khai mapping.
        /// </summary>
        [FKey(RemoveIfNull = true)] public Dictionary<string, object> offerLabels;

        /// <summary>
        /// Tính bằng GIÂY, SDK tự đo. Nghĩa chính xác: <b>từ lúc offer hiện tới lúc người chơi RỜI
        /// bề mặt offer</b> — đóng popup HOẶC rời app, cái nào đến trước.
        /// <br/>Vế "rời app" không phải ca hiếm mà là ca CHUYỂN ĐỔI: bấm mua → dialog billing bật
        /// lên → app xuống nền → SDK chốt impression ngay tại đó (chống mất bản tin nếu app bị
        /// kill). Nên với offer có mua, số này là <b>thời gian quyết định</b>, không phải tổng thời
        /// gian popup tồn tại.
        /// <br/>⚠ Vì thế đừng so thẳng thời lượng giữa nhóm mua và nhóm không mua rồi kết luận
        /// "xem lâu thì ít mua" — hai nhóm đang đo hai đoạn khác nhau.
        /// <br/>(Tên không mang đơn vị là để giữ nguyên key đã lên hợp đồng.)
        /// </summary>
        public long impressionDuration;

        [Preserve]
        public FIapOfferLog()
        {
        }

        public FIapOfferLog(IapOfferParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{GetType().Name}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }


        public override string Event => "f_sdk_iap_offer_data";

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Remove(nameof(param));
            foreach (var (key, value) in param.ToDictionary())
                result.PutIfAbsentAndNotNull(key, value);
            return result;
        }
    }
}