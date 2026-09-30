using SuperScrollView;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;

namespace Falcon.Modules.ChatRoom.Runtime
{
    public class ChatBoxBase : MonoBehaviour
    {
        public string room_type;
        public string room_id;
        //public int thresholeToLoadNewPage = 10;

        private List<ChatRoomMessageData> _chatRoomMessages = new List<ChatRoomMessageData>();
        private int _currentPage = 0, _totalPages = 0;
        private int _newPageWaiting = 0;
        private bool _getFirstPageSuccess = false;
        private CSGetMessages _csGetFirstPage = null;
        private DateTime _nextTimeCanGetPage = DateTime.MinValue;

        public virtual int NewPageWaiting { get => _newPageWaiting; set
            {
                _newPageWaiting = value;
                //Có thể override và thêm phần này
                //if (_newPageWaiting == 0)
                //    SetActiveLoading(false);
                //else
                //    SetActiveLoading(true);
            }
        }

        protected virtual void OnEnable()
        {
            waitForInsertPage = false;
            _csGetFirstPage = null;
            _getFirstPageSuccess = false;
            //ChatRoomEventManager.onSCGetRoomMessagesAction += OnGetPageSuccess;
            ChatRoomEventManager.onSCNewMessageAction += OnNewMessage;
            ChatRoomEventManager.onSCUpdateMessageAction += OnUpdateMessage;
            ChatRoomEventManager.onSCDeleteMessageAction += OnDeleteMessage;
            ChatRoomEventManager.onSCInteractMessageAction += OnInteractMessage;

            if (room_type != "" && room_id != "")
                GetFirstPage();
        }

        protected virtual void OnDisable()
        {
            _csGetFirstPage = null;
            _getFirstPageSuccess = false;
            //ChatRoomEventManager.onSCGetRoomMessagesAction -= OnGetPageSuccess;
            ChatRoomEventManager.onSCNewMessageAction -= OnNewMessage;
            ChatRoomEventManager.onSCUpdateMessageAction -= OnUpdateMessage;
            ChatRoomEventManager.onSCDeleteMessageAction -= OnDeleteMessage;
            ChatRoomEventManager.onSCInteractMessageAction -= OnInteractMessage;
        }

        #region Get data / On data
        public virtual void GetFirstPage()
        {
            _totalPages = 0;
            NewPageWaiting = 0;
            _chatRoomMessages.Clear();
            ShowScrollviewByData();

            ForceGetPage(0);
        }

        private bool waitForInsertPage = false;
        IEnumerator IEWaitAndGetPage(int page)
        {
            waitForInsertPage = true;
            yield return new WaitUntil(() => DateTime.Now >= _nextTimeCanGetPage);
            ForceGetPage(page);
        }
        protected virtual void ForceGetPage(int page)
        {
            _currentPage = page;
            ++NewPageWaiting;
            CSGetMessages cs = new CSGetMessages(room_type, room_id, page);
            if (page == 0)
            {
                _csGetFirstPage = cs;
                _getFirstPageSuccess = false;
            }
            cs.AddSCListenerExt<SCGetMessages>((csMessage, scMessage, timeout, success) =>
            {
                if (success && scMessage != null)
                {
                    if (csMessage == _csGetFirstPage)
                        _getFirstPageSuccess = true;
                    
                    if (_getFirstPageSuccess == true)
                        OnGetPageSuccess(scMessage);
                }
                else if (timeout)
                    Debug.LogError("Time out!");
                else
                    Debug.LogError("Failed!");

            }, int.MaxValue).Send();
        }

        protected virtual void OnGetPageSuccess(SCGetMessages data)
        {
            if (_csGetFirstPage == null) return; // Đang không bật object
            if (gameObject == null || gameObject.activeInHierarchy == false) return;
            if (data.room_type != room_type || data.room_id != room_id) return;

            _totalPages = data.total_pages;

            StartCoroutine(InsertPageAtomic(data));
        }
        IEnumerator InsertPageAtomic(SCGetMessages data)
        {
            var sr = _loopListView.ScrollRect;

            // Tạm dừng chuyển động để lệnh MovePanel không bị ghi đè
            bool inertiaWas = sr && sr.inertia;
            if (sr) { sr.StopMovement(); sr.velocity = Vector2.zero; sr.inertia = false; }

            // Chèn & cập nhật view (KHÔNG reset pos)

            var pos = _loopListView.GetFirstShownItemIndexAndOffset();

            _chatRoomMessages.InsertRange(0, data.messages);

            ShowScrollviewByData();//Debug.LogError("DONE");

            Canvas.ForceUpdateCanvases();

            // Giữ nguyên viewport (bù số item vừa thêm)

            if (data.page == 0)
            {
                MoveToBottom();
            }
            else
            {
                _loopListView.MovePanelToItemIndex(pos.mItemIndex + data.messages.Count, pos.mItemOffset);
            }

            // Cho hệ thống UI/drag khép frame hiện tại
            yield return null;

            // Khôi phục
            if (sr) sr.inertia = inertiaWas;
            sr.velocity = Vector2.zero;

            waitForInsertPage = false;
            --NewPageWaiting;
            _nextTimeCanGetPage = DateTime.Now.AddSeconds(1);
        }

        protected virtual void OnNewMessage(SCNewMessage data)
        {
            if (_csGetFirstPage == null || _getFirstPageSuccess == false) return;
            if (data.message.room_type != room_type || data.message.room_id != room_id) return;

            bool isAtBottom = IsAtBottom();
            _chatRoomMessages.Add(data.message);
            ShowScrollviewByData();
            if (isAtBottom)
                MoveToBottom();
        }

        protected virtual void OnUpdateMessage(SCUpdateMessage data)
        {
            if (_csGetFirstPage == null || _getFirstPageSuccess == false) return;
            if (data.message.room_type != room_type || data.message.room_id != room_id) return;

            int messageNeedToUpdateIndex = GetMessageIndexByUuid(data.message.message_uuid);
            if (messageNeedToUpdateIndex == -1) return;

            _chatRoomMessages[messageNeedToUpdateIndex] = data.message;
            int itemIndex = GetItemIndexByMessageIndex(messageNeedToUpdateIndex);
            var shown = _loopListView.GetShownItemByItemIndex(itemIndex);
            if (shown != null)
            {
                shown.GetComponent<ChatRoomMessageUIBase>().UpdateMessage(data.message);
                ShowScrollviewByData(); // Để Init nếu chưa init thôi hehe
                _loopListView.RefreshItemByItemIndex(itemIndex);
                _loopListView.OnItemSizeChanged(itemIndex);
            }
        }

        protected virtual void OnDeleteMessage(SCDeleteMessage data)
        {
            if (_csGetFirstPage == null || _getFirstPageSuccess == false) return;
            if (data.room_type != room_type || data.room_id != room_id) return;

            int messageNeedToDeleteIndex = GetMessageIndexByUuid(data.message_uuid);
            if (messageNeedToDeleteIndex == -1) return;

            var pos = _loopListView.GetFirstShownItemIndexAndOffset();
            _chatRoomMessages.RemoveAt(messageNeedToDeleteIndex);
            ShowScrollviewByData();

            //Cũ
            /*int shift = (messageNeedToDeleteIndex <= pos.mItemIndex) ? 1 : 0;
            int targetIndex = Mathf.Clamp(pos.mItemIndex - shift, 0, Mathf.Max(0, _chatRoomMessages.Count - 1));
            _loopListView.MovePanelToItemIndex(targetIndex, pos.mItemOffset);*/

            // 4) Tính bù index theo hệ VIEW
            int deletionViewIndex = GetItemIndexByMessageIndex(messageNeedToDeleteIndex);
            int shift = (deletionViewIndex <= pos.mItemIndex) ? 1 : 0;
            // targetIndex là VIEW index (cùng hệ với pos.mItemIndex)
            int maxViewIndex = Mathf.Max(0, ViewCount - 1);      // nếu không muốn nhắm vào footer, dùng ViewCount - 2
            int targetIndex = Mathf.Clamp(pos.mItemIndex - shift, 0, maxViewIndex);
            // 5) Trả viewport về vị trí cũ
            _loopListView.MovePanelToItemIndex(targetIndex, pos.mItemOffset);
        }

        //Tạm bỏ, vô dụng
        protected virtual void OnInteractMessage(SCInteractMessage data)
        {
            if (_csGetFirstPage == null || _getFirstPageSuccess == false) return;
            if (data.room_type != room_type || data.room_id != room_id) return;

            int messageNeedToUpdateIndex = GetMessageIndexByUuid(data.message_uuid);
            if (messageNeedToUpdateIndex == -1) return;

            int itemIndex = GetItemIndexByMessageIndex(messageNeedToUpdateIndex);
            _loopListView.GetShownItemByItemIndex(itemIndex)?.GetComponent<ChatRoomMessageUIBase>().InteractMessage(data.message_interact_content);

            ShowScrollviewByData();
        }

        private int GetMessageIndexByUuid(string uuid)
        {
            for (int i = 0; i < _chatRoomMessages.Count; i++)
                if (_chatRoomMessages[i].message_uuid == uuid)
                    return i;
            return -1;
        }
        private int GetItemIndexByMessageIndex(int index)
        {
            return index + headerCount;
        }
        #endregion

        #region Super Scrollview
        [SerializeField] protected LoopListView2 _loopListView; 
        [SerializeField] float _topPadding = 460f;
        [SerializeField] float _bottomPadding = 550f;
        int headerCount = 1;
        int footerCount = 1;

        private bool _initScroll = false;
        private int ViewCount => _chatRoomMessages.Count + headerCount + footerCount;

        private void ShowScrollviewByData(bool resetPos = false, bool refreshAllItem = false)
        {
            //Debug.LogError(_initScroll + " " + _chatRoomMessages.Count);
            if (!_initScroll)
            {
                _initScroll = true;
                _loopListView.InitListView(ViewCount, OnGetItemByIndex);
            }
            else
            {
                _loopListView.SetListItemCount(ViewCount, resetPos);
            }
            if (refreshAllItem == true)
                _loopListView.RefreshAllShownItem();
        }

        private LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
        {
            int indexInData = index - headerCount;
            //Debug.LogError("INDEX: " + index + " INDEX_IN_DATA: " + indexInData + " Count: " + _chatRoomMessages.Count + " " + ViewCount + " " + _loopListView.ItemTotalCount);

            if ((headerCount == 1 && index == 0) || (footerCount == 1 && index == ViewCount - 1))
            {
                var spacer = _loopListView.NewListViewItem("FooterSpacer"); // prefab rỗng có RectTransform
                if (index == ViewCount - 1) 
                    spacer.CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _bottomPadding);
                else
                    spacer.CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _topPadding);
                return spacer;
            }

            //if (index <= thresholeToLoadNewPage && NewPageWaiting == 0 && _currentPage < _totalPages - 1)
            if (indexInData <= 0 && NewPageWaiting == 0 && _currentPage < _totalPages - 1 && waitForInsertPage == false)
            {
                StartCoroutine(IEWaitAndGetPage(_currentPage + 1));
                //ForceGetPage(_currentPage + 1); 
                return null;
            }
            if (_chatRoomMessages == null || indexInData < 0 || indexInData >= _chatRoomMessages.Count) return null;

            ChatRoomMessageData itemData = _chatRoomMessages[indexInData];
            if (itemData == null)
                return null;
            var item = listView.NewListViewItem(ChatRoomManager.GetMessagePrefabName(itemData));
            var itemScript = item.GetComponent<ChatRoomMessageUIBase>();
            if (!item.IsInitHandlerCalled)
            {
                item.IsInitHandlerCalled = true;
            }

            itemScript.InitChatBox(this);
            itemScript.Init(itemData);

            return item;
        }
        #endregion

        private bool IsAtBottom()
        {
            // Xác định “đang ở đáy” bằng nearest snap index hoặc khoảng cách
            int last = ViewCount - 1;
            //var nearest = _loopListView.CurSnapNearestItemIndex;
            //if (nearest == last) return true;

            // fallback nhẹ
            var shown = _loopListView.GetShownItemByItemIndex(last);
            return shown != null; // item cuối đang hiển thị ~ đang rất gần đáy
        }
        protected void MoveToBottom()
        {
            //Debug.LogError("MOVE TO BOTTOM");
            _loopListView.MovePanelToItemIndex(ViewCount - 1, 0);
        }
    }
}
