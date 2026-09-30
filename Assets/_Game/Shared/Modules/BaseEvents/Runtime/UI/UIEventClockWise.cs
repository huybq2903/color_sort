/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-12
 */

using System.Linq;
using Falcon.Shared.Common.Time;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

namespace Falcon.Shared.BaseEvents
{
    public class UIEventClockWise : MonoBehaviour
    {
        [ValueDropdown(nameof(GetKeyEventList)), SerializeField] private string keyEvent;
        
        private readonly float[] _rotates = { 0, -90, -180, -270 };
        private int _rotateIndex;
        private bool _isAnim;
        private bool _active = true;

        public event Action OnFinish;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                if (!_active)
                {
                    WrapperTime.RemoveAction(keyEvent + "_end_time", DoCountTimer);
                    StopAnim();
                    return;
                }

                WrapperTime.AddAction(keyEvent + "_end_time", DoCountTimer);
            }
        }

        private void OnEnable()
        {
            if (!_active) return;
            if (!WrapperTime.AddAction(keyEvent + "_end_time", DoCountTimer))
            {
                StopAnim();
            }
        }

        private void OnDisable()
        {
            WrapperTime.RemoveAction(keyEvent + "_end_time", DoCountTimer);
            StopAnim();
        }

        private void DoCountTimer(long time)
        {
            if (time <= 0)
            {
                StopAnim();
                OnFinish?.Invoke();
                return;
            }

            DoAnim();
        }
        
        public void DoAnim()
        {
            if (_isAnim) return;
        
            _rotateIndex++;
            if (_rotateIndex >= _rotates.Length)
            {
                _rotateIndex = 0;
            }

            _isAnim = true;

            DOTween.Sequence()
                .AppendCallback(() => _isAnim = true)
                .AppendInterval(0.5f)
                .AppendCallback(() => _isAnim = false)
                .AppendInterval(0.3f)
                .Append(transform.DOLocalRotate(new Vector3(0, 0, _rotates[_rotateIndex]), 0.2f).SetEase(Ease.InSine))
                .SetTarget(this);
        }

        public void StopAnim()
        {
            this.DOKill();
            _isAnim = false;
        }
        
        private static string[] GetKeyEventList()
        {
            return EventsRegister.DictWrapper.Select(e => e.Key).ToArray();
        }
    }
}
