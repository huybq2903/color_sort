using System;
using Cysharp.Threading.Tasks;
using Falcon.Modules.Core.UI.Runtime;
using TMPro;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    using SuperScrollView;

    /// <summary>Popup xem thông tin clan; prefab mang UIPopupAutoFade lo fade và nút back.</summary>
    public class UIPopupClanInfo : MonoBehaviour
    {
        [SerializeField] private ClanLogoUI _logo;

        [Header("Buttons")]
        [SerializeField] private GameObject _joinBtn;
        [SerializeField] private GameObject _pendingBtn;
        [SerializeField] private GameObject _leaveBtn;
        [SerializeField] private GameObject _editBtn;

        [Space]
        [SerializeField] private TextMeshProUGUI _txtName;
        [SerializeField] private TextMeshProUGUI _txtDescription;
        [SerializeField] private TextMeshProUGUI _txtMemberCount;
        [SerializeField] private TextMeshProUGUI _txtScore;
        [SerializeField] private TextMeshProUGUI _txtRequiredLevel;
        [SerializeField] private TextMeshProUGUI _txtTeamType;
        [SerializeField] private GameObject _loadingObj;
        [SerializeField] private GameObject _nullContentObj;
        [SerializeField] private GameObject _mainContentObj;
        [SerializeField] private EditClanPanel _editClanPanel;
        [SerializeField] private LeaveConfirmPanel _leaveConfirmPanel;

        [Space]
        [SerializeField] private LoopListView2 _loopListView;
        [SerializeField] private float _topPadding = 0f;
        [SerializeField] private float _bottomPadding = 550f;

        [Space]
        [SerializeField] private RemoveConfirmPanel _removeConfirmPanel;

        private int _clanCode;
        private ClanData _clanData;
        private bool _initScroll;
        private const int HeaderCount = 1;
        private const int FooterCount = 1;
        private int ViewCount => (_clanData?.member_infos?.Count ?? 0) + HeaderCount + FooterCount;

        private bool _joining;

        /// <summary>Hàng đang mở bubble, dùng bởi ClanMemberRowUI.</summary>
        public ClanMemberRowUI LastRowSelected { get; set; }
        /// <summary>Gọi khi cần ẩn bubble của hàng member, dùng bởi ClanMemberRowUI.</summary>
        public Action HideBubbleAction { get; set; }
        /// <summary>Được set true khi mở từ panel danh sách clan, đóng popup ngay nếu join thành công.</summary>
        public bool CloseOnJoinSuccess { get; set; }

        private void OnEnable()
        {
            Center.GetOrCreate<ClanService>().OnClanChanged += OnMyClanChanged;
            Center.GetOrCreate<ClanService>().OnClanInfo += OnClanInfo;
            Center.GetOrCreate<ClanService>().OnSetMemberRole += OnSetMemberRoleInClan;
            Center.GetOrCreate<ClanService>().OnChangeUserData += RefreshNoLoading;
        }

        private void OnDisable()
        {
            Center.GetOrCreate<ClanService>().OnClanChanged -= OnMyClanChanged;
            Center.GetOrCreate<ClanService>().OnClanInfo -= OnClanInfo;
            Center.GetOrCreate<ClanService>().OnSetMemberRole -= OnSetMemberRoleInClan;
            Center.GetOrCreate<ClanService>().OnChangeUserData -= RefreshNoLoading;
        }

        /// <summary>Task 6 gọi hàm này ngay sau khi mở popup để đổ dữ liệu clan.</summary>
        public void Bind(ClanData data)
        {
            ApplyData(data);
        }

        public void JoinOnClick()
        {
            if (_joining)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }

            _joining = true;
            DoJoin().Forget();
        }

        // Qua UniTask thay vì tự gửi CSJoinClanReq/nghe SCResponse_Clan trực tiếp, nhưng vẫn nhận đúng SCResponse_Clan để giữ 3 nhánh gốc
        private async UniTaskVoid DoJoin()
        {
            try
            {
                var scMessage = await Center.GetOrCreate<ClanService>().Join(_clanCode, this.GetCancellationTokenOnDestroy());

                if (scMessage == null)
                {
                    // Gộp timeout + lỗi gửi khác thành 1 toast, ClanService.Join không phân biệt hai ca này
                    Center.GetOrCreate<ClanService>().ShowToastFailed();
                    return;
                }

                if (scMessage.success)
                {
                    if (scMessage.message != "Successfully joined team")
                    {
                        SetActiveSafe(_joinBtn, false);
                        SetActiveSafe(_pendingBtn, true);
                    }
                    else if (CloseOnJoinSuccess) // Được join luôn, và popup mở từ panel danh sách clan
                    {
                        Close();
                    }
                    // Được join luôn nhưng mở từ nơi khác: để OnClanChanged/OnClanInfo tự refresh nút
                }
                else
                {
                    Center.GetOrCreate<ClanService>().ShowToast(scMessage.message);
                }
            }
            finally
            {
                _joining = false;
            }
        }

        public void LeaveOnClick()
        {
            LeaveConfirmPanel.onConfirmSuccess = Close;
            _leaveConfirmPanel?.OnShow();
        }

        public void EditOnClick()
        {
            _editClanPanel?.InitClanData(_clanData);
            _editClanPanel?.OnShow();
        }

        /// <summary>Ẩn bubble của hàng member đang chọn.</summary>
        public void HideBubble()
        {
            LastRowSelected = null;
            HideBubbleAction?.Invoke();
        }

        /// <summary>Mở panel xác nhận xoá member tại index, dùng bởi ClanMemberRowUI.</summary>
        public void ShowRemoveConfirm(int index)
        {
            HideBubble();

            if (_removeConfirmPanel == null) return;

            _removeConfirmPanel.onSuccessAction = () =>
            {
                if (_clanData?.member_infos == null || index < 0 || index >= _clanData.member_infos.Count) return;
                _clanData.num_member--;
                UpdateMemberCountText();
                _clanData.member_infos.RemoveAt(index);
                ShowScrollviewByData();
            };

            if (_clanData?.member_infos != null && index >= 0 && index < _clanData.member_infos.Count)
                _removeConfirmPanel.OnShow(_clanData.member_infos[index].code);
        }

        public void Close() => UIWrapper.ClosePopup(transform);

        private void RefreshNoLoading() => Refresh(false);

        private void Refresh(bool showLoading = true)
        {
            HideBubble();

            if (showLoading)
            {
                SetActiveSafe(_mainContentObj, false);
                SetActiveSafe(_nullContentObj, false);
                SetActiveSafe(_loadingObj, true);
            }

            DoRefresh().Forget();
        }

        // Qua UniTask thay vì tự gửi CSClanInfo/nghe SCClanInfo
        private async UniTaskVoid DoRefresh()
        {
            var scMessage = await Center.GetOrCreate<ClanService>().FetchClanInfo(_clanCode, this.GetCancellationTokenOnDestroy());
            if (scMessage == null) Center.GetOrCreate<ClanService>().ShowToastTimeout();
            if (scMessage?.clan_info != null)
            {
                scMessage.clan_info.editable = scMessage.editable;
                ApplyData(scMessage.clan_info);
            }
            SetActiveSafe(_loadingObj, false);
        }

        private void OnClanInfo(SCClanInfo data)
        {
            if (data?.clan_info == null)
            {
                Debug.LogError("Data null");
                return;
            }
            if (data.clan_info.code != _clanCode) return;
            data.clan_info.editable = data.editable;
            ApplyData(data.clan_info);
        }

        private void ApplyData(ClanData data)
        {
            _clanData = data;
            _clanCode = data.code;

            _logo?.Init(data.avatar_id);

            // Join/Pending: chỉ khi người chơi CHƯA ở clan nào (clanCode mặc định -1), không phải "khác clan đang xem"
            bool notInAnyClan = Center.GetOrCreate<ClanService>().MyClanCode <= 0;
            SetActiveSafe(_joinBtn, notInAnyClan && !data.requested);
            SetActiveSafe(_pendingBtn, notInAnyClan && data.requested);
            SetActiveSafe(_leaveBtn, Center.GetOrCreate<ClanService>().MyClanCode == data.code);
            SetActiveSafe(_editBtn, data.editable);

            if (_txtName != null) _txtName.text = data.name;
            if (_txtDescription != null)
            {
                _txtDescription.gameObject.SetActive(!string.IsNullOrEmpty(data.description));
                _txtDescription.text = data.description;
            }
            UpdateMemberCountText();
            if (_txtScore != null) _txtScore.text = data.score.ToString();
            if (_txtRequiredLevel != null) _txtRequiredLevel.text = data.required_level.ToString();
            if (_txtTeamType != null) _txtTeamType.text = data.open ? "Public" : "Private";

            SetActiveSafe(_mainContentObj, true);
            SetActiveSafe(_nullContentObj, false);

            ShowScrollviewByData();
        }

        private void UpdateMemberCountText()
        {
            if (_txtMemberCount != null && _clanData != null)
                _txtMemberCount.text = $"{_clanData.num_member}/{_clanData.max_member}";
        }

        /// <summary>Gộp onJoinClanAction+onLeaveClanAction cũ: leave thì refresh mọi popup, join thì refresh đúng clan đang xem.</summary>
        private void OnMyClanChanged()
        {
            int myClanCode = Center.GetOrCreate<ClanService>().MyClanCode;
            if (myClanCode == 0 || _clanCode == myClanCode) RefreshNoLoading();
        }

        private void OnSetMemberRoleInClan(SCSetMemberRoleInClan sc)
        {
            if (_clanCode == Center.GetOrCreate<ClanService>().MyClanCode) RefreshNoLoading();
        }

        private void ShowScrollviewByData()
        {
            if (_clanData == null || _loopListView == null) return;

            int count = ViewCount;
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
        }

        private LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
        {
            int indexInData = index - HeaderCount;

            if ((HeaderCount == 1 && index == 0) || (FooterCount == 1 && index == ViewCount - 1))
            {
                var spacer = _loopListView.NewListViewItem("Spacer");
                spacer.CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    index == ViewCount - 1 ? _bottomPadding : _topPadding);
                return spacer;
            }

            if (_clanData?.member_infos == null || indexInData < 0 || indexInData >= _clanData.member_infos.Count)
                return null;

            var itemData = _clanData.member_infos[indexInData];
            if (itemData == null) return null;

            var item = listView.NewListViewItem(_loopListView.ItemPrefabDataList[0].mItemPrefab.name);
            var itemScript = item.GetComponent<ClanMemberRowUI>();
            itemScript.SetItemData(itemData, indexInData);
            return item;
        }

        private void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
