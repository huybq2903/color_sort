/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Một bước trang trí log trong pipeline gửi (xem EntityLifecycle-Design.md §4b — kiến trúc chiều xuôi):
    /// log là DTO thuần, mọi enrichment chạy qua LogDecorService lúc log đi vào cửa trước
    /// (FalconBigDataController.Send/SendAll/SendNow) hoặc backstop tại LogScheduleService.
    /// <br/>Đây là interface hệ thống dùng: implement thẳng cũng được (tự khai
    /// <see cref="DecorLogType"/>), hoặc dùng bản rút gọn <see cref="ILogDecorator{T}"/>.
    /// <br/><br/>⚠ <b>LUẬT CTOR — decorator KHÔNG được ctor-inject bất kỳ service nào có
    /// <c>LogScheduleService</c> trong chuỗi ctor của nó.</b> Vòng: LogScheduleService →
    /// LogDecorService → (mảng decorator, dựng hết lúc boot) → decorator → service →
    /// LogScheduleService = SingletonException deadlock ngay lúc DI dựng đồ thị — và test suite
    /// KHÔNG bắt được vì chỉ test model thuần, Play mode mới nổ. Cách đúng: entity tách vai
    /// GIỮ-STATE thành cache riêng (AdViewCache / OfferImpressionCache / PurchaseAttemptCache —
    /// cùng hình LevelTurnService) và decorator inject CACHE; đường cùng lắm mới lấy lười qua
    /// <c>Xxx.Instance</c> trong Decor. Có DecoratorCycleTests canh bằng reflection.
    /// </summary>
    public interface ILogDecorator : IMySingleton
    {
        /// <summary>Type gốc mà decorator này áp lên (mọi subclass của nó cũng được áp).</summary>
        Type DecorLogType { get; }

        /// <summary>Thứ tự chạy khi một log khớp nhiều decorator — nhỏ chạy trước. Mặc định 0.</summary>
        int Priority => 0;

        void Decor(IDataLog log);
    }

    /// <summary>
    /// Bản rút gọn của <see cref="ILogDecorator"/>: khai log type ngay ở dòng khai báo class,
    /// khỏi viết <see cref="ILogDecorator.DecorLogType"/> và khỏi tự cast.
    /// Cast luôn an toàn vì LogDecorService chỉ dispatch log khớp type.
    /// <code>
    /// public class AdLogService : MySingleton&lt;AdLogService&gt;, ILogDecorator&lt;FAdLog&gt;
    /// {
    ///     public void Decor(FAdLog log) { /* enrich */ }
    /// }
    /// </code>
    /// </summary>
    public interface ILogDecorator<T> : ILogDecorator where T : IDataLog
    {
        Type ILogDecorator.DecorLogType => typeof(T);

        void ILogDecorator.Decor(IDataLog log) => Decor((T)log);

        void Decor(T log);
    }
}
