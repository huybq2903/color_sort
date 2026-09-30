# Tracking Master League — TODO cho dev + tham chiếu log

> **Dev chỉ cần đọc phần TODO ngay dưới đây** — mỗi việc: sửa file nào, chép đoạn code nào, kiểm
> ra sao. Tick `[x]` khi xong. Giải thích "vì sao" và tài liệu cho team data nằm ở nửa dưới.
> Cập nhật 17/09/2026 theo module thực tế + đối soát dữ liệu knit.jam / bee.flow.

# ✅ TODO

## A. Module Master League — owner module làm

File chính: `Scripts/Runtime/Analytic/MasterLeagueTracking.cs`

### [ ] M5 — Đổi tên 6 hằng key (dễ nhất, làm trước)

Trong `MasterLeagueTracking.cs`, sửa **giá trị chuỗi** của 6 hằng (tên hằng giữ nguyên):

```csharp
public const string KEY_ROUND      = "league_round";       // cũ: "round"
public const string KEY_MULTIPLIER = "league_multiplier";  // cũ: "multiplier"
public const string KEY_DIFFICULTY = "league_difficulty";  // cũ: "difficulty"
public const string KEY_RANK       = "league_rank";        // cũ: "rank"
public const string KEY_STAGE      = "league_stage";       // cũ: "stage"
public const string KEY_STEP       = "league_step";        // cũ: "step"
```

Không đổi: `KEY_LEAGUE_ID`, `KEY_LEAGUE_START_DAY`.
**Kiểm:** log level Start có `league_round`; dòng thưởng có `detail.league_stage`.

### [ ] M3 — Bỏ tên mùa dự phòng

Thay **toàn bộ** property `LeagueId` bằng:

```csharp
public static string LeagueId
{
    get
    {
        var data = DeclareMasterLeague.GetData();
        return data is { season: > 0 } ? LEAGUE_ID_PREFIX + data.season : null;
    }
}
```

Xoá hằng `SEASON_FALLBACK_FORMAT` (không còn dùng).
**Kiểm:** tắt mạng, mở app → không có dòng funnel nào mang `cycleId` dạng `master_league_2026…`.

### [ ] M2 — Gắn thông tin mùa lên eligible / open / join

**Bước 1.** Thêm hàm này vào `MasterLeagueTracking`:

```csharp
private static FunnelParam CycleStep(string leagueId, string action, int priority)
{
    var param = new FunnelParam
    {
        funnelName = FUNNEL_NAME,
        cycleId = leagueId,
        action = action,
        priority = priority
    };

    var season = DeclareMasterLeague.GetData()?.season ?? 0;
    if (season > 0) param.cycleIndex = season;

    var endTs = SeasonEndTs();
    if (endTs > 0) param.cycleEndTs = endTs;

    return param;
}
```

**Bước 2.** Trong 3 hàm, thay đoạn `FalconBigDataController.Funnel.OnStep(...)` bằng một dòng:

```csharp
// OnEligible():
FalconBigDataController.Funnel.OnStep(CycleStep(leagueId, ACTION_ELIGIBLE, PRIORITY_ELIGIBLE));

// OnOpen():
FalconBigDataController.Funnel.OnStep(CycleStep(leagueId, ACTION_OPEN, PRIORITY_OPEN));

// OnJoin() — xoá luôn đoạn tự set cycleIndex / cycleEndTs cũ:
FalconBigDataController.Funnel.OnStep(CycleStep(leagueId, FFunnelAction.Join, PRIORITY_JOIN));
```

**Kiểm:** dòng `eligible`, `open`, `join` đều có `cycleIndex` và `cycleEndTs`.

### [ ] M1 — Ghi "đã mở" ở popup trước khi join

Thêm **một dòng** vào `OnEnable()` của 2 file:

```csharp
// Scripts/Runtime/UI/Join/MasterLeagueJoinPopup.cs
protected override void OnEnable()
{
    base.OnEnable();
    DeclareMasterLeague.IsSessionPopupUnlockShowed = true;
    MasterLeagueTracking.OnOpen();          // ← THÊM
    Behaviour?.OnEnable();
}

// Scripts/Runtime/UI/Unlock/MasterLeagueUnlockPopup.cs
protected override void OnEnable()
{
    base.OnEnable();
    DeclareMasterLeague.IsSessionPopupUnlockShowed = true;
    MasterLeagueTracking.OnOpen();          // ← THÊM
}
```

Giữ nguyên lời gọi `OnOpen()` đang có trong `MasterLeagueLeaderboard`.
**Kiểm:** user **chưa join** mở popup → có dòng `open`.

### [ ] M4 — Gắn giờ bắt đầu mùa (làm sau khi server xong S1)

Khi config Master League đã có `timeStart` (epoch ms UTC, cùng kiểu `timeEnd`), thêm vào
`CycleStep` ở M2, ngay dưới dòng `cycleEndTs`:

```csharp
var start = DeclareMasterLeague.GetConfig()?.timeStart ?? default;
if (start != default)
    param.cycleStartTs = (long)(DateTime.SpecifyKind(start, DateTimeKind.Utc) - UnixEpoch).TotalMilliseconds;
```

**Kiểm:** dòng `eligible`, `open`, `join` có `cycleStartTs`.

### [ ] Phát hành module

- `package.json`: thêm dependency `"falcon.modules.core.bigdata": "1.3.8"` (hoặc bản game đang
  dùng nếu cao hơn); tăng version module.
- CHANGELOG module ghi: "đổi tên key extras sang `league_*`" — **báo data team số bản** để họ đọc
  dữ liệu cũ/mới đúng mốc.

## B. Game — dev từng game làm

### [ ] G1 — Gọi `OnEligible()` mỗi lần về Home

Chỗ game xử lý **vào Home / mở app** (chỗ nào chạy mỗi phiên là được):

```csharp
// Gọi lặp thoải mái — SDK tự nén còn 1 dòng/mùa.
var config = DeclareMasterLeague.GetConfig();
if (config != null && !DeclareMasterLeague.HasEnded() && DaThangHetLevelChapterHienCo())
    MasterLeagueTracking.OnEligible();
```

`DaThangHetLevelChapterHienCo()` = hàm game tự có: user đã thắng **hết level chapter đang có**.

⚠ **KHÔNG** gọi ở khoảnh khắc "vừa xong chapter" — user xong chapter từ mùa trước sẽ không bao giờ
có dòng này.
**Kiểm:** user xong chapter từ lâu, mở app giữa mùa → có dòng `eligible`.

### [ ] G2 — Round league chỉ log một lần (bee.flow đang lỗi)

Module **đã tự log** level cho round. Tìm chỗ game gọi `FLevelManager.OnLevelStart(...)` và
`FLevelManager.OnLevelResult(...)`, bọc lại:

```csharp
if (!DeclareMasterLeague.IsPlayingRound)     // ← round league thì KHÔNG gọi
    levelManager.OnLevelStart(...);

if (!DeclareMasterLeague.IsPlayingRound)
    levelManager.OnLevelResult(...);
```

⚠ `FLevelManager.OnLevelResult(win: true)` còn **tăng level chapter** (`LevelData.level++`). Round
league đi qua đó là thắng round = lên level chapter — kiểm luôn. Nếu game đang dựa vào
`FLevelManager` cho việc khác ngoài log (vd gửi kết quả lên server) thì báo team BigData trước khi
sửa.
**Kiểm:** chơi 1 round → đúng **1** dòng Start + **1** dòng Pass/Fail, dòng Start có `league_id`.

### [ ] G3 — Báo tên key revive

Nhắn data team: trong `boostersUsed`, revive/hồi sinh dùng key tên gì. (knit.jam đã báo:
`revive`. bee.flow: còn thiếu.)

### [ ] Nâng phiên bản (theo từng game)

| Game | Cần nâng |
|---|---|
| knit.jam | Master League (bản có M1–M5), BigData ≥ 1.3.8, Events Core legacy bản có `Grant(..., context)`, GameData ≥ 1.1.7 — **nâng đủ cả 4**, chỉ nâng Master League thì dòng thưởng vẫn thiếu thông tin |
| bee.flow | Master League (bản có M1–M5) + làm G1, G2, G3 |

## C. Server / backend event

### [ ] S1 — Thêm `timeStart` vào config Master League

Epoch ms UTC, cùng định dạng `timeEnd`. Cần cho M4.

### [ ] S2 — Chốt nghĩa `progressionStage`

Trả lời một câu: số này là **số chặng đã hoàn thành** (0 = chưa qua chặng nào) hay **chặng đang
ở** (0 = đang ở Gold)? Và 1/2/3 ứng với chặng nào.

---

# 📖 Giải thích & tham chiếu (không cần đọc để sửa)

## 1. Vì sao từng việc

| Việc | Thiếu thì sao |
|---|---|
| M1 | `open` hiện chỉ gọi ở leaderboard — chỉ vào được sau khi join → tỉ lệ join/open luôn ≈ 100%, vô nghĩa (chỉ số 2) |
| M2 | Không chia được nhóm countdown cho người xem mà không tham gia (chỉ số 2) |
| M3 | Chưa biết mùa, module tự đặt tên `master_league_<yyyyMMdd>` → không khớp `master_league_<season>` của dòng join, không nối được |
| M4 | Không có giờ mở mùa trên log → không tính được mốc bắt đầu mùa, ngày launch (chỉ số 1). Phía data **không hardcode** độ dài mùa — dòng thiếu `cycleStartTs` là thiếu dữ liệu. Config hiện chỉ có `timeEnd` + `duration` (mẫu `2.0`, không rõ đơn vị, không code nào dùng) → cần S1 |
| M5 | Key chung chung (`stage`, `round`, `rank`…) đụng event khác gắn lên cùng loại log: extras trải phẳng theo luật ai-ghi-trước-giữ → event sau mất số im lặng; cùng tên khác kiểu (`stage` chữ ở đây, số ở Race) làm hỏng cột kho |
| G1 | Mất tệp gốc "user đã hoàn thành chapter" của chỉ số 2. Module không tự biết điều kiện chapter |
| G2 | Mỗi round hai bộ dòng level; Pass/Fail rơi vào lượt chơi của dòng Start không có `league_id` → mất liên kết round (chỉ số 3, 6). Data đo: 86% round trên bee.flow |
| G3 | Không tính được revive rate (chỉ số 6) |
| S2 | Client đánh chỉ số chặng từ 0 theo chặng đang cày (GOLD = 0). Nếu server cùng quy ước thì "0 = chưa đạt" là đọc sai, Max Stage lệch một bậc |

G2 còn cách thứ hai (chỉ chọn khi game cần field riêng của `FLevelManager`, vd key `buyMoreTime`
nó tự thêm vào `boostersUsed`): giữ `FLevelManager`, **tắt** log round trong module, và tự truyền
`league_id` / `league_round` / `league_start_day` vào `extraMeta` của `OnLevelStart`.

**Tình trạng dữ liệu thật (data đối soát 17/09):**

| Game | Đang lệch |
|---|---|
| knit.jam | Module cũ: funnel tên `master_league`, không có `cycleEndTs` (android + ios); thưởng progression đi đường GameData cũ → mất `resourceWhere` + `detail`; chưa thấy `open` trước join |
| bee.flow | Round log hai lần qua FLevelManager (86% round); chưa thấy `open` trước join và `eligible` |

**Lưu ý chất lượng dữ liệu đang biết:**

- Dòng thưởng rank/progression đi qua GameData: `itemType` và `itemId` đang bị điền bằng
  `resourcePlace` (mặc định `event_master_league`) — **lỗi phía GameData, đừng dùng hai cột này**
  cho thưởng; dùng `currency` (= tên phần thưởng) + `resourceWhere` + `detail`.
- Phần thưởng dạng **thời gian x2 Collectible** không đi qua kho tài nguyên → **không có dòng
  resource**.

Tên funnel **`event_master_league`**. Tên cũ `master_league` là dữ liệu lỗi đời trước — **không
JOIN lẫn** với tên mới.

## 2. Khoá chung

| Khoá | Giá trị | Ở đâu |
|---|---|---|
| `league_id` | `master_league_<season>` (vd `master_league_12`) | `cycleId` của funnel; key `league_id` trên level Start và `detail` của resource |
| `league_start_day` | `yyyy-MM-dd` — ngày user **join** mùa này | level Start, `detail` resource. Funnel dùng cột `funnelDay` (SDK tự đóng, cùng nghĩa) |
| `playTurnId` | SDK tự sinh | Nối Start ↔ Pass/Fail của cùng một round |

### 2b. Tiền tố key — luật và bảng đổi tên (việc M5)

Mọi key extras/detail **riêng của event** mang tiền tố `league_` — tránh đụng với event khác gắn
lên cùng loại log (luật chung: `LiveOpsTrackingPattern.md` §1). Key đã có tiền tố giữ nguyên
(`league_id`, `league_start_day`, `eventMasterLeague*`).

| Log | Key cũ | Key mới |
|---|---|---|
| Level Start (phẳng) | `round` | `league_round` |
| Funnel complete (phẳng) | `rank` | `league_rank` |
| Resource `detail` | `multiplier` | `league_multiplier` |
| Resource `detail` | `difficulty` | `league_difficulty` |
| Resource `detail` | `rank` | `league_rank` |
| Resource `detail` | `stage` | `league_stage` |
| Resource `detail` | `step` | `league_step` |

Dữ liệu đã lên với key cũ (bee.flow) là **era cũ** — data đọc hai tên theo mốc bản module có M5.
`difficulty` trên dòng level là field CHUẨN của level, **không** đổi.

## 3. Danh mục log

### 3.1 Funnel — `f_sdk_funnel_data`

`funnelName = event_master_league`, `cycleId = league_id`, `funnelShape = once_per_cycle`
→ **mỗi bước tối đa 1 dòng / user / mùa**.

| `action` | `priority` | Bắn khi | Trường thêm |
|---|---|---|---|
| `eligible` | 10 | Game gọi — user đang đủ điều kiện trong mùa *(việc G1)* | `cycleIndex`, `cycleEndTs` *(M2)*, `cycleStartTs` *(M4)* |
| `open` | 15 | User mở UI league *(việc M1: phải có ở popup trước join)* | `cycleIndex`, `cycleEndTs` *(M2)*, `cycleStartTs` *(M4)* |
| `join` | 20 | Server xác nhận join | `cycleIndex` = số mùa, `cycleEndTs` = giờ kết thúc mùa (epoch ms UTC), `cycleStartTs` = giờ bắt đầu mùa *(M4)* |
| `complete` | 30 | User **mở leaderboard thấy kết quả** mùa đã đóng | `league_rank` (vắng nếu chưa biết rank) |
| `claim` | 40 | User nhận thưởng xếp hạng | — |

⚠ `complete` = "đã xem kết quả", **không phải** "mùa kết thúc" — user không mở leaderboard thì
mùa đó không có dòng complete.

### 3.2 Level (mỗi Round) — `f_sdk_level_data`

| `status` | Trường liên quan |
|---|---|
| `Start` | `currentLevel` = **vị trí trong bộ level của mùa** (không phải số level chapter); `difficulty` = `normal` / `hard` / `very_hard`; key phẳng `league_id`, `league_round`, `league_start_day` |
| `Pass` | `score`, `duration`, `movesUsed`, `boostersUsed` |
| `Fail` | như Pass + `levelProgress` (= score/totalScore×100); quit giữa round: `failReason = Quit` |

- `league_round` = số thứ tự round hiện tại, **chỉ tăng khi thắng** (thua thì chơi lại round đó).
- Pass/Fail **không lặp lại** `league_id` — lấy qua `playTurnId` từ dòng Start.
- Dòng level **có `league_id`** là round league; **không có** là level chapter.

### 3.3 Resource — `f_sdk_resource_data`

Chung: `flowType = Source`, `resourceWhen = event_master_league`.

| `resourceWhere` | Bắn khi | `currency` / `amount` | `detail` | `exchangeId` |
|---|---|---|---|---|
| `level_end` | Thắng round | `golden_yarn` / số len **đã nhân** độ khó và x2 | `league_id`, `league_start_day`, `league_multiplier` (hệ số x2 Collectible; 1 = không có), `league_difficulty` | — |
| `rank_reward` | Nhận thưởng xếp hạng | tên phần thưởng / số lượng | `league_id`, `league_start_day`, `league_rank` | chung cho cả lần nhận |
| `progression_reward` | Nhận thưởng một step progression | tên phần thưởng / số lượng | `league_id`, `league_start_day`, `league_stage` (`gold`/`emerald`/`diamond`), `league_step` (1..N, đếm xuyên chặng) | chung cho các món của một step |

Thua / quit round: **không có dòng resource** (vắng mặt chính là dữ liệu).

### 3.4 Trạng thái gắn trên MỌI log (custom params)

Module gắn thêm 4 trường vào lớp custom params của **mọi event** (đọc tươi lúc log):

| Key | Nghĩa |
|---|---|
| `eventMasterLeagueJoining` | `true` nếu user đã join và mùa còn chạy |
| `eventMasterLeagueRound` | round hiện tại |
| `eventMasterLeagueStage` | chặng progression — ⚠ **nghĩa chưa chốt**, xem ghi chú dưới |
| `eventMasterLeagueYarn` | tổng len vàng mùa này |

⚠ `eventMasterLeagueStage` do **server** gửi xuống (`progressionStage`), client chỉ chép lại.
Dữ liệu thật: bắt đầu từ 0. Nhưng config phía client đánh chỉ số chặng **từ 0 theo chặng đang
cày** (GOLD = 0, EMERALD = 1, DIAMOND = 2) — nếu server cùng quy ước thì `0` là "đang ở Gold",
không phải "chưa đạt chặng nào", và Max Stage lệch một bậc. **Người viết API server cần chốt**:
số này là *số chặng đã hoàn thành* hay *chặng đang ở*. Chưa chốt thì dùng `detail.league_stage` của
dòng `progression_reward` (chỉ số 4).

Đây là **ảnh chụp trạng thái lúc log**, không phải nguyên nhân: một giao dịch IAP có
`eventMasterLeagueJoining = true` chỉ nói "mua **trong lúc** đang tham gia", không nói "mua **vì**
league".

## 4. Công thức cho 7 chỉ số GD

**Nhóm countdown** (dùng ở chỉ số 2, 3, 4, 5, 6, 7):

```
countdown_h = (cycleEndTs − ts) / 3 600 000
  ≥ 96h        → nhóm "≥ 4 ngày"
  24h – 96h    → nhóm "1–4 ngày"
  < 24h        → nhóm "< 1 ngày"   (thực tế 4h–24h: luật không cho bắt đầu khi countdown < 4h)
```

- Người **tham gia**: `ts` = dòng `join`.
- Người **xem mà không tham gia**: `ts` = dòng `open`. Van chỉ giữ dòng `open` ĐẦU TIÊN của mùa →
  nhóm của họ là nhóm tại **lần xem đầu** (mở popup ngày 1 rồi ngày 6 vẫn thuộc "≥ 4 ngày").
- Giờ bắt đầu mùa = `cycleStartTs` (việc M4). Không có thì để trống — không suy ra.

GD viết lẫn "≥ 4 ngày" và "> 4 ngày" giữa các chỉ số — cần chốt một mốc, đề xuất `≥ 96h`.

**"Khi tham gia vs khi không tham gia"** (chỉ số 5, 6, 7) — hai cách so, đều không cần log thêm:
- **Cùng user, trước/sau**: tuần event (từ `ts_join`) so với tuần liền trước của chính user đó.
- **Cùng tệp, join vs không join**: user có `eligible` + có `join` so với user có `eligible`
  nhưng không `join` trong cùng mùa (cần việc G1).

| # | Chỉ số | Cách tính |
|---|---|---|
| 1 | Churn tệp end-game sau launch | Tệp = user có dòng level **không có `league_id`** đạt `currentLevel` = level chapter cao nhất tại ngày launch. Churn = không còn `f_sdk_session_data` sau N ngày kể từ ngày launch. So với tệp tương đương trước launch. Ngày launch ≈ giờ bắt đầu của mùa sớm nhất có trong log (`min(cycleStartTs)` — cần M4) — lệch nếu event bật giữa mùa hoặc bản app lên store muộn, nên đối chiếu lịch release |
| 2 | % tham gia sau khi xem popup (trong tệp đã hoàn thành chapter) | Theo `cycleId`: tệp = distinct user có `eligible`; đã xem = có `eligible` **và** `open`; tham gia = có thêm `join`. Tỉ lệ = tham gia / đã xem. Nhóm countdown: người join theo dòng `join`, người chỉ xem theo dòng `open` (mục trên). Mẫu DAU: distinct user có session trong tuần event. **Phụ thuộc việc M1, M2, M3, G1** (M4 cho giờ bắt đầu mùa) |
| 3 | Max Round (phân vị 0.1 / 0.25 / 0.5, nhóm ≥ 4 ngày) | Mỗi user × `league_id`: `max(league_round)` trên dòng level `Start` có `league_id`. Lọc user thuộc nhóm countdown ≥ 4 ngày, lấy phân vị |
| 4 | Max Stage (phân vị, nhóm ≥ 4 ngày) | Mỗi user × mùa: stage cao nhất **đã nhận thưởng** = max `detail.league_stage` trên `progression_reward` (thứ tự gold < emerald < diamond). Stage **đã đạt** (kể cả chưa nhận) = max `eventMasterLeagueStage` — ⚠ chỉ dùng sau khi server chốt nghĩa số này (mục 3.4); chưa chốt thì dùng `detail.league_stage` |
| 5 | Source / Sink | `f_sdk_resource_data` group theo `flowType`, `currency`, nhóm countdown, trước/sau hoặc join/không join. Muốn tách phần do league phát: lọc `resourceWhen = 'event_master_league'` |
| 6 | Session time / count, play time, revive rate | Session: `f_sdk_session_data`. Play time: tổng `duration` dòng level Pass/Fail. Revive rate = dòng Pass/Fail có `boostersUsed[<key revive>] > 0` / tổng dòng Pass/Fail — key theo game ở bảng dưới. Tách round league / level chapter bằng `league_id` (qua `playTurnId`) |
| 7 | Paying rate, ARPDAU, ARPPDAU | `f_sdk_in_app_data` theo nhóm countdown, trước/sau hoặc join/không join |

**Key revive trong `boostersUsed` theo game** (chỉ số 6):

| Game | Key | Nguồn |
|---|---|---|
| knit.jam | `revive` | data đối soát 17/09 |
| bee.flow | chưa biết — đọc lại sau khi sửa G2 | nếu round đi đường FLevelManager, ứng viên là `buyMoreTime` (FLevelManager tự thêm key này) — cần game xác nhận |

Game mới: thêm dòng vào bảng trước khi dựng biểu đồ.

Số liệu phụ cho GD:

```
len vàng / user / mùa    = SUM(amount) WHERE resourceWhere='level_end' GROUP BY user, detail.league_id
hiệu ứng x2 Collectible  = so sánh dòng level_end có detail.league_multiplier > 1 vs = 1
win-rate theo độ khó     = Pass / (Pass + Fail) trên round league GROUP BY difficulty
giá trị thưởng rank      = SUM(amount) WHERE resourceWhere='rank_reward' GROUP BY detail.league_rank, currency
giá trị thưởng progress  = SUM(amount) WHERE resourceWhere='progression_reward' GROUP BY detail.league_stage, currency
event retention ngày N   = ngày log − league_start_day (funnel: funnelDay)
```

## 5. Không log (cố ý)

| Không log | Vì sao |
|---|---|
| Chuyển qua lại giữa UI Chapter ↔ League | Volume cao, gần như không có thông tin |
| Funnel step cho từng round | Round đã có log level |
| Cosmetic khi thắng chặng (màu profile, icon, giao diện) | Biết stage là biết cosmetic |
| Số bot trong bảng đấu | Không chỉ số nào dùng |
| Thưởng đi **cả** `Resource.OnEarned` lẫn `EventRewardGranter` | Một món thưởng hai dòng → đếm đôi. Thưởng chỉ đi đường `Grant(..., context)` |

## 6. Đăng ký với data team

- Funnel `event_master_league`, shape `once_per_cycle`, action: `eligible`, `open`, `join`,
  `complete`, `claim`.
- `resourceWhen`: `event_master_league`. `resourceWhere`: `level_end`, `rank_reward`,
  `progression_reward`.
- Key phẳng trên level: `league_id`, `league_round`, `league_start_day`. Key phẳng trên funnel
  complete: `league_rank`. Key trong `detail` resource: `league_id`, `league_start_day`,
  `league_multiplier`, `league_difficulty`, `league_rank`, `league_stage`, `league_step`.
- Custom params: `eventMasterLeagueJoining`, `eventMasterLeagueRound`, `eventMasterLeagueStage`,
  `eventMasterLeagueYarn`.
