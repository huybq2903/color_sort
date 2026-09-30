# Viết lại module Leaderboard — Design

Ngày: 2026-08-12
Trạng thái: đã duyệt hướng C + lựa chọn (b), phạm vi bỏ Hall of Fame

## Mục tiêu

Thay module `Falcon/Modules/UIModular/UILeaderboard` bằng một module gọn hơn đặt trong
`_Game/Shared/Modules/Leaderboard`, **tái sử dụng nguyên các class CS/SC** của module cũ để
không phải đổi gì phía server.

Không phải mục tiêu: đổi protocol, đổi tên FAMessage, xoá/sửa module cũ, làm Hall of Fame.

## Ràng buộc kỹ thuật đã xác minh

| Phát hiện | Nguồn | Hệ quả cho thiết kế |
|---|---|---|
| `scEvt2Type[eventName] = type` — map theo tên, ghi đè im lặng | `FNetManager.cs:103` | **Không được** khai báo lại class có `[FAMessage("sc_get_lb_data_compress")]`. Module mới reference asmdef cũ để dùng đúng class đó. |
| Cả `FCallbackManager.OnSCResponse` lẫn `scMessage.OnData()` đều chạy | `FSession.cs:115-119` | Module mới dùng `AddSCListener` per-request; `OnData()` cũ vẫn ghi vào `LeaderboardManager.dataDic` — vô hại, ta không đọc. |
| FReflection chỉ scan assembly có tên chứa `"Falcon"` hoặc `Assembly-CSharp` | `FReflection.cs:45` | asmdef mới **bắt buộc** đặt tên `Falcon.Shared.Leaderboard`. |
| `refreshInterval`, `refreshByUserInterval`, `NeedGetDataFromServer()`, `CanRefreshOnClick()` là `internal` | `LeaderboardManager.cs:60-68,117-119` | Không đọc trực tiếp được từ asmdef khác. Đọc gián tiếp qua 2 field public `nextTimeGetDataFromServer` / `nextTimeCanRefreshOnClick`, và gọi `ClearDataOnly()` để module cũ tự set lại 2 mốc theo config server. |
| `LoopListView2` chỉ có `mPadding` (khoảng cách giữa item), không có padding trên/dưới | `LoopListView2.cs:14` | Giữ nguyên cách cũ: 1 spacer item ở đầu và 1 ở cuối. |
| `LeaderboardManager.GetLBConfig()` có `[RuntimeInitializeOnLoadMethod]` tự gửi `CSGetConfigLB` | `LeaderboardManager.cs:50-54` | Module mới **không** cần tự xin config, chỉ đọc `LevelUnlock()` / `ConnectedToServer()`. |
| Leaderboard là **tab của menu**, không phải popup: `SO_UI_Menu_FeaturesConfig.featuresTab_2` nạp addressable `UILearderBoard_ComingSoon` qua `MenuManager.LoadFeatures()` | `MenuManager.cs:67-89` | Prefab đặt tên addressable `UILeaderboard`, thay vào `featuresTab_2`. Tab được instantiate sẵn và luôn active nên `OnEnable` chỉ chạy 1 lần → phải bám `MenuConst.EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED` (`MenuNavigator.cs:146`) để biết lúc nào tab được chọn, tránh gọi server ngay khi vào Home. |
| `categoryId` server dùng là `TopPlayer` / `Team`; `type` là `Global` / `National` / `HallOfFame` | `Leaderboard.prefab:2253-2259, 9855-9862` | Module mới chỉ khai `Global` và `National`. `HallOfFame` vẫn tồn tại phía server nhưng client không dựng tab cho nó. |

## Phạm vi

**Có:** BXH người chơi (`TopPlayer`) và BXH clan (`Team`), mỗi loại 2 type `Global` / `National`.

**Không có:** Hall of Fame. Bỏ HOF loại được `LbSeason`, `LbSeasonRow` + prefab của nó, enum
`LbTabKind` và toàn bộ nhánh rẽ theo kind trong service lẫn panel — chỉ còn đúng một dạng dòng.

Nếu sau này cần HOF: thêm lại `LbSeason` (`{season, playerDatas, clanDatas}` với property
`Entries => playerDatas ?? clanDatas`), một `LbSeasonRow`, và một cờ trên `LbType` để chọn
prefab dòng. Không có phần nào khác phải sửa.

## Kiến trúc

```
LeaderboardService  (thuần C#, IInitialize, sống trong Center)
        │  UniTask<LbPage> Fetch(category, type)
        │  UniTask<DateTime> FetchEndTime(category)
        ▼
UILeaderboardPanel  (MonoBehaviour duy nhất, 1 LoopListView2)
        │  _categories × _typeIds — lưới, không phải cây GameObject
        │  2 × UITab_Parent (Falcon.Shared.Tab) lo toggle
        ▼
LbRankRow  (MonoBehaviour, Bind(LbEntry, bool isMe))
```

So với bản cũ: bỏ hẳn 3 tầng `LeaderboardMainPanel → LeaderboardCategory → ALeaderboardPanel`,
bỏ pattern `_Virtual` (property forwarding), bỏ 4 class panel type, bỏ deserialize bằng reflection,
bỏ global static `dataDic` + polling `WaitUntil`.

### Vị trí file

```
Assets/_Game/Shared/Modules/Leaderboard/
├── Falcon.Shared.Leaderboard.asmdef
├── Scripts/
│   ├── LeaderboardService.cs
│   ├── LbData.cs
│   ├── UILeaderboardPanel.cs
│   └── LbRankRow.cs
└── Prefabs/
    ├── UILeaderboard.prefab   (addressable, thay UILearderBoard_ComingSoon ở featuresTab_2)
    ├── LbRankRow.prefab
    ├── LbClanRow.prefab
    └── LbSpacer.prefab
```

asmdef references:
`Falcon.Shared.Common`, `Falcon.Shared.Tab`, `Falcon.Helpers.EventBus`,
`Falcon.Modules.Core.Network.Runtime`, `Falcon.Modules.Core.UI.Runtime`,
`Falcon.Modules.UI.Menu.Runtime`, `Falcon.Modules.UIModular.UILeaderboard.Runtime`,
`SuperScrollView`, `Unity.TextMeshPro`, `UniTask`.

Reference tới `UILeaderboard.Runtime` chỉ để dùng: `CSGetLBData`, `SCGetLBData`,
`CSGetCategoryDataLB`, `SCGetCategoryDataLB`, `CountdownTmpBasic`, `LeaderboardManager` (đọc config).

## Data model

Server trả `rows` và `bonusData` dưới dạng **string JSON**, nên client tự do chọn shape.
Bản cũ có `PlayerLBData` và `ClanLBData` riêng; chúng chỉ khác vài field và Newtonsoft bỏ qua
field thiếu → gộp còn một.

```csharp
// Ăn được cả PlayerLBData {code,rank,name,score,level,profileData}
// lẫn ClanLBData      {code,rank,name,score,iconId}
public class LbEntry
{
    public int code, rank, score, level, iconId;
    public string name, profileData;
}

// Kết quả 1 lần fetch
public class LbPage
{
    public List<LbEntry> entries = new();
    public LbEntry me;              // từ bonusData.myData, null nếu không có
}
```

`bonusData` parse bằng `JObject`, đọc field `myData` nếu có. Không cần class `BonusDataType*`.

## LeaderboardService

Đăng ký trong `StartBehaviour.InitModules()`:
`try { Center.GetOrCreate<LeaderboardService>(); } catch (Exception e) { Debug.LogException(e); }`

API:

```csharp
bool Ready    => LeaderboardManager.ConnectedToServer();
bool Unlocked => Ready && CurrentLevel >= LeaderboardManager.LevelUnlock();
int  LevelUnlock => LeaderboardManager.LevelUnlock();

UniTask<LbPage> Fetch(string category, string type, CancellationToken ct);
UniTask<DateTime> FetchEndTime(string category, CancellationToken ct);
bool CanManualRefresh();
void Invalidate();
```

`CurrentLevel` = `GameRequest<int>.Request(GameKeys.GET_LEVEL)` — bỏ hẳn `ILeaderboardCustom`
và `LBCusByAu` (đang hardcode `return 10000`). Module mới **không** gọi
`LeaderboardManager.Register()` và **không** gọi `LeaderboardManager.Unlocked()`
(hàm đó cần `_custom`, sẽ log error nếu null).

Cache: `Dictionary<string, LbPage>` key `$"{category}_{type}"`, cộng cache riêng cho endTime.

TTL: trước mỗi `Fetch`, nếu `DateTime.Now >= LeaderboardManager.nextTimeGetDataFromServer`
thì gọi `LeaderboardManager.ClearDataOnly()` (module cũ tự set lại 2 mốc theo config server)
rồi xoá cache của mình. Cách này dùng lại được `refreshInterval` dù nó `internal`.

`CanManualRefresh()` = `DateTime.Now >= LeaderboardManager.nextTimeCanRefreshOnClick`.

Fetch bọc callback thành UniTask:

```csharp
var tcs = new UniTaskCompletionSource<SCGetLBData>();
new CSGetLBData { leaderboardCategoryStr = category, leaderboardTypeStr = type }
    .AddSCListener<SCGetLBData>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
    .Send();
var sc = await tcs.Task.AttachExternalCancellation(ct);
```

Trả `null` khi timeout/lỗi — panel hiển thị empty state, không throw.

### Điểm khác biệt so với bản cũ

Bản cũ không huỷ request khi user đổi tab nhanh: data của tab A về sau có thể ghi đè
màn hình tab B. Bản mới mỗi lần đổi tab tạo `CancellationTokenSource` mới và huỷ cái trước.

## UILeaderboardPanel

```csharp
[Serializable]
public class LbCategory
{
    public string id;              // "TopPlayer" | "Team"
    public string rowPrefab;       // "LbRankRow" | "LbClanRow"
    public bool active = true;     // ẩn category chưa mở (vd Team)
}

[SerializeField] private List<LbCategory> _categories;
[SerializeField] private string[] _typeIds;                     // "Global", "National"
[SerializeField] private UITab_Parent _categoryTabs, _typeTabs; // Falcon.Shared.Tab
```

Category × type là một **lưới**, không phải danh sách phẳng: một nút category dùng chung cho
mọi type. Trạng thái hiện tại là cặp `(_categoryTabs.CurrentIndexTab, _typeTabs.CurrentIndexTab)`.
Prefab dòng lấy từ `category.rowPrefab`.

Thứ tự `_categories` phải khớp thứ tự `_categoryTabs.lsChild` / `lsButton`; `_typeIds` khớp `_typeTabs`.

So với bản cũ: vẫn là 2 chiều như `LeaderboardMainPanel` + `LeaderboardCategory`, nhưng là 2
`List` phẳng trong cùng một MonoBehaviour thay vì 2 tầng MonoBehaviour + cây GameObject bật/tắt.

Luồng:

```
OnEnable → chỉ đăng ký EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED
Lần đầu tab được chọn (index == _menuTabIndex)
  → RefreshToggles()  (ẩn nút của category active == false; ẩn cả hàng nếu chỉ còn 1)
  → await UniTask.WaitUntil(() => Service.Ready)      [hiện NoInternetPanel trong lúc chờ]
  → await UniTask.WaitUntil(() => Service.Unlocked)   [hiện LockedPanel, text {[LEVEL]} ← LevelUnlock]
  → _suspendSelect = true; _typeTabs.Active(0); _categoryTabs.Active(first); _suspendSelect = false
  → await Select(first, 0)

Select(categoryIndex, typeIndex)   ← gọi từ UITab_Child.OnActive
  → bỏ qua nếu _suspendSelect hoặc trùng lựa chọn hiện tại
  → huỷ CTS cũ, tạo CTS mới
  → bật loading, tắt empty (visual toggle do UITab_Parent tự lo)
  → page = await Service.Fetch(category.id, type.id, ct)
  → nếu category đổi: endTime = await Service.FetchEndTime(category.id, ct) → countdown
  → Bind(page)  → LoopListView2.SetListItemCount(n, resetPos: true)
  → tắt loading; empty.SetActive(n == 0); myRow.SetActive(page?.me != null)

OnRefreshClick
  → CanManualRefresh() ? (Invalidate(); Select lại đúng tab hiện tại) : FakeLoading(0.5s)

OnDisable → huỷ CTS
```

`myRow` (dòng của mình) là một `LbRankRow` cố định **ngoài** scroll, bind từ `page.me`.
Bản cũ nhét nó vào bonusData rồi xử lý rải rác trong `HandleBonusDataFromSc` của từng panel.

Header/footer spacer: giữ như cũ (`headerCount = footerCount = 1`, prefab `LbSpacer`),
vì `LoopListView2` không có padding trên/dưới.

Countdown: dùng lại `CountdownTmpBasic` của module cũ, không viết mới.

## Row

```csharp
public class LbRankRow : MonoBehaviour { public void Bind(LbEntry e, bool isMe); }
```

Một class duy nhất, không abstract base, không `GetRowType()`. Clan dùng cùng script nhưng
prefab khác (`LbClanRow`) có điền mảng sprite theo `iconId`.

Avatar/khung tên: row tự đọc `entry.profileData` (JSON string từ server) và `entry.iconId`.
Bỏ `ILeaderboardCustom.OnSetAvatarUI` / `OnSetNameStyleUI`.

## Toggle: dùng lại `Falcon.Shared.Tab`

Không viết class toggle mới. Module `_Game/Shared/Modules/Tab` đã có sẵn `UITab_Parent` +
`UITab_Child` làm đúng việc này, và `PopupEditProfile.cs:37-39` đã dùng chính pattern đó:

```csharp
_categoryTabs.lsChild[i].OnActive += () => OnCategoryChanged(i);
_typeTabs.lsChild[i].OnActive     += () => OnTypeChanged(i);
```

`UITab_Parent` lo click, đổi visual `btnEnable`/`btnDisable`, và giữ `CurrentIndexTab`.
Bản cũ có 3 class (`ALBToggleBase` + `LBCategoryToggle` + `LBTypeToggle`) cho việc này; bản mới có 0.

Hai chỗ cần lưu ý khi dùng:

- **`activeOnEnable = false`** trên cả hai `UITab_Parent`. Mặc định là `true`, sẽ `Active(0)` ngay
  ở `OnEnable` — tức là gọi server trước khi qua cổng ready/unlock.
- **Chống đệ quy khi chọn tab lần đầu.** `Select` được gọi từ chính `OnActive`, mà `Open` lại phải
  gọi `Active()` để dựng trạng thái ban đầu. Dùng một cờ `_suspendSelect` bao quanh hai lệnh
  `Active()` khởi tạo, rồi `await Select(...)` một lần duy nhất — nếu không sẽ có 2 request thừa
  bị huỷ lẫn nhau.

Ẩn category chưa mở: `_categoryTabs.lsButton[i].gameObject.SetActive(active)` cộng với
`_categoryTabs.ConditionSelect = i => _categories[i].active` (field có sẵn của `UITab_Parent`).

## Xử lý lỗi

| Tình huống | Hành vi |
|---|---|
| Chưa có session / mất mạng | `NoInternetPanel`, tự vào lại khi `Ready` (module cũ có `ISessionListener` cập nhật `ConnectToServer`) |
| Chưa nhận `sc_get_config_lb` | `LevelUnlock()` trả `-1` → `Unlocked` false → `LockedPanel`, tự thoát khi có config |
| Fetch timeout | `Fetch` trả `null` → empty state, nút refresh vẫn bấm được |
| Đổi tab khi request chưa về | CTS huỷ request cũ, kết quả cũ bị bỏ |
| JSON rác | `try/catch` quanh deserialize, `Debug.LogError` + trả `null` |

## Kiểm chứng

Ponytail: một check chạy được, không framework.

`LbData.cs` có `#if UNITY_EDITOR` static `SelfCheck()` với `[MenuItem]`, assert rằng `LbEntry`
parse đúng cả hai shape JSON server gửi:

```csharp
Assert(JsonConvert.DeserializeObject<LbEntry>("{\"code\":1,\"rank\":2,\"name\":\"a\",\"score\":3,\"level\":4,\"profileData\":\"x\"}").level == 4);
Assert(JsonConvert.DeserializeObject<LbEntry>("{\"code\":1,\"rank\":2,\"name\":\"a\",\"score\":3,\"iconId\":7}").iconId == 7);
```

Đây là chỗ duy nhất có logic dễ sai âm thầm (gộp 2 class server thành 1). Phần UI kiểm bằng
nút `[Button] BindFakeData(50)` trên `UILeaderboardPanel` — dựng 50 entry giả, không cần server.

## Phạm vi không đụng tới

- `Assets/Falcon/Modules/UIModular/UILeaderboard/` — giữ nguyên 100%
- `Assets/FalconAssets/Modules/UIModular/UILeaderboard/Scripts/*_ByUser.cs` — giữ nguyên
- Prefab `UILeaderboard.prefab` / `UILearderBoard_Comingsoon.prefab` cũ — giữ nguyên, chỉ ngừng dùng

Hệ quả: `LeaderboardManager.dataDic` vẫn bị `SCGetLBData.OnData()` ghi vào (tối đa
số category × type entry). Chấp nhận được; nếu sau này xoá module cũ thì phần CS/SC
chuyển sang module mới và bỏ được `LeaderboardManager`.
