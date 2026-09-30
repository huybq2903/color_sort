/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Helpers.UI
{
    public class UIButtonExtension : UISelectableExtension
    {
        [SerializeField] private Transform target;
        [PropertyOrder(-100)]
        public float ratioOffsetX = 0.95f;

        [PropertyOrder(-100)]
        public float ratioOffsetY = 0.95f;

        [PropertyOrder(-100)]
        [HorizontalGroup("Scale")]
        [Button]
        private void Scale()
        {
            target.localScale = new Vector3(ratioOffsetX, ratioOffsetY, 1);
        }

        [PropertyOrder(-100)]
        [Button]
        [HorizontalGroup("Scale")]
        private void Restore()
        {
            target.localScale = Vector3.one;
        }

        [HideInInspector]
        public Button button;

        private bool isPress = false;
        private Tween _tween;

        protected virtual void Awake()
        {
            if (target == null)
            {
                target = transform;
            }
            button = GetComponent<Button>();

            OnButtonPress.AddListener(x =>
            {
                if (button != null && button.interactable)
                {
                    if (isPress)
                    {
                        return;
                    }

                    if (_tween != null && _tween.IsActive())
                    {
                        _tween.Complete();
                    }

                    var scale = target.localScale;
                    _tween = target.DOScale(new Vector3(scale.x * ratioOffsetX, scale.y * ratioOffsetY, 1), 0.1f).From(scale);
                    isPress = true;
                }
            });

            OnButtonRelease.AddListener(x =>
            {
                if (button != null && button.interactable)
                {
                    if (_tween != null && _tween.IsActive())
                    {
                        _tween.Complete();
                    }
                    if (!isPress)
                    {
                        return;
                    }
                    var scale = target.localScale;

                    _tween = target.DOScale(new Vector3(scale.x / ratioOffsetX, scale.y / ratioOffsetY, 1), 0.1f).From(scale);
                    isPress = false;
                }
            });
        }

        private void OnDestroy()
        {
            target.DOKill();
        }

        private void OnEnable()
        {
            target.DOKill(true);
            CheckReset();
        }

        private void OnDisable()
        {
            target.DOKill(true);
            CheckReset();
        }

        private void CheckReset()
        {
            if (isPress)
            {
                isPress = false;
                var scale = target.localScale;
                target.localScale = new Vector3(scale.x / ratioOffsetX, scale.y / ratioOffsetY, 1);
            }
        }
    }
}
