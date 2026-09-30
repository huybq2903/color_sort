using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.OutGame.Core
{
    /// <summary>Tấm chặn input toàn cục. Đếm số lần xin chặn nên nhiều hệ thống cùng chặn không nhả sớm của nhau.</summary>
    [RequireComponent(typeof(Image))]
    public class UIBlocker : MonoBehaviour
    {
        /// <summary>true = xin chặn, false = nhả. Phải gọi đủ cặp.</summary>
        public const string EVENT_BLOCK_INPUT = "falcon.modules.ui.block_input";

        private Image _block;
        private int _depth;

        private void Awake()
        {
            _block = GetComponent<Image>();
            _block.color = Color.clear;
            _block.raycastTarget = true;
            _block.enabled = false;
        }

        private void OnEnable()
        {
            _depth = 0;
            _block.enabled = false;
            GameEvent<bool>.Register(EVENT_BLOCK_INPUT, SetBlock, this);
        }

        private void OnDisable() => GameEvent<bool>.Unregister(EVENT_BLOCK_INPUT, SetBlock, this);

        private void SetBlock(bool block)
        {
            _depth = Mathf.Max(0, _depth + (block ? 1 : -1));
            _block.enabled = _depth > 0;
        }
    }
}
