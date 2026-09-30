// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-08-12

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.UI.Menu.Runtime;
using Falcon.Shared.Common;
using Falcon.Shared.Tab;
using TMPro;
using UnityEngine;

namespace Game.Shared.Leaderboard
{
    /// <summary>Điều hướng giữa các LbTabView: cổng chờ và hai hàng nút. Không đụng gì tới list.</summary>
    public class UILeaderboardPanel : MonoBehaviour
    {
        [Serializable]
        public class LbCategory
        {
            public string id;

            [Tooltip("Bỏ tick để ẩn category chưa mở, ví dụ Team")]
            public bool active = true;

            [Tooltip("Khung hiển thị của category này; đổi type thì nạp lại chính khung đó")]
            public LbTabView view;
        }

        private const float FAKE_LOADING = 0.5f;

        // Nút bấm là lưới category x type, nhưng art chỉ có một list cho mỗi category.
        [Header("Tab")]
        [SerializeField] private List<LbCategory> _categories = new();
        [SerializeField] private string[] _typeIds = { "Global", "National" };
        [SerializeField] private UITab_Parent categoryTabs;
        [SerializeField] private UITab_Parent typeTabs;

        [SerializeField] private GameObject _categoryRow;

        [Header("State")]
        [SerializeField] private GameObject _content;
        [SerializeField] private GameObject _lockedPanel, _noInternetPanel;
        [SerializeField] private TextMeshProUGUI _lockedText;

        [Tooltip("Đếm ngược hết mùa giải; để trống nếu bảng không cần")]
        [SerializeField] private LbCountdown _countdown;

        [Header("Menu")]
        [Tooltip("Index tab của leaderboard trong SO_UI_Menu_FeaturesConfig, 0-based")]
        [SerializeField] private int _menuTabIndex = 1;

        private CancellationTokenSource _cts, _tabCts;

        // -1 chu khong phai 0: Select bo qua khi trung cap dang chon, de 0 la lan dau khong nap gi
        private int _category = -1, _type = -1;
        private string _lockedTemplate;
        private bool _opened;

        private LbTabView _current;

        protected LeaderboardService Service { get; private set; }

        protected virtual void Awake()
        {
            Service = Center.GetOrCreate<LeaderboardService>();
            _lockedTemplate = _lockedText != null ? _lockedText.text : "";

            for (var i = 0; i < categoryTabs.lsChild.Length; i++)
            {
                var index = i;
                // Mathf.Max: chua chon type lan nao (_type = -1) thi lay type dau tien
                categoryTabs.lsChild[i].OnActive += () => Select(index, Mathf.Max(_type, 0)).Forget();
            }

            for (var i = 0; i < typeTabs.lsChild.Length; i++)
            {
                var index = i;
                typeTabs.lsChild[i].OnActive += () => Select(_category, index).Forget();
            }

            for (var c = 0; c < _categories.Count; c++)
            {
                if (_categories[c].view != null) _categories[c].view.Init();
            }

            categoryTabs.ConditionSelect = i => i >= 0 && i < _categories.Count && _categories[i].active;
        }

        // Tab được instantiate sẵn và luôn active nên OnEnable chỉ chạy 1 lần, phải bám event của navigator
        protected virtual void OnEnable()
        {
            GameEvent<int>.Register(MenuConst.EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED, OnMenuTabChanged, this);
            Service.OnConnectionChanged += OnConnectionChanged;
        }

        protected virtual void OnDisable()
        {
            GameEvent<int>.Unregister(MenuConst.EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED, OnMenuTabChanged, this);
            Service.OnConnectionChanged -= OnConnectionChanged;
            CancelAll();
            _opened = false;
            _category = _type = -1;   // mo lai phai nap lai tu dau
        }

        /// <summary>Mất hoặc nối lại kết nối giữa chừng: chạy lại từ cổng chờ, nạp lại tab đang xem.</summary>
        private void OnConnectionChanged()
        {
            if (!_opened) return;

            Service.Invalidate();     // data cũ có thể đã hết hạn
            _category = _type = -1;
            _opened = false;          // cho Open() chạy lại
            Open();
        }

        private void OnMenuTabChanged(int index)
        {
            if (index == _menuTabIndex) Open(); // Open() tu chan goi lai
        }

        /// <summary>Mở bảng; gọi lại lúc đang mở thì bỏ qua.</summary>
        public void Open()
        {
            if (_opened) return;
            _opened = true;
            OpenAsync().Forget();
        }

        private async UniTaskVoid OpenAsync()
        {
            CancelAll();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            var ct = _cts.Token;

            RefreshToggles();
            _content.SetActive(false);
            _lockedPanel.SetActive(false);
            _noInternetPanel.SetActive(false);

            if (!await WaitGate(ct)) return;

            _content.SetActive(true);

            var firstCategory = _categories.FindIndex(c => c.active);
            if (firstCategory < 0 || _typeIds.Length == 0) return;

            categoryTabs.Active(firstCategory);
            await Select(firstCategory, 0);
        }

        /// <summary>Chờ tới lúc được xem bảng. Trả false nếu bị huỷ giữa chừng.</summary>
        protected virtual async UniTask<bool> WaitGate(CancellationToken ct)
        {
            try
            {
                if (!Service.Ready)
                {
                    _noInternetPanel.SetActive(true);
                    await UniTask.WaitUntil(() => Service.Ready, cancellationToken: ct);
                    _noInternetPanel.SetActive(false);
                }

                if (!Service.Unlocked)
                {
                    if (_lockedText != null)
                        _lockedText.text = _lockedTemplate.Replace("{[LEVEL]}", Service.LevelUnlock.ToString());
                    _lockedPanel.SetActive(true);
                    await UniTask.WaitUntil(() => Service.Unlocked, cancellationToken: ct);
                    _lockedPanel.SetActive(false);
                }
            }
            catch (OperationCanceledException) { return false; }

            return true;
        }

        private async UniTask Select(int categoryIndex, int typeIndex)
        {
            if (_cts == null) return;
            if (categoryIndex < 0 || categoryIndex >= _categories.Count) return;
            if (typeIndex < 0 || typeIndex >= _typeIds.Length) return;
            if (!_categories[categoryIndex].active) return;
            if (categoryIndex == _category && typeIndex == _type) return;

            var category = _categories[categoryIndex];
            if (category.view == null)
            {
                Debug.LogError($"[Leaderboard] category {category.id} chưa gán view.");
                return;
            }

            _tabCts?.Cancel();
            _tabCts?.Dispose();
            _tabCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            var ct = _tabCts.Token;

            // Gán trước mọi await: lần Select trùng cặp gọi sau sẽ bị guard ở trên chặn
            _category = categoryIndex;
            _type = typeIndex;
            _current = category.view;

            category.view.SetLoading(true);

            LbPage page;
            try
            {
                page = await Service.Fetch(category.id, _typeIds[typeIndex], category.view.EntryType, ct);
                category.view.Show(page);

                if (_countdown != null)
                    _countdown.Begin(category.id, await Service.FetchEndSecond(category.id, ct));
            }
            catch (OperationCanceledException) { }
        }

        private void RefreshToggles()
        {
            for (var i = 0; i < _categories.Count && i < categoryTabs.lsButton.Length; i++)
                categoryTabs.lsButton[i].gameObject.SetActive(_categories[i].active);

            if (_categoryRow != null) _categoryRow.SetActive(_categories.Count(c => c.active) > 1);
        }

        /// <summary>Gắn vào onClick của nút TopBtn.</summary>
        public void GoTop() { if (_current != null) _current.GoTop(); }

        /// <summary>Gắn vào onClick của nút BottomBtn.</summary>
        public void GoBottom() { if (_current != null) _current.GoBottom(); }

        public void GoMe() { if (_current != null) _current.GoMe(); }

        /// <summary>Gắn vào onClick của nút Reload trong Inspector.</summary>
        public void OnRefreshClick()
        {
            if (_category < 0 || _type < 0 || _current == null) return;

            if (Service.CanManualRefresh())
            {
                Service.Invalidate();
                var c = _category;
                var t = _type;
                _category = _type = -1; // ép Select chạy lại đúng tab hiện tại
                Select(c, t).Forget();
            }
            else
            {
                FakeLoading(_current).Forget();
            }
        }

        /// <summary>Bấm reload lúc còn cooldown: quay vòng cho có phản hồi chứ không gọi server.</summary>
        private async UniTaskVoid FakeLoading(LbTabView view)
        {
            view.SetLoading(true);
            try { await UniTask.Delay(TimeSpan.FromSeconds(FAKE_LOADING), cancellationToken: _cts.Token); }
            catch (OperationCanceledException) { return; }
            view.SetLoading(false);
        }

        private void CancelAll()
        {
            _tabCts?.Cancel();
            _tabCts?.Dispose();
            _tabCts = null;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
