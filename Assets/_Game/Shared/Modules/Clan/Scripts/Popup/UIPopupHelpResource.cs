using System.Collections.Generic;
using Falcon.Modules.Core.UI.Runtime;
using TMPro;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    using SuperScrollView;

    /// <summary>Popup nhận tài nguyên trợ giúp từ member trong clan.</summary>
    public class UIPopupHelpResource : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtTotal;
        [SerializeField] private GameObject _emptyObj;
        [SerializeField] private GameObject _loadingObj;
        [SerializeField] private LoopListView2 _loopListView;

        private readonly List<HelpResourceRowData> _data = new();
        private bool _initScroll;

        private void OnEnable() => Center.GetOrCreate<ClanService>().OnGetHelpResourceData += OnGetData;

        private void OnDisable() => Center.GetOrCreate<ClanService>().OnGetHelpResourceData -= OnGetData;

        /// <summary>Task 6 gọi hàm này ngay sau khi mở popup để tải danh sách yêu cầu trợ giúp.</summary>
        public void Bind()
        {
            SetActiveSafe(_emptyObj, false);
            SetActiveSafe(_loadingObj, true);
            _data.Clear();
            ShowScrollViewByData();

            new CSGetHelpResourceData().Send();
        }

        private void OnGetData(SCGetHelpResourceData payload)
        {
            _data.Clear();
            if (payload?.data != null) _data.AddRange(payload.data);

            SetActiveSafe(_loadingObj, false);
            SetActiveSafe(_emptyObj, _data.Count == 0);

            ShowScrollViewByData();
        }

        /// <summary>Gọi khi 1 hàng nhận thành công, dùng bởi HelpResourceRow.</summary>
        public void IncreaseSuccess(int index)
        {
            if (index < 0 || index >= _data.Count) return;
            --Center.GetOrCreate<ClanService>().TotalHelpResource;
            _data.RemoveAt(index);
            SetActiveSafe(_emptyObj, _data.Count == 0);
            ShowScrollViewByData();
        }

        public void Close() => UIWrapper.ClosePopup(transform);

        private void ShowScrollViewByData()
        {
            int count = _data.Count;
            if (count == 0 || _loopListView == null) return;

            if (!_initScroll)
            {
                _initScroll = true;
                _loopListView.InitListView(count, OnGetItemByIndex);
            }
            else
            {
                _loopListView.SetListItemCount(count, false);
            }

            _loopListView.RefreshAllShownItem();

            if (_txtTotal != null) _txtTotal.text = count.ToString();
        }

        private LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
        {
            if (index < 0 || index >= _data.Count) return null;

            var itemData = _data[index];
            if (itemData == null) return null;

            var item = listView.NewListViewItem(_loopListView.ItemPrefabDataList[0].mItemPrefab.name);
            var itemScript = item.GetComponent<HelpResourceRow>();
            itemScript.Init(itemData, this, index);
            return item;
        }

        private void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
