using Falcon.Shared.Common.Time;
using TMPro;
using UnityEngine;

namespace Game.Shared.Clan
{
    /// <summary>Đếm ngược cooldown xin trợ giúp, chạy nhờ WrapperTime nên không tốn Update riêng.</summary>
    public class ClanCountdown : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private GameObject _root;

        private string _key;

        /// <summary>endSecond là giây unix; 0 hoặc đã qua thì ẩn luôn.</summary>
        public void Begin(string key, long endSecond)
        {
            Stop();

            if (endSecond <= WrapperTime.CurrentSecond)
            {
                SetVisible(false);
                return;
            }

            _key = key;
            SetVisible(true);
            WrapperTime.AddTick(_key, endSecond, OnTick, OnEnd);
        }

        public void Stop()
        {
            if (_key == null) return;

            // RemoveAction chứ không RemoveTick: schedule dùng chung key giữa các lần mở
            WrapperTime.RemoveAction(_key, OnTick);
            _key = null;
        }

        private void OnDisable() => Stop();

        private void OnTick(long remain)
        {
            if (_text != null) _text.text = remain.ToTime();
        }

        private void OnEnd()
        {
            _key = null;
            SetVisible(false);
        }

        private void SetVisible(bool on)
        {
            var go = _root != null ? _root : gameObject;
            if (go.activeSelf != on) go.SetActive(on);
        }
    }
}
