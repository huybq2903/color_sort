using Cysharp.Threading.Tasks;
using SuperScrollView;
using TMPro;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class SearchClanPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _loadingPref;
        [SerializeField] private TMP_InputField _inputTeamName;
        [SerializeField] protected LoopListView2 _loopListView;
        [SerializeField] float _topPadding = 0f;
        [SerializeField] float _bottomPadding = 350f;

        private GameObject _loadingObj;
        private bool _initScroll = false;
        int headerCount = 1;
        int footerCount = 1;
        int ViewCount => _data.Length + headerCount + footerCount;

        private ClanDataShort[] _data;
        private bool _searching;
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
            ++_requestId; // huy ngam ket qua tim kiem cu neu panel tung tat giua chung roi bat lai
            SetActiveSafe(LoadingObj, false);
        }

        public void ClearOnClick()
        {
            if (_inputTeamName != null) _inputTeamName.text = string.Empty;
        }

        public void SearchOnClick()
        {
            if (_inputTeamName == null) return;
            if (_searching)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }

            _searching = true;
            SetActiveSafe(LoadingObj, true);
            DoSearch(_inputTeamName.text).Forget();
        }

        // Qua UniTask thay vi tu gui CSSearchClan/nghe SCResponse_Clan roi cho broadcast SCSearchClan
        private async UniTaskVoid DoSearch(string name)
        {
            int requestId = ++_requestId;
            try
            {
                var sc = await Center.GetOrCreate<ClanService>().Search(name, this.GetCancellationTokenOnDestroy());
                if (requestId != _requestId) return; // co request moi hon (bam lai hoac panel tat/bat lai), bo ket qua tre

                SetActiveSafe(LoadingObj, false);
                if (sc == null) { Center.GetOrCreate<ClanService>().ShowToastTimeout(); return; }
                OnRowData(sc.clan_infos);
            }
            finally
            {
                _searching = false;
            }
        }

        private void OnRowData(ClanDataShort[] clans)
        {
            _data = clans ?? System.Array.Empty<ClanDataShort>();
            ShowScrollviewByData();
        }

        private void ShowScrollviewByData()
        {
            if (_loopListView == null) return;
            int count = ViewCount;

            if (!_initScroll)
            {
                _initScroll = true;
                _loopListView.InitListView(count, OnGetItemByIndex);
            }
            else
            {
                _loopListView.SetListItemCount(count);
            }

            _loopListView.RefreshAllShownItem();
        }

        private LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
        {
            int indexInData = index - headerCount;
            if ((headerCount == 1 && index == 0) || (footerCount == 1 && index == ViewCount - 1))
            {
                var spacer = _loopListView.NewListViewItem("Spacer"); // prefab rong co RectTransform
                if (index == ViewCount - 1)
                    spacer.CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _bottomPadding);
                else
                    spacer.CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _topPadding);
                return spacer;
            }

            if (_data == null || indexInData < 0 || indexInData >= _data.Length) return null;

            var itemData = _data[indexInData];
            if (itemData == null) return null;

            var item = listView.NewListViewItem(_loopListView.ItemPrefabDataList[0].mItemPrefab.name);
            var itemScript = item.GetComponent<ClanRowUI>();
            if (!item.IsInitHandlerCalled) item.IsInitHandlerCalled = true;

            itemScript.Init(itemData);
            return item;
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
