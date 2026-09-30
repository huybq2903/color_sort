/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-06
 */

using Falcon.Helpers.EventBus;
using Falcon.Shared.EasyPopup;
using Falcon.Shared.Tab;
using SuperScrollView;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Falcon.Shared.Common;

namespace Game.Shared.Profile
{
    public class PopupEditProfile : UIPopupBase<PopupEditProfile>, IPopupBackOnFade
    {
        [SerializeField] private Button btnClose, btnSave;
        [SerializeField] private TMP_InputField inputFieldName;
        [SerializeField] private UIProfileElement avatar, frame;

        private (int id, int indexInGrid) _currentAvatar, _currentFrame = (-1, -1);
        private LoopGridView _loopGrid;
        private UITab_Parent _uiTab;

        private void Awake()
        {
            _loopGrid = GetComponentInChildren<LoopGridView>();
            _loopGrid.InitGridView(0, OnGetItemByRowColumn);

            _uiTab = GetComponent<UITab_Parent>();
            _uiTab.lsChild[0].OnActive += OnTabAvatar;
            _uiTab.lsChild[1].OnActive += OnTabFrame;

            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(OnClickBack);

            btnSave.onClick.RemoveAllListeners();
            btnSave.onClick.AddListener(OnSave);
        }

        protected override void OnUIPopupEnable()
        {
            base.OnUIPopupEnable();
            SetAvatar(FGameDataProfile.Instance.avatarId);
            SetFrame(FGameDataProfile.Instance.frameId);
            ReloadAvatar();
            ReloadFrame();
            inputFieldName.text = FGameDataProfile.Instance.playerName;
            _uiTab.Active(0);
        }

        private LoopGridViewItem OnGetItemByRowColumn(LoopGridView gridView, int index, int row, int column)
        {
            if (index < 0) return null;
            LoopGridViewItem item = null;
            if (_uiTab.CurrentIndexTab == 0)
            {
                if (index >= ProfileManager.Config.avatarConfigs.Length) return null;
                item = gridView.NewListViewItem("AvatarItem");
                var avaItem = item.GetComponent<UIProfile_SelectItem>();
                var id = ProfileManager.Config.avatarConfigs[index].id;
                if (id == _currentAvatar.id) _currentAvatar.indexInGrid = index;
                avaItem.SetUnlock(ProfileManager.IsAvatarUnlock(id));
                avaItem.SetData(id, index, OnChosenAvatar);
                avaItem.SetSelected(id == _currentAvatar.id);
            }
            else if (_uiTab.CurrentIndexTab == 1)
            {
                if (index >= ProfileManager.Config.frameConfigs.Length) return null;
                item = gridView.NewListViewItem("FrameItem");
                var frameItem = item.GetComponent<UIProfile_SelectItem>();
                var id = ProfileManager.Config.frameConfigs[index].id;
                if (id == _currentFrame.id) _currentFrame.indexInGrid = index;
                frameItem.SetUnlock(ProfileManager.IsFrameUnlock(id));
                frameItem.SetData(id, index, OnChosenFrame);
                frameItem.SetSelected(id == _currentFrame.id);
            }

            return item;
        }

        private void OnChosenAvatar(int id, int indexInGrid)
        {
            var item = _loopGrid.GetShownItemByItemIndex(_currentAvatar.indexInGrid);
            if (item) item.GetComponent<UIProfile_SelectItem>().SetSelected(false);

            _currentAvatar = (id, indexInGrid);
            item = _loopGrid.GetShownItemByItemIndex(indexInGrid);
            if (item) item.GetComponent<UIProfile_SelectItem>().SetSelected(true);
            ReloadAvatar();
        }

        private void OnChosenFrame(int id, int indexInGrid)
        {
            var item = _loopGrid.GetShownItemByItemIndex(_currentFrame.indexInGrid);
            if (item) item.GetComponent<UIProfile_SelectItem>().SetSelected(false);

            _currentFrame = (id, indexInGrid);
            item = _loopGrid.GetShownItemByItemIndex(indexInGrid);
            if (item) item.GetComponent<UIProfile_SelectItem>().SetSelected(true);
            ReloadFrame();
        }

        private void OnTabAvatar()
        {
            CenterGrid(ProfileManager.Config.avatarConfigs.Length);
            _loopGrid.SetListItemCount(ProfileManager.Config.avatarConfigs.Length);
            _loopGrid.RefreshAllShownItem();
            _loopGrid.MovePanelToItemByIndex(_currentAvatar.indexInGrid, 0, -150);
        }

        private void OnTabFrame()
        {
            CenterGrid(ProfileManager.Config.frameConfigs.Length);
            _loopGrid.SetListItemCount(ProfileManager.Config.frameConfigs.Length);
            _loopGrid.RefreshAllShownItem();
            _loopGrid.MovePanelToItemByIndex(_currentFrame.indexInGrid, 0, -150);
        }

        /// <summary>LoopGridView chỉ xếp từ trái sang, bù padding trái/phải để khối grid nằm giữa viewport.</summary>
        private void CenterGrid(int itemCount)
        {
            // GetItemIndexByRowColumn(1,0) = 1 * số cột cố định, lib không expose field này
            var fixedColumnCount = _loopGrid.GetItemIndexByRowColumn(1, 0);
            if (fixedColumnCount <= 0 || _loopGrid.ItemSizeWithPadding.x <= 0) return;

            var columns = Mathf.Min(itemCount, fixedColumnCount);
            var contentWidth = columns * _loopGrid.ItemSizeWithPadding.x - _loopGrid.ItemPadding.x;
            var pad = Mathf.Max(0, Mathf.RoundToInt((_loopGrid.ViewPortWidth - contentWidth) / 2));

            var current = _loopGrid.Padding;
            if (current.left == pad && current.right == pad) return;

            _loopGrid.Padding = new RectOffset(pad, pad, current.top, current.bottom);
        }

        private void OnSave()
        {
            if (inputFieldName.text.IsValidName())
            {
                FGameDataProfile.Instance.avatarId = _currentAvatar.id;
                FGameDataProfile.Instance.frameId = _currentFrame.id;
                FGameDataProfile.Instance.playerName = inputFieldName.text;
                FGameDataProfile.Instance.UpdateToServer();

                GameEvent.Emit(ProfileManager.EVENT_SAVED);
                OnClickBack();
            }
            else
            {
                GameEvent<string>.Emit(GameKeys.TOAST_OPEN, "toast_name_is_not_valid");
            }
        }

        private void SetAvatar(int id) => _currentAvatar.id = id;
        private void ReloadAvatar() => avatar.ActiveItem(_currentAvatar.id);
        private void SetFrame(int id) => _currentFrame.id = id;
        private void ReloadFrame() => frame.ActiveItem(_currentFrame.id);
    }
}