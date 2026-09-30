// Author: Bui Quang Huy
// Company: Falcon Games

using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Shared.Common;
using Sirenix.OdinInspector;

namespace Falcon.Shared.Lives
{
    /// <summary>Số tim và mốc đếm giờ hồi. Thay cho LivesAmountData bên FalconAssets.</summary>
    [ResourceInfo(LivesResourceId.AMOUNT)]
    public class LivesAmountResource : AResource
    {
        [ShowInInspector] public int Quantity { get; set; } = -1;
        // Bắt đầu đếm giờ hồi từ giây này. Tim kế tiếp về lúc RegenStart + RegenSeconds. -1 là đang đầy.
        [ShowInInspector] public long RegenStart { get; set; } = -1;

        protected override bool AddInternal(int amount, string data)
        {
            Center.Get<WrapperLives>()?.Add(amount, where: data);
            return true;
        }

        protected override bool RemoveInternal(int amount, string data) => false;

        // Không cho set thẳng, phải đi qua WrapperLives để giữ clamp và mốc hồi
        protected override int SetInternal(int value, string data) => 0;

        protected override int ResetInternal() => 0;

        public override object Get => Quantity;
        public override int GetInt => Quantity;
        public override string ToString() => "Q: " + Quantity;
    }

    /// <summary>Thời điểm hết hạn vé vô hạn, epoch giây. Thay cho LivesUnlimitedData cũ.</summary>
    [ResourceInfo(LivesResourceId.UNLIMITED)]
    public class LivesUnlimitedResource : AResource
    {
        [ShowInInspector] public long EndSecond { get; set; } = -1;

        protected override bool AddInternal(int amount, string data)
        {
            Center.Get<WrapperLives>()?.AddUnlimitedSeconds(amount, where: data);
            return true;
        }

        protected override bool RemoveInternal(int amount, string data) => false;

        protected override int SetInternal(int value, string data) => 0;

        protected override int ResetInternal() => 0;

        public override object Get => EndSecond;
        public override string ToString() => "End: " + EndSecond;
    }

    public static class LivesResourceId
    {
        public const string AMOUNT = "live_amount";
        public const string UNLIMITED = "unlimited_live";
    }

    /// <summary>Đưa 2 resource tim vào GameDataCore để save/load chung.</summary>
    public class LivesResourceInject : AResourceInject
    {
        public override void Inject(Injection injection)
        {
            injection.Invoke(new LivesAmountResource());
            injection.Invoke(new LivesUnlimitedResource());
        }
    }
}
