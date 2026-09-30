/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-11
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Loại VẬT được gán nhãn (§D11). Nhãn VẬT KHÔNG bắn event riêng — nó được đóng dấu lên
    /// stream event của chính vật đó (level → mọi level event), nên mỗi kind cần một field bundle
    /// riêng trên wire (<c>levelLabels</c>, <c>offerLabels</c>) và loader phải khai mapping cho nó.
    /// <br/>⇒ **Thêm kind mới = SỬA HỢP ĐỒNG**, không phải việc client tự quyết; và kind nào KHÔNG
    /// có stream event dày thì không dùng được khuôn stamp này.
    /// </summary>
    public enum LabelEntityKind
    {
        /// <summary>Màn chơi — bundle đi field <c>levelLabels</c> trên chính level event.</summary>
        Level,

        /// <summary>
        /// BẢN THIẾT KẾ offer (khoá theo <c>offerId</c>, không phải theo lần hiển thị) — bundle đi
        /// field <c>offerLabels</c> trên chính event offer.
        /// <br/>Cặp với <c>Label.IapOffer(...)</c> y như Level cặp với <c>Label.LevelPlayTurn(...)</c>:
        /// cái kia tả MỘT LẦN hiển thị, cái này tả thiết kế — đúng cho mọi lần offer đó hiện.
        /// <br/>⚠ NGOÀI hợp đồng §D11 (đợt 1 mới mở level) — client gửi trước, cần loader khai
        /// mapping cho field <c>offerLabels</c> thì dữ liệu mới vào hộp.
        /// </summary>
        Offer
    }
}
