using UnityEngine;

namespace SuperScrollView
{
    public class LoopListViewManager : MonoBehaviour
    {
        [SerializeField] private LoopListView2 _loopListView;
        [SerializeField] private int _maxItemPerRow = 3;

        private bool _init = false;
        private int _totalItemCount;

        public bool IsInit => _init;
        public int TotalItemCount => _totalItemCount;
        public LoopListView2 LoopListView => _loopListView;
        public int MaxItemPerRow => _maxItemPerRow;

        /// <summary>
        /// Initialize or update the underlying LoopListView with total item count and optional init parameters.
        /// Calculates the required number of rows based on <see cref="_maxItemPerRow"/> and configures the list view.
        /// </summary>
        /// <param name="totalItemCount">Total number of items to be displayed.</param>
        /// <param name="initParam">Optional initialization parameters for the list view.</param>
        /// <param name="onGetItemSizeByIndex">Optional delegate to provide item size and padding by item index.</param>
        public virtual void Setup(int totalItemCount, LoopListViewInitParam initParam = null, System.Func<int, (float, float)> onGetItemSizeByIndex = null)
        {
            if (_maxItemPerRow <= 0) _maxItemPerRow = 1;

            if (_loopListView == null) _loopListView.GetComponent<LoopListView2>();

            //Cache
            _totalItemCount = totalItemCount;

            int row = totalItemCount / _maxItemPerRow;
            if (totalItemCount % _maxItemPerRow > 0) row++;

            if (!_init)
            {
                _loopListView.InitListView(row, OnGetItemByIndex, initParam, onGetItemSizeByIndex);

                //Cache
                _init = true;
            }
            else
            {
                _loopListView.SetListItemCount(row, false);
            }
        }

        /// <summary>
        /// Refreshes the list view, optionally forcing an update by sending an "Update" message.
        /// This resets internal state and refreshes all visible items.
        /// </summary>
        /// <param name="isForceUpdate">If true, an "Update" message will be sent to force update handlers.</param>
        public virtual void RefreshListView(bool isForceUpdate = false)
        {
            _loopListView.ResetListView(false);
            _loopListView.RefreshAllShownItem();
            if (isForceUpdate) _loopListView.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Refreshes all currently shown items without resetting the list view state.
        /// </summary>
        public virtual void RefreshAllShowItem()
        {
            _loopListView.RefreshAllShownItem();
        }


        /// <summary>
        /// Moves the viewport to a specific row index in the list view.
        /// </summary>
        /// <param name="rowIndex">Target row index. If negative, will move to the first row.</param>
        /// <param name="offset">Offset to apply when moving the panel.</param>
        /// <param name="duration">Duration in seconds for the move animation.</param>
        public virtual void MoveToRowIndex(int rowIndex = -1, float offset = 0, float duration = 0)
        {
            if (rowIndex >= _loopListView.ItemTotalCount)
            {
                Debug.LogWarning("MoveToRowIndex: rowIndex out of range.");
                return;
            }

            _loopListView.MovePanelToItemIndex(rowIndex < 0 ? 0 : rowIndex, offset, duration);
        }

        /// <summary>
        /// Moves the viewport to the row that contains the specified item index.
        /// </summary>
        /// <param name="index">Item index to move to.</param>
        /// <param name="offset">Offset to apply when moving the panel.</param>
        /// <param name="duration">Duration in seconds for the move animation.</param>
        public virtual void MoveToIndex(int index = -1, float offset = 0, float duration = 0)
        {
            if (index < 0 || index >= _totalItemCount)
            {
                Debug.LogWarning("MoveToIndex: index out of range.");
                return;
            }

            int rowIndex = index / _maxItemPerRow;
            _loopListView.MovePanelToItemIndex(rowIndex, offset, duration);
        }

        /// <summary>
        /// Notify the list view that the size of a specific row has changed and trigger a layout update for that row.
        /// </summary>
        /// <param name="rowIndex">Index of the row whose size changed.</param>
        public virtual void OnRowItemSizeChanged(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _loopListView.ItemTotalCount)
            {
                Debug.LogWarning("OnRowItemSizeChanged: rowIndex out of range.");
                return;
            }

            _loopListView.OnItemSizeChanged(rowIndex);
        }

        /// <summary>
        /// Notify the list view that the size of a specific item has changed.
        /// The manager converts the item index to a row index before notifying the view.
        /// </summary>
        /// <param name="index">Index of the item whose size changed.</param>
        public virtual void OnItemSizeChanged(int index)
        {
            if (index < 0 || index >= _totalItemCount)
            {
                Debug.LogWarning("OnItemSizeChanged: index out of range.");
                return;
            }

            int rowIndex = index / _maxItemPerRow;
            _loopListView.OnItemSizeChanged(rowIndex);
        }

        /// <summary>
        /// Internal callback used by <see cref="LoopListView2"/> to create or update an item row when it is needed.
        /// This method prepares the row item and updates each child subitem according to <see cref="_maxItemPerRow"/>.
        /// </summary>
        /// <param name="view">Reference to the calling LoopListView2.</param>
        /// <param name="rowIndex">Row index being requested.</param>
        /// <returns>The prepared <see cref="LoopListViewItem2"/> for the requested row.</returns>
        protected virtual LoopListViewItem2 OnGetItemByIndex(LoopListView2 view, int rowIndex)
        {
            if (rowIndex < 0) return null;

            var item = view.NewListViewItem(view.ItemPrefabDataList[0].mItemPrefab.name);
            var itemScript = item.GetComponent<LoopListViewItem>();

            if (item.IsInitHandlerCalled == false)
            {
                item.IsInitHandlerCalled = true;
                itemScript.Init();
            }

            //Update all items in the row
            for (int i = 0; i < _maxItemPerRow; ++i)
            {
                int itemIndex = rowIndex * _maxItemPerRow + i;
                if (itemIndex >= _totalItemCount)
                {
                    itemScript.childItemList[i].gameObject.SetActive(false);
                    continue;
                }

                //update the subitem content.
                itemScript.childItemList[i].gameObject.SetActive(true);
                SetItemData(itemScript.childItemList[i], itemIndex);
            }

            return item;
        }

        /// <summary>
        /// Fill the provided item transform with data corresponding to the specified item index.
        /// Override this method in subclasses to populate item UI elements.
        /// </summary>
        /// <param name="item">Transform of the item to populate.</param>
        /// <param name="itemIndex">Index of the item to display.</param>
        protected virtual void SetItemData(Transform item, int itemIndex)
        {

        }
    }
}
