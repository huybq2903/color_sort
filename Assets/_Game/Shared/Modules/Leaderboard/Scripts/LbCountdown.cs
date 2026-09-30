// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-08-14

using DG.Tweening;
using Falcon.Shared.Common.Time;
using TMPro;
using UnityEngine;

namespace Game.Shared.Leaderboard
{
    /// <summary>Đếm ngược tới lúc hết mùa giải, chạy nhờ WrapperTime nên không tốn Update riêng.</summary>
    public class LbCountdown : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        [Tooltip("Node bật/tắt theo việc có mốc thời gian hay không; để trống thì dùng chính object này")]
        [SerializeField] private GameObject _root;

        [Tooltip("Kim đồng hồ, quay 90° mỗi nhịp giống UIEventClockWise; để trống nếu art không có")]
        [SerializeField] private Transform _clock;

        // Quay theo 4 nấc cố định thay vì cộng dồn góc, tránh trôi số sau nhiều vòng
        private static readonly float[] Rotates = { 0f, -90f, -180f, -270f };
        private int _rotateIndex;
        private bool _spinning;

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

            // RemoveAction chứ không RemoveTick: schedule dùng chung key giữa các lần mở,
            // gỡ nguyên schedule sẽ làm hỏng lần đăng ký sau.
            WrapperTime.RemoveAction(_key, OnTick);
            _key = null;
            StopSpin();
        }

        private void OnDisable() => Stop();

        private void OnTick(long remain)
        {
            if (_text != null) _text.text = remain.ToTime();
            Spin();
        }

        private void OnEnd()
        {
            _key = null;
            StopSpin();
            SetVisible(false);
        }

        /// <summary>Một nhịp: đứng yên ~0.8s rồi giật 90°. _spinning chặn chồng chuỗi khi tick dồn.</summary>
        private void Spin()
        {
            if (_clock == null || _spinning) return;

            _rotateIndex = (_rotateIndex + 1) % Rotates.Length;
            _spinning = true;

            DOTween.Sequence()
                .AppendInterval(0.8f)
                .AppendCallback(() => _spinning = false)
                .Append(_clock.DOLocalRotate(new Vector3(0f, 0f, Rotates[_rotateIndex]), 0.2f).SetEase(Ease.InSine))
                .SetTarget(this);
        }

        private void StopSpin()
        {
            this.DOKill();
            _spinning = false;
        }

        private void SetVisible(bool on)
        {
            var go = _root != null ? _root : gameObject;
            if (go.activeSelf != on) go.SetActive(on);
        }
    }
}
