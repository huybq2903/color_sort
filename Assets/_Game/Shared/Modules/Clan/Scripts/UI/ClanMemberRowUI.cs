using Falcon.Modules.Core.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class ClanMemberRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rankTmp;
        [SerializeField] private TextMeshProUGUI _nameTxt;
        [SerializeField] private TextMeshProUGUI _scoreTmp;
        [SerializeField] private TextMeshProUGUI _levelTmp;
        [SerializeField] private Image _topImg;
        [SerializeField] private Sprite[] _topSprs;
        [SerializeField] private GameObject _ownerObj, _coOwnerObj;

        [SerializeField] private UIPopupClanInfo _infoPopup;
        [SerializeField] private Transform _avatarUIPos;
        [SerializeField] private Transform _nameStyleUIPos;

        [Header("For row interaction")]
        [SerializeField] private Image _bubble;
        [SerializeField] private RectTransform _rectPosBottom, _rectPosTop, _viewport;
        [SerializeField] private Sprite _topBubbleSprite, _bottomBubbleSprite;

        [Header("Bubble Btns")]
        [SerializeField] private GameObject _setCoLeaderBtn;
        [SerializeField] private GameObject _removeCoLeaderBtn;
        [SerializeField] private GameObject _removeMemberBtn;
        [SerializeField] private GameObject _profileBtn;

        private ClanMemberInfo _data;
        private int _index = 0;
        private CSSetMemberRoleInClan _csSetMemberRoleInClan = null;
        private HorizontalLayoutGroup _horizontalLayoutGroupBubble = null;

        private void Awake()
        {
            if (_nameTxt != null) _nameTxt.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void OnEnable()
        {
            if (_infoPopup != null)
                _infoPopup.HideBubbleAction += HideBubble;
        }

        private void OnDisable()
        {
            if (_infoPopup != null)
                _infoPopup.HideBubbleAction -= HideBubble;
        }

        public void SetItemData(ClanMemberInfo data, int index)
        {
            HideBubble();
            _csSetMemberRoleInClan = null;
            _data = data;
            _index = index;

            if (_data == null)
            {
                Debug.LogError("ClanMemberRowUI: Data null");
                return;
            }

            int rank = (index >= 0) ? index + 1 : 0;
            if (_rankTmp != null)
                _rankTmp.text = rank > 0 ? rank.ToString() : "999+";

            if (_nameTxt != null) _nameTxt.text = _data.name;
            if (_scoreTmp != null) _scoreTmp.text = _data.contribution.ToString();
            if (_levelTmp != null) _levelTmp.text = _data.level.ToString();

            SetActiveSafe(_ownerObj, (ClanRole)_data.role == ClanRole.owner);
            SetActiveSafe(_coOwnerObj, (ClanRole)_data.role == ClanRole.co_owner);

            if (_topImg != null)
            {
                if (rank > 0 && _topSprs != null && rank <= _topSprs.Length)
                {
                    SetActiveSafe(_topImg.gameObject, true);
                    _topImg.sprite = _topSprs[rank - 1];
                }
                else
                {
                    SetActiveSafe(_topImg.gameObject, false);
                }
            }

            if (_avatarUIPos != null && _avatarUIPos.childCount > 0)
            {
                GameObject avtUIObj = _avatarUIPos.GetChild(0).GetChild(0).gameObject;
                Center.GetOrCreate<ClanService>().OnSetAvatarUI(avtUIObj, _data);

                var btn = avtUIObj.GetComponent<Button>();
                if (btn != null) Destroy(btn);
            }

            if (_nameStyleUIPos != null && _nameStyleUIPos.childCount > 0)
            {
                GameObject nameStyleUIObj = _nameStyleUIPos.GetChild(0).GetChild(0).gameObject;
                Center.GetOrCreate<ClanService>().OnSetNameStyleUI(_nameTxt.gameObject, _nameStyleUIPos.gameObject, nameStyleUIObj, _data);
            }
        }

        public void OnClick() => ShowBubble();

        private void ShowBubble()
        {
            if (_bubble == null || _rectPosBottom == null || _rectPosTop == null || _viewport == null)
                return;

            if (_infoPopup != null)
            {
                if (_infoPopup.LastRowSelected == this)
                {
                    _infoPopup.HideBubble();
                    return;
                }
                _infoPopup.HideBubble();
                _infoPopup.LastRowSelected = this;
            }

            transform.SetAsLastSibling();
            ResetButtonInBubble();

            _bubble.rectTransform.position = _rectPosBottom.position;
            _bubble.sprite = _bottomBubbleSprite;
            SetActiveSafe(_bubble.gameObject, true);

            // Reposition if overflow bottom
            var bubbleCorners = new Vector3[4];
            var viewportCorners = new Vector3[4];
            _bubble.rectTransform.GetWorldCorners(bubbleCorners);
            _viewport.GetWorldCorners(viewportCorners);

            float bubbleBottomY = bubbleCorners[0].y;
            float viewportBottomY = viewportCorners[0].y;
            if (_horizontalLayoutGroupBubble == null)
                _horizontalLayoutGroupBubble = _bubble.GetComponent<HorizontalLayoutGroup>();
            if (bubbleBottomY < viewportBottomY)
            {
                _bubble.rectTransform.position = _rectPosTop.position;
                _bubble.sprite = _topBubbleSprite;
                if (_horizontalLayoutGroupBubble != null)
                    _horizontalLayoutGroupBubble.childAlignment = TextAnchor.UpperLeft;
            }
            else
            {
                if (_horizontalLayoutGroupBubble != null)
                    _horizontalLayoutGroupBubble.childAlignment = TextAnchor.LowerLeft;
            }
        }

        public void HideBubble()
        {
            if (_bubble != null)
                SetActiveSafe(_bubble.gameObject, false);
        }

        private void ResetButtonInBubble()
        {
            if (_setCoLeaderBtn != null)
                SetActiveSafe(_setCoLeaderBtn, _data.canSetRole && (ClanRole)_data.role == ClanRole.member);
            if (_removeCoLeaderBtn != null)
                SetActiveSafe(_removeCoLeaderBtn, _data.canSetRole && (ClanRole)_data.role == ClanRole.co_owner);
            if (_removeMemberBtn != null)
                SetActiveSafe(_removeMemberBtn, _data.canRemove && _data.code != Center.GetOrCreate<ClanService>().YourPlayerCode());
            if (_profileBtn != null)
                SetActiveSafe(_profileBtn, true);
        }

        public void SetCoLeaderOnClick()
        {
            var cs = new CSSetMemberRoleInClan(_data.code, ClanRole.co_owner);
            SendCsSetRole(cs);
        }

        public void RemoveCoLeaderOnClick()
        {
            var cs = new CSSetMemberRoleInClan(_data.code, ClanRole.member);
            SendCsSetRole(cs);
        }

        public void RemoveOnClick()
        {
            if (_infoPopup != null)
                _infoPopup.ShowRemoveConfirm(_index);
        }

        public void ProfileOnClick()
        {
            if (_infoPopup != null) _infoPopup.HideBubble();
            Center.GetOrCreate<ClanService>().ShowPlayerInfo(_data.code);
        }

        private void SendCsSetRole(CSSetMemberRoleInClan cs)
        {
            if (_csSetMemberRoleInClan != null)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }

            _csSetMemberRoleInClan = cs;
            int newRole = cs.role;
            cs.AddSCListenerExt<SCResponse_Clan>((csMessage, scMessage, timeout, success) =>
            {
                if (csMessage == _csSetMemberRoleInClan)
                {
                    if (success && scMessage != null)
                    {
                        if (scMessage.success)
                        {
                            Center.GetOrCreate<ClanService>().ShowToastSuccess();
                            if (_infoPopup != null) _infoPopup.HideBubble();
                            _data.role = newRole;
                            SetItemData(_data, _index);
                        }
                        else
                        {
                            Center.GetOrCreate<ClanService>().ShowToast(scMessage.message);
                        }
                    }
                    else if (timeout) Center.GetOrCreate<ClanService>().ShowToastTimeout();
                    else Center.GetOrCreate<ClanService>().ShowToastFailed();
                }
                _csSetMemberRoleInClan = null;
            }, int.MaxValue).Send();
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
