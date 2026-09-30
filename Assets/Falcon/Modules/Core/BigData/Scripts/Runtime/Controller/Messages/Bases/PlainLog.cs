/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public abstract class PlainLog : IDataLog
    {
        [FKey(Ignore = true)] public long clientCreateDate = MySingletonService.Instance<ITimeRepository>().CurrentTimeMillis;

        // Flag decorate-once của LogDecorService — internal nên không bị FKeyService encode vào payload
        internal bool decorated;

        [JsonIgnore] public long CreatedTime => clientCreateDate;

        /// <summary>
        /// Bản tin này TỔNG HỢP nhiều khoảnh khắc (span-record) chứ không tả một khoảnh khắc —
        /// vd log banner gộp mang <c>impressionCount</c> = N.
        /// <br/>Pipeline dùng cờ này để KHÔNG đóng dấu các định danh của-một-khoảnh-khắc
        /// (<c>playTurnId</c>, <c>adViewId</c>, độ trễ fill…): trên bản tổng hợp, giá trị lúc gửi
        /// chỉ đúng cho khoảnh khắc CUỐI, dán lên cả cụm là bịa cho N-1 cái còn lại.
        /// Đo cộng được (doanh thu, số lần) thì vẫn gộp bình thường.
        /// <br/>Property nên FKeyService (chỉ encode field public) không đưa nó vào payload.
        /// </summary>
        [JsonIgnore] public virtual bool IsSpanRecord => false;

        /// <summary>
        /// Bản tin này có được phép gửi không. Mặc định có; log tự đánh trượt mình (vd
        /// <see cref="FFilteredFunnelLog"/> khi bước funnel sai luật) thì trả false.
        /// <br/>Kiểm ở FUNNEL của pipeline chứ không ở <c>Send()</c>: giờ có ba đường vào
        /// (log.Send, Controller.Send, LogScheduleService.Enqueue), chặn ở một đường thì hai
        /// đường kia lọt.
        /// </summary>
        [JsonIgnore] public virtual bool IsSendable => true;

        public virtual string Event => GetType().Name;

        public virtual Dictionary<string, object> ToDictionary()
        {
            return FKeyService.Encode(this);
        }

        // Send/SendNow chỉ là forwarder tiện tay (Active Record mỏng) — mọi log đi qua cửa trước
        // FalconBigDataController để được decor rồi mới vào pipeline gửi.
        public virtual void Send()
        {
            FalconBigDataController.Instance.Send(this);
        }

        public virtual void SendNow()
        {
            FalconBigDataController.Instance.SendNow(this);
        }
    }
}