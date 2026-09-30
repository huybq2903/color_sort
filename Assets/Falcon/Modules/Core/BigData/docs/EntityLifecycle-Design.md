# Entity Lifecycle & Param Cache — thiết kế (BigData)

> Trạng thái: V1 đã implement cho **entity level (turn)**. Các entity khác (purchase attempt,
> offer impression, ad view) áp dụng cùng pattern — xem doc request gửi owner từng module.
> Căn cứ: hợp đồng `sdk-contract-vnext.md` (§A luật id/state, §B3 label, §C carry, §D param level)
> + hồ sơ lệch chuẩn `client-key-deviations.md`.

## 1. Vấn đề

Với entity đa-event (turn, session, purchase attempt...), mỗi log trong vòng đời đều bắt
người dùng cung cấp lại các tham số đã biết từ event mở (levelId, currentLevel, difficulty...).
Hệ quả thực địa (hồ sơ lệch chuẩn): dev nhập tay → typo, sai vocab, sai ngữ nghĩa
(án `ReplayPass` — 1 chiến dịch convert 18 ngày data), adoption thấp (`play_turn_id` giao tay
dev = 3% fleet vs `session_uid` SDK tự đóng = 97%).

**Nguyên tắc**: tham số bất biến trong vòng đời entity → SDK cache từ event mở và tự điền;
người dùng chỉ **phải** nhập phần mới phát sinh (vẫn **được** ghi đè mọi thứ nếu cần).

## 2. Mô hình turn 2 pha (entity level)

```
Start ────── OPEN ──────► terminal đầu (Pass/Fail/Skip) ────── ENDED ──────► Start kế tiếp
        stamp playTurnId                 chỉ level log còn nhận id + cache;      (turn mới đè)
        lên MỌI log (ad/iap/...)         ad/iap/... THÔI stamp (đúng §C)
```

- **Biên turn của dòng level = TERMINAL ĐÓNG LƯỢT** (sửa 04/09, bản 1.3.8 — trước đó là
  Start-to-Start). Căn cứ: game-theory hiện hành chỉ log fail khi hết đường lật kèo (revive
  bằng booster xảy ra TRƯỚC khi fail được log) → không tồn tại ca revive-cùng-turn, nên
  **2 terminal = 2 lượt chơi khác nhau**. Terminal đến khi lượt đã đóng: khác level/kết quả
  → seed id MỚI + anomaly (quên Start tự lành MỌI lần — luật cũ chỉ tự lành ván mồ côi đầu,
  từ ván thứ hai id ván trước bị dùng lại và `argMax` phía server có thể LẬT kết quả ván
  trước); cùng kết quả không mâu thuẫn định danh → nghi double-fire, giữ id cũ (`argMax`
  server khử như cũ). Rời-ván-dở đi pha SUSPENDED riêng — log trễ vẫn thuộc ván đó.
- **Cross-entity stamping chỉ ở pha OPEN**: ad giữa 2 màn tự nhiên không có id (§C —
  "không bịa last_turn_id").
- 4 policy §A3 map thẳng vào state machine:
  - Terminal/heartbeat không Start → mở ngầm, sinh id tại chỗ (server nhận diện "cụt đầu").
  - Start không terminal → server tự đóng; client chỉ việc đè khi Start mới tới.
  - Lồng nhau → MỘT turn mở duy nhất, Start mới đè Start cũ (+ dev-warning).
  - Restart app → chấp nhận mất state, SDK KHÔNG persist.

**Exit / re-open** — cặp API đối xứng, KHÔNG phát event, dùng chung cho cả same-session lẫn qua restart:
- **Rời ván dở**: `var snapshot = Level.OnPauseExit()` — trả `LevelTurnSnapshot` (serializable)
  để game persist vào save, đồng thời chuyển turn sang ENDED → ad/iap ở menu THÔI mang
  playTurnId (đúng §C), cache vẫn giữ cho level log trễ. Null nếu không có turn mở.
- **Mở lại ván**: `Level.OnResume(snapshot)` — trả snapshot lại là chơi tiếp cùng turn
  (1 ván = 1 turn dù qua restart app). Snapshot null/mất → cứ `Level.OnStart` lượt mới,
  chấp nhận vỡ theo A3.4.
- **Bỏ ván vĩnh viễn**: `Level.OnFail(failReason: Quit)` — hợp đồng không có event exit riêng.
- **Trách nhiệm persist snapshot thuộc GAME** — SDK giữ nguyên A3.4 (không tự persist).
  ⚠ Cần báo loader: turn có thể span 2 session, xác nhận cook gom theo id chứ không theo
  time-window.

## 3. Cache — 3 luật an toàn

1. **Chỉ cache field bất biến trong vòng đời entity**: `playTurnId`, `currentLevel`,
   `currentLevelId`, `difficulty`, `movesLimit`, `timeLimitSec`. Field biến thiên per-event
   (`score`, `levelProgress`, `movesUsed`, `boostersUsed`, `extraMeta`, `elo`) KHÔNG BAO GIỜ cache.
   → trong một span nhận diện đúng, giá trị điền không thể sai; sai chỉ có thể ở BIÊN span,
   mà mọi ca biên đều suy biến về pattern server đã xử lý (bảng trên).
2. **"Chưa nhập" = giá trị default** (`0` / `"Unknown"` / `null`) → cache điền.
   **Giá trị user nhập luôn thắng cache.**
3. **Chuỗi bất thường → warning lúc dev, im lặng lúc prod**: Start đè Start đang mở,
   terminal không Start → `AnalyticLogger.Warning` (compile-out theo `FALCON_LOG_DEBUG`).

Vị trí code — 3 lớp tách theo trách nhiệm (một entity level, ba việc khác nhau):

| Class | Vai trò |
|---|---|
| `LevelTurnState` (Model) | State machine thuần — pure, không IO/DI, test không cần mock |
| `LevelTurnService` | Nguồn chân lý về turn đang mở: Apply (điền cache) + warning, OpenPlayTurnId, Suspend/Restore |
| `LevelLogDecorService` (`ILogDecorator`) | Trang trí level log: gọi turn Apply → maxPassedLevel/failCount/playCount → streak (+ Reset streak) |
| `LevelLogService` | Dựng param + gửi log từ khoảnh khắc gameplay (controller gọi xuống) |

`playTurnId` lên mọi log qua `TurnCustomInfoRepository : IFCustomInfoRepository` (cơ chế mở rộng
sẵn của Devkit, đọc `LevelTurnService.OpenPlayTurnId` tươi mỗi log, inject qua `PutIfAbsent` —
log tự set vẫn thắng).

## 4. API facade — dev báo KHOẢNH KHẮC, SDK lo event

**Mặt API chia theo NHÓM ENTITY** (`FalconBigDataController.<Nhóm>.<Việc>`) thay vì một mặt
phẳng 40 hàm: tên hàm hết lặp tiền tố (`Level.OnStart` chứ không phải `OnLevelStart`), và
IntelliSense sau dấu chấm chỉ hiện đúng những việc của entity đang làm.

| Nhóm | Đọc / sửa cache | Báo khoảnh khắc |
|---|---|---|
| `Level` | `CurrentTurnId` · `CurrentTurn` · `WinStreak`/`LoseStreak` · `UpdateTurn` · `ResetWinStreak`/`ResetLoseStreak` · `OnPauseExit` · `OnResume` | `OnStart` · `OnPass` · `OnFail` |
| `Offer` | `CurrentImpressionId` · `Current` | `OnShown` · `OnClicked` · `OnClosed` |
| `Ad` | `CurrentViewId(type)` · `CurrentView(type)` · `UpdateContext` | `OnRequested` · `OnShown` · `OnClosed` |
| `Purchase` | `CurrentAttemptId` · `CurrentAttempt` | `OnStarted` · `OnFailed` |
| `Resource` | — | `OnExchange` |
| `Funnel` | `Register` | `OnStep` |
| `App` | — | `ReportOpenSource` · `ReportAttStatus` · `ReportAdsConsent` · `ReportPushPermission` (+ `ReportPermission` bản thô) |
| `Label` | — | `User` · `Session` · `LevelPlayTurn` · `AdView(type)` · `Offer` · `Purchase` · `Entity(kind, id)` |
| `Common` | `Set` · `SetAll` · `Remove` · `Has` · `Keys` | — (không phát event; xem §5b) |
| `Player` | hồ sơ người chơi SDK đang giữ (`General` · `Session` · `Iap` · `Ad` · `Custom`) | — |

`Player` là **passthrough thẳng** tới `FPlayerInfoService` của Devkit, không bọc lại: bọc thì
phải chép hơn ba chục property và chắc chắn lệch pha khi Devkit thêm field, mà cũng chẳng chặn
được gì vì `FPlayerInfoService.Instance` vốn đã public. Mục đích chỉ là **một cửa duy nhất** cho
dev game — số nào SDK đang giữ (maxPassedLevel, totalPlayTime, inAppCount, adLtv…) thì đọc ở đó
thay vì đếm bản thứ hai.
⚠ Mấy repository đó có cả setter (Devkit dùng để tự cập nhật). Game ghi vào là tự khai số đè lên
số SDK đo được — claim không bằng chứng, đúng thứ §H3 cấm; cần đổi thì báo khoảnh khắc tương ứng
rồi để SDK tự cộng.

Bản thân `FalconBigDataController` chỉ còn giữ ống dẫn (`Send`/`SendAll`/`SendNow`) và 8 lối vào
static trỏ về các nhóm — mỗi nhóm là một class `F*Api` mỏng, không state, chỉ điều hướng xuống
service. Vì sao static: `FalconBigDataController.Level.OnStart(...)` ngắn hơn hẳn
`...Instance.Level.OnStart(...)` mà vẫn đi qua đúng một `Instance` bên trong; instance vẫn lấy
được qua property `LevelApi`/`AdApi`/... nếu cần inject.

`FalconBigDataController.<Nhóm>.On<ChuyệnVừaXảyRa>(...)` — nâng §H2 ("SDK cấp constant,
cấm string tay") lên tầng API: vòng đời khai báo tường minh qua tên method thay vì suy
đoán từ thứ tự log; IntelliSense dạy dev luôn lifecycle.

```csharp
// Đường mới (khuyến nghị) — identity tự lấy từ cache:
FalconBigDataController.Level.OnStart(currentLevel: 5, difficulty: "Hard", movesLimit: 30);
FalconBigDataController.Level.OnFail(failReason: LevelFailReason.OutOfMoves, levelProgress: 80,
    score: 1500, duration: TimeSpan.FromSeconds(95));

// Đường cũ vẫn chạy nguyên và vẫn hưởng cache (hai đường hội tụ về 1 state duy nhất):
new FLevelLog(new LevelPassParamV2 { score = 1500, duration = ... }).Send();
```

Facade là sugar MỎNG — dựng param + đi qua pipeline log sẵn có; state machine vẫn một chỗ
duy nhất trong `LevelLogService`.

### 4a. Đọc & sửa cache — mặt còn lại của facade

SDK cache khá nhiều thứ hộ game, nên phải trả lại đường **đọc** (và một ít đường **sửa**),
không thì game buộc phải giữ bản sao thứ hai của cùng dữ liệu — mà hai bản đếm riêng thì sớm
muộn lệch nhau và không ai biết bên nào đúng.

| Đọc | Trả về | Dùng để |
|---|---|---|
| `Level.CurrentTurnId` · `Ad.CurrentViewId(type)` · `Offer.CurrentImpressionId` · `Purchase.CurrentAttemptId` | id entity đang mở | nối dữ liệu DWH ↔ game server bằng khoá chung |
| `Level.CurrentTurn` | `LevelTurnSnapshot` | **autosave giữa ván** — đọc thuần, KHÔNG đóng turn (`Level.OnPauseExit` mang nghĩa "rời ván") |
| `Ad.CurrentView(type)` | `AdViewSnapshot` | id + adWhere/adWhen/adMediation + hai mốc thời gian |
| `Offer.Current` | `OfferSnapshot` | id + sản phẩm/giá/bề mặt + đã bấm/đã mua + thời lượng hiển thị |
| `Purchase.CurrentAttempt` | `PurchaseAttemptSnapshot` | id + productId/where/giá/tiền tệ |
| `Level.WinStreak` / `Level.LoseStreak` | int | game hiện UI chuỗi thắng mà khỏi đếm lần hai |

**Mọi snapshot là bản SAO**, không phải object state sống: trả state ra thì game sửa được cache
của SDK, mất luôn ý nghĩa "SDK là nguồn chân lý". `Offer.Current` cũng KHÔNG trả offer đang nằm
trong cửa sổ ân hạn — với game thì offer đó đã đóng thật.

| Sửa | Vì sao được sửa |
|---|---|
| `Level.SetUserElo(() => rating)` | chỉ game biết điểm trình độ người chơi; khai một lần rồi SDK đọc tươi và điền vào mọi log level — truyền tay từng lời gọi thì chỗ nào quên là chỗ đó thủng dữ liệu |
| `Level.UpdateTurn(difficulty:, movesLimit:, timeLimitSec:)` | cấu hình ván đổi giữa chừng là chuyện thật (mua thêm lượt đi, revive đổi độ khó). Trước đây muốn đổi phải `Level.OnStart` lại = **đè turn** = một ván thành hai lượt trong dữ liệu |
| `Ad.UpdateContext(type, adWhere:, adWhen:, adMediation:)` | preload lúc chưa biết chiếu ở đâu / trong ngữ cảnh nào; biết lúc nào ghi lúc đó, show/impression/close tự mang theo |
| `App.ReportOpenSource` · `Funnel.Register` · `Level.ResetWinStreak/LoseStreak` | xem §4c / §4h / §3 |

**Cache của entity ad view** (bổ sung 2026-08-06): slot giữ thêm `adWhere` / `adWhen` /
`adMediation` — cả ba bất biến trong một lần xem ad nhưng trước đây bị vứt ngay sau log request,
nên mốc show/close phải nhập lại. Đồng bộ **hai chiều**: mốc nào bỏ trống thì lấy từ cache, mốc
nào tự nhập thì giá trị đó thắng VÀ cập nhật cache cho các mốc sau (preload không biết chỗ chiếu,
lúc show mới biết — thông tin mới nhất là thông tin đúng nhất). Log impression do mediation bắn
cũng được điền theo luật đó, chỉ khi mediation bỏ trống.
⚠ `adWhen` là **field MỚI trên 3 event ad_request/ad_show/ad_close** — 3 event này vốn đã đang
chờ loader xác nhận, gộp luôn vào lần báo đó.

**Một base cho cả họ param**: `AdParam : AdViewParam` và `InAppParam : IapPurchaseAttemptParam` —
định danh của vòng đời (`adViewId`, `purchaseAttemptId`…) khai MỘT chỗ, thêm field mới khỏi phải
nhớ sửa hai nơi (án lệ: thêm `adWhen` phải sửa hai chỗ). Mốc nào cần chặt hơn thì đặt ràng buộc ở
`CorrectValues` **của riêng lớp đó** chứ không đẩy lên cha — impression bắt buộc có `adWhere`,
log mua bắt buộc có `isoCurrencyCode`, còn các mốc khác vẫn để trống được.
⚠ Tuyệt đối không khai lại field trùng tên ở lớp con để "che" của cha: `FKeyService` duyệt
`GetFields(Instance | Public)` nên sẽ thấy CẢ HAI, ghi đè nhau trong dictionary theo thứ tự mà
.NET không đảm bảo — giá trị lên server thành hên xui.

**Trạng thái ≠ cần đường riêng** (án §D8, 2026-08-10): ban đầu tưởng "trạng thái không phải
khoảnh khắc nên phải có kênh riêng" → định mượn `property_data`. Sai hai lần: log đó là di sản
sinh biểu đồ động chứ không phải kênh trạng thái, và **cú ĐỔI trạng thái CHÍNH LÀ một khoảnh
khắc** — event đúng nghĩa đen, còn "trạng thái hiện tại" chỉ là last-value của chuỗi event.
Kèm luật phân xử dùng cho mọi ca sau: *vocab ĐÓNG toàn-fleet mà analyst lọc trực tiếp → nằm trong
TÊN EVENT (permission tách 3 event); vocab MỞ per-game → nằm trong param (`funnelName` để trong
param).* Căn cứ là phí migrate bất đối xứng: gộp→tách là chiến dịch convert, tách→gộp thì
`LIKE 'f_sdk_permission_%'` là xong.

**Nhãn NGƯỜI CHƠI ≠ nhãn instance** (gold-rollups §14 "KIỂU B"): `Label.User` có ngữ nghĩa
**từ-mốc-trở-đi** — server lấy `clientSendTime` làm `effective_from`, nạp vào profile, rồi mọi
event SAU đó tự mang nhãn. Nên nó trả lời được "user này là VIP từ lúc nào" mà không nhuộm ngược
quá khứ. Đổi lại nó **đắt hơn hẳn** nhãn instance: một key user đi kèm mọi dòng event của user đó,
nên client tự áp trần 12 key (§14 chốt 10-15) và cảnh báo khi chạm.
Ba kênh dễ lú, phân biệt bằng câu hỏi chúng trả lời:
| Kênh | Trả lời | Chi phí |
|---|---|---|
| `Label.User` | "user này thuộc nhóm nào, **từ lúc nào**" | 1 event/lần gán + 1 cột trên profile |
| `Label.Session/Turn/...` | "lần chơi/lần xem NÀY thuộc loại nào" | 1 event/lần gán |
| `Common.Set` | "giá trị đó phải có mặt trên **từng dòng log** để lọc thẳng" | +1 field × MỌI log — đắt nhất |

**Nhãn LƯỢT CHƠI vs nhãn MÀN — dễ tưởng là một**: `Label.LevelPlayTurn` tả *một lần chơi*,
`Label.Level(id, …)` tả *bản thiết kế màn*. Dán được cái sau bằng cái trước, nhưng trả giá
ba lần:

| | `LevelPlayTurn` | `Entity(Level)` |
|---|---|---|
| Tả cái gì | ván NÀY (dùng revive, ván tutorial) | MÀN (tune tay, chương 3, tier B) — đúng với mọi lần chơi |
| Volume | đánh lại màn 20 lần = **20 bản tin** cho cùng một sự thật | **0 bản tin** — đi ké level event có sẵn (như `playTurnId`) |
| Đọc ở đâu | hộp turn — muốn hỏi "màn tune tay ăn bao nhiêu doanh thu" phải gom ngược rồi khử trùng | bridge `user_level_daily`/`level_daily` nhặt `argMax` trong ngày → JSONB `labels`, đọc thẳng |
| Thời gian | trọn instance | **as-of-day** — đổi thiết kế màn về sau không nhuộm ngược quá khứ |

**Ranh giới CLIENT ↔ SERVER, lấy `elo` làm ví dụ**: field `elo` = trình độ NGƯỜI CHƠI (đã chốt
nghĩa; không thêm key mới vì dữ liệu lịch sử đã nằm dưới key này và §H cấm rename). Độ khó của
MÀN thì client KHÔNG gửi — hàm của kết quả cả fleet, server tự giải từ tỉ lệ pass.
Mà chính `elo` người chơi cũng vậy: muốn tính ở client thì phải biết độ khó màn, muốn biết độ khó
màn thì phải có dữ liệu nhiều người — con gà quả trứng chỉ cắt được ở phía server. Ép client tính
bằng cách gán mọi màn một độ khó cố định thì con số thoái hoá thành hàm của tỉ lệ thắng, tức là
mã hoá lại `playCount`/`failCount`/streak mà log đã gửi — không thêm thông tin, lại đóng băng công
thức trong bản build (server đổi công thức thì tính lại được cả lịch sử, client thì không).
⇒ **Server tổng hợp elo; client chỉ gửi khi game vốn đã có hệ elo riêng, hoặc để echo lại giá trị
server đã tính** (lúc đó nó là bản ghi "đã gán giá trị nào", giống ab-variant — đối chiếu được
"gán 1200 mà chơi như 900"). Chưa có thì để TRỐNG, đừng điền 0.
Đây là §H3 nối dài: *máy suy được thì máy điền* — và server cũng là máy.

**Ranh giới của quyền sửa** — cùng một luật, ba lần áp:
1. **Id do SDK sinh** (`playTurnId`, `adViewId`, `offerImpressionId`, `purchaseAttemptId`):
   không setter. Cho set là mở cửa cho hai lượt cùng id.
2. **Danh tính** (`currentLevel`, `currentLevelId`): không sửa giữa lượt — đổi danh tính nghĩa
   là lượt KHÁC, mà lượt khác thì mở turn mới chứ không phải vá turn cũ.
3. **Số SDK tự tính** (streak, counter): chỉ `Reset`, không set tuỳ ý — set tay là claim không
   bằng chứng (§H3).
Cái được sửa đúng bằng phần **game vốn sở hữu và có thể đổi thật trong lúc chơi**.

### 4b. Kiến trúc pipeline chiều xuôi (đã implement)

Log class là **DTO thuần** (ctor chỉ còn data + CorrectValues + uuid — không gọi service);
mọi enrichment chạy qua **decorator registry** khi log đi vào pipeline gửi:

```
dev/game ─┬─ Controller.OnLevel<X>(...)      (đường khuyến nghị)
          ├─ log.Send()/SendNow()            (Active Record mỏng — chỉ forward)
          └─ pause generators ─ collector ─ Controller.SendAll
                        │
                        ▼
        FalconBigDataController.Send/SendAll/SendNow   ← CỬA TRƯỚC duy nhất
                        ▼
        LogScheduleService (funnel) ── LogDecorService.Decor  ← decorate-once
                        │                   │ match IsAssignableFrom + sort Priority
                        │                   ├ BaseLogDecorService (-100: ids/date/sendId)
                        │                   └ LevelLogDecor / Ad / Iap / SessionLogService (0)
                        ▼
              DataWrapper → queue bền → batch send
```

- **Decorate-once flag** trên PlainLog: cửa trước + backstop cùng tồn tại không decor đôi;
  retry/re-enqueue ở mức wrapper nên counter monotonic (createId/sendId) không bao giờ nhảy nấc.
- **Code TRONG module BigData KHÔNG gọi `log.Send()`** — inject `LogScheduleService` và
  `Enqueue(log)` thẳng. `Send()`/`SendNow()` là đường tiện tay cho code NGOÀI module: chúng vòng
  qua controller, nên gọi từ trong module sẽ đi qua controller hai lần
  (`Level.OnStart → LevelLogService → log.Send() → Controller.Send`) và tạo phụ thuộc
  vòng ở runtime. Enqueue thẳng vẫn được decor đầy đủ vì funnel nằm ở chính `Enqueue`.
- **Gọi thẳng `LogScheduleService.Enqueue` vẫn an toàn** (backstop tự decor) — nhưng đường
  chuẩn là qua Controller.
- **Ngoại lệ chủ đích**: `FunnelLogService.Check` vẫn ở ctor funnel log — nó là
  validation-có-veto (quyết định log có được gửi) + side effect dedupe gắn thời điểm construct,
  không phải enrichment. Muốn đưa vào pipeline phải thiết kế cơ chế decorator-veto — chờ demand.
- **Các `*LogService` TỰ implement decorator** (không có class adapter trung gian) — bản rút gọn
  `ILogDecorator<TLog>` lo sẵn `DecorLogType`, cast và `Priority` mặc định (default interface
  method), nên service chỉ còn ĐÚNG hàm enrich của nó:
  ```csharp
  public class AdLogService : MySingleton<AdLogService>, ILogDecorator<FAdLog>
  {
      public void Decor(FAdLog log) { /* enrich */ }     // hết, không dòng thừa nào
  }
  ```
  Ai cần tự quyết log type (hoặc không dùng được generic) thì implement thẳng `ILogDecorator`
  và tự khai `DecorLogType` — hệ thống chỉ làm việc với interface non-generic, generic thuần
  là đường rút gọn.
  <br/>⚠ **Trạng thái verify DIM**: compile OK trong Unity 6000 editor + EditMode tests pass
  (2026-07-31). **CHƯA verify trên IL2CPP** — làm 1 bản dev build (Android/iOS) trước khi phát
  hành. Nếu IL2CPP không nuốt: fallback là bỏ DIM, mỗi decorator khai lại 3 dòng
  (`DecorLogType`, `Priority`, cast trong `void ILogDecorator.Decor(IDataLog)`) — không đụng
  kiến trúc pipeline.
  <br/>Muốn chạy trước/sau decorator khác thì override `Priority` (mặc định 0;
  `BaseLogDecorService` = -100 để ids/date/sendId luôn có trước).
- Thêm log type/entity mới: service của nó implement `ILogDecorator<TLog>` là tự đăng ký qua DI —
  KHÔNG thêm hook ctor.

## 4c. Entity session — mốc mở phiên (`app_open`, hợp đồng §B)

Session trong SDK này = **một process**: `session_uid` sinh một lần lúc khởi tạo và đã được
stamp lên mọi log qua central user params (đây chính là "mẫu thành công 97%" hợp đồng khen).
Entity session vì thế **có id nhưng thiếu event mở** — đúng khoảng trống §A2 chỉ ra.

`FAppOpenLog` (`f_sdk_app_open_data`) lấp khoảng trống đó:

| Param | Ai điền | Ghi chú |
|---|---|---|
| `launchType` | SDK-core | `cold` = process vừa khởi tạo (**đồng thời là mốc mở phiên**), `hot` = quay lại từ background. Unity KHÔNG sinh `warm` — không đoán cái không đo được |
| `backgroundDurationSec` | SDK-core | null khi cold (không có nền để đo) hoặc khi không đo được — không bịa 0 |
| `openIndex` | SDK-core | thứ tự lần vào foreground trong phiên, **đếm cả lần bị lọc** |
| `startupDurationMs` | SDK-core | chỉ ở `cold` — xem phần dưới |
| `openSource` / `pushCampaignId` | **module push/deeplink báo vào** | xem phần dưới |

**KHÔNG có event `session_start` riêng**: session = process nên `session_start` và
`app_open[cold]` trùng khít 1:1 (session mới ⟺ process mới ⟺ cold start), giữ cả hai là dữ
liệu thừa vĩnh viễn + một event-id phải nuôi. ⚠ Phải báo loader:
*"`session_start` §B được implement dưới dạng `app_open` với `launch_type=cold`; cần event-id
riêng thì báo lại"* — kẻo họ ngồi đợi một event không bao giờ tới.

**Cũng KHÔNG đổi `session_uid` sang kiểu hết hạn theo gap 30 phút**: đổi ngữ nghĩa một key
đang có 97% adoption là đúng loại lệch đắt nhất trong hồ sơ (§2.2 — án `ReplayPass`). Server
vẫn tự định nghĩa session theo gap được vì đã có `backgroundDurationSec`.

**Lọc nhiễu**: mỗi lần xem rewarded/inter cũng tạo một cặp pause/resume, và người chơi hay
thoát ra vài giây (copy OTP, nghe điện thoại). Warm open có nền ngắn hơn
`fAppOpenMinBackgroundSec` (remote config, default 30s) thì không log. Ngưỡng để ở config chứ
không hardcode vì đây là quyết định kinh doanh sẽ đổi (rule 22): ops hạ về 0 để nghiên cứu
hành vi quay-ra-quay-vào, hoặc nâng lên khi volume phình — không cần release client.
Lưu ý 30s **không lọc sạch được ad** (rewarded + end card có thể 30-45s); client không đoán
"resume này do ad" vì đó là claim không bằng chứng — server đối chiếu `FAdLog` theo timestamp
nếu cần.

**Thời điểm gửi**: enqueue thẳng ngay lúc mở, kể cả trong `Init`. Central user params được
chốt vào `DataWrapper` ngay tại `Enqueue` (không phải lúc flush) nên thứ tự init mới đáng quan
tâm — và nó đã được bảo đảm: `IInit` chạy theo `DependencyOrder`, mà `AppOpenLogService` phụ
thuộc `LogScheduleService → LogDecorService → BaseLogDecorService → FCentralUserParamService →
các player repository`, nên chúng init xong trước.

**`startupDurationMs` — ngoại lệ đo-thời-gian duy nhất của client (§B4)**: mọi khoảng cách thời
gian khác server tự trừ `client_sent_time` giữa hai event (án lệ sống: `play_time` client tự cộng,
reset là phình số). Riêng đoạn khởi động nằm TRƯỚC khi hệ thống event sống nên server mù —
buộc client đo. Mốc: `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` → lúc dựng bản tin
app_open cold, bằng `Stopwatch` (đơn điệu — chỉnh giờ máy giữa chừng không đẻ số âm).
⚠ **Thiếu native bootstrap** (OS tạo process → Unity runtime lên): Unity không có API nào nhìn
thấy khoảng đó. Nên đây là **số tương đối** — so được giữa các bản build và giữa các máy, KHÔNG
phải cold-start tuyệt đối để so với native app. Đã chốt với loader như vậy; mốc cố định trong
SDK, đổi thì báo.
Cố tình **không** đo tới "thực sự chơi được": mốc đó mỗi game một kiểu, đo được cũng không so
được cross-game (và loader đã bảo đừng).

**`openSource` / `pushCampaignId` — SDK-core KHÔNG tự biết**: Unity không đọc được intent
(Android) / launchOptions (iOS), đó là sân của module push/deeplink. Nên đây là param duy nhất
của app_open cần bên ngoài gọi vào: `App.ReportOpenSource(source, pushCampaignId)`.
- Không ai gọi → field **vắng mặt**, KHÔNG mặc định `icon`: mặc định thì con số "mở từ icon"
  chỉ đang đếm những game chưa wire, tệ hơn là không có số.
- Gọi MUỘN (sau khi bản tin app_open đã bắn) → SDK cảnh báo trong log và **bỏ**, không dán sang
  lần mở kế tiếp. Bấm push lúc 9h rồi quay lại app lúc 11h là hai chuyện khác nhau; dán sang là
  đẻ dữ liệu sai — và dữ liệu sai đắt hơn dữ liệu thiếu.
- Báo cũ hơn 10s cũng bị bỏ theo luật trên.
- Chỗ gọi đúng: `RuntimeInitializeOnLoadMethod`/`IPioneer` cho ca Cold (bản tin cold bắn ngay
  trong pha init), callback nhận notification cho ca Hot.

## 4d. Entity offer impression (hợp đồng §C + §D3)

Một lần hiển thị offer IAP là entity đa-event: **hiện → (click / mua) → đóng**. Log
`f_sdk_iap_offer_data` bắn **lúc đóng** (span-record theo §G — một event cho cả lượt hiển thị,
không log per-frame), mang thời lượng hiển thị + đã bấm chưa + có dẫn tới mua không.

| API (controller) | Việc |
|---|---|
| `Offer.OnShown(IapOfferParam)` | Sinh `offerImpressionId`, bấm giờ, giữ state |
| `Offer.OnClicked()` | Đánh dấu `isClicked` |
| `Offer.OnClosed()` | Chốt `impressionDuration` + `isPurchased` → bắn log |

**Carry sang log mua (§C)**: `IapLogService` (decorator của `FInAppLog`) hỏi state
"giao dịch này có khớp offer đang hiển thị không". **Khớp theo `offerProductId` chứ KHÔNG theo
luật "có state thì stamp"** như `playTurnId` — vì offer dạng card nằm lì trên UI (hồ sơ lệch
chuẩn §3.2: 49 lần/user/ngày) sẽ ăn nhầm mọi giao dịch khác trong lúc nó hiển thị, tạo claim
không bằng chứng. Khớp thì log mua mang `offerImpressionId` và offer được đánh dấu `isPurchased`.

**`isPurchased` chỉ kết luận khi kiểm chứng được**: `true` = có giao dịch khớp TRƯỚC khi log
được gửi; `false` = đóng thật + có khai `offerProductId` mà không ai mua; **`null`** = không
kiểm chứng được (không khai `offerProductId`, hoặc log gửi lúc pause khi impression chưa xong).
Đây cũng là động lực để game khai `offerProductId`: không khai thì mất luôn cả conversion.

⚠ **`offerImpressionId` mới là nguồn chân lý để đo conversion, không phải `isPurchased`** —
giao dịch về sau khi log impression đã gửi (pause / ân hạn) vẫn mang id nhưng không sửa được
`isPurchased` của bản log đã đi. Join theo id luôn đúng; `isPurchased` chỉ là tiện ích.
Vì cùng một khoá `offerImpressionId` nằm trên CẢ hai log, join chạy được từ cả hai phía — không
cần stamp thêm chiều ngược lại (và cũng không nên: log offer gửi trước khi giao dịch xong nên
chiều đó không đáng tin).

**App pause lúc offer đang mở**: chốt và log ngay (chống mất impression nếu app bị kill) nhưng
**giữ state** — app quay lại mà người chơi mua thì vẫn quy được giao dịch về offer đó.
`Offer.OnClosed` sau đó KHÔNG log lại (cờ `_logged`) nên **một impression luôn chỉ có một log**.
Bản chốt lúc pause để `isPurchased = null` chứ không phải `false`: impression chưa thực sự kết
thúc, kết luận "không mua" lúc đó sẽ mâu thuẫn với log mua mang cùng id ngay sau.

**Cửa sổ ân hạn sau khi đóng** (`fOfferAttributionGraceSec`, config, default 120s): luồng phổ
biến là bấm mua → popup đóng / forward sang shop → callback giao dịch mới về. Đóng state ngay
lúc `Offer.OnClosed` là mất attribution của đúng những lượt CÓ chuyển đổi. Nên offer vừa đóng
vẫn nhận attribution thêm một khoảng, vẫn khớp theo `productId`.
Ưu tiên: **offer đang hiển thị trước, offer trong ân hạn sau** — mua sản phẩm mà cả hai cùng
chào thì tính cho cái đang hiển thị, không mập mờ. Giao dịch quy qua ân hạn KHÔNG cập nhật
`isPurchased` (log đã gửi rồi).

**Offer mới khi offer cũ chưa đóng**: offer cũ được chốt và log ngay (kèm warning lúc dev) —
surface cũ đã thực sự kết thúc, bỏ im lặng là mất hẳn một impression.

⚠ **Luật volume (§G)**: kệ shop nhiều offer = **MỘT** lần `Offer.OnShown` mang mảng
`offerProductId`, KHÔNG gọi mỗi offer một lần. Án lệ combat-log per-hit trong hợp đồng chính là
vụ này phóng đại 2 bậc.

## 4e. Entity purchase attempt — phễu checkout (hợp đồng §D4)

Hiện server **chỉ thấy mua-XONG** nên abandonment mù hoàn toàn. Entity này bổ sung hai mốc còn
thiếu, nối với nhau (và với log mua) bằng `purchaseAttemptId` do SDK sinh:

```
Purchase.OnStarted(productId, where)   → f_sdk_iap_start_purchase_data   [mở phễu]
        │
        ├─ billing callback lỗi/huỷ/treo → Purchase.OnFailed(reason)
        │                                  → f_sdk_iap_purchase_fail_data  [cùng attemptId]
        └─ billing callback thành công   → log mua (f_sdk_in_app_data) tự mang purchaseAttemptId
```

| API (controller) | Việc |
|---|---|
| `Purchase.OnStarted(productId, where)` | Sinh `purchaseAttemptId`, log mốc mở, giữ state |
| `Purchase.OnFailed(reason)` | Log fail mang cùng id, đóng lượt |
| *(không có API)* | Thành công: `IapLogService` tự đóng dấu id lên log mua khi `productId` khớp |

**⚠ Phụ thuộc ngoài module**: BigData không nhìn thấy billing flow. Owner module
`InAppPurchase` (hoặc game tự lái luồng mua) phải gọi 2 API trên từ `launchBillingFlow` và
billing callback — nội dung này nằm trong doc request gửi owner module IAP.

**3 bẫy của §D4 đã xử lý:**
1. `Pending` **không phải fail thật** — có giá trị vocab riêng; log mua vẫn bắn khi giao dịch
   hoàn tất sau (có thể vài ngày). Lúc đó state đã đóng nên log mua KHÔNG mang `purchaseAttemptId`;
   server nối bằng phễu theo product/user/time. Phễu phía server phải đọc hiểu điều này.
2. **App kill giữa dialog** → "start cụt đuôi", chính là số đo abandonment. KHÔNG persist state
   qua process (§A3.4). Callback về ở phiên sau mà không còn state → `TakeFail` trả null, không
   log (không bịa ra lượt mua không tồn tại).
3. `purchaseAttemptId` **carry lên CẢ hai nhánh kết thúc** → phễu khớp chính xác + đo được
   time-to-complete.

**Khớp thành công theo `productId`** (như offer): lượt mua sản phẩm A không ăn nhầm giao dịch
của sản phẩm B; giao dịch không khớp thì lượt vẫn mở chờ terminal của nó.

**Vocab `failReason`**: `user_canceled` / `item_unavailable` / `billing_unavailable` / `network` /
`developer_error` / `pending` theo hợp đồng, **cộng thêm `unknown`** (additive) cho trường hợp
không map được responseCode — nói thẳng "không biết" còn hơn gán bừa một lý do sai.
⚠ Cần báo loader: xác nhận 2 event id + giá trị `unknown` bổ sung.

## 4f. Entity ad view — phễu fill (hợp đồng §B + §C)

Hiện server chỉ có called/impression nên **không đo được fill**: bao nhiêu lần xin ad mà không
ra ad. `f_sdk_ad_request_data` bổ sung mốc mở, nối với log impression bằng `adViewId`:

```
Ad.OnRequested(type, ...)  → f_sdk_ad_request_data   [mở vòng đời, sinh adViewId theo format]
Ad.OnShown(type, where)    → f_sdk_ad_show_data      [+ requestToShowMs, SDK tự đo]
   (mediation tự log)     → f_sdk_ads_data          [impression, tự mang adViewId]
Ad.OnClosed(type, ...)     → f_sdk_ad_close_data     [+ shownDurationSec, SDK tự đo]
```

Bốn mốc **cùng một `adViewId`** → server dựng được trọn vòng đời và mọi khoảng giữa:
`request→show` = chờ fill · `show→impression` = thiếu thì là display failure ·
`show→close` = thời lượng xem · `request` không có `impression` = **view FILL-FAIL**.

**Một `adViewId` cho CẢ vòng đời** (request → show → impression → close), không tiêu thụ ở
impression: id chỉ xoay khi có request mới của cùng format — mà mediation load lại ngay sau khi
đóng ad, nên id tự xoay đúng lúc **không cần ai gọi "đóng" tường minh**.

**Không cần event fail riêng** — sự vắng mặt của impression mang cùng id đã là tín hiệu.

⚠ **`ad_show` và `ad_close` là 2 event NGOÀI hợp đồng** (§B chỉ xin `ad_request`) — thêm vì
entity ad kiểu gì cũng phải chờ Mediation wire, nên cho họ wire trọn vòng đời một lần thay vì
hai lần. **Phải báo loader** để họ biết đường tổng hợp. Volume: chỉ inter/rewarded mới có
show/close (banner không), cỡ chục event/user/ngày — nhỏ so với level/ad impression.

Mediation cũng có thể gọi `Ad.CurrentViewId(type)` để gắn cùng id lên `CSAdStart`/`CSAdFinish`
(message gửi **game server**) → nối dữ liệu DWH ↔ game server bằng một khoá chung.

**Vì sao slot theo `AdType` chứ không phải token truyền tay** (khảo sát `FalconIronSourceService`):
- Impression callback của mediation là **global theo format** (`OnImpressionDataReady` → map
  `AdFormat` → `AdType` → tra `AdInfos[type]`), không phải per-ad-object; nên bản thân Mediation
  cũng không có handle để nối request↔impression.
- Nhưng mỗi format chỉ có **một ad object và một load in-flight** (`LoadInterstitial` →
  `OnAdLoaded`/`OnAdLoadFailed` → retry backoff), các format thì song song nhau.
⇒ Slot `Dictionary<AdType, string>` là đúng mức chính xác thực tế, và Mediation chỉ cần thêm
**một dòng** vào mỗi hàm `Load*` thay vì phải thread token qua adapter.

**Các ca đã tính:**
- **Retry load**: mỗi lần xin là một request mới, request cũ thành fill-fail — đúng ngữ nghĩa.
- **Banner tự refresh** trong SDK (không gọi `LoadAd` lại): các impression sau mang **cùng** id
  với request đã tạo ra banner đó — đúng ngữ nghĩa "một banner instance đẻ N impression"; server
  đếm view thật bằng dedupe theo id.
- **Ad lên hình mà chưa từng có request** (đường load nằm ngoài SDK): `adViewId` vắng mặt.

⚠ Lưu ý: Mediation đã có `_idAd` (guid sinh lúc **Show**, dùng cho `CSAdStart`/`CSAdFinish`)
nhưng đó là message gửi **game server**, không phải DWH, và sinh lúc show nên không đo được fill.
Hai thứ khác nhau, không gộp.

## 4g. Giao dịch tài nguyên — `exchangeId` (hợp đồng §D10), CỐ TÌNH không phải entity

"Mua 3 booster bằng 500 gold" hiện là **hai event rời**: `resource_sink(gold)` +
`resource_source(booster)`. Server không nối được hai vế ⇒ không tính được **giá thật per item**,
và bundle đa vế (gems → gold + booster + heart) thì vỡ hẳn. Fix bằng đúng một khoá chung:

```
Resource.OnExchange(sinks: [gold 500], sources: [booster x3])
   → f_sdk_resource_data (Sink,   gold,    exchangeId=X)
   → f_sdk_resource_data (Source, booster, exchangeId=X)
```

**Zero event mới, event cũ không đổi** — server chỉ cần mở thêm `GROUP BY exchangeId` lúc query.

**Không có state, không có hộp entity.** Giao dịch là một *khoảnh khắc*, không có vòng đời để
quản: id sinh ra và chết ngay trong lời gọi (`ResourceExchange.Combine` là hàm thuần trong
`Model/`, service chỉ enqueue). Đây là ranh giới của bổ đề entity ở §2 — có nhiều event thuộc
chung một chuyện **không** đủ để thành entity; phải có *quá trình mở rồi đóng* thì mới có thứ để
giữ. Nhét exchange vào khuôn entity sẽ đẻ ra state phải dọn mà không đổi lấy được gì.

**Các quyết định nhỏ:**
- `flowType` do SDK set theo vế (sinks/sources) — bớt một chỗ dev điền sai được.
- Game tự set `exchangeId` (vd id giao dịch của **game server**) thì SDK lấy chính giá trị đó
  làm id chung cho cả bộ → nối được DWH ↔ game server, đúng luật "user nhập là thắng".
- Cả bộ enqueue **một lượt** — không có khe để app pause cắt đôi giao dịch.
- Gọi với đúng một vế: vẫn gửi (không nuốt dữ liệu) nhưng cảnh báo dev-time, vì id một mình
  không ghép được với ai. Thưởng cho không (chỉ có source) thì cứ `FResourceLog` như cũ.

## 4h. Vocab + registry funnel (§D6 + §D9) — cái thiếu là CONVENTION, không phải event

Feature engagement (battle pass / daily quest / piggy bank / lucky wheel / event mùa) đều chung
một hình dạng: **tiến độ + ví thưởng + (đôi khi) cửa thu tiền**. Cả ba mặt đều có event sẵn,
thiếu duy nhất bộ từ vựng chung — nên bản này **không thêm param, không thêm event**, chỉ thêm
hằng số + luật:

| Mặt | Event sẵn có | Hằng số SDK cấp | Field thật |
|---|---|---|---|
| Tiến độ | `funnel_data` | `FFunnelName` + `FFunnelAction` | `funnelName` / `action` / `priority` |
| Ví thưởng | `resource_source`/`_sink` | `FResourceWhen` | `resourceWhen` |
| Tiền thật | `in_app_data` | `FIapPlacement` | `where` |

⚠ **Hai chỗ cần loader xác nhận** (hợp đồng nói "vocab `source`" và "vocab `placement`" nhưng
SDK không có field nào tên như vậy — đây là suy diễn của bên client, có căn cứ nhưng phải chốt):
- **`source` → `itemType`**: căn cứ `buy_count_by_class (source item_type='iap')` trong
  gold-rollups §C-economy và bảng map `itemType → sub_event_2` ("category cha: shop/reward/gift").
- **`placement` → `where`**: căn cứ `where`/`adWhere` → `event_where` = "placement" trong
  event-catalog.

**Hằng số chứ không phải enum**: game PHẢI thêm được tên riêng (additive) — enum thì chặn cứng.
Đổi lại, hằng số không mang ngữ nghĩa nào; mọi luật nằm ở registry, không nằm trong vocab.

### Vì sao phải có registry hình dạng funnel

`FunnelLogService.Check` (có từ trước) cài đúng **một** hình dạng: một-lần-trên-đời-máy, priority
tăng đều không hở — tức là khuôn của `ftue`. Mọi funnel §D9 đều vi phạm:

| Ca thật | Vi phạm luật cũ |
|---|---|
| `battle_pass` tier 3 có cả `milestone` lẫn `claim` | trùng khoá `(funnelName, priority)` → "already joined" |
| `daily_quest` `join` lại ngày mai | trùng `(funnelName, 0)` |
| Người chơi mua vượt tier / bỏ mốc | hở priority → "not created in order" |

Nếu chỉ ship vocab mà không gỡ luật này thì hằng số vô dụng: log bị **chính SDK** đánh trượt.
Mà `FFilteredFunnelLog` thì **veto thật** (`logValid=false` → `Send()` không gửi), `FFunnelLog`
chỉ `LogError` rồi vẫn gửi.

⚠ **Hệ quả cần báo**: game nào đang dùng `FFilteredFunnelLog` cho funnel lặp lại thì **từ trước
tới nay đã âm thầm mất log** — không phải bản này gây ra, nhưng bản này là lúc phát hiện.

Cách gỡ giữ nguyên hành vi game đang chạy: **gate bằng đăng ký, không sửa luật tại chỗ**.
- Funnel **chưa đăng ký** → nguyên luật cũ, byte-identical.
- Funnel đăng ký `Repeatable` (5 tên hợp đồng đã đăng ký sẵn + mọi `event_*`) → bỏ luật thứ tự
  và luật trùng mốc; `funnelDay` lấy từ lần `join` **gần nhất** thay vì lần đầu đời máy — nếu
  không, mùa 5 mang cohort mùa 1 và `event_duration` server derive ra thành số vô nghĩa.

### 5b. Tham số dùng chung — hai đường, chọn theo tuổi thọ

Trước đây muốn thêm một key vào MỌI log chỉ có một đường: khai một class
`IFCustomInfoRepository`. Đúng cho tham số của cả một module (ab-test, mmp, account — có ctor
để bắt sự kiện, có state để giữ), nhưng để thêm hai key lẻ thì đó là cả một file + một node DI.
Thêm đường nhanh `FalconBigDataController.Common`:

```csharp
Common.Set("guildId", guild.Id);                // giá trị cố định, đổi thì set lại
Common.Set("gold", () => wallet.Gold);          // đọc tươi mỗi log, khỏi nhớ set lại
Common.Set("cohort", "abc", persist: true);     // sống qua restart
```

| | Class `IFCustomInfoRepository` | `Common.Set` |
|---|---|---|
| Hợp với | tham số của cả một module, cần state/sự kiện | vài key lẻ của game |
| Sống qua restart | có (class được dựng lại mỗi lần chạy) | mặc định KHÔNG; bật `persist: true` từng key |
| Chi phí thêm 1 key | 1 file + 1 node DI | 1 dòng |

**Ba hàng rào**, vì đây là chỗ đắt nhất để thêm field (một key × mọi log × mọi user):
1. **Trùng key trung tâm thì CHẶN NGAY lúc set** (`level`, `sessionUid`…). Lúc merge, tham số
   trung tâm luôn thắng (`PutIfAbsent`) nên key trùng sẽ vô hình — chặn lúc set để dev khỏi ngồi
   đoán vì sao giá trị của mình không ra. Danh sách key trung tâm đọc bằng reflection từ
   `FCentralUserParamService.ParamKey` chứ không chép tay, để Devkit thêm key thì đây tự biết.
2. **Quá 15 key thì cảnh báo** — kèm câu nhắc: tham số chỉ có nghĩa với một loại khoảnh khắc thì
   để trên param của log đó, đừng đẩy lên đây.
3. **Provider ném exception thì nuốt + cảnh báo** — một key hỏng không được phép làm chết cả
   đường log; các key khác vẫn gửi bình thường.

Giá trị null (hoặc provider trả null) = key **vắng mặt** khỏi payload, không gửi null (§H4).

**Vì sao `persist` là opt-in chứ không mặc định**: bật thì key có mặt ngay từ log ĐẦU phiên —
quãng game còn chưa chạy tới chỗ set, mà đó lại đúng là quãng có `app_open[cold]`. Đổi lại, giá
trị của phiên trước có thể đã sai (người chơi rời guild lúc app đóng) và nó sẽ sai **ngay từ log
đầu**, không ai sửa hộ được — cùng cái cân "sai đắt hơn thiếu" đã dùng cho `openSource`. Nên
quyền chọn để ở phía game, mặc định là bên an toàn.
Ba chi tiết đi kèm: giá trị của phiên NÀY không bị bản đã lưu đè (mới thì đúng hơn cũ); set lại
cùng key mà không bật `persist` thì bản lưu bị xoá; và bản `Set(key, provider)` không lưu được
(provider là code) nên nó cũng xoá bản lưu cũ của key đó — để lần khởi động sau không ăn phải một
giá trị chết.

### 5c. Field nào ở param, field nào ở log — và vá ở tầng nào

Luật một câu: **param = cái GAME khai, log = cái SDK thêm.**

Trước đợt refactor này, field SDK tự sinh (`adViewId`, `purchaseAttemptId`, `offerImpressionId`)
và field SDK tự đo (`requestToShowMs`, `fillLatencyMs`, `impressionDuration`…) nằm lẫn trong
param. Hai hậu quả thật, không phải chuyện thẩm mỹ:

1. **Game set đè được id của SDK.** `param.adViewId = "..."` là hợp lệ về cú pháp, và không có
   chỗ nào chặn — phễu fill gãy im lặng.
2. **Cùng một field bị ba tầng giành nhau ghi.** `adWhere` từng được điền ở cả `AdViewState`
   (lúc MarkShown/MarkClosed) lẫn `AdLogService.Decor`, hai luật khác nhau, không ai nhớ cái nào
   thắng. Nay chỉ còn `AdViewState.ApplyContext` là bản luật duy nhất.

Kéo theo: **model không mutate object của caller.** `Open`/`MarkShown`/`Close` trả `(id, số đo)`
cho service gắn lên log, thay vì thò tay vào param mà caller vừa đưa.

Vá ở tầng nào thì theo câu hỏi *"giá trị phải đúng tại thời điểm nào?"*:

| Giá trị phải đúng… | Vá ở | Ví dụ |
|---|---|---|
| tại KHOẢNH KHẮC sự kiện xảy ra | service/state, lúc dựng log | `requestToShowMs`, `purchaseAttemptId` |
| lúc bản tin RỜI hàng đợi | `ILogDecorator`, lúc trang trí | `typeCount`, `adLtv`, nhãn entity |

Đo trễ mà vá ở decorator thì số đo tính cả thời gian nằm trong hàng đợi — sai. Ngược lại, số cộng
dồn mà chốt sớm thì bỏ sót phần phát sinh giữa lúc dựng và lúc gửi.

**Ngoại lệ có chủ ý — param toàn-SDK giữ nguyên.** `AppOpenParam` do SDK đo 100%, dev game không
điền gì. Nó không có mập mờ nào để gỡ: một người ghi (`AppOpenLogService`), một chỗ ghi. Luật trên
nhắm vào param **lẫn lộn** hai nguồn; param thuần-đo giữ vai "gói số đo", tách ra chỉ tốn một
public type mà không sửa được lỗi nào. Cùng lý do, `networkType` ở lại param của cả ba họ.

Có test canh: `SdkFieldPlacementTests` — field SDK phải nằm trên log, và **không lớp payload nào
được khai trùng tên field với lớp cha** (`FKeyService` quét field public thấy cả hai, thứ tự
`GetFields` không xác định nên bản tin lấy phải cái nào là hên xui; lỗi này đã dính hai lần với
`purchaseAttemptId` và `extraMeta`).

### 5d. Field của CHỦ THỂ NÀO — và vì thế nằm ở NHÀ nào (§H6, amendment 2026-08-11)

§5c hỏi "ai điền". Câu hỏi thứ hai, độc lập: **field này tả CHỦ THỂ nào** — `event` / `turn` /
`session` / `level` / `user`? Một dòng `f_sdk_level_*` cõng lẫn cả ba: `fail_reason` tả LƯỢT,
`difficulty` tả MÀN, `first_login` tả NGƯỜI CHƠI. Cook phải biết field nào `argMax` lên trục nào,
mà tri thức đó trước giờ nằm trong đầu người chứ không nằm trong cấu trúc.

Luật đặt nhà:

| Loại field | Nhà | Ví dụ |
|---|---|---|
| tả CHÍNH chủ thể của event | **flat**, mặc định | `failReason`, `levelProgress` trên level event |
| **attribute của chủ thể NGOẠI** | **container mang tên chủ thể** | `levelLabels`, `profileSnapshotProps`, tiền tố `mmp_` |
| **id ngoại** (chìa khoá JOIN, không phải attribute) | **flat hợp lệ** | `playTurnId` trên ad event, `offerImpressionId` trên purchase |

Ba thứ đã dời theo luật này:

| Field | Chủ thể | Nhà mới |
|---|---|---|
| `movesLimit` · `timeLimitSec` | LEVEL (thiết kế màn) | bundle `levelLabels`, tên hợp đồng `FLevelLabelKey` |
| `elo` | USER | bản tin `f_sdk_user_label`, tên hợp đồng `FUserLabelKey.ELO` |
| nhãn thiết kế offer (mới) | OFFER (thiết kế, khoá `offerId`) | bundle `offerLabels` trên chính event offer |

**`elo` vì sao là bản tin chứ không phải container.** `profile_snapshot_props` — container user
mà §H6 nhắc tới — do **server** dựng từ profile, client không ghi vào đó được. Kênh client cho
thuộc tính user là `f_sdk_user_label`. Mà elo lại đúng ô (c)(3) của `param-taxonomy`: state
quan-sát-được, mất thì đọc lại bằng `argMax` chuỗi event ⇒ **gửi khi ĐỔI là đủ**. Nên
`Level.SetUserElo` đổi từ `Func<int?>` (đọc tươi mỗi log, đóng phẳng lên mọi level event) thành
`int?` (khai khi đổi). `LabelLogService.LabelUser` được thêm chốt chặn trùng theo PHIÊN — gán lại
đúng giá trị cũ không mang thông tin gì mà vẫn tốn một bản tin; không persist chốt chặn đó vì
profile server có TTL 90 ngày LRU, user ngủ lâu quay lại là server "chưa từng gặp".

**Nhãn thiết kế offer — cặp thứ hai của khuôn "vật / lần".** Y hệt cặp level ↔ lượt-chơi:
`Label.IapOffer(...)` dán cho MỘT LẦN hiển thị (bắn `f_sdk_iap_offer_label`), còn
`Label.IapOffer(offerId, ...)` dán cho BẢN THIẾT KẾ — đi ké mọi event của
offer đó, không tốn bản tin. Kho nhãn vật vì thế đổi khoá từ `int` sang `string` (offerId là
chuỗi) và luôn kèm kind, để level "42" không đụng offer "42". Chỗ giữ 48B chỉ trừ vào loại vật
CÓ nhãn hợp đồng (hiện là level) — trừ ở offer là bóp oan phần của game.

**Cố tình KHÔNG dời — `CommonParamRepository`.** Nhìn thì giống bệnh (thuộc tính user thả phẳng
lên mọi event) nhưng `param-taxonomy` §3(a) nói ngược: giá trị state của user do **client gửi
phẳng, mỗi event**, rồi loader mới gấp vào profile — `total_play_time`, `current_hard_currency`
là ví dụ được nêu đích danh và hôm nay vẫn phẳng. Lý do nằm ở §(b) BÃI-TẬP-KẾT: profile hot-state
có TTL, hệ sinh sau đời user, và loader không hỏi ngược được ai — nên mấy field đó là **DNA tái
tạo**, phải đi từng bản tin. Thêm nữa `Set(key, provider)` (đọc tươi lúc dựng log) không có bản
tương đương ở kênh nhãn, vốn mang nghĩa từ-mốc-trở-đi. ⇒ Đây là câu để hỏi loader (container này
server dựng, client nửa kia làm gì?), không phải việc để tự refactor.

**Grandfather + không phải bệnh.** `difficulty` (`sub_event`) và `offerCategory` (`sub_event_2`)
được hợp đồng grandfather — chiếm slot indexed đang sống, khai chủ thể trong catalog là đủ.
`currentLevel` trên ad/iap/funnel/custom event KHÔNG phải bệnh: **id ngoại**, hợp đồng cho phép
flat vì nó là chìa khoá JOIN chứ không phải attribute.

**Còn treo — nhóm offer design vẫn phẳng.** `offerId` · `offerLayout` · `offerSurfaceType` ·
`discountRate` · `offerPrice` · `offerProductId` · `triggerType` · `triggerEvent` ·
`placementScene` đều tả THIẾT KẾ offer chứ không tả lần hiển thị. Giữ nguyên theo quyết định
11/08 (cột đang sống, dời là gãy dashboard); đường mới cho nhu cầu chi tiết hơn là bundle
`offerLabels` ở trên.

### 5e. Kill-safe — ba khuôn, chọn theo HÌNH DẠNG dữ liệu (đừng chế khuôn thứ tư)

App bị kill cứng (crash, hết pin, force-stop) không phát tín hiệu gì — mọi dữ liệu chỉ nằm trong
RAM là mất. Hệ đang có BA khuôn chống, mỗi khuôn khớp một hình dạng dữ liệu; cần kill-safety cho
thứ mới thì CHỌN trong bảng, đừng phát minh:

| Dữ liệu hình gì | Khuôn | Đang dùng | Giá phải trả |
|---|---|---|---|
| Counter TÍCH LUỸ lớn dần theo thời gian — không replay được (không biết process chết lúc nào để tính nốt quãng cuối) | **Fragment-tick**: gửi phần-chưa-log theo ngưỡng luỹ tiến 1′→16′; log pause TRỪ phần đã gửi để tổng không đổi | `SessionCheckService` (total_play_time) | +2–3 event/phiên; kill mất ≤ 1 ngưỡng |
| State BOUNDED, replay được nguyên vẹn | **Persist + boot-replay**: chụp đĩa mỗi lần đổi (idempotent — dựng lại từ cache), nạp-rồi-gửi lúc boot, luật **XOÁ-TRƯỚC-GỬI** (kill giữa quãng gửi thì mất-như-cũ, xoá sau mà kill là gửi ĐÔI) | `BannerLogService` (cụm impression) | ghi đĩa mỗi update; bản tin phục hồi mang timestamp phiên SAU |
| INSTANCE đang mở, có id riêng | **Pause-snapshot + kênh bổ sung**: chốt và gửi sớm lúc app pause (1 instance = 1 bản tin, cờ chống gửi đôi); fact đến muộn đi bản tin riêng mang id, server vá bằng join | `OfferImpressionState` (+ nhãn `clicked`) | field đóng băng tại mốc pause — phải khai luật đọc |

Phần CHUNG của cả ba là cái khớp "flush lúc pause" — đã tách sẵn thành `IAppPauseLogGenerator`,
đừng tách thêm: ba khuôn khác nhau ở cả ba trục (hình dạng dữ liệu / cách hồi / nhịp), một
abstraction phủ cả ba sẽ phải tham số hoá đúng những chỗ chúng khác nhau. Điều kiện mở lại: xuất
hiện counter tích luỹ THỨ HAI cần tick → lúc đó tách `EscalatingTick` với hai ca thật trước mặt.

### 4i. Bản tin TỔNG HỢP (span-record) — cái gì gộp được, cái gì không

Banner refresh theo giây nên log từng impression là vỡ volume (§G). `BannerLogService` gộp theo
`BannerKey`; số đo thực địa của một game: **39.079 dòng/ngày, trung bình 4,19 impression/dòng,
p99 = 31** — bỏ gộp là thêm ~125k event/ngày cho một game.

Luật để biết field nào còn đúng trên dòng gộp:

> **Cái gì không nằm trong KEY thì hoặc phải cộng được, hoặc phải bỏ trống.**

| Field | Trên dòng gộp | Vì sao |
|---|---|---|
| `adRev`, `impressionCount` | ✓ giá trị đúng | cộng được |
| `adNetwork`, `adPrecision`, `adCountry`, `adWhere`, `currentLevel` | ✓ giá trị đúng | nằm trong key — cùng giá trị mới được gộp chung |
| `adViewId`, `fillLatencyMs`, `playTurnId` | **bỏ trống** | định danh/thời điểm của MỘT lần xem; giá trị lúc gửi chỉ đúng cho impression cuối trong cụm |

Cơ chế: `PlainLog.IsSpanRecord` (property, không phải field — `FKeyService` chỉ encode field nên
cờ không lọt vào payload). `FAggregatedAdLog` bật cờ; `AdLogService.Decor` thấy cờ thì thôi đóng
dấu id/độ trễ nhưng **vẫn cộng doanh thu vào sổ cái**; `BaseLogDecorService.InjectDictionary` lọc
`playTurnId` khỏi central params của dòng gộp.

**Hệ quả chấp nhận có biên bản**: không đo được fill-rate banner phía client (server không nối
được impression về `ad_request`). Đổi lại là 125k event/ngày. Fill banner vốn có sẵn và chính xác
hơn ở dashboard mediation — họ thấy cả lần load hỏng mà SDK không thấy. `fillLatencyMs` cho banner
thì bản thân đã vô nghĩa: banner tự refresh chứ không phải người chơi bấm xin.

⚠ **KHÔNG áp cách này cho inter/rewarded**: hai loại đó được phân tích theo TỪNG LẦN (chuỗi mệt
mỏi, eCPM theo vị trí, phễu fill) vì mỗi lần là một quyết định của người chơi. Banner là cái đồng
hồ — "impression thứ n" của nó chỉ là cách đo thời lượng chơi vòng vo, mà thời lượng đã có
`total_play_time` đo thẳng.

**Ghi chú cho loader**: đếm impression banner phải là `sum(impression_count)`, không phải
`count(*)`. Còn `typeCount` trên dòng gộp thì tuỳ bản cài sổ cái — game dùng module Mediation thì
nó đếm đúng từng impression (Mediation tự cộng), game chỉ dùng Devkit thì nó đếm số BẢN TIN.

## 5. Param vs Label vs extraMeta — chống lú + chống lách

| | Trả lời câu | Hình dạng cú pháp | Server dùng để |
|---|---|---|---|
| **Param** | "Chuyện gì xảy ra, đo bao nhiêu?" | Đối số CÓ TÊN, CÓ KIỂU của method `On...` | Tính toán (sum/avg/funnel) |
| **Label** | "Instance này THUỘC LOẠI nào?" | `Label.Turn/Label.Session(LabelKey, value)` | Filter/segment |
| **extraMeta** | Param game-specific CHƯA vào hợp đồng | Dictionary trên param object | Lưu event_extra_props, cook hồi tố khi đăng ký |

Litmus test: muốn **tính** trên nó → param; chỉ muốn **lọc** theo nó → label.
(A/B variant KHÔNG phải label — RemoteConfig tự bơm vào user params rồi.)

### Chống "khôn lỏi" nhét param qua label — 4 tầng (đã implement)

1. **Vocab đóng**: label khai báo kèm tập giá trị hữu hạn
   (`new LabelKey("turn_type", "tutorial", "normal", "liveops")`); giá trị ngoài tập →
   warning + drop tại client. Số đo (giá trị liên tục) về mặt cơ học KHÔNG đi qua nổi tập đóng.
2. **Cardinality guard** (`LabelGuardState`): SDK đếm distinct value per key per session; vượt
   20 → warning "key này có hành vi của param" + ngừng gửi key đó tới hết phiên. Hàng rào này
   tồn tại cho những key khai TRỐNG vocab — key có vocab thì không bao giờ chạm tới.
3. **Lách không được gì**: server drop key vô danh + DQ đếm điểm danh game (hợp đồng §B3) —
   data lách rơi hố đen VÀ bị nhìn thấy.
4. **Đường đúng rẻ hơn**: so sánh cho dev thấy —

| Đường | Kết cục |
|---|---|
| Nhét qua label | Drop tại client hoặc server — mất sạch, bị điểm danh |
| Nhét qua `extraMeta` | Data ĐƯỢC LƯU, xin hợp đồng sau là cook hồi tố được |
| Xin hợp đồng | 1 dòng đăng ký, cột dựng chờ sẵn, số tự chảy |

## 6. Phạm vi V1 (đã implement) & việc còn chờ

**V1**: entity level — `LevelTurnState` + cache + 2 pha, `TurnCustomInfoRepository` stamp
`playTurnId` lên mọi log, facade `FalconBigDataController.Level` (OnStart/OnPass/OnFail), field hợp
đồng §D (`movesLimit`, `timeLimitSec` — bundle chung; `failReason` — chỉ Fail, enum vocab
`out_of_moves/out_of_time/died/quit`). Field mới theo §H4: nullable + `FKey(RemoveIfNull)` +
KHÔNG đi qua clamp/Unknown (field cũ giữ nguyên CorrectValues — đổi giữa chừng gãy baseline).

**Đã thêm ngoài hợp đồng (cần đăng ký key với loader)**: `winStreak` / `loseStreak` trên
level log — SDK tự tính (luật §H3), xuyên level, persist qua IDataPool; Pass/Fail cập nhật
(mỗi turn+status đếm 1 lần, double-fire không lệch), Start/HeartBeat mang streak trước trận.
Ứng viên tương lai CÓ TRIGGER (không xin trước): nâng streak thành central user param trên
mọi log khi có demand phân tích frustration-purchase / rescue-ad — cơ chế sẵn
(IFCustomInfoRepository đọc từ 2 key dataPool), chỉ chờ demand + hợp đồng.

**§D3 IapOfferParam (đã implement)**: `offerPrice` + `offerCurrencyCode` — giá LOCAL +
currency code từ store, server convert USD (⚠ feedback loader: hợp đồng ghi `offer_price_usd`
là chưa khả thi phía client — client không biết tỷ giá; đề nghị sửa hợp đồng thành cặp
local price + currency, dùng chung pipeline FX của iap_purchase). `isPurchased` — lý tưởng
SDK-core điền khi flow mua qua module IAP (chưa có, nằm trong doc request cho owner IAP);
tạm thời game tự điền, không biết thì để null.

**Label API (đã implement §B3)**: `Label.Session` / `Label.Turn` / `Label.AdView(type)` /
`Label.IapOffer` / `Label.IapPurchase`
→ `f_sdk_label_assign_data` (⚠ event-id do client tự đặt theo nếp sẵn có — **cần loader xác nhận**).
Cả 4 tầng chống lách ở §5 đều có mặt: vocab đóng trong `LabelKey`, cardinality guard
(`LabelGuardState`, 20 giá trị/key/phiên rồi ngừng gửi key đó), key vô danh server tự drop, và
cảnh báo chỉ thẳng đường đúng (param/extraMeta). `Label.Turn` lúc không có lượt mở thì bỏ qua kèm
cảnh báo — nhãn không có `playTurnId` thì server không biết dán vào đâu.

**Chờ/việc tiếp**:
- Xin team loader: `gold-rollups.md` §17 ("3 lớp entity") để chuẩn hoá taxonomy trước khi
  nhân pattern ra entity khác.
- **Xin loader mở 3 instance-label**: `f_sdk_ad_view_label` / `f_sdk_iap_offer_label` /
  `f_sdk_iap_purchase_label` (đổi tên 12/08 cho khớp họ `f_sdk_iap_*`, TRƯỚC khi game nào ship — đã báo loader). Client đã gửi sẵn; id nằm trong param (SDK đã sinh từ trước), cook dùng
  chung câu `argMax` theo instance như session/turn — nên chi phí phía loader chỉ là mapping rule.
  ⚠ Giá trị thấp có chủ đích: ba instance này sống vài giây tới ~30 giây, nhãn thường thuộc về
  PHIÊN hoặc NGƯỜI CHƠI đúng hơn. Mở khi có nhu cầu thật.
- Doc request đã viết: `Request-Ump-PermissionLog.md` (owner module `Core/ThirdParty/Ump` — ATT +
  ads_consent, §D8).
- Doc request per module (owner khác): Mediation (ad_request §B, 3 param §D2 — carry §C đã
  tự có nhờ stamping), InAppPurchase (§D4 checkout funnel + isPurchased snapshot).

## 7. Ghi chú naming gửi kèm loader

Key gửi camelCase (server tự normalize key) — nhưng **acronym không viết hoa liền**
(`campaignId`, không `campaignID` — converter tách `ID` → `_i_d`). VALUE không được
normalize — enum vocab phải đúng nguyên văn hợp đồng (LevelStatus PascalCase,
failReason snake per §D — SDK dùng `[EnumMember]` đảm bảo, dev không gõ tay).
