// Author: Bui Quang Huy
// Company: Falcon Games

using System;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.Common;
using Falcon.Shared.Common.Time;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.Lives
{
    /// <summary>Thanh tim trên HUD: số tim, giờ hồi, vé vô hạn, extra nhận từ clan.</summary>
    public class UILivesBar : MonoBehaviour
    {
        private const string EVENT_CLAN_GET_EXTRA = "falcon.modules.clan.get_total_help_resources";
        private const string EVENT_CLAN_OPEN_POPUP = "falcon.modules.clan.open_help_resources_popup";

        [SerializeField] private Button btnOpen;
        [SerializeField] private TMP_Text txtQuantity;
        [SerializeField] private TMP_Text txtCountdown;
        [SerializeField] private GameObject objInfinity;
        [SerializeField] private GameObject objFull;
        [SerializeField] private GameObject objPlus;

        [Header("Extra lives từ clan")]
        [SerializeField] private GameObject objExtra;
        [SerializeField] private TMP_Text txtExtra;

        private string _tickKey;
        private WrapperLives _wrapper;

        // Giây và chế độ đang hiển thị, để không SetText lại y hệt
        private long _shownSeconds = long.MinValue;
        private bool _shownUnlimited;

        private void Awake()
        {
            _tickKey = $"lives_bar_{GetInstanceID()}";
            _wrapper = Center.GetOrCreate<WrapperLives>();
            if (btnOpen) btnOpen.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            _wrapper.OnChanged += Refresh;
            _shownSeconds = long.MinValue;
            // Một tick duy nhất tự đọc lại trạng thái mỗi giây, không cần đăng ký lại khi data đổi
            WrapperTime.AddTick(_tickKey, long.MaxValue, _ => RefreshCountdown(), null);
            Refresh();
        }

        private void OnDisable()
        {
            _wrapper.OnChanged -= Refresh;
            // Xoá đồng bộ, RemoveTick chỉ xếp hàng nên enable lại sẽ chồng thêm delegate
            WrapperTime.RemoveAndEndTick(_tickKey);
        }

        private void Refresh()
        {
            var unlimited = _wrapper.IsUnlimited;
            var isMax = _wrapper.IsMax;

            if (txtQuantity)
            {
                txtQuantity.gameObject.SetActive(!unlimited);
                if (!unlimited) txtQuantity.SetText(_wrapper.Quantity.ToString());
            }

            if (objInfinity) objInfinity.SetActive(unlimited);
            if (objFull) objFull.SetActive(!unlimited && isMax);
            if (objPlus) objPlus.SetActive(!unlimited && !isMax);

            RefreshExtra();
            RefreshCountdown();
        }

        private void RefreshCountdown()
        {
            if (txtCountdown == null) return;

            var unlimited = _wrapper.IsUnlimited;
            var visible = unlimited || !_wrapper.IsMax;

            var go = txtCountdown.gameObject;
            if (go.activeSelf != visible) go.SetActive(visible);
            if (!visible) return;

            // SecondsToNextRegen tự Recalc, về 0 là WrapperLives cộng tim và bắn OnChanged
            var seconds = unlimited ? _wrapper.UnlimitedSecondsLeft : _wrapper.SecondsToNextRegen;
            if (seconds == _shownSeconds && unlimited == _shownUnlimited) return;

            _shownSeconds = seconds;
            _shownUnlimited = unlimited;
            txtCountdown.SetText(unlimited ? seconds.ToTime() : seconds.ToTimeMinute());
        }

        private void RefreshExtra()
        {
            if (objExtra == null && txtExtra == null) return;

            var extra = GetClanExtra();
            if (objExtra) objExtra.SetActive(extra > 0);
            if (txtExtra) txtExtra.SetText(extra.ToString());
        }

        private static int GetClanExtra()
        {
            var total = 0;
            GameEvent<Action<int>>.Emit(EVENT_CLAN_GET_EXTRA, value => total = value);
            return total;
        }

        private void OnClick()
        {
            // Có tim clan cho thì nhận trước, bản cũ thiếu return nên mở chồng 2 popup
            if (GetClanExtra() > 0)
            {
                GameEvent.Emit(EVENT_CLAN_OPEN_POPUP);
                return;
            }

            if (_wrapper.IsUnlimited || _wrapper.IsMax) return;
            UIWrapper.OpenPopup(nameof(UIPopupRefillLives));
        }
    }
}
