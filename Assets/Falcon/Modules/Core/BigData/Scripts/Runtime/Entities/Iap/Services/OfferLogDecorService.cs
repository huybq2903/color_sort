/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-11
 */

using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Đóng dấu nhãn của BẢN THIẾT KẾ OFFER lên event offer — bản sao khuôn
    /// <see cref="LevelLogDecorService"/> cho entity offer (§H6: attribute của thiết kế đi trong
    /// container mang tên chủ thể, không thả flat).
    /// </summary>
    public class OfferLogDecorService : MySingleton<OfferLogDecorService>, ILogDecorator<FIapOfferLog>
    {
        private readonly EntityLabelService _entityLabelService;

        public OfferLogDecorService(EntityLabelService entityLabelService)
        {
            _entityLabelService = entityLabelService;
        }

        public void Decor(FIapOfferLog log)
        {
            // offerId CỦA CHÍNH LOG NÀY, không phải offer đang mở: log của một offer đã đóng (hoặc
            // bị đè) vẫn phải mang nhãn của đúng thiết kế mà nó tả — cùng lý do levelLabels dùng
            // currentLevel của log thay vì màn đang chơi.
            log.offerLabels ??= _entityLabelService.BundleFor(LabelEntityKind.Offer, log.param?.offerId);
        }
    }
}
