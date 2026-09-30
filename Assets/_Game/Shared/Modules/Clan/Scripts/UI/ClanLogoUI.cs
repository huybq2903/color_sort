using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared.Clan
{
    public class ClanLogoUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;

        public Action onClickAction;
        private Button _button;

        public void Init(int id)
        {
            if (_icon == null) return;

            var sprite = ClanLogoDatabase.Instance?.GetById(id);
            if (sprite == null) Debug.LogWarning($"[Clan] khong tim thay logo id {id} trong ClanLogoDatabase.");
            _icon.sprite = sprite;
        }

        // Hien dang goi o ChooseLogoPopup
        public void SetButton(Action onClick)
        {
            onClickAction = onClick;

            if (_button == null)
            {
                _button = gameObject.AddComponent<Button>();
                _button.targetGraphic = _icon;
                _button.onClick.AddListener(OnClick);
            }
            else
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }
        }

        public void OnClick()
        {
            onClickAction?.Invoke();
        }
    }
}
