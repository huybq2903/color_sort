using Cysharp.Threading.Tasks;
using SuperScrollView;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class ListClanPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _loadingPref;
        [SerializeField] protected LoopListView2 _loopListView;
        [SerializeField] float _topPadding = 0f;
        [SerializeField] float _bottomPadding = 350f;

        private GameObject _loadingObj;
        private bool _initScroll = false;
        int headerCount = 1;
        int footerCount = 1;
        int ViewCount => _data.Length + headerCount + footerCount;

        private ClanDataShort[] _data = new ClanDataShort[0];
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
            // Request random list khi panel mo, qua UniTask thay vi tu gui CS/nghe SC
            SetActiveSafe(LoadingObj, true);
            _data = new ClanDataShort[0];
            ShowScrollviewByData();
            RequestRandomList().Forget();
        }

        private async UniTaskVoid RequestRandomList()
        {
            int requestId = ++_requestId;
            var sc = await Center.GetOrCreate<ClanService>().FetchRandomList(this.GetCancellationTokenOnDestroy());
            if (requestId != _requestId) return; // co request moi hon (panel disable/enable lai), bo ket qua tre

            if (sc != null) OnGetListClan(sc);
            else Center.GetOrCreate<ClanService>().ShowToastTimeout();
            SetActiveSafe(LoadingObj, false);
        }

        private void OnGetListClan(SCRandomListClan sc)
        {
            _data = sc?.clan_infos;
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
