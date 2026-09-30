# Hướng dẫn log tracking event — BigData 1.3.0

Dành cho dev game tích hợp SDK. Đọc 5 phút là log đúng; phần "vì sao" nằm ở
`EntityLifecycle-Design.md`, danh mục field trên wire nằm ở `DWH-EventLog-API-Spec.md`.

## Triết lý 30 giây

1. **Báo KHOẢNH KHẮC, đừng dựng event.** Game nói "người chơi vừa qua màn" — SDK lo chuyện dựng
   bản tin, điền id lượt chơi, cache tham số, tính counter. Đừng tự new log rồi tự điền những thứ
   SDK điền được.
2. **Param = cái GAME khai, phần còn lại SDK thêm.** Field nào bạn không thấy trên API nghĩa là
   SDK tự lo (`playTurnId`, `adViewId`, `winStreak`, `fillLatencyMs`…) — không có cách set tay,
   và đó là có chủ ý.
3. **Một cửa duy nhất:** `FalconBigDataController.<Nhóm>.<Hàm>`. Không cần giữ reference.
4. **Vocab có hằng số thì CẤM gõ string tay** (luật hợp đồng §H2) — mỗi game một kiểu chữ là
   dữ liệu chéo game mù. Thêm giá trị mới = sửa hợp đồng với đội data, không tự chế.

## Bản đồ 10 nhóm API

| Nhóm | Dành cho | Hàm chính |
|---|---|---|
| `Level` | lượt chơi màn | `OnStart` `OnPass` `OnFail` `UpdateTurn` `OnPauseExit`/`OnResume` `SetUserElo` |
| `Ad` | vòng đời một lần xem ad | `OnRequested` `OnShown` `OnImpression` `OnClicked` `OnClosed` `UpdateContext` — thường **module Mediation gọi hộ** |
| `IapOffer` | một lần hiển thị offer IAP | `OnShown` `OnClicked` `OnClosed` |
| `Iap` | phễu checkout | `OnStarted` `OnPurchased` `OnFailed` — thường **module IAP gọi hộ** |
| `Resource` | dòng tài nguyên | `OnEarned` `OnSpent` `OnExchange` |
| `Funnel` | phễu FTUE / feature | `Register` `OnStep` `CurrentPriority` `HasPassed` `LastJoinDay` |
| `Label` | gán nhãn / tham số động | trục LẦN: `User` `Session` `LevelPlayTurn` `AdView` `IapOfferImpression` `IapPurchase` · trục VẬT: `Level` `IapOffer` |
| `Common` | tham số đi theo MỌI log | `Set` `Remove` |
| `App` | mốc mở app, permission | `ReportOpenSource` `ReportAttStatus`… — thường **module khác gọi hộ** |
| `Player` | đọc chỉ số người chơi | (read-only) |

**Convention chữ ký**: mốc nào cũng nhận **param object** (cửa chuẩn — payload phình thêm field
thì chữ ký không đổi); các bản knob phẳng (`OnRequested(type, adWhere: ...)`) chỉ là shorthand
dựng param rồi gọi vào đúng cửa đó — một đường xử lý, hai cách viết. Dùng param object thì
`param.type`/id là của bạn khai — SDK không đoán hộ.

---

## Level — lượt chơi màn

```csharp
var BD = FalconBigDataController.Level;

BD.OnStart(currentLevel: 42, difficulty: "hard", currentLevelId: "abc12",
           movesLimit: 30, timeLimitSec: 90);          // mở LƯỢT — SDK sinh playTurnId

BD.OnPass(score: 15300, duration: TimeSpan.FromMinutes(2),
          boostersUsed: new() { ["hammer"] = 1 }, movesUsed: 24);

BD.OnFail(failReason: LevelFailReason.OutOfMoves, levelProgress: 85);
```

- Sau `OnStart`, các mốc sau **không nhập lại** định danh (level, difficulty, id…) — cache tự điền,
  giá trị bạn tự nhập luôn thắng.
- `movesLimit`/`timeLimitSec` là config của **BẢN THIẾT KẾ màn** — SDK tự bỏ vào bundle nhãn màn,
  khai một lần là mọi lần chơi sau tự có.
- **Bỏ ván giữa chừng** (quit/drop): nếu game coi đó là thua thì gọi
  `OnFail(failReason: LevelFailReason.Quit)`. Không gọi gì thì chuỗi thắng/thua ĐỨNG YÊN đi qua —
  SDK không suy "bỏ = thua" hộ.
- **Rời ván để chơi tiếp sau**: `var snap = BD.OnPauseExit();` → persist `snap` vào save →
  `BD.OnResume(snap);` khi mở lại. Một ván = một `playTurnId` dù qua restart.
- **Elo người chơi**: `BD.SetUserElo(1350);` mỗi khi nó đổi (gọi lại cùng giá trị SDK tự bỏ qua).
  **ĐỪNG** gửi độ khó của màn — server tự giải từ tỉ lệ pass của cả fleet.
- **`winStreak`/`loseStreak` trên log là chuỗi THỐNG KÊ chuẩn fleet** — SDK tính, không set được.
  Feature streak riêng của game (first-attempt-only, milestone…) → `Label.User("streak_status", n)`.

## IapOffer — một lần hiển thị offer

```csharp
FalconBigDataController.IapOffer.OnShown(new IapOfferParam {
    offerId = "black_friday_gem",
    offerCategory = "gem_bundle", offerLayout = "discount",
    offerProductId = new[] { "gem_100", "gem_500" },
    offerPrices    = new() { ["gem_100"] = 29000, ["gem_500"] = 129000 },   // giá LOCAL
    offerDiscounts = new() { ["gem_100"] = 0.3f,  ["gem_500"] = 0.5f },
    offerCurrencyCode = "VND",
    placementScene = "shop", triggerEvent = "on_login",
});
FalconBigDataController.IapOffer.OnClicked();
FalconBigDataController.IapOffer.OnClosed();
```

- **Một kệ nhiều offer = MỘT lần `OnShown`** mang mảng `offerProductId` — không gọi mỗi offer một
  lần (luật volume).
- Khai `offerProductId` thì SDK tự quy giao dịch về offer (conversion thật, kể cả mua ngay sau khi
  popup đóng). `offerImpressionId`/`impressionDuration`/`isClicked`/`isPurchased` — SDK lo hết.
- Giá gửi tiền LOCAL + mã ISO, **không tự quy đổi USD**.

## Resource — dòng tài nguyên

```csharp
// "Mua 3 booster bằng 500 gold" = MỘT giao dịch hai vế, SDK sinh exchangeId chung
FalconBigDataController.Resource.OnExchange(
    sink:   new ResourceParam { flowType = FlowType.Sink,   itemType = "currency", itemId = "gold",
                                amount = 500, currency = "gold",
                                resourceWhere = "shop_screen", resourceWhen = FResourceWhen.Shop },
    source: new ResourceParam { flowType = FlowType.Source, itemType = "booster",  itemId = "hammer",
                                amount = 3, currency = "gold",
                                resourceWhere = "shop_screen", resourceWhen = FResourceWhen.Shop });
```

Ba field hay nhầm — nhớ đúng một câu: **`where` = Ở ĐÂU, `when` = VÌ SAO, `itemType` = LOẠI GÌ**:

| Field | Nghĩa | Vocab |
|---|---|---|
| `resourceWhere` | màn/panel nào | game tự đặt |
| `resourceWhen` | **nguyên nhân** tài nguyên chảy | `FResourceWhen.*` (battle_pass, level_reward, shop…) |
| `itemType` | loại vật phẩm/giao dịch | game tự đặt |
| `resourceWhen = FResourceWhen.Iap` + `transactionId` | vế nhận từ TIỀN THẬT | nối về event mua bằng transactionId |
| `adViewId` | thưởng từ rewarded ad | `Ad.CurrentViewId(type)` NGAY LÚC grant — SDK không auto-stamp |

⚠ `playTurnId` SDK tự đóng lên log resource chỉ nói "xảy ra TRONG lúc chơi màn" — **không** phải
"vì màn". Mua gói trong shop giữa ván vẫn mang playTurnId nhưng `resourceWhen` là `Shop`.

Giao dịch MỘT chiều (không phải trao đổi) có cửa riêng, SDK tự set `flowType`:

```csharp
// Nhận thưởng hoàn thành level — chỉ có vế nhận
FalconBigDataController.Resource.OnEarned(new ResourceParam {
    itemType = "currency", itemId = "gold", currency = "gold", amount = 100,
    resourceWhere = "level_end_popup", resourceWhen = FResourceWhen.LevelReward });

// Tiêu coin để revive — thứ nhận về (lượt hồi sinh) không phải tài nguyên đếm được
FalconBigDataController.Resource.OnSpent(new ResourceParam {
    itemType = "currency", itemId = "coin", currency = "coin", amount = 900,
    resourceWhere = "revive_popup", resourceWhen = FResourceWhen.Reward });
```

Ranh giới với `OnExchange`: thứ nhận về **là tài nguyên đếm được** (gold → booster) thì là trao
đổi — đi `OnExchange` để hai vế chung `exchangeId`. Context tuỳ ý nhét vào `ResourceParam.detail`.
Đường `new FResourceLog(param).Send()` cũ vẫn chạy nguyên.

## Funnel — FTUE và feature

Fact thật của event này là **mốc TIẾN ĐỘ của một tính năng** ("người chơi đạt bước X của Y") —
tên "funnel" là di sản; giá trị của nó là chuẩn hoá cross-game: mọi tính năng đổ tiến độ vào chung
một hình dạng nên dashboard funnel phía server generic được, không cần config per game.

**Litmus trước khi gọi `OnStep`**: mốc đó đã có event CHỦ THỂ riêng chưa?

| Phễu | Dựng bằng | KHÔNG log qua Funnel |
|---|---|---|
| Checkout | 3 mốc iap + JOIN `purchaseAttemptId` | ✗ |
| Fill/hiển thị ad | 4 mốc ad + JOIN `adViewId` | ✗ |
| Level flow | level events + `playTurnId` | ✗ |
| FTUE, battle pass, event mùa — tiến độ tính năng KHÔNG có event chủ thể | `Funnel.OnStep` | ✓ đúng chỗ |

Log lại thứ đã có event chủ thể qua Funnel là double coverage — hai nguồn cho một sự thật, sớm
muộn lệch nhau.

```csharp
var F = FalconBigDataController.Funnel;
F.Register(FFunnelName.BattlePass, FunnelShape.Repeatable);   // 1 lần lúc khởi động
F.OnStep(FFunnelName.BattlePass, FFunnelAction.Join,      priority: 1);
F.OnStep(FFunnelName.BattlePass, FFunnelAction.Milestone, priority: 2);

// Đọc tiến độ — cùng sổ mà van lọc trùng dùng (sổ LOCAL đời máy, không phải chân lý cross-device)
int? step = F.CurrentPriority(FFunnelName.Ftue);            // mốc cao nhất đã đi (chỉ OnceOrdered)
bool done = F.HasPassed(FFunnelName.Ftue, 3);               // OnceOrdered đã qua mốc 3?
bool claimed = F.HasPassed("battle_pass", "season_5", FFunnelAction.Claim, 3); // OncePerCycle
DateTime? joined = F.LastJoinDay("battle_pass");            // lần Join gần nhất (mốc của funnelDay)
```

- Ba hình dạng — chọn lúc `Register` (gọi lại mỗi lần khởi động, như mọi khai báo từ code):

| Shape | Luật | Dùng cho |
|---|---|---|
| `OnceOrdered` | một lần trên đời máy, bước tăng đều — sai thì **chặn** | `ftue` |
| `Repeatable` | không lọc gì — lặp/nhảy cóc đều hợp lệ | `daily_quest`, `lucky_wheel` (bước lặp trong ngày là thật) |
| `OncePerCycle` | mỗi (action, priority) MỘT lần trong một `cycleId` — trùng thì **chặn**; sang chu kỳ mới tự sạch | `battle_pass` theo mùa |

```csharp
F.Register("clan_war", FunnelShape.OncePerCycle);
F.OnStep("clan_war", FFunnelAction.Milestone, priority: 3, cycleId: "season_5");
```

- `OncePerCycle` **bắt buộc** truyền `cycleId` ("season_5", "2026-08-12"…) — thiếu thì bước đó đi
  qua KHÔNG lọc kèm cảnh báo. Shape khác truyền `cycleId` cũng tốt: server tách mùa chính xác.
- Tên chưa `Register`: tiền tố `event_` mặc định `Repeatable`; còn lại rơi về luật ftue — **tên tự
  chế mà quên Register là bước lặp thứ hai bị chặn**, nhớ đăng ký lúc khởi động.
- Tên funnel/action lấy từ `FFunnelName`/`FFunnelAction`; liveops per-game dùng tiền tố
  `FFunnelName.EventPrefix + "halloween_2026"`.

### Register ở đâu — recipe registrar

`_shapes` KHÔNG persist (khai báo từ code, persist là đẻ nguồn chân lý thứ hai cãi nhau với code
sau update) — nên game phải Register lại **mỗi lần khởi động, trước bước funnel đầu tiên**. Chỗ
đúng là `ISingletonServiceReady`:

```csharp
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.BigData;

/// Khai hình dạng funnel riêng của game — một class này cho cả game.
/// [NoLazy] BẮT BUỘC: singleton lazy không ai reference sẽ không bao giờ được tạo,
/// thiếu attribute là cả cụm khai báo im lặng biến mất.
[NoLazy]
public class MyGameFunnelShapes : MySingleton<MyGameFunnelShapes>, ISingletonServiceReady
{
    private readonly FunnelRegistry _registry;

    // Ctor-inject FunnelRegistry (leaf, không dep) — KHÔNG gọi FalconBigDataController
    // từ constructor: DI đang dựng đồ thị, kéo cả pipeline dậy là khuôn deadlock kinh điển.
    public MyGameFunnelShapes(FunnelRegistry registry)
    {
        _registry = registry;
    }

    public void OnSingletonServiceReady()
    {
        // Chạy đồng bộ tại SubsystemRegistration — TRƯỚC mọi code scene/UI,
        // nên không tồn tại cửa sổ cho OnStep đến trước khai báo
        _registry.Register("clan_war", FunnelShape.OncePerCycle);
        _registry.Register(FFunnelName.EventPrefix + "halloween_2026", FunnelShape.OncePerCycle);
        // Chỉ ghi dict — ĐỪNG làm IO/log ở phase này
    }
}
```

Vì sao không phải hook khác: **constructor** chạy giữa lúc DI dựng đồ thị (khuôn deadlock);
**`IInit`** là chuỗi tuần tự có thể bị Init-chờ-mạng treo hàng chục giây khi offline trong khi
UI không đợi chuỗi đó — FTUE step đầu có thể đến trước Init của bạn; **`IPioneer`/`ITerminal`**
là hook pause/resume, sai ngữ nghĩa.

## Label — nhãn và tham số động

Kênh nhãn có **hai trục**, đọc tên hàm là biết trục nào:

```csharp
var L = FalconBigDataController.Label;

// TRỤC LẦN — nhãn của instance ĐANG MỞ, mỗi hàm bắn một event nhãn mang id của instance
L.User("vip_tier", "gold");              // theo NGƯỜI CHƠI, hiệu lực từ lúc này; null = GỠ
L.User("spend_bucket", 3);               // giá trị SỐ cũng được — nhãn gánh cả tham số động
L.Session("entered_event", true);        // theo PHIÊN
L.LevelPlayTurn("used_hint", true);      // theo LƯỢT CHƠI đang mở
L.AdView(AdType.Interstitial, "k", 1);   // theo LẦN XEM AD đang mở
L.IapOfferImpression("variant", "b");    // theo LẦN HIỂN THỊ OFFER đang mở
L.IapPurchase("promo", "TET26");         // theo LƯỢT MUA đang mở

// TRỤC VẬT — nhãn của BẢN THIẾT KẾ, không bắn event nào, đi ké bundle trên event của vật
L.Level(42, "cluster", "hard_1");        // màn 42 — mọi lần chơi màn đó đều mang
L.IapOffer("black_friday", "theme", "bf2026"); // offer design — mọi lần hiện đều mang
```

Cặp đôi: `Level` ↔ `LevelPlayTurn`, `IapOffer` ↔ `IapOfferImpression` — vật tả thiết kế (đúng
mãi), lần tả một lượt (đúng một lần). Đánh lại màn 20 lần: nhãn lượt = 20 bản tin, nhãn vật = 0.

- **Litmus**: muốn *TÍNH* trên nó (sum/avg) → param của event; chỉ muốn *LỌC/chia nhóm* → label.
- Nhãn VẬT không bắn bản tin nào — đi ké mọi event của vật đó, gọi bao nhiêu lần cũng
  không tốn; trần 256 B/bundle. Nhãn user: trần 20 key, value chữ ≤ 64 ký tự — server DROP nếu vượt,
  SDK chặn sớm kèm cảnh báo.
- ⚠ **Key trùng tên cột hợp đồng bị từ chối** (vd `win_streak`) — đặt tên theo nghĩa của game:
  `streak_status`, không phải `win_streak`.

## Common — tham số đi theo MỌI log

```csharp
FalconBigDataController.Common.Set("guild_id", "g_1207", persist: true); // bền qua restart
FalconBigDataController.Common.Set("gold_balance", () => Wallet.Gold);   // đọc TƯƠI mỗi log
FalconBigDataController.Common.Remove("guild_id");
```

- Đây là chỗ **đắt nhất** để thêm field (nhân với mọi log của mọi user) — quá ~15 key SDK sẽ càu nhàu.
- `persist` chỉ bật cho giá trị bền thật; giá trị phiên trước có thể đã sai thì đừng.
- Key phải đăng ký với đội data (key vô danh server không nhặt), và không được trùng cột hợp đồng
  (SDK từ chối kèm warning).

## Custom event — khi không có nhóm nào khớp

```csharp
new FEventLog("boss_rush_enter", new Dictionary<string, object> { ["boss_id"] = 7 }).Send();
```

Param game-specific chưa vào hợp đồng → nhét vào `extraMeta` của event tương ứng (được LƯU, đăng ký
hợp đồng sau là cook hồi tố được) — **đừng** lách qua label (bị drop) hay common (bị soát).

## Ad / Iap / App — thường KHÔNG phải việc của dev game

Ba nhóm này do module khác gọi hộ khi bạn dùng trọn bộ Falcon (Mediation, IAP, Push/UMP — xem các
file `Request-*.md`). Chỉ đụng tay khi tự tích hợp: `Ad.OnRequested/OnShown/OnClosed` theo vòng đời
load→show→close; `Iap.OnStarted` lúc mở billing + `OnFailed(reason)` lúc lỗi/huỷ;
`App.ReportOpenSource(OpenSource.Push, campaignId)` khi mở từ notification.

## Những lỗi kinh điển

| Đừng | Mà hãy |
|---|---|
| Gọi lại `Level.OnStart` khi mua thêm lượt đi giữa ván | `Level.UpdateTurn(...)` — Start mới là ĐÈ lượt |
| Tự đếm winStreak rồi gửi qua label `win_streak` | đọc `Level.WinStreak`; feature streak → `streak_status` |
| Gửi độ khó màn tự tính lên server | không gửi — server giải từ tỉ lệ pass cả fleet |
| Log mỗi offer trên kệ shop một event | MỘT `Offer.OnShown` mang mảng productId |
| Quy đổi giá về USD ở client | gửi giá local + mã ISO |
| String tay `"battle_pass"`, `"claim"`… | hằng số `FFunnelName` / `FFunnelAction` / `FResourceWhen` / `FIapPlacement` |
| Nhét số đo vào label để "tiện lọc" | số cần TÍNH thì để param/extraMeta |
| Coi thiếu field = điền 0/""/“Unknown” | để null — vắng mặt khỏi payload là câu trả lời đúng |

## Đường log cũ (`new F*Log(...).Send()`)

Vẫn chạy nguyên — controller là lớp vỏ gọi xuống cùng pipeline. Code cũ không phải sửa; code mới
nên đi cửa controller để được cache/id/counter tự động.
