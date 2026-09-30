
# Module `FalconAnalytics`

## Tổng quan

* **Tên:** `FalconAnalytics`
* **Giới thiệu:** Module thu thập, quản lý và phân tích hành vi người chơi trong game. Hỗ trợ ghi log theo chuẩn, log tự động, và log tùy chỉnh nhằm phục vụ cho hệ thống phân tích dữ liệu lớn.
* **Các thành phần chính:**
  * `AFalconLog`: Lớp cơ sở chuẩn hóa cấu trúc các log.
  * Các hàm log thủ công: Ghi nhận sự kiện như level, quảng cáo, funnel, v.v.
  * Hệ thống log tùy chỉnh: Tự tạo lớp log phù hợp nhu cầu riêng biệt.

---

## Cấu trúc thư mục

Module này chia theo **ENTITY trước, LỚP sau** — ngược với nếp Model/Repository/Service của các
module khác. Lý do: ở cỡ 120+ file, trục hay-đổi-cùng-nhau là entity chứ không phải lớp (thêm một
param cho ad thì chạm 2 file ad; đổi thiết kế label thì chạm 7 file label — chưa lần nào phải sửa
"tất cả Params" hay "tất cả Services"). Chia theo lớp ở ngoài thì một entity nằm rải 5–6 thư mục.

```
Runtime/
├── FalconBigDataController.cs   cửa vào duy nhất cho dev game (10 nhóm API)
├── Pipeline/                    hạ tầng dùng chung: log base, param base, hàng đợi,
│                                decor registry, gửi batch — KHÔNG thuộc entity nào
├── Config/                      remote config + logger của module
└── Entities/                    mỗi entity một thư mục
      Level/  Ad/  Iap/  Session/  Funnel/  Label/  Resource/  Permission/  Common/  Custom/
```

**Luật 4 lớp vẫn giữ nguyên** — chỉ đổi thứ tự lồng. Trong mỗi entity, thư mục lớp
(`Model/` `Params/` `Messages/` `Services/` `Api/` `Vocab/`) **chỉ mọc khi lớp đó có từ 2 file trở
lên**; ít hơn thì để phẳng, vì tên file đã nói rõ lớp (`*State`/`*Type` = model thuần,
`*Param`, `F*Log`, `*Service`, `*Repository`, `F*Api`).

Luật quan trọng nhất của 4 lớp không đụng tới: **model phải thuần** — không IO, không DI — để test
được mà không phải fake `IDataPool` (`LevelTurnState`, `EntityLabelState`, `LabelGuardState`,
`CommonParamState`, `PerfSampleState` đều tách ra khỏi service vì lý do đó).

Namespace vẫn phẳng (`Falcon.Modules.Core.BigData`) cho mọi file, nên thư mục thuần tuý là chuyện
sắp xếp — đổi chỗ file không đụng compile.

---

## Quick Start — log qua MỘT cửa

Từ 1.3.0, mọi log đi qua `FalconBigDataController.<Nhóm>.<Hàm>` — dev báo **khoảnh khắc**, SDK lo
dựng event, sinh id, cache tham số, tính counter:

```csharp
// Lượt chơi màn — SDK tự sinh playTurnId, cache định danh cho các mốc sau
FalconBigDataController.Level.OnStart(currentLevel: 42, difficulty: "hard");
FalconBigDataController.Level.OnPass(score: 15300, duration: TimeSpan.FromMinutes(2));

// Giao dịch tài nguyên nhiều vế — SDK sinh exchangeId chung
FalconBigDataController.Resource.OnExchange(sinkGold, sourceBooster);

// Nhãn / tham số động
FalconBigDataController.Label.User("vip_tier", "gold");

// Tham số đi theo MỌI log
FalconBigDataController.Common.Set("guild_id", "g_1207", persist: true);
```

10 nhóm: `Level` · `Ad` · `IapOffer` · `Iap` · `Resource` · `Funnel` · `Label` · `Common` ·
`App` · `Player`. Trong đó `Ad`/`Iap`/`App` thường do module Mediation/IAP/Push gọi hộ —
dev game chủ yếu đụng `Level`, `IapOffer`, `Resource`, `Funnel`, `Label`, `Common`.

📖 **Log thế nào cho đúng — đọc [TrackingGuide.md](docs/design/TrackingGuide.md)** (5 phút): recipe từng nhóm,
vocab hằng số, litmus param-vs-label, và bảng lỗi kinh điển.

Đường log cũ (`new FLevelLog(...).Send()`) vẫn chạy nguyên — controller là lớp vỏ gọi xuống cùng
pipeline, code cũ không phải sửa.

### Gửi log tùy chỉnh

```csharp
[Serializable]
public class MyCustomLog : AFalconLog {
    public string action;
    public int value;
}

new MyCustomLog { action = "SpecialEvent", value = 99 }.Send();
```

---

## Tài liệu

Thư mục `docs/` chia theo NGƯỜI ĐỌC: `design/` nền tảng sống lâu · `modules/` thư gửi owner
module SDK · `games/` hướng dẫn tracking feature cho team game · `loader/` thư gửi data team
(xoá sau khi ack, bản lưu ở git history).

| File | Cho ai / để làm gì |
|---|---|
| [docs/design/TrackingGuide.md](docs/design/TrackingGuide.md) | **dev game** — cách gọi log đúng, recipe + lỗi kinh điển |
| [docs/design/DWH-EventLog-API-Spec.md](docs/design/DWH-EventLog-API-Spec.md) | tra cứu field trên wire của từng event |
| [docs/design/EntityLifecycle-Design.md](docs/design/EntityLifecycle-Design.md) | thiết kế: vòng đời entity, luật param/label, vì-sao của mọi quyết định |
| [CHANGELOG.md](CHANGELOG.md) | lịch sử thay đổi theo bản |
| [docs/modules/Request-Mediation-AdLifecycle.md](docs/modules/Request-Mediation-AdLifecycle.md) | owner **Mediation** — nối vòng đời ad view |
| [docs/modules/Request-Iap-CheckoutFunnel.md](docs/modules/Request-Iap-CheckoutFunnel.md) | owner **IAP** — nối phễu checkout |
| [docs/modules/Request-Push-OpenSource.md](docs/modules/Request-Push-OpenSource.md) | owner **Push/Deeplink** — báo nguồn mở app |
| [docs/modules/Request-Ump-PermissionLog.md](docs/modules/Request-Ump-PermissionLog.md) | owner **UMP** — ATT/consent |
| [docs/modules/Request-Inventory-ResourceFlow.md](docs/modules/Request-Inventory-ResourceFlow.md) | owner **Inventory/Shop/Lives** — nối dòng tài nguyên |
| [docs/games/Request-Game-MasterLeague.md](docs/games/Request-Game-MasterLeague.md) | team game — log event Master League |
| [docs/games/Request-Game-RaceEvent.md](docs/games/Request-Game-RaceEvent.md) | team game — log Race Event (đua bot 3 chặng) |
| [docs/loader/](docs/loader/) | thư đang chờ data team ack — đọc xong xoá |

---

## Danh sách đầy đủ API



### `AFalconLog`

* Kế thừa từ `PlainLog`, chứa các thông tin chuẩn như `apiId`, `uuid`, `time`, ...
* Các lớp log con kế thừa từ đây sẽ tự động được xử lý bởi `BaseLogDecorService`.

### `SessionCheckService`

* Kiểm tra retention mỗi phút, tự động gửi `FRetentionLog`.
* Khi app pause, tự động gửi `FSessionLog` tổng thời gian chơi (`USER_TOTAL_TIME`) qua cơ chế `IAppPauseLogGenerator`.
* Chống kill cứng: định kỳ gửi thêm `FSessionLog` bảo hiểm cho phần thời gian chơi chưa log, với khoảng cách lũy tiến 1 → 16 phút tính từ log gần nhất (~2-3 log thêm mỗi session). Log lúc pause tự trừ phần đã gửi bảo hiểm nên tổng thời gian không bị đếm đôi.

### Các log định nghĩa sẵn

Không có service log tập trung — mỗi loại log là 1 class, khởi tạo rồi gọi `.Send()` (vào queue, gửi theo batch) hoặc `.SendNow()` (gửi ngay):

* `new FLevelLog(ALevelParamV2 param)`: Log level — param V2 theo status: `LevelStartParamV2`, `LevelPassParamV2`, `LevelFailParamV2`, `LevelHeartBeatParamV2`.
* `new FAdLog(AdParam)`: Log quảng cáo.
* `new FInAppLog(InAppParam)`: Log giao dịch mua hàng.
* `new FIapOfferLog(IapOfferParam)`: Log IAP offer.
* `new FResourceLog(ResourceParam)`: Log thay đổi tài nguyên.
* `new FSessionLog(SessionParam)`: Log phiên chơi.
* `new FFunnelLog(FunnelParam)` / `new FFilteredFunnelLog(funnelName, action, priority, ...)`: Log tiến trình phễu (funnel).
* `new FPropertyLog(pName, pValue, priority, ...)`: Log thuộc tính tùy chỉnh.
* `new FEventLog(eventName, param, ...)`: Log sự kiện tùy chỉnh.

---

## Chi tiết
---
Tham khảo tại link https://falcon-game-studio.gitbook.io/falcon-bigdata-1/vietnamese/falcon-analytics/1.-cac-ham-log-co-ban
