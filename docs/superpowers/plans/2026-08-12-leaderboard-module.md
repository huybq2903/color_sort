# Leaderboard Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay module `Falcon/Modules/UIModular/UILeaderboard` bằng module gọn hơn ở `_Game/Shared/Modules/Leaderboard`, tái sử dụng nguyên các class CS/SC nên server không phải đổi gì.

**Architecture:** Một service thuần C# (`LeaderboardService`) bọc CS/SC thành `UniTask`, cộng một MonoBehaviour (`UILeaderboardPanel`) giữ category × type dưới dạng lưới data và dùng chung một `LoopListView2`. Bỏ 3 tầng MonoBehaviour lồng nhau, bỏ pattern `_Virtual`, bỏ global static `dataDic` + polling.

**Tech Stack:** Unity, UniTask, Newtonsoft.Json, SuperScrollView (`LoopListView2`), TextMeshPro, Odin (`[Button]`), Falcon EventBus/Center/Network.

**Spec:** `docs/superpowers/specs/2026-08-12-leaderboard-rewrite-design.md`

## Global Constraints

- **Không chạy `git commit`.** Sửa xong file thì để nguyên ở working tree và báo lại. Đây là quy tắc của user, đè lên mọi bước "commit" mà skill khác đề xuất.
- **Không làm Hall of Fame.** Chỉ có type `Global` và `National`, cả hai đều là bảng xếp hạng phẳng. Không có `LbSeason`, `LbSeasonRow`, `LbTabKind`.
- **asmdef mới bắt buộc tên `Falcon.Shared.Leaderboard`** — `FReflection.cs:45` chỉ scan assembly có tên chứa `"Falcon"` hoặc `Assembly-CSharp`.
- **Không khai báo lại class nào có attribute `[FAMessage(...)]`** — `FNetManager.cs:103` map theo tên event và ghi đè im lặng. Dùng lại class của module cũ qua asmdef reference.
- **Không sửa/xoá file nào trong `Assets/Falcon/` và `Assets/FalconAssets/`.**
- **Không viết class toggle mới.** Dùng `UITab_Parent` / `UITab_Child` của `_Game/Shared/Modules/Tab`, đúng như `PopupEditProfile.cs:37-39` đang làm.
- Comment trong code chỉ 1 dòng ngắn, kể cả `<summary>` (quy tắc CLAUDE.md).
- Namespace: `Falcon.Shared.Leaderboard`.
- Không có test framework trong project này. Verify bằng: Unity compile sạch + menu self-check + nút `[Button]` bind data giả + play mode.

## File Structure

| File | Trách nhiệm |
|---|---|
| `Falcon.Shared.Leaderboard.asmdef` | Khai báo assembly + reference |
| `Scripts/LbData.cs` | `LbEntry`, `LbPage` + self-check parse |
| `Scripts/LeaderboardService.cs` | Bọc CS/SC thành UniTask, cache, gate unlock/ready |
| `Scripts/LbRankRow.cs` | Bind 1 dòng xếp hạng |
| `Scripts/UILeaderboardPanel.cs` | Controller: lưới category × type, state, scroll list, countdown, refresh |
| `Prefabs/*.prefab` | `LbSpacer`, `LbRankRow`, `LbClanRow`, `UILeaderboard` |

Task 1-2 là data + network (chạy được không cần UI). Task 3-4 là UI code. Task 5 dựng prefab trong Editor. Task 6 nối vào game.

---

### Task 1: asmdef + data model + self-check

**Files:**
- Create: `Assets/_Game/Shared/Modules/Leaderboard/Falcon.Shared.Leaderboard.asmdef`
- Create: `Assets/_Game/Shared/Modules/Leaderboard/Scripts/LbData.cs`

**Interfaces:**
- Consumes: không có (task đầu tiên)
- Produces: `LbEntry`, `LbPage` — namespace `Falcon.Shared.Leaderboard`

- [ ] **Step 1: Tạo asmdef**

Tạo `Assets/_Game/Shared/Modules/Leaderboard/Falcon.Shared.Leaderboard.asmdef`:

```json
{
    "name": "Falcon.Shared.Leaderboard",
    "rootNamespace": "Falcon.Shared.Leaderboard",
    "references": [
        "Falcon.Shared.Common",
        "Falcon.Shared.Tab",
        "Falcon.Helpers.EventBus",
        "Falcon.Modules.Core.Network.Runtime",
        "Falcon.Modules.UI.Menu.Runtime",
        "Falcon.Modules.UIModular.UILeaderboard.Runtime",
        "SuperScrollView",
        "Unity.TextMeshPro",
        "UniTask"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Viết `LbData.cs`**

Tạo `Assets/_Game/Shared/Modules/Leaderboard/Scripts/LbData.cs`:

```csharp
// Author: Bui Quang Huy
// Company: Falcon Games

using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Falcon.Shared.Leaderboard
{
    /// <summary>Một dòng xếp hạng, ăn được cả PlayerLBData lẫn ClanLBData của server.</summary>
    public class LbEntry
    {
        public int code, rank, score, level, iconId;
        public string name, profileData;
    }

    /// <summary>Kết quả một lần fetch.</summary>
    public class LbPage
    {
        public List<LbEntry> entries = new();
        public LbEntry me;
    }

#if UNITY_EDITOR
    internal static class LbDataSelfCheck
    {
        [UnityEditor.MenuItem("Falcon/Modules/Leaderboard/Self Check")]
        private static void Run()
        {
            var player = JsonConvert.DeserializeObject<LbEntry>(
                "{\"code\":1,\"rank\":2,\"name\":\"a\",\"score\":3,\"level\":4,\"profileData\":\"x\"}");
            Check(player.level == 4 && player.profileData == "x" && player.iconId == 0, "PlayerLBData shape");

            var clan = JsonConvert.DeserializeObject<LbEntry>(
                "{\"code\":1,\"rank\":2,\"name\":\"a\",\"score\":3,\"iconId\":7}");
            Check(clan.iconId == 7 && clan.level == 0 && clan.profileData == null, "ClanLBData shape");

            var list = JsonConvert.DeserializeObject<List<LbEntry>>("[{\"rank\":1},{\"rank\":2},{\"rank\":3}]");
            Check(list.Count == 3 && list[2].rank == 3, "List<LbEntry>");

            Debug.Log("[Leaderboard] Self check xong.");
        }

        private static void Check(bool ok, string label)
        {
            if (!ok) Debug.LogError($"[Leaderboard] FAIL: {label}");
            else Debug.Log($"[Leaderboard] OK: {label}");
        }
    }
#endif
}
```

- [ ] **Step 3: Đợi Unity compile, kiểm tra Console sạch lỗi**

Chuyển sang cửa sổ Unity, đợi compile xong. Expected: không có lỗi đỏ. Nếu báo thiếu assembly `UniTask` hoặc `SuperScrollView`, mở asmdef bằng Inspector và chọn lại đúng tên assembly trong danh sách (tên hiển thị có thể khác chuỗi trong file).

- [ ] **Step 4: Chạy self-check**

Menu: `Falcon → Modules → Leaderboard → Self Check`
Expected: Console in ra 3 dòng `OK:` và 1 dòng `Self check xong.`, không có dòng `FAIL:` nào.

Nếu có `FAIL: ClanLBData shape` → nghĩa là Newtonsoft đang throw thay vì bỏ qua field thiếu; kiểm tra lại là `LbEntry` dùng field public chứ không phải property có `[JsonRequired]`.

- [ ] **Step 5: Báo lại kết quả cho user, không commit**

---

### Task 2: LeaderboardService

**Files:**
- Create: `Assets/_Game/Shared/Modules/Leaderboard/Scripts/LeaderboardService.cs`

**Interfaces:**
- Consumes: `LbEntry`, `LbPage` (Task 1)
- Produces:
  - `class LeaderboardService : IInitialize`
  - `bool Ready { get; }`
  - `int LevelUnlock { get; }`
  - `bool Unlocked { get; }`
  - `bool CanManualRefresh()`
  - `void Invalidate()`
  - `UniTask<LbPage> Fetch(string category, string type, CancellationToken ct)`
  - `UniTask<DateTime> FetchEndTime(string category, CancellationToken ct)`

- [ ] **Step 1: Viết `LeaderboardService.cs`**

Tạo `Assets/_Game/Shared/Modules/Leaderboard/Scripts/LeaderboardService.cs`:

```csharp
// Author: Bui Quang Huy
// Company: Falcon Games

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.UIModular.UILeaderboard.Runtime;
using Falcon.Shared.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Falcon.Shared.Leaderboard
{
    /// <summary>Bọc CS/SC của module Leaderboard cũ thành UniTask, cache theo category_type.</summary>
    public class LeaderboardService : IInitialize
    {
        private const int TIMEOUT = 10;

        private readonly Dictionary<string, LbPage> _pages = new();
        private readonly Dictionary<string, DateTime> _endTimes = new();

        // Config do LeaderboardManager tự xin lúc RuntimeInitializeOnLoad, ta chỉ đọc
        public void OnInitialize() { }

        public bool Ready => LeaderboardManager.ConnectedToServer();
        public int LevelUnlock => LeaderboardManager.LevelUnlock();
        public bool Unlocked => Ready && LevelUnlock >= 0 && CurrentLevel >= LevelUnlock;

        private static int CurrentLevel => GameRequest<int>.Request(GameKeys.GET_LEVEL);

        public bool CanManualRefresh() => DateTime.Now >= LeaderboardManager.nextTimeCanRefreshOnClick;

        /// <summary>Xoá cache; ClearDataOnly của module cũ tự set lại 2 mốc thời gian theo config server.</summary>
        public void Invalidate()
        {
            LeaderboardManager.ClearDataOnly();
            _pages.Clear();
        }

        public async UniTask<LbPage> Fetch(string category, string type, CancellationToken ct)
        {
            if (DateTime.Now >= LeaderboardManager.nextTimeGetDataFromServer) Invalidate();

            var key = $"{category}_{type}";
            if (_pages.TryGetValue(key, out var cached)) return cached;

            var tcs = new UniTaskCompletionSource<SCGetLBData>();
            new CSGetLBData { leaderboardCategoryStr = category, leaderboardTypeStr = type }
                .AddSCListener<SCGetLBData>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null)
            {
                Debug.LogWarning($"[Leaderboard] fetch {key} thất bại (timeout hoặc lỗi).");
                return null;
            }

            var page = Parse(sc);
            if (page != null) _pages[key] = page;
            return page;
        }

        /// <summary>Mốc kết thúc mùa giải của category. DateTime.MinValue nếu không lấy được.</summary>
        public async UniTask<DateTime> FetchEndTime(string category, CancellationToken ct)
        {
            if (_endTimes.TryGetValue(category, out var cached)) return cached;

            var tcs = new UniTaskCompletionSource<SCGetCategoryDataLB>();
            new CSGetCategoryDataLB(category)
                .AddSCListener<SCGetCategoryDataLB>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null) return DateTime.MinValue;

            var end = DateTime.Now.AddMilliseconds(sc.timeLeft);
            _endTimes[category] = end;
            return end;
        }

        private static LbPage Parse(SCGetLBData sc)
        {
            var page = new LbPage();
            try
            {
                if (!string.IsNullOrEmpty(sc.rows))
                    page.entries = JsonConvert.DeserializeObject<List<LbEntry>>(sc.rows) ?? new List<LbEntry>();

                if (!string.IsNullOrEmpty(sc.bonusData))
                {
                    var me = JObject.Parse(sc.bonusData)["myData"];
                    if (me != null && me.Type != JTokenType.Null) page.me = me.ToObject<LbEntry>();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Leaderboard] parse lỗi: {e}");
                return null;
            }

            return page;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Falcon/Modules/Leaderboard/Log State")]
        private static void LogState()
        {
            var s = Center.GetOrCreate<LeaderboardService>();
            Debug.Log($"[Leaderboard] Ready={s.Ready} LevelUnlock={s.LevelUnlock} Unlocked={s.Unlocked} CanManualRefresh={s.CanManualRefresh()}");
        }
#endif
    }
}
```

- [ ] **Step 2: Đợi Unity compile**

Expected: không lỗi. Lỗi thường gặp và cách xử lý:
- `LeaderboardManager không tồn tại` → asmdef thiếu reference `Falcon.Modules.UIModular.UILeaderboard.Runtime`.
- `GameRequest không tồn tại` → thiếu `Falcon.Helpers.EventBus`.
- `GameKeys` / `Center` không tồn tại → thiếu `Falcon.Shared.Common`.
- `nextTimeGetDataFromServer không truy cập được` → field đó phải là `public static` trong `LeaderboardManager.cs:57-58`; nếu không, `NeedGetDataFromServer()` là `internal` nên không thay thế được — báo lại user để chọn cách khác.

- [ ] **Step 3: Kiểm chứng phần đọc config bằng play mode**

Vào play mode ở scene Home, đợi vài giây cho session kết nối, chạy menu `Falcon → Modules → Leaderboard → Log State`.
Expected: `Ready=True`, `LevelUnlock` là số >= 0 (giá trị server trả), `Unlocked` đúng với level hiện tại của tài khoản test.

Nếu `LevelUnlock=-1` mãi → `sc_get_config_lb` chưa về; kiểm tra Console xem `CSGetConfigLB` có được gửi không (`LeaderboardManager.cs:50`).

- [ ] **Step 4: Báo kết quả cho user, không commit**

---

### Task 3: LbRankRow

**Files:**
- Create: `Assets/_Game/Shared/Modules/Leaderboard/Scripts/LbRankRow.cs`

**Interfaces:**
- Consumes: `LbEntry` (Task 1)
- Produces: `LbRankRow` với `void Bind(LbEntry e, bool isMe)`

Không có class toggle: dùng `UITab_Parent` / `UITab_Child` có sẵn ở `_Game/Shared/Modules/Tab`.

- [ ] **Step 1: Viết `LbRankRow.cs`**

```csharp
// Author: Bui Quang Huy
// Company: Falcon Games

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.Leaderboard
{
    /// <summary>Một dòng xếp hạng. Dùng cho cả list lẫn dòng "của tôi" cố định dưới đáy.</summary>
    public class LbRankRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rank, _name, _score;
        [SerializeField] private GameObject _highlight;
        [SerializeField] private Image _icon;
        [Tooltip("Sprite theo iconId, dùng cho bảng Clan. Để trống nếu bảng người chơi.")]
        [SerializeField] private Sprite[] _iconsById;

        public void Bind(LbEntry e, bool isMe)
        {
            if (_rank != null) _rank.text = e.rank.ToString();
            if (_name != null) _name.text = e.name;
            if (_score != null) _score.text = e.score.ToString();
            if (_highlight != null) _highlight.SetActive(isMe);

            if (_icon == null) return;
            var hasIcon = _iconsById != null && e.iconId >= 0 && e.iconId < _iconsById.Length;
            _icon.gameObject.SetActive(hasIcon);
            if (hasIcon) _icon.sprite = _iconsById[e.iconId];
        }
    }
}
```

- [ ] **Step 2: Đợi Unity compile**

Expected: không lỗi.

- [ ] **Step 3: Báo lại cho user, không commit**

---

### Task 4: UILeaderboardPanel

**Files:**
- Create: `Assets/_Game/Shared/Modules/Leaderboard/Scripts/UILeaderboardPanel.cs`

**Interfaces:**
- Consumes: `LeaderboardService` (Task 2), `LbRankRow` (Task 3), `LbPage` (Task 1), `CountdownTmpBasic` từ `Falcon.Modules.UIModular.UILeaderboard.Runtime`, `UITab_Parent` / `UITab_Child` từ `Falcon.Shared.Tab`
- Produces: `UILeaderboardPanel` với `public void OnRefreshClick()` (gắn vào Button trong Inspector ở Task 5), và class serializable lồng trong nó: `UILeaderboardPanel.LbCategory { id, rowPrefab, active }`

- [ ] **Step 1: Viết `UILeaderboardPanel.cs`**

```csharp
// Author: Bui Quang Huy
// Company: Falcon Games

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.UI.Menu.Runtime;
using Falcon.Modules.UIModular.UILeaderboard.Runtime;
using Falcon.Shared.Common;
using Falcon.Shared.Tab;
using Sirenix.OdinInspector;
using SuperScrollView;
using TMPro;
using UnityEngine;

namespace Falcon.Shared.Leaderboard
{
    /// <summary>Toàn bộ UI leaderboard trong 1 MonoBehaviour; category × type là lưới data chứ không phải cây GameObject.</summary>
    public class UILeaderboardPanel : MonoBehaviour
    {
        [Serializable]
        public class LbCategory
        {
            public string id;
            [Tooltip("Tên prefab dòng, phải khớp ItemPrefabDataList của LoopListView2")]
            public string rowPrefab = "LbRankRow";
            [Tooltip("Bỏ tick để ẩn category chưa mở, ví dụ Team")]
            public bool active = true;
        }

        private const string SPACER = "Spacer";
        private const float FAKE_LOADING = 0.5f;

        // Category × Type là một lưới: 1 nút category dùng chung cho mọi type.
        // Thứ tự 2 list phải khớp thứ tự lsChild/lsButton của UITab_Parent tương ứng.
        [Header("Tab")]
        [SerializeField] private List<LbCategory> _categories = new();
        [SerializeField] private string[] _typeIds = { "Global", "National" };
        [SerializeField] private UITab_Parent _categoryTabs, _typeTabs;
        [SerializeField] private GameObject _categoryRow;

        [Header("List")]
        [SerializeField] private LoopListView2 _list;
        [SerializeField] private float _paddingTop = 50f, _paddingBottom = 80f;

        [Header("State")]
        [SerializeField] private GameObject _content, _loading, _empty, _lockedPanel, _noInternetPanel;
        [SerializeField] private TextMeshProUGUI _lockedText;

        [Header("Phụ")]
        [SerializeField] private LbRankRow _myRow;
        [SerializeField] private CountdownTmpBasic _countdown;

        [Header("Menu")]
        [Tooltip("Index tab của leaderboard trong SO_UI_Menu_FeaturesConfig, 0-based")]
        [SerializeField] private int _menuTabIndex = 1;

        private LeaderboardService _service;
        private CancellationTokenSource _cts, _tabCts;
        private LbPage _page;
        private int _category = -1, _type = -1;
        private string _lockedTemplate;
        private bool _listInited, _opened, _suspendSelect;

        private int RowCount => _page?.entries?.Count ?? 0;

        // 1 spacer đầu + 1 spacer cuối, vì LoopListView2 chỉ có mPadding giữa các item
        private int ViewCount => RowCount + 2;

        private void Awake()
        {
            _service = Center.GetOrCreate<LeaderboardService>();
            _lockedTemplate = _lockedText != null ? _lockedText.text : "";

            // UITab_Parent lo click + đổi visual; ta chỉ nghe kết quả (giống PopupEditProfile)
            for (int i = 0; i < _categoryTabs.lsChild.Length; i++)
            {
                var index = i;
                _categoryTabs.lsChild[i].OnActive += () => Select(index, _typeTabs.CurrentIndexTab).Forget();
            }

            for (int i = 0; i < _typeTabs.lsChild.Length; i++)
            {
                var index = i;
                _typeTabs.lsChild[i].OnActive += () => Select(_categoryTabs.CurrentIndexTab, index).Forget();
            }

            _categoryTabs.ConditionSelect = i => i >= 0 && i < _categories.Count && _categories[i].active;
        }

        private void OnEnable()
        {
            GameEvent<int>.Register(MenuConst.EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED, OnMenuTabChanged, this);
        }

        private void OnDisable()
        {
            GameEvent<int>.Unregister(MenuConst.EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED, OnMenuTabChanged, this);
            CancelAll();
        }

        // Tab được instantiate sẵn và luôn active nên OnEnable chỉ chạy 1 lần, phải bám event của navigator
        private void OnMenuTabChanged(int index)
        {
            if (index != _menuTabIndex || _opened) return;
            _opened = true;
            Open().Forget();
        }

        private async UniTaskVoid Open()
        {
            CancelAll();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            var ct = _cts.Token;

            RefreshToggles();
            _content.SetActive(false);
            _loading.SetActive(false);
            _empty.SetActive(false);
            _lockedPanel.SetActive(false);
            _noInternetPanel.SetActive(false);

            try
            {
                if (!_service.Ready)
                {
                    _noInternetPanel.SetActive(true);
                    await UniTask.WaitUntil(() => _service.Ready, cancellationToken: ct);
                    _noInternetPanel.SetActive(false);
                }

                if (!_service.Unlocked)
                {
                    _lockedText.text = _lockedTemplate.Replace("{[LEVEL]}", _service.LevelUnlock.ToString());
                    _lockedPanel.SetActive(true);
                    await UniTask.WaitUntil(() => _service.Unlocked, cancellationToken: ct);
                    _lockedPanel.SetActive(false);
                }
            }
            catch (OperationCanceledException) { return; }

            _content.SetActive(true);

            var firstCategory = _categories.FindIndex(c => c.active);
            if (firstCategory < 0 || _typeIds.Length == 0) return;

            // Active() bắn OnActive -> Select; chặn lại để không sinh 2 request huỷ lẫn nhau
            _suspendSelect = true;
            _typeTabs.Active(0);
            _categoryTabs.Active(firstCategory);
            _suspendSelect = false;

            await Select(firstCategory, 0);
        }

        private async UniTask Select(int categoryIndex, int typeIndex)
        {
            if (_suspendSelect) return;
            if (categoryIndex < 0 || categoryIndex >= _categories.Count) return;
            if (typeIndex < 0 || typeIndex >= _typeIds.Length) return;
            if (!_categories[categoryIndex].active) return;
            if (categoryIndex == _category && typeIndex == _type) return;

            _tabCts?.Cancel();
            _tabCts?.Dispose();
            _tabCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            var ct = _tabCts.Token;

            var prevCategoryId = _category >= 0 ? _categories[_category].id : null;
            _category = categoryIndex;
            _type = typeIndex;

            _loading.SetActive(true);
            _empty.SetActive(false);
            if (_myRow != null) _myRow.gameObject.SetActive(false);

            var category = _categories[categoryIndex];

            try
            {
                _page = await _service.Fetch(category.id, _typeIds[typeIndex], ct);

                if (_countdown != null && category.id != prevCategoryId)
                {
                    var end = await _service.FetchEndTime(category.id, ct);
                    var valid = end > DateTime.Now;
                    _countdown.gameObject.SetActive(valid);
                    if (valid) _countdown.InitTimeAndAutoCountdown(end);
                }
            }
            catch (OperationCanceledException) { return; }

            Rebuild();
        }

        private void Rebuild()
        {
            if (!_listInited)
            {
                _listInited = true;
                _list.InitListView(ViewCount, OnGetItemByIndex);
            }
            else
            {
                _list.SetListItemCount(ViewCount);
            }

            _list.RefreshAllShownItem();

            _loading.SetActive(false);
            _empty.SetActive(RowCount == 0);

            if (_myRow == null) return;
            var hasMe = _page?.me != null;
            _myRow.gameObject.SetActive(hasMe);
            if (hasMe) _myRow.Bind(_page.me, true);
        }

        private LoopListViewItem2 OnGetItemByIndex(LoopListView2 list, int index)
        {
            if (index == 0 || index == ViewCount - 1)
            {
                var spacer = list.NewListViewItem(SPACER);
                spacer.CachedRectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, index == 0 ? _paddingTop : _paddingBottom);
                return spacer;
            }

            var i = index - 1;
            if (i < 0 || i >= RowCount) return null;

            var item = list.NewListViewItem(_categories[_category].rowPrefab);
            var e = _page.entries[i];
            item.GetComponent<LbRankRow>().Bind(e, _page.me != null && e.code == _page.me.code);
            return item;
        }

        private void RefreshToggles()
        {
            for (int i = 0; i < _categories.Count && i < _categoryTabs.lsButton.Length; i++)
                _categoryTabs.lsButton[i].gameObject.SetActive(_categories[i].active);

            if (_categoryRow != null) _categoryRow.SetActive(_categories.Count(c => c.active) > 1);
        }

        /// <summary>Gắn vào onClick của nút Reload trong Inspector.</summary>
        public void OnRefreshClick()
        {
            if (_category < 0 || _type < 0) return;

            if (_service.CanManualRefresh())
            {
                _service.Invalidate();
                var c = _category;
                var t = _type;
                _category = _type = -1; // ép Select chạy lại đúng tab hiện tại
                Select(c, t).Forget();
            }
            else
            {
                FakeLoading().Forget();
            }
        }

        private async UniTaskVoid FakeLoading()
        {
            _loading.SetActive(true);
            try { await UniTask.Delay(TimeSpan.FromSeconds(FAKE_LOADING), cancellationToken: _cts.Token); }
            catch (OperationCanceledException) { return; }
            _loading.SetActive(false);
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

        [Button("Bind data giả (chỉ để test layout)")]
        private void BindFakeData(int count = 50)
        {
            _category = _categories.FindIndex(c => c.active);
            if (_category < 0) { Debug.LogError("[Leaderboard] chưa cấu hình category nào active."); return; }
            _type = 0;

            _page = new LbPage();
            for (int i = 0; i < count; i++)
                _page.entries.Add(new LbEntry
                {
                    code = i, rank = i + 1, name = $"Player {i + 1}",
                    score = (count - i) * 100, level = i + 1, iconId = i % 4
                });
            _page.me = _page.entries[0];

            _content.SetActive(true);
            Rebuild();
        }
    }
}
```

- [ ] **Step 2: Đợi Unity compile**

Expected: không lỗi. Lỗi thường gặp:
- `LoopListView2 không tồn tại` → asmdef thiếu `SuperScrollView`.
- `MenuConst không tồn tại` → thiếu `Falcon.Modules.UI.Menu.Runtime`.
- `UITab_Parent không tồn tại` → thiếu `Falcon.Shared.Tab`.
- `Sirenix không tồn tại` → Odin là dll auto-referenced; asmdef để `overrideReferences: false` nên phải chạy được.
- `GetCancellationTokenOnDestroy không tồn tại` → thiếu `using Cysharp.Threading.Tasks;`.

- [ ] **Step 3: Chưa test được ở task này**

`BindFakeData` cần prefab và `LoopListView2` đã cấu hình → verify ở Task 5. Task này chỉ cần compile sạch.

- [ ] **Step 4: Báo lại cho user, không commit**

---

### Task 5: Dựng prefab trong Unity Editor

Task này làm thủ công trong Unity, không sinh code. Tham chiếu layout: mở `Assets/FalconAssets/Modules/UIModular/UILeaderboard/Addressable/UILeaderboard.prefab` để xem art/bố cục cũ và copy phần visual sang.

**Files:**
- Create: `Assets/_Game/Shared/Modules/Leaderboard/Prefabs/LbSpacer.prefab`
- Create: `Assets/_Game/Shared/Modules/Leaderboard/Prefabs/LbRankRow.prefab`
- Create: `Assets/_Game/Shared/Modules/Leaderboard/Prefabs/LbClanRow.prefab`
- Create: `Assets/_Game/Shared/Modules/Leaderboard/Prefabs/UILeaderboard.prefab`

- [ ] **Step 1: `LbSpacer.prefab`**

GameObject rỗng + `RectTransform` + `LoopListViewItem2`. Không cần gì khác — panel tự set chiều cao.

- [ ] **Step 2: `LbRankRow.prefab`**

Root: `RectTransform` + `LoopListViewItem2` + `LbRankRow`.
Con: `TextMeshProUGUI` cho rank / name / score, `GameObject` highlight (tắt sẵn), `Image` icon (tuỳ chọn).
Gán 3 text + highlight + icon vào các field của `LbRankRow`. `_iconsById` để trống cho bảng người chơi.

- [ ] **Step 3: `LbClanRow.prefab`**

Duplicate `LbRankRow.prefab`, đổi tên `LbClanRow`, điền mảng `_iconsById` bằng bộ sprite logo clan.
Clan còn ComingSoon nên chưa cần art chuẩn, nhưng vẫn phải tạo prefab để `rowPrefab` của category `Team` không trỏ hụt.

- [ ] **Step 4: `UILeaderboard.prefab`**

Hierarchy tối thiểu:

```
UILeaderboard                 ← UILeaderboardPanel
├── CategoryRow               ← _categoryRow + UITab_Parent (_categoryTabs)
│   ├── Btn_TopPlayer         ← Button + UITab_Child
│   └── Btn_Team              ← Button + UITab_Child
├── TypeRow                   ← UITab_Parent (_typeTabs)
│   ├── Btn_Global            ← Button + UITab_Child
│   └── Btn_National          ← Button + UITab_Child
├── Content                   ← _content
│   ├── ScrollRect            ← LoopListView2 (_list)
│   ├── Countdown             ← CountdownTmpBasic (_countdown)
│   ├── MyRow                 ← LbRankRow (_myRow)
│   ├── Empty                 ← _empty
│   └── BtnReload             ← Button, onClick → UILeaderboardPanel.OnRefreshClick
├── Loading                   ← _loading
├── LockedPanel               ← _lockedPanel
│   └── Text                  ← _lockedText, nội dung chứa chuỗi "{[LEVEL]}"
└── NoInternetPanel           ← _noInternetPanel
```

Trên `LoopListView2`, mục `ItemPrefabDataList` khai đúng 3 prefab với tên khớp chuỗi dùng trong code:
`Spacer` → `LbSpacer.prefab`, `LbRankRow` → `LbRankRow.prefab`, `LbClanRow` → `LbClanRow.prefab`.

Trên `UILeaderboardPanel`, cấu hình 2 danh sách đúng theo chuỗi server đang dùng — đã trích từ prefab cũ
`Assets/Falcon/Modules/UIModular/UILeaderboard/Prefab/Leaderboard.prefab`
(`_categoryAndGameObjectList` dòng 9855-9862, `_typeAndLeaderboardList` dòng 2253-2259):

`_categories` (thứ tự phải khớp `_categoryTabs.lsChild` / `lsButton`):

| # | id | rowPrefab | active |
|---|---|---|---|
| 0 | `TopPlayer` | `LbRankRow` | ✔ |
| 1 | `Team` | `LbClanRow` | ✘ |

`_typeIds` (thứ tự phải khớp `_typeTabs.lsChild` / `lsButton`): `["Global", "National"]`

Prefab cũ còn type thứ ba `HallOfFame` — **cố ý không khai**, xem Global Constraints.

`Team` để `active = 0` giống prefab cũ (clan còn ComingSoon). Bật lại khi mở clan.

`_menuTabIndex` để `1` (leaderboard là `featuresTab_2`, 0-based).

**Cấu hình 2 `UITab_Parent`:**

- `lsChild` và `lsButton` phải cùng độ dài và **cùng thứ tự** — `UITab_Parent.Awake` ghép chúng theo index.
- **`activeOnEnable` bỏ tick trên cả hai.** Mặc định là `true`, nó sẽ `Active(0)` ngay ở `OnEnable`, tức gọi server trước khi qua cổng ready/unlock. Panel tự gọi `Active()` sau khi mở khoá.
- Mỗi `UITab_Child` phải gán `btnEnable` và `btnDisable` (2 GameObject trạng thái bật/tắt của nút). `UITab_Child.Active()` không null-check nên bỏ trống sẽ `NullReferenceException`.
- `lsObjs` của `UITab_Child` để **trống**: bản mới không có GameObject nội dung riêng cho từng tab, panel tự đổi data trong cùng một list.

- [ ] **Step 5: Test layout bằng data giả**

Kéo `UILeaderboard.prefab` vào một scene bất kỳ có Canvas, **vào Play mode** (`LoopListView2` khởi tạo item ở runtime, bấm ở edit mode sẽ không dựng list), chọn root, bấm nút Inspector **"Bind data giả (chỉ để test layout)"**.
Expected: list hiện 50 dòng `Player 1..50`, cuộn mượt, dòng đầu có highlight (vì `me` = entry 0), `MyRow` dưới đáy hiện `Player 1`.

Nếu list trống: kiểm tra tên prefab trong `ItemPrefabDataList` có khớp chính xác `rowPrefab` và `Spacer` không.
Nếu `NullReferenceException` ở `Rebuild`: còn field `_content` / `_loading` / `_empty` chưa gán.
Nếu `NullReferenceException` ở `UITab_Child.Active`: còn `btnEnable` / `btnDisable` chưa gán.

Thoát Play mode, xoá object khỏi scene (chỉ giữ prefab).

- [ ] **Step 6: Đánh addressable**

Chọn `UILeaderboard.prefab` → tick Addressable → đặt Address đúng bằng `UILeaderboard`, nhóm `Common` (cùng nhóm với các feature khác trong `Assets/AddressableAssetsData/AssetGroups/Common.asset`).

- [ ] **Step 7: Báo lại cho user, không commit**

---

### Task 6: Nối vào game

**Files:**
- Modify: `Assets/_Game/Shared/Scripts/StartBehaviour.cs:131` (thêm 1 dòng vào `InitModules`)
- Modify: `Assets/_Game/Shared/Resources/SO_UI_Menu_FeaturesConfig.asset` (sửa trong Inspector, không sửa YAML tay)

**Interfaces:**
- Consumes: `LeaderboardService` (Task 2), addressable `UILeaderboard` (Task 5)
- Produces: không có

- [ ] **Step 1: Đăng ký service lúc khởi động**

Trong `Assets/_Game/Shared/Scripts/StartBehaviour.cs`, hàm `InitModules()`, thêm ngay sau dòng `Center.GetOrCreate<WrapperLives>()`:

```csharp
        try { Center.GetOrCreate<LeaderboardService>(); } catch (Exception e) { Debug.LogException(e); }
```

Thêm `using Falcon.Shared.Leaderboard;` ở đầu file. Kiểm tra asmdef chứa `StartBehaviour.cs` đã reference `Falcon.Shared.Leaderboard`; nếu file nằm trong `Assembly-CSharp` (không có asmdef) thì không cần làm gì thêm.

Ghi chú: `Awake` của panel cũng gọi `Center.GetOrCreate<LeaderboardService>()` nên bước này chỉ để service tồn tại sớm; không bắt buộc nhưng đồng nhất với `WrapperLives` / `WrapperGoldHome`.

- [ ] **Step 2: Đổi feature config**

Mở `Assets/_Game/Shared/Resources/SO_UI_Menu_FeaturesConfig.asset` trong Inspector.
Đổi phần tử duy nhất của `featuresTab_2` từ `UILearderBoard_ComingSoon` thành `UILeaderboard`.

Không đụng `featuresTab_4` (`UIClan_ComingSoon`) — clan vẫn coming soon.

- [ ] **Step 3: Test luồng thật**

Vào play mode ở scene Home với tài khoản test đã đạt level mở khoá.
Expected:
1. Vào Home, chưa vuốt sang tab 2 → Console **không** có request leaderboard nào (panel chỉ mở khi tab được chọn).
2. Vuốt sang tab leaderboard → hiện loading, rồi hiện danh sách thật từ server.
3. Đổi giữa `Global` và `National` → data đổi, không bị nháy sang data tab cũ.
4. Bấm nút reload lần 1 → data load lại. Bấm lần 2 ngay lập tức → chỉ thấy loading giả 0.5s (bị throttle bởi `refreshByUserInterval`).
5. Tắt wifi rồi vào tab → hiện `NoInternetPanel`; bật lại wifi → tự vào được.

- [ ] **Step 4: Test với tài khoản chưa đủ level**

Dùng cheat để hạ level xuống dưới `LevelUnlock`, vào tab leaderboard.
Expected: `LockedPanel` hiện, text đã thay `{[LEVEL]}` bằng số level thật. Lên đủ level thì panel tự mở (nhờ `WaitUntil(() => _service.Unlocked)`).

- [ ] **Step 5: Kiểm tra module cũ không bị ảnh hưởng**

Menu `Falcon → Modules → Leaderboard → Log State` vẫn chạy, `LevelUnlock` vẫn ra số đúng → chứng tỏ `LeaderboardManager` cũ vẫn nhận config bình thường và không có xung đột đăng ký `FAMessage`.

Xác nhận trong Console không có dòng `ILeaderboardCustom chưa đăng ký` — nếu có, nghĩa là còn chỗ nào đó đang gọi `LeaderboardManager.YourCurrentLevel()`, tìm và gỡ.

- [ ] **Step 6: Báo lại cho user, không commit**

---

## Sau khi xong

Prefab cũ `UILearderBoard_ComingSoon` và toàn bộ `Assets/Falcon/Modules/UIModular/UILeaderboard/` vẫn còn nguyên trên disk, không dùng nữa nhưng vẫn compile và vẫn giữ vai trò cung cấp CS/SC + config.

**Thêm lại Hall of Fame sau này:** server vẫn có type `HallOfFame` với shape khác (`{season, playerDatas}` thay vì mảng entry phẳng). Cần: class `LbSeason { season, playerDatas, clanDatas, Entries => playerDatas ?? clanDatas }`, một `LbSeasonRow` bind N slot, một cờ trên `LbType` để `Parse` và `OnGetItemByIndex` rẽ nhánh. Không có phần nào khác phải sửa.

**Xoá hẳn module cũ sau này:** chuyển 6 file trong `Script/Runtime/CS SC/` sang `Falcon.Shared.Leaderboard`, viết lại `SCGetLBData.OnData()` cho rỗng, thay 4 chỗ gọi `LeaderboardManager` trong `LeaderboardService` bằng state tự giữ, rồi mới xoá thư mục cũ. Không làm nửa vời — hai bộ class cùng tên event sẽ ghi đè nhau.
