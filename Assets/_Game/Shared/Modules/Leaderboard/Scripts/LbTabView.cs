// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-08-14

using System;
using SuperScrollView;
using UnityEngine;

namespace Game.Shared.Leaderboard
{
    /// <summary>
    /// Khung hiển thị một bảng xếp hạng. Không biết category/type là gì, không gọi server —
    /// đưa cho nó một LbPage là nó tự dựng. Bê sang màn khác dùng được ngay.
    /// </summary>
    public class LbTabView : MonoBehaviour
    {
        [SerializeField] private LoopListView2 _list;

        [Tooltip("Dòng của mình, để trống nếu bảng này không có")]
        [SerializeField] private ALbRowItem _myRow;

        [SerializeField] private GameObject _loading, _empty;

        [Header("List")]
        [SerializeField] private float _paddingTop = 50f;
        [SerializeField] private float _paddingBottom = 80f;

        [Tooltip("Thời gian cuộn của GoTop/GoBottom/GoMe; 0 là nhảy tức thì")]
        [SerializeField] private float _scrollDuration = 0.25f;

        private LbPage _page;
        private string _rowName, _spacerName;

        /// <summary>Vị trí dòng của mình trong list (đã cộng spacer đầu); -1 nếu không có trong bảng.</summary>
        private int _myViewIndex = -1;

        /// <summary>Kiểu data cần lấy về, suy từ prefab dòng. Bên gọi đọc để biết parse ra gì.</summary>
        public Type EntryType { get; private set; }

        private int RowCount => _page?.entries?.Count ?? 0;

        // 1 spacer đầu + 1 spacer cuối, vì LoopListView2 chỉ có mPadding giữa các item
        private int ViewCount => RowCount + 2;

        /// <summary>Đổ một trang data ra màn hình; null coi như bảng rỗng.</summary>
        public void Show(LbPage page)
        {
            if (_rowName == null) return;
            _page = page;
            _list.ResetListView(false);
            _list.SetListItemCount(ViewCount);
            _list.MovePanelToItemIndex(0, 0);
            _list.RefreshAllShownItem();

            _myViewIndex = _page?.entries?.FindIndex(e => e.IsMe) + 1 ?? -1;
            UpdateMyRow();
            if (_empty != null) _empty.SetActive(RowCount == 0);
            if (_myRow != null && _page?.me != null) _myRow.Bind(_page.me);
            SetLoading(false);
        }

        public void SetLoading(bool on)
        {
            if (_loading != null) _loading.SetActive(on);
        }

        public void Init()
        {
            if (_list == null) { Debug.LogError($"[Leaderboard] {name}: chưa gán list."); return; }

            // Doc thang ItemPrefabDataList: co ALbRowItem la dong, khong co la spacer
            foreach (var data in _list.ItemPrefabDataList)
            {
                if (data.mItemPrefab == null) continue;

                if (data.mItemPrefab.TryGetComponent<ALbRowItem>(out var row))
                {
                    _rowName = data.mItemPrefab.name;
                    EntryType = row.EntryType;
                }
                else
                {
                    _spacerName = data.mItemPrefab.name;
                }
            }

            if (_spacerName == null) Debug.LogError($"[Leaderboard] {name}: ItemPrefabDataList thiếu prefab đệm.");
            if (_rowName == null)
            {
                Debug.LogError($"[Leaderboard] {name}: ItemPrefabDataList thiếu prefab có ALbRowItem.");
                return;
            }

            // Init trước, data sau — giống NestedLeftRightItem trong demo của SuperScrollView
            _list.InitListView(0, CreateItem);
            _list.ScrollRect.onValueChanged.AddListener(_ => UpdateMyRow());
        }

        private LoopListViewItem2 CreateItem(LoopListView2 list, int index)
        {
            if (index == 0 || index == ViewCount - 1)
            {
                var spacer = list.NewListViewItem(_spacerName);
                spacer.CachedRectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, index == 0 ? _paddingTop : _paddingBottom);
                return spacer;
            }

            var i = index - 1;
            if (i < 0 || i >= RowCount) return null;

            var item = list.NewListViewItem(_rowName);
            item.GetComponent<ALbRowItem>().Bind(_page.entries[i]);
            return item;
        }

        /// <summary>Dòng thật của mình trồi lên trên thanh MyRow thì giấu thanh đó đi, khỏi hiện hai lần.</summary>
        private void UpdateMyRow()
        {
            if (_myRow == null) return;

            var show = _page?.me != null && !(_myViewIndex >= 0 && PassedMyRow());
            _myRow.gameObject.SetActive(show);
        }

        /// <summary>Dòng thật đã vượt lên trên thanh MyRow chưa; cuộn khuất hẳn lên trên vẫn tính là rồi.</summary>
        private bool PassedMyRow()
        {
            // world space: MyRow neo cung nen doc duoc ca khi dang tat
            var item = _list.GetShownItemByItemIndex(_myViewIndex);
            if (item != null)
                return item.CachedRectTransform.position.y > _myRow.transform.position.y;

            // Khong con hien: o tren dau danh sach dang thay nghia la da vuot qua, con lai la chua toi
            var first = _list.GetShownItemByIndex(0);
            return first != null && _myViewIndex < first.ItemIndex;
        }

        public void GoTop() => MoveTo(0);

        public void GoBottom() => MoveTo(ViewCount - 1);

        public void GoMe()
        {
            if (_myViewIndex < 0) return;
            MoveTo(_myViewIndex, true);
        }

        private void MoveTo(int viewIndex, bool centered = false)
        {
            if (RowCount == 0) return;

            // offset tính từ mép đầu viewport, càng lớn càng lùi sâu vào trong
            var offset = centered ? _list.ViewPortSize * 0.5f : 0f;
            _list.MovePanelToItemIndex(viewIndex, offset, _scrollDuration);

            // Nhay tuc thi thi ScrollRect chua kip ban onValueChanged, cap nhat luon cho khoi giat 1 frame
            if (_scrollDuration <= 0f) UpdateMyRow();
        }
    }
}
