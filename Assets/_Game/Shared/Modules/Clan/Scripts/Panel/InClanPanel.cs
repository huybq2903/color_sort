using Cysharp.Threading.Tasks;
using Falcon.Modules.Core.Network;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class InClanPanel : MonoBehaviour
    {
        [SerializeField] private ChatRoomPanel _chatRoomPanel;
        [SerializeField] private RectTransform _loadingPref;
        private GameObject _loadingObj;
        private ClanData _clanData;
        private int _requestId;

        private GameObject LoadingObj
        {
            get
            {
                if (_loadingObj == null)
                {
                    // Khong co prefab thi chiu
                    if (_loadingPref == null) return null;

                    // Instantiate duoi owner, giu local = identity
                    RectTransform inst = Instantiate(_loadingPref, transform, false);

                    // Stretch full (neo full parent)
                    inst.anchorMin = Vector2.zero;
                    inst.anchorMax = Vector2.one;
                    inst.offsetMin = Vector2.zero;
                    inst.offsetMax = Vector2.zero;
                    inst.pivot = new Vector2(0.5f, 0.5f);
                    inst.anchoredPosition = Vector2.zero;
                    inst.sizeDelta = Vector2.zero;
                    inst.SetAsLastSibling();

                    _loadingObj = inst.gameObject;
                }
                return _loadingObj;
            }
        }

        private void OnEnable()
        {
            Center.GetOrCreate<ClanService>().OnClanInfo += OnMyClanData;
            DisableAll();

            // Request my clan info qua UniTask thay vi tu gui CS/nghe SC
            SetActiveSafe(LoadingObj, true);
            RequestMyClanInfo().Forget();
        }

        private async UniTaskVoid RequestMyClanInfo()
        {
            int requestId = ++_requestId;
            var sc = await Center.GetOrCreate<ClanService>().FetchClanInfo(0, this.GetCancellationTokenOnDestroy());
            if (requestId != _requestId) return; // co request moi hon (panel disable/enable lai), bo ket qua tre

            if (sc == null) Center.GetOrCreate<ClanService>().ShowToastTimeout();
            else OnMyClanData(sc);
            SetActiveSafe(LoadingObj, false);
        }

        private void OnDisable()
        {
            Center.GetOrCreate<ClanService>().OnClanInfo -= OnMyClanData;
        }

        private void OnMyClanData(SCClanInfo data)
        {
            if (!gameObject.activeInHierarchy) return;
            if (data?.clan_info == null || data.clan_info.code == 0 || data.my_clan == false) return;

            _clanData = data.clan_info;
            if (_chatRoomPanel != null)
            {
                _chatRoomPanel.InitClanData(_clanData);
                if (!_chatRoomPanel.gameObject.activeSelf)
                    SetActiveSafe(_chatRoomPanel.gameObject, true);
            }
        }

        private void DisableAll()
        {
            if (_chatRoomPanel != null)
                SetActiveSafe(_chatRoomPanel.gameObject, false);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
