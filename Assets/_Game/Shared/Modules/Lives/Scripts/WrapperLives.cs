// Author: Bui Quang Huy
// Company: Falcon Games

using System;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.BigData;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Shared.Common;
using Falcon.Shared.Common.Time;

namespace Falcon.Shared.Lives
{
    /// <summary>Hệ thống tim: hồi theo mốc thời gian, giữ tạm khi vào màn, vé vô hạn.</summary>
    public class WrapperLives : IInitialize
    {
        private const long NONE = -1;

        /// <summary>Bắn mỗi khi số tim, vé vô hạn hoặc mốc hồi đổi.</summary>
        public event Action OnChanged;

        public int Max => LivesConfig.Get("lives_max", 5);
        public int RegenAmount => LivesConfig.Get("lives_regen_amount", 1);
        public int RegenSeconds => LivesConfig.Get("lives_regen_interval_seconds", 1800);
        public int PerUse => LivesConfig.Get("lives_per_use", 1);

        private int _quantity;
        private long _unlimitedEndSec = NONE;
        private long _regenStart = NONE;
        private int _cache;

        private static long Now => WrapperTime.CurrentSecond;

        private static LivesAmountResource Amount =>
            ResourceCollector.Instance.GetResourceInCollector<LivesAmountResource>(LivesResourceId.AMOUNT);

        private static LivesUnlimitedResource Unlimited =>
            ResourceCollector.Instance.GetResourceInCollector<LivesUnlimitedResource>(LivesResourceId.UNLIMITED);

        public void OnInitialize()
        {
            // Load trước để ResourceCollector kịp đăng ký OnUpdateFromServer sớm hơn dòng dưới
            Load();

            AccountManager.Instance.OnUpdateFromServer += _ => Load();
            // Đồng hồ nhảy (resume app / server trả time) thì tính lại ngay
            WrapperTime.OnOverrideTime += () => { Recalc(); OnChanged?.Invoke(); };

            GameRequest<bool>.Register(GameKeys.CAN_USE_LIVE, CanUse);

            GameEvent.Register(GameKeys.USE_LIVE, Use);
            GameEvent.Register(GameKeys.CLEAR_CACHE_LIVE, ClearCache);
            GameEvent.Register(GameKeys.RELEASE_CACHE_LIVE, ReleaseCache);
        }

        #region Đọc

        public int Quantity { get { Recalc(); return _quantity; } }
        public int QuantityWithCache => Quantity + _cache;
        public int Cache => _cache;
        public bool IsMax => Quantity >= Max;
        public bool IsUnlimited => UnlimitedSecondsLeft > 0;

        public long UnlimitedSecondsLeft =>
            _unlimitedEndSec < 0 ? 0 : Math.Max(0, _unlimitedEndSec - Now);

        /// <summary>Giây còn lại tới lần hồi kế tiếp. 0 nếu đang đầy hoặc chưa có mốc.</summary>
        public long SecondsToNextRegen
        {
            get
            {
                Recalc();
                if (_quantity >= Max || _regenStart < 0) return 0;
                return Math.Max(0, RegenSeconds - (Now - _regenStart));
            }
        }

        #endregion

        #region Log BigData

        // Loader có currency_class = live riêng cho tim
        private const string CURRENCY = "live";
        // Vé vô hạn đếm theo giây, tách currency để không cộng nhầm vào số tim
        private const string CURRENCY_UNLIMITED = "live_unlimited";
        private const string ITEM_TYPE = "currency";

        private const string WHERE_LEVEL_START = "level_start";
        private const string WHERE_LEVEL_END = "level_end";
        private const string WHERE_REGEN_TIMER = "regen_timer";

        // amount luôn dương, chiều nằm ở flowType do SDK set
        private static void Log(FlowType flow, string currency, string where, string when, long amount, long before, long after)
        {
            if (amount <= 0) return;

            var param = new ResourceParam
            {
                itemType = ITEM_TYPE,
                itemId = currency,
                currency = currency,
                amount = amount,
                valueBefore = before,
                valueAfter = after,
            };
            // Để trống thì giữ UNKNOWN, không ghi đè bằng chuỗi rỗng
            if (!string.IsNullOrEmpty(where)) param.resourceWhere = where;
            if (!string.IsNullOrEmpty(when)) param.resourceWhen = when;

            if (flow == FlowType.Sink) FalconBigDataController.Resource.OnSpent(param);
            else FalconBigDataController.Resource.OnEarned(param);
        }

        #endregion

        #region Ghi

        public bool CanUse()
        {
            Recalc();
            return IsUnlimited || _quantity >= PerUse;
        }

        /// <summary>Vào màn: trừ tim và giữ tạm, chờ ReleaseCache khi thắng hoặc ClearCache khi thua.</summary>
        public void Use()
        {
            Recalc();

            if (IsUnlimited)
            {
                // Vé vô hạn: ví tim không đổi nên không log, vé đã được log lúc mua
                _cache = 0;
                return;
            }

            _cache = PerUse;
            Decrease(PerUse);
        }

        /// <summary>Thắng màn: trả lại số tim đang giữ tạm.</summary>
        public void ReleaseCache()
        {
            if (_cache <= 0) return;
            var temp = _cache;
            _cache = 0;
            Add(temp, where: WHERE_LEVEL_END);
        }

        /// <summary>Thua hoặc thoát màn: bỏ hẳn số tim đang giữ tạm.</summary>
        public void ClearCache()
        {
            if (_cache == 0) return;
            _cache = 0;
            OnChanged?.Invoke();
        }

        /// <summary>where = màn/panel phát sinh, when = hằng số <see cref="FResourceWhen"/>.</summary>
        public void Add(int amount, bool allowOverMax = false, string where = null, string when = null)
        {
            if (amount <= 0) return;
            Recalc();
            var before = _quantity;
            _quantity += amount;
            if (!allowOverMax) _quantity = Math.Min(_quantity, Max);
            // Log phần thực nhận sau clamp, không log tham số truyền vào
            Log(FlowType.Source, CURRENCY, where, when, _quantity - before, before, _quantity);
            Save();
        }

        /// <inheritdoc cref="Add"/>
        public void AddUnlimitedSeconds(long seconds, string where = null, string when = null)
        {
            if (seconds <= 0) return;
            var before = UnlimitedSecondsLeft;
            // Hết hạn rồi thì cộng từ bây giờ, còn hạn thì cộng dồn
            if (_unlimitedEndSec < Now) _unlimitedEndSec = Now;
            _unlimitedEndSec += seconds;
            Log(FlowType.Source, CURRENCY_UNLIMITED, where, when, seconds, before, UnlimitedSecondsLeft);
            Save();
        }

        private void Decrease(int amount)
        {
            if (_quantity <= 0) return;

            var wasMax = _quantity >= Max;
            var before = _quantity;
            _quantity = Math.Max(0, _quantity - amount);
            // Rời khỏi mức đầy mới bắt đầu đếm giờ hồi
            if (wasMax && _quantity < Max) _regenStart = Now;

            Log(FlowType.Sink, CURRENCY, WHERE_LEVEL_START, null, before - _quantity, before, _quantity);
            Save();
        }

        #endregion

        #region Hồi theo thời gian

        private void Recalc()
        {
            if (_quantity >= Max || _regenStart < 0) return;

            var elapsed = Now - _regenStart;
            if (elapsed < 0)
            {
                // Đồng hồ lùi so với mốc đã lưu, kéo mốc về hiện tại
                _regenStart = Now;
                Save();
                return;
            }

            var steps = elapsed / RegenSeconds;
            if (steps <= 0) return;

            // Cộng dồn theo bội số interval để không mất phần dư
            _regenStart += steps * RegenSeconds;
            var before = _quantity;
            _quantity = Math.Min(Max, _quantity + (int)(steps * RegenAmount));
            if (_quantity >= Max) _regenStart = NONE;

            // Nhiều step gộp một log, không bắn từng nhịp hồi
            Log(FlowType.Source, CURRENCY, WHERE_REGEN_TIMER, null, _quantity - before, before, _quantity);
            Save();
        }

        #endregion

        #region Data

        private void Load()
        {
            var amount = Amount;

            if (amount.Quantity < 0)
            {
                // Lần đầu chơi: phát đầy tim
                _quantity = Max;
                _unlimitedEndSec = NONE;
                _regenStart = NONE;
                Save();
                return;
            }

            _quantity = amount.Quantity;
            _regenStart = amount.RegenStart;
            _unlimitedEndSec = Unlimited.EndSecond;

            // Chưa đầy mà mất mốc thì tim sẽ đứng vĩnh viễn, mồi lại từ bây giờ
            if (_quantity < Max && _regenStart < 0) _regenStart = Now;

            Recalc();
            OnChanged?.Invoke();
        }

        private void Save()
        {
            var amount = Amount;
            amount.Quantity = _quantity;
            amount.RegenStart = _regenStart;
            Unlimited.EndSecond = _unlimitedEndSec;

            // Cả 2 resource đều nằm trên GameDataCore nên một lần save là đủ
            GameDataCore.Instance.Save();
            GameDataCore.Instance.UpdateToServer();
            OnChanged?.Invoke();
        }

        #endregion
    }
}
