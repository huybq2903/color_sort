using System;
using Falcon.Modules.Core.UI.Runtime;
using UnityEngine;

namespace Game.Shared.Clan
{
    /// <summary>Popup chọn logo clan.</summary>
    public class UIPopupChooseLogo : MonoBehaviour
    {
        [SerializeField] private Transform _content;
        [SerializeField] private ClanLogoUI _itemPrefab;

        private Action<int> _onPick;

        public void Bind(Action<int> onPick)
        {
            _onPick = onPick;
            Build();
        }

        private void Build()
        {
            var db = ClanLogoDatabase.Instance;
            if (db == null) { Debug.LogWarning("[Clan] chưa có logo database."); return; }

            // Popup được cache và Bind lại mỗi lần mở, phải dọn item cũ trước khi dựng lại
            for (var i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            for (var i = 0; i < db.Count; i++)
            {
                var item = Instantiate(_itemPrefab, _content);
                var id = i;
                item.Init(id);
                item.SetButton(() => OnPick(id));
            }
        }

        private void OnPick(int id)
        {
            _onPick?.Invoke(id);
            Close();
        }

        public void Close() => UIWrapper.ClosePopup(transform);
    }
}
