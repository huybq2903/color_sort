# Tracking Race Event (Go-Kart) — TODO cho dev + tham chiếu log

> **Dev chỉ cần đọc phần TODO ngay dưới đây** — mỗi việc: sửa file nào, chép đoạn code nào, kiểm
> ra sao. Tick `[x]` khi xong. Giải thích "vì sao" và spec đầy đủ nằm ở nửa dưới.
> Cập nhật 17/09/2026 theo module Go-Kart thực tế + đối soát dữ liệu bee.flow (đợt
> `go_kart_race_20260914`, `go_kart_race_20260918`). Funnel (open / join / milestone / complete)
> **đang log đúng**, không phải sửa.

# ✅ TODO

## 0. 🔴 LÀM NGAY — nút event có thể đang bị giấu (mất người tham gia thật)

Đợt 0918: 67 người mở event, chỉ 4 người tham gia (đợt trước 79%). Nghi phạm: luật **giấu nút +
đóng popup với người chưa tham gia khi "sắp hết giờ"** (`DeclareGoKartRace.ShouldHideForStartCutoff`).

### [ ] X1 — Kiểm config đợt đang chạy (người cấu hình event / owner module)

1. Mở config Go-Kart của đợt đang chạy, xem `startMinRemainingSeconds`.
2. So với **độ dài đợt**. Nếu `startMinRemainingSeconds` ≥ độ dài đợt (vd đợt 4 ngày mà đặt ≥
   345600) → nút bị giấu **cả đợt**. Sửa về giá trị đúng (vd chặn khi còn < 4h = `14400`).
3. Kiểm xem luật giấu nút này có phải mới vào module ở build app **1.0.6** không.

### [ ] X2 — Data tách lỗi app hay lỗi config

Trong **riêng đợt 0918**, chia tỉ lệ thấy-nút / tham gia theo bản app 1.0.5 và 1.0.6. User 1.0.5
cũng tụt → lỗi config (X1). Chỉ 1.0.6 tụt → lỗi bản app.

## A. Module Go-Kart — owner module làm

File chính: `Scripts/Runtime/Analytic/GoKartRaceTracking.cs`

### [ ] A1 — Đổi tên hằng key sang tiền tố `race_` (dễ nhất)

Trong `GoKartRaceTracking.cs`, sửa **giá trị chuỗi** (tên hằng giữ nguyên):

```csharp
public const string KEY_LAP           = "race_lap";            // cũ: "lap"
public const string KEY_STAGE         = "race_stage";          // cũ: "stage"
public const string KEY_ATTEMPT       = "race_attempt";        // cũ: "attempt"
public const string KEY_ATTEMPTS_USED = "race_attempts_used";  // cũ: "attempts_used"
public const string KEY_STREAK_POS    = "race_streak_pos";     // cũ: "streak_pos"
public const string KEY_RETRY_REASON  = "race_retry_reason";   // cũ: "retry_reason"
```

**Xoá** hằng `KEY_BOARD_ID`, rồi trong `StageRewardContext` sửa dòng:

```csharp
if (!string.IsNullOrEmpty(state.boardId)) detail[KEY_RACE_BOARD_ID] = state.boardId;   // cũ: KEY_BOARD_ID
```

Không đổi: `KEY_RACE_ID`, `KEY_RACE_BOARD_ID`, và các giá trị `level_fail` / `bot_won`.
**Kiểm:** dòng milestone có `race_lap`; dòng thưởng có `detail.race_stage`, `detail.race_board_id`.

### [ ] A2 — Chỉ ghi "đủ điều kiện" khi event đã mở khoá

`Scripts/Runtime/UI/Button/GoKartRaceButton.cs`, hàm `OnEnable()`:

```csharp
if (!isActiveAndEnabled) return;

if (DeclareGoKartRace.IsUnlocked())         // ← THÊM điều kiện
    GoKartRaceTracking.OnEligible();
GoKartRaceTracking.OnEntryImpression();
```

**Kiểm:** user chưa đủ level (nút đang khoá) → **không** có dòng `eligible`.

### [ ] A3 — Chưa biết đợt nào thì không đếm "thấy / bấm"

`GoKartRaceTracking.cs`, thay 2 hàm:

```csharp
public static void OnEntryImpression()
{
    var meta = RaceMeta();
    if (meta == null) return;               // config chưa về → chưa biết race_id
    FalconBigDataController.Ui.OnImpression(SURFACE_ENTRY, meta);
}

public static void OnEntryClicked()
{
    var meta = RaceMeta();
    if (meta == null) return;
    FalconBigDataController.Ui.OnClicked(SURFACE_ENTRY, meta);
}
```

### [ ] A4 — Đếm "thấy / bấm" cho popup tự bật

**Bước 1.** `GoKartRaceTracking.cs` — thêm hằng và 2 hàm:

```csharp
public const string SURFACE_POPUP = "race_event_popup";

public static void OnPopupImpression()
{
    var meta = RaceMeta();
    if (meta == null) return;
    FalconBigDataController.Ui.OnImpression(SURFACE_POPUP, meta);
}

public static void OnPopupClicked()
{
    var meta = RaceMeta();
    if (meta == null) return;
    FalconBigDataController.Ui.OnClicked(SURFACE_POPUP, meta);
}
```

**Bước 2.** `Scripts/Runtime/UI/Join/UIGoKartRaceJoin.cs`:

```csharp
protected override void SetView(GoKartRaceConfig config)
{
    if (DeclareGoKartRace.ShouldHideForStartCutoff()) { Close(); return; }

    GoKartRaceTracking.OnOpen();
    GoKartRaceTracking.OnPopupImpression();          // ← THÊM
    ...
}

public void StartRace()
{
    GoKartRaceTracking.OnPopupClicked();             // ← THÊM (dòng đầu tiên)
    ...
}
```

**Kiểm:** mở app, popup tự bật → cụm `f_sdk_ui_impression` có `surfaceId = race_event_popup`.

### [ ] A5 — Thưởng đầu mùa kèm thông tin event

**Bước 1.** `GoKartRaceTracking.cs` — thêm:

```csharp
public const string WHERE_START_SEASON_REWARD = "start_season_reward";

public static ResourceParam StartSeasonRewardContext()
{
    var detail = new Dictionary<string, object>();
    var raceId = RaceId;
    if (!string.IsNullOrEmpty(raceId)) detail[KEY_RACE_ID] = raceId;

    return new ResourceParam
    {
        resourceWhen = RESOURCE_WHEN,
        resourceWhere = WHERE_START_SEASON_REWARD,
        exchangeId = Guid.NewGuid().ToString("N"),
        detail = detail
    };
}
```

**Bước 2.** `Scripts/Runtime/DeclareEvent/DeclareGoKartRace.cs`, hàm `GrantStartSeasonRewards`:

```csharp
if (startSeasonRewards is { Length: > 0 })
{
    var context = GoKartRaceTracking.StartSeasonRewardContext();   // ← THÊM
    ShowRewardPopup(startSeasonRewards, () =>
    {
        GrantReward(startSeasonRewards, context);                  // ← thêm context
    });
}
```

### [ ] A6 — Phát hành module

- `package.json`: thêm dependency `"falcon.modules.core.bigdata": "1.3.9"` (Ui API cần ≥ 1.3.9);
  tăng version module.
- CHANGELOG module ghi "đổi tên key sang `race_*`, thêm surface `race_event_popup`" — **báo data
  team số bản**.

## B. Game — dev từng game làm

### [ ] G1 — Gắn thông tin race vào log bắt đầu ván

Chỗ game gọi `FLevelManager.OnLevelStart(...)` cho **level thường**:

```csharp
// Game CHƯA truyền extraMeta:
levelManager.OnLevelStart(extraMeta: GoKartRaceTracking.LevelStartMeta());

// Game ĐANG truyền extraMeta (dict cũ tên là meta):
var race = GoKartRaceTracking.LevelStartMeta();
if (race != null)
{
    meta ??= new Dictionary<string, object>();
    foreach (var (key, value) in race) meta[key] = value;
}
levelManager.OnLevelStart(extraMeta: meta);
```

- `LevelStartMeta()` tự trả `null` khi user không đang đua → level thường không bị gắn gì.
- ⚠ Gọi **đúng một lần** mỗi ván (hàm "tiêu" lý do retry sau lần gọi đầu).

**Kiểm:** đang đua, bắt đầu ván → dòng level Start có `race_id`, `race_board_id`, `race_stage`,
`race_attempt`, `race_streak_pos` (ván đầu sau khi thua thêm `race_retry_reason`).

### [ ] G2 — Chuyển tiếp thông tin thưởng

Trong class game kế thừa `GoKartRaceCustomBase`, **thêm** override bản 2 tham số:

```csharp
public override void GrantReward(GoKartRaceRewardConfig reward, ResourceParam context)
{
    // Chép y những gì GrantReward(reward) 1 tham số của game đang làm,
    // chỉ khác: truyền context vào Grant.
    EventRewardGranter.Grant(reward, "<place game đang dùng>", context: context);
}
```

⚠ **KHÔNG** gọi thêm `FalconBigDataController.Resource.OnEarned` — kho tài nguyên đã tự ghi một
dòng, gọi thêm là một món thưởng thành hai dòng (đếm đôi).
**Kiểm:** thắng chặng, nhận thưởng → **đúng số dòng = số món thưởng**, mỗi dòng có
`resourceWhen = event_race`, `resourceWhere = stage_reward`, `detail.race_stage`.

### [ ] G3 — Nếu event có THU tiền (phí retry, mua lượt…)

Chỉ làm nếu game có. Trừ tiền qua kho, kèm context:

```csharp
var context = new ResourceParam
{
    resourceWhen = GoKartRaceTracking.RESOURCE_WHEN,
    resourceWhere = "retry_fee",                     // tên chỗ thu, tự đặt
    detail = new Dictionary<string, object> { ["race_id"] = GoKartRaceTracking.RaceId }
};
ResourceCollector.Instance.ResourceRemove("<resourceId>", amount, data, "retry_fee", param: context);
```

Không cần gắn gì cho booster trong ván, phí vào ván thường, tiền chơi tiếp sau khi thua — GD
không tính vào event.

### [ ] G4 — Nâng phiên bản (theo từng game)

| Game | Cần nâng |
|---|---|
| bee.flow | Go-Kart (bản có A1–A6), GameData ≥ 1.1.7, Events Core legacy bản có `Grant(..., context)` + làm G1, G2 (G3 nếu có thu tiền) |
| knit.jam | Cài Go-Kart (bản có A1–A6), BigData ≥ 1.3.9, GameData ≥ 1.1.7, Events Core legacy bản có `Grant(..., context)` + làm G1, G2 (G3 nếu có) — chỉ nâng BigData thì dòng thưởng vẫn `resource_when = Unknown` |

---

# 📖 Giải thích & tham chiếu (không cần đọc để sửa)

## Vì sao từng việc

| Việc | Thiếu thì sao |
|---|---|
| X1, X2 | Luật giấu nút bật cả đợt → người chưa tham gia không còn nút, popup tự đóng → không có đường tham gia. Khớp dữ liệu 0918: 4 người tham gia đúng là 4 người thấy nút |
| A1 | Race gắn extras lên **level thường** — nơi event khác cũng gắn; extras trải phẳng theo luật ai-ghi-trước-giữ → key chung chung (`stage`, `attempt`) là event sau mất số im lặng; `stage` bên Master League là chữ, bên này là số → hỏng cột kho. `board_id` resource thống nhất thành `race_board_id` cho khớp level |
| A2 | Nút hiện cả ở trạng thái khoá → user chưa đủ level bị tính là đủ điều kiện, kéo thấp tỉ lệ tham gia |
| A3 | Nút bật trước khi config về → đếm "thấy" mà không có `race_id`, không quy được về đợt nào |
| A4 | 96–100% user gặp event lần đầu qua popup tự bật (5–10 giây sau mở app) — không đếm là mù đúng kênh chính |
| A5 | Thưởng đầu mùa không mang `event_race` → không tách được khỏi tài nguyên thường |
| G1 | Không có `race_stage` / `race_attempt` trên ván → không tính được số lần retry từng chặng (metric 5), bot dễ/khó, drop vì bí. Hàm `LevelStartMeta()` có sẵn trong module nhưng không ai gọi — Go-Kart chạy trên level thường nên chỗ log nằm ở game |
| G2 | Hook `GrantReward(reward, context)` mặc định **bỏ** context → dòng thưởng mất `resourceWhen` + `detail` (metric 6) |
| G4 | GameData cũ không có chỗ nhận context → knit.jam đang `resource_when = Unknown`, `detail` rỗng |

**Chưa làm (không bắt buộc):** `race_user_time_sec` / `race_best_bot_time_sec` trên milestone —
module không có dữ liệu thời gian của user/bot; `race_retry_reason = quit` — kết quả ván chỉ có
thắng/thua nên quit đang tính là `level_fail`. Milestone hiện chỉ bắn khi user **bấm nhận thưởng
chặng** → data đọc là "đã nhận thưởng chặng".

**Phía BigData (ưu tiên thấp):** cụm đếm "thấy" giữ `race_id` của lần đầu trong phiên — phiên
kéo dài qua lúc đổi đợt có thể dán nhầm đợt. Data đã kiểm: không phải nguyên nhân lần này.

## Tiền tố key `race_` — bảng đổi tên (việc A1)

| Log | Key cũ trong module | Key mới |
|---|---|---|
| Level Start (phẳng) | `stage`, `attempt`, `streak_pos`, `retry_reason` | `race_stage`, `race_attempt`, `race_streak_pos`, `race_retry_reason` |
| Funnel milestone/complete (phẳng) | `lap`, `attempts_used` | `race_lap`, `race_attempts_used` |
| Resource `detail` | `board_id`, `stage`, `lap` | `race_board_id`, `race_stage`, `race_lap` |

Extras level và resource **chưa lên dữ liệu thật** → đổi không phát sinh era; funnel `lap` /
`attempts_used` đã lên bee.flow với tên cũ → data đọc hai tên theo mốc bản module.


## Spec đầy đủ

Race Event **không cần API mới** — ghép từ 3 cửa có sẵn: `Funnel` (tiến độ + đã-xem),
`Level` (từng ván trong chặng), `Resource` (thưởng/tiêu). Toàn bộ 8 metric GD yêu cầu và 3
main-concern đọc được từ đúng các dòng dưới đây — cuối doc có mục "data team đọc gì" đối chiếu
từng metric.

### Bộ id — khai một lần, dán lên mọi log

| Id | Sinh lúc nào | Vai |
|---|---|---|
| `race_id` | Config event — một countdown = một id | Gom mọi log về event window |
| `race_board_id` | Mỗi lần user ấn **Start** (hệ tạo bảng đấu mới) | `cycleId` của funnel tiến độ — một vòng chơi trọn vẹn. Thắng cả 3 chặng chơi lại = **board mới** |
| `race_lap` | 1, 2, 3… mỗi vòng chơi lại trong cùng event | Đi extraMeta cùng board |
| `race_stage` | 1 / 2 / 3 | Trục chính của các metric |
| `race_attempt` | Bắt đầu là 1; streak của chặng bị reset (thua level / quit / bot về nhất) → +1 | Nguồn metric Retry |
| `race_streak_pos` | Ván thứ mấy trong chuỗi win liên tiếp (1..3/5/7) | Nguồn tuning bot |

### Bảng tra NHANH: khoảnh khắc nào → gọi gì

| Khoảnh khắc | Dòng cần gọi |
|---|---|
| Khởi động app (một lần) | `Register("event_race", OncePerCycle)` — xem mục 0 |
| Entry/popup event THẬT SỰ hiện ra | `FalconBigDataController.Ui.OnImpression("race_event_entry")` — SDK tự gộp cụm |
| User bấm vào entry/popup | `FalconBigDataController.Ui.OnClicked("race_event_entry")` |
| User MỞ UI event (mỗi lần — van nén 1 dòng/event) | `Funnel.OnStep("event_race", action = "open", cycleId = raceId)` |
| Trong thời gian event, user ĐANG đủ điều kiện vào game/mở UI event | `Funnel.OnStep("event_race", action = "eligible", cycleId = raceId)` — mốc TRẠNG THÁI theo event, xem mục 1 |
| User ấn Start đăng ký (hệ tạo bảng đấu) | `Funnel.OnStep("event_race", FFunnelAction.Join, cycleId = boardId)` |
| Bắt đầu MỖI ván trong chặng | `Level.OnStart(...)` + extras race (mục 3) |
| Thắng / thua / quit ván | `Level.OnPass` / `OnFail(failReason:)` — như level thường |
| User về nhất một chặng | `Funnel.OnStep(FFunnelAction.Milestone, priority = stage)` |
| Nhận thưởng chặng | mỗi loại thưởng một `Resource.OnEarned` |
| Tiêu tiền TẠI event (phí retry, mua lượt…) | `Resource.OnSpent` với `resourceWhen = "event_race"` |
| Thắng cả 3 chặng | `Funnel.OnStep(FFunnelAction.Complete, priority = 40)` — vòng mới thì Start lại = board mới |

### 0. Đăng ký funnel — MỘT lần lúc khởi động

```csharp
// Tiến độ bảng đấu: mỗi bước một lần TRONG MỘT board, board mới tự sạch
FalconBigDataController.Funnel.Register("event_race", FunnelShape.OncePerCycle);
```

(Sửa 05/09: bỏ funnel `race_event_ui` Repeatable — shape đó nghỉ hưu. Mẫu số phễu "đã xem"
chuyển sang bước `open` trong funnel chính — mục 1; số ĐẾM impression/CTR per-lần-hiện của
metric 1 GD đi event exposure riêng `f_sdk_ui_impression` — CHƯA build, phải chốt với BigData
TRƯỚC khi ship event vì metric 1 đòi đích danh số đếm theo ngày.)

### 1. Eligible + đã-xem (hai mốc grain EVENT) — và số phận metric CTR

⚠ **Funnel này chạy HAI grain `cycleId` — đọc kỹ kẻo lú**: `eligible`/`open` là chuyện của cả
ĐỢT event → `cycleId = raceId` (mỗi đợt một dòng); `join`/`milestone`/`complete` là chuyện của
từng BẢNG ĐẤU → `cycleId = boardId` (mỗi vòng chơi một bộ). Van OncePerCycle lọc theo đúng
chuỗi cycleId truyền vào nên hai grain sống chung một funnel không đụng nhau — nhưng PHẢI
truyền đúng id cho đúng mốc, tráo là van nén nhầm.

```csharp
// Đủ điều kiện — mốc TRẠNG THÁI theo đợt: mỗi lần vào game/mở UI trong event mà ĐANG đủ
// điều kiện (kể cả xong chapter từ lâu) thì gọi — van nén một dòng/đợt. KHÔNG móc vào
// khoảnh khắc xong-chapter (bài học Master League):
FalconBigDataController.Funnel.OnStep(new FunnelParam
{
    funnelName = "event_race", cycleId = raceId, action = "eligible", priority = 3
});

// Mỗi lần user MỞ UI event (thấy panel/popup thật sự) — cứ gọi, van nén một dòng/đợt;
// grain phễu "% tham gia sau khi đã xem" chỉ cần vậy:
FalconBigDataController.Funnel.OnStep(new FunnelParam
{
    funnelName = "event_race", cycleId = raceId, action = "open", priority = 5
});
```

Còn **tổng impression + CTR theo NGÀY** (metric 1) đi máy đếm riêng — `Ui` API (BigData
≥ 1.3.9), van GỘP CỤM bắt buộc nên spam thoải mái, không có đường per-lần-hiện lên wire:

```csharp
// Mỗi lần entry/popup event THẬT SỰ hiện ra với user (không phải mỗi frame, không phải lúc
// nằm ngoài màn hình) — cứ gọi, SDK gộp theo (surface × action), flush lúc app pause thành
// MỘT dòng mang count:
FalconBigDataController.Ui.OnImpression("race_event_entry",
    new Dictionary<string, object> { ["race_id"] = raceId });   // extras = ngữ cảnh BẤT BIẾN
                                                                // của surface — cụm giữ bản lần đầu

// User bấm vào — CÙNG surfaceId thì server mới ghép được CTR:
FalconBigDataController.Ui.OnClicked("race_event_entry",
    new Dictionary<string, object> { ["race_id"] = raceId });
```

- `surfaceId` đặt tên ỔN ĐỊNH per bề mặt ("race_event_entry", "race_event_popup") — đổi tên là
  gãy chuỗi so sánh.
- Phân vai với bước `open` ở trên: `open` = MỐC phễu ("đã xem hay chưa", một dòng/đợt);
  `Ui.OnImpression` = MÁY ĐẾM ("hiện bao nhiêu lần") — hai câu hỏi, đừng dùng lẫn.
- Cụm không persist qua kill — mất stretch dở chấp nhận (telemetry exposure, không phải tiền).

### 2. Đăng ký tham gia (metric 2)

```csharp
// Trong handler nút Start, SAU khi hệ tạo xong bảng đấu (đã có boardId).
// Bộ meta chu kỳ (field CHUẨN của FunnelParam từ BigData 1.3.8 — thay miếng vá
// countdown_remain_hours cũ): chu kỳ của funnel này là BẢNG ĐẤU, nên
//   cycleIndex   = lap (bảng thứ mấy của user trong event)
//   cycleStartTs = lúc tạo bảng (chính là lúc ấn Start), epoch ms UTC
//   cycleEndTs   = lúc event ĐÓNG (bảng không sống quá event) — đúng số UI countdown đang vẽ.
// Server tự tính countdown-lúc-join = cycleEndTs − ts → cắt nhóm "join khi còn ≥4/1–4/<1 ngày"
// không cần bảng lịch event từ ops.
FalconBigDataController.Funnel.OnStep(new FunnelParam
{
    funnelName = "event_race", cycleId = boardId,
    action = FFunnelAction.Join, priority = 10,
    cycleIndex = lap,
    cycleStartTs = boardCreatedTsMs,
    cycleEndTs = raceEndTsMs,
    extraMeta = new Dictionary<string, object> { ["race_id"] = raceId }
});
```

- `cycleId = race_board_id` chứ KHÔNG phải `race_id`: user thắng cả 3 chặng rồi chơi lại là bảng đấu
  mới — chu kỳ mới thì Join/Milestone mới hợp lệ; lấy race_id làm cycle là vòng thứ hai bị van
  lọc trùng nuốt sạch.
- ⚠ Action tham gia PHẢI là `FFunnelAction.Join` (đúng chuỗi `"join"`) — SDK ghi sổ ngày tham gia
  từ nó (cột `funnelDay` trên mọi bước sau).
- %DAU là phép chia phía server — không log gì thêm.

### 3. Từng ván trong chặng — stream level cân 3 metric (5, concern 1, concern 3)

Ván trong event = level thường + bộ extras race. **Đây là chỗ ăn tiền nhất của thiết kế — đừng
bớt field nào:**

```csharp
FalconBigDataController.Level.OnStart(new LevelStartParamV2
{
    currentLevel = levelNumber,
    difficulty = difficulty,
    extraMeta = new Dictionary<string, object>
    {
        ["race_id"]       = raceId,
        ["race_board_id"] = boardId,
        ["race_stage"]    = 2,            // chặng 1/2/3
        ["race_attempt"]  = 4,            // lần thử thứ mấy của chặng này (bắt đầu = 1)
        ["race_streak_pos"] = 3,            // ván thứ 3 trong chuỗi 5 ván phải win liên tiếp
        ["race_retry_reason"] = "bot_won"     // CHỈ điền ở ván ĐẦU của attempt ≥ 2:
                                          // "level_fail" / "quit" / "bot_won" — vì sao phải làm lại.
                                          // Attempt 1 hoặc ván giữa chuỗi: BỎ TRỐNG key này.
    }
});

// Thắng/thua/quit ván: OnPass / OnFail như level thường — extras không cần lặp lại
// (playTurnId SDK tự đóng lên cả bộ start/pass/fail của ván)
FalconBigDataController.Level.OnFail(failReason: LevelFailReason.Quit);
```

Server đọc ra từ đúng stream này:
- **Retry chặng N** (metric 5) = `max(race_attempt) − 1` per board — tổng + phân vị.
- **Bot dễ/khó** (concern 1) = win-rate và duration per (`race_stage`, `race_streak_pos`) — gãy ở khúc nào
  của chuỗi; tỷ lệ `race_retry_reason = "bot_won"` = bot cướp chặng bao nhiêu phần.
- **Drop vì bí** (concern 3) = `max(race_attempt)` cuối cùng trước khi user biến mất khỏi event —
  phân vị ra ngưỡng "retry thứ mấy thì người ta chán".

### 4. Về nhất một chặng / thắng cả vòng (metric 3 + 4)

```csharp
// User hoàn thành đủ số level TRƯỚC mọi bot → pass chặng:
FalconBigDataController.Funnel.OnStep(new FunnelParam
{
    funnelName = "event_race", cycleId = boardId,
    action = FFunnelAction.Milestone, priority = stage,     // 1 / 2 / 3
    extraMeta = new Dictionary<string, object>
    {
        ["race_id"] = raceId, ["race_lap"] = lap,
        ["race_attempts_used"]    = attempt,       // qua chặng sau bao nhiêu lần thử
        ["race_user_time_sec"]    = userTimeSec,   // thời gian chuỗi thắng của user
        ["race_best_bot_time_sec"] = bestBotSec    // bot nhanh nhất — cùng nhau trả lời "thắng sát
                                              // nút hay thắng dễ", số trực tiếp để chỉnh % win bot
    }
});

// Thắng cả 3 chặng (trước khi reset toàn bộ cho vòng mới):
FalconBigDataController.Funnel.OnStep(new FunnelParam
{
    funnelName = "event_race", cycleId = boardId,
    action = FFunnelAction.Complete, priority = 40,
    extraMeta = new Dictionary<string, object> { ["race_id"] = raceId, ["race_lap"] = lap }
});
```

- Metric 3 (theo USER — pass 10 lần vẫn 1 user): server đếm distinct user có Milestone stage N.
- Metric 4 (theo LẦN): đếm tất cả dòng — mỗi board tối đa một dòng per stage, 10 vòng = 10 board.
- **Thua chặng KHÔNG có bước funnel** — nó đã nằm trong `race_retry_reason` của stream level (mục 3).
  Log thêm là double coverage.

### 5. Thưởng và tiêu (metric 6 + 7)

```csharp
// Thưởng khi pass chặng — MỖI loại currency một OnEarned, cùng lần nhận chung exchangeId:
FalconBigDataController.Resource.OnEarned(new ResourceParam
{
    currency = reward.currency, itemType = reward.type, itemId = reward.id,
    amount = reward.amount,                  // số THỰC nhận
    resourceWhen  = "event_race",            // khuôn event_ của FResourceWhen
    resourceWhere = "stage_reward",
    exchangeId = claimId,
    detail = new Dictionary<string, object>
        { ["race_id"] = raceId, ["race_board_id"] = boardId, ["race_stage"] = stage, ["race_lap"] = lap }
});

// Tiêu TRỰC TIẾP tại event (phí retry, mua lượt...) — OnSpent, cùng resourceWhen/detail như trên
```

**Sink có HAI nguồn, đừng gộp** (metric 7):
1. Tiêu trực tiếp tại event → `OnSpent` với `resourceWhen = "event_race"` như trên.
2. Booster tiêu **trong ván** thuộc event → log như thường, KHÔNG stamp gì thêm — `playTurnId`
   SDK tự đóng, server JOIN sang ván level có `race_stage`. Đó mới là phép cộng đúng của "sink trong
   quá trình tham gia".

⚠ **KHÔNG stamp `event_race` lên chi tiêu shop chung** trong thời gian event — mua gói lúc event
đang mở không có nghĩa là VÌ event (ĐỒNG THỜI ≠ NGUYÊN NHÂN). Ngoại lệ đúng duy nhất: gói bày
bán trong UI event thì `where` của `Iap.OnStarted` đặt placement event.

### 6. Những thứ ĐỪNG log

| Đừng làm | Vì sao |
|---|---|
| Bước funnel "thua chặng" / "reset streak" | Đã nằm trong `race_attempt` + `race_retry_reason` của stream level — double coverage |
| Bước funnel cho TỪNG ván | Ván có event chủ thể (level) — phễu ván JOIN `race_board_id` là ra |
| Impression mỗi frame / mỗi lần icon vẽ lại | Impression = lần entry point thật sự hiện ra với user |
| `cycleId = race_id` | Vòng chơi lại bị van lọc trùng nuốt — chu kỳ là BẢNG ĐẤU |
| Tự cộng CTR/retry/percentile ở client | Mọi phép tính là việc của server — client chỉ chở sự kiện |
| Điền `race_retry_reason` ở attempt 1 | Chưa retry thì không có lý do — vắng mặt, đừng bịa |

### 7. Data team đọc gì — đối chiếu đủ 8 metric

```
1. Impression/CTR ngày      = f_sdk_ui_impression: sum(count|click) / sum(count|impression)
                              per surfaceId per ngày; grain đã-xem/chưa = distinct(open)
2. Tham gia + %DAU          = distinct user có Join / DAU
3. User pass mốc 1/2/3      = distinct user có Milestone priority N (10 lần vẫn 1 user)
4. LẦN pass mốc 1/2/3       = đếm mọi dòng Milestone priority N (mỗi board 1 dòng/stage)
5. Retry per mốc            = max(race_attempt) − 1 per (race_board_id, race_stage) từ stream level — tổng + phân vị
6. Source/user per mốc      = SUM(amount) OnEarned WHERE event_when='event_race' GROUP BY
                              detail.race_stage, currency — phân vị per currency
7. Sink/user per mốc        = OnSpent event_race (trực tiếp) + resource sink JOIN playTurnId
                              → ván level có race_stage (trong ván)
8. Trước/sau + join vs không = ZERO việc client: mốc chia đôi là timestamp dòng Join; paying
                              rate / session count / session time / playtime lấy từ stream
                              session + iap sẵn có — bài cohort thuần server
Cắt nhóm theo countdown     = cycleEndTs − ts trên dòng Join (field chuẩn 1.3.8) — không cần
                              bảng lịch event; các log khác của board suy nhóm qua race_board_id
                              → dòng Join của board đó; sort vòng chơi bằng cycleIndex
```

Việc phía các bạn ngoài code: báo data team đăng ký funnel `event_race` (eligible / open /
join / milestone / complete, kèm shape OncePerCycle) và vocab `event_race` của `event_when`.
