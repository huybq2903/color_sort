# Tracking Endless Treasure — TODO cho dev + tham chiếu log

> **Dev chỉ cần đọc phần TODO ngay dưới đây** — chép class mẫu, gọi đúng chỗ, kiểm theo hướng dẫn.
> Tick `[x]` khi xong. Giải thích "vì sao" và công thức cho team data nằm ở nửa dưới.
> Viết 22/09/2026. Cần BigData ≥ 1.3.9 (Ui API + hằng `FIapPlacement.EndlessTreasure`).

Endless Treasure là **gói bán IAP dạng chuỗi** (Stage free → bundle trả phí → free…), không phải
event chơi level → **không** gắn gì lên log level. Trục chính: IAP Offer + IAP + Resource.

**Luật số một: MỌI con số đọc từ config / data của game.** Giờ bắt đầu/kết thúc đợt, đợt thứ mấy,
Stage nào, luật lặp Stage — dev lấy từ config/data thật rồi truyền vào. **Không** tự cộng 168h,
**không** tự tính luật lặp: mỗi game cấu hình một kiểu, server chỉ đọc lại số dev gửi.

# ✅ TODO

## A. Dev Endless Treasure (module hoặc game — ai viết tính năng này)

### [ ] A1 — Chép class tracking vào project

Chép nguyên class dưới đây (không cần sửa gì). Dev chỉ **gọi** các hàm của nó ở các bước sau.

```csharp
using System;
using System.Collections.Generic;
using Falcon.Modules.Core.BigData;

public static class EndlessTreasureTracking
{
    public const string FUNNEL_NAME   = "event_endless_treasure";
    public const string RESOURCE_WHEN = "event_endless_treasure";
    public const string OFFER_ID      = "endless_treasure";
    public const string SURFACE_ICON  = "endless_treasure_icon";

    public const string WHERE_FREE_STEP = "free_step";
    public const string WHERE_PAID_STEP = "paid_step";

    public const string KEY_CYCLE_ID = "endless_treasure_cycle_id";
    public const string KEY_PROGRESS = "endless_treasure_progress";
    public const string KEY_STAGE    = "endless_treasure_stage";

    /// <summary>Thông tin ĐỢT — mọi số đọc từ config/data của game.</summary>
    public struct Cycle
    {
        public string id;       // id đợt do game/server cấp; không có thì dùng ngày bắt đầu đợt "yyyy-MM-dd"
        public int index;       // đợt thứ mấy của user (1, 2, 3…)
        public long startTs;    // giờ đợt bắt đầu, epoch ms UTC — đọc từ data, KHÔNG tự tính
        public long endTs;      // giờ đợt kết thúc, epoch ms UTC — đọc từ data, KHÔNG tự tính
    }

    /// <summary>Vị trí của user trong chuỗi.</summary>
    public struct Position
    {
        public int progress;    // bậc thứ mấy trong ĐỢT (1, 2, …, 40, 41…) — tăng mãi, về 1 khi reset đợt
        public int stage;       // Stage trong CONFIG (vd lặp 34→39 thì bậc 41 có stage = 35)
    }

    // ── Popup ─────────────────────────────────────────────────────────────

    /// <summary>Popup hiện ra (MỖI lần hiện).</summary>
    public static void OnPopupShown(Cycle cycle, Position pos, string nextPaidProductId,
        string triggerEvent, OfferTriggerType triggerType)
    {
        FalconBigDataController.Funnel.OnStep(new FunnelParam
        {
            funnelName = FUNNEL_NAME,
            cycleId = cycle.id,
            action = "open",
            priority = 10,
            cycleIndex = cycle.index,
            cycleStartTs = cycle.startTs,
            cycleEndTs = cycle.endTs
        });

        FalconBigDataController.IapOffer.OnShown(new IapOfferParam
        {
            offerId = OFFER_ID,
            offerCategory = "progressive",
            offerLayout = "chain",
            offerSurfaceType = OfferSurfaceType.Popup,
            placementScene = "lobby",
            triggerType = triggerType,
            triggerEvent = triggerEvent,
            discountRate = 0f,
            offerProductId = string.IsNullOrEmpty(nextPaidProductId) ? null : new[] { nextPaidProductId },
            extraMeta = Meta(cycle, pos)
        });
    }

    /// <summary>User bấm nút mua trên popup.</summary>
    public static void OnPopupClicked() => FalconBigDataController.IapOffer.OnClicked();

    /// <summary>Popup đóng (tắt, mua xong, rời màn hình).</summary>
    public static void OnPopupClosed() => FalconBigDataController.IapOffer.OnClosed();

    // ── Icon Lobby ────────────────────────────────────────────────────────

    public static void OnIconShown(Cycle cycle) =>
        FalconBigDataController.Ui.OnImpression(SURFACE_ICON, CycleMeta(cycle));

    public static void OnIconClicked(Cycle cycle) =>
        FalconBigDataController.Ui.OnClicked(SURFACE_ICON, CycleMeta(cycle));

    // ── Nhận quà ──────────────────────────────────────────────────────────

    /// <summary>
    /// Ngữ cảnh cho MỘT lần nhận quà của một Stage — dùng chung cho mọi món của Stage đó.
    /// isPaid = true với nội dung bundle trả phí; transactionId của giao dịch IAP nếu lấy được.
    /// </summary>
    public static ResourceParam RewardContext(Cycle cycle, Position pos, bool isPaid,
        string transactionId = null)
    {
        return new ResourceParam
        {
            resourceWhen = RESOURCE_WHEN,
            resourceWhere = isPaid ? WHERE_PAID_STEP : WHERE_FREE_STEP,
            exchangeId = Guid.NewGuid().ToString("N"),
            transactionId = transactionId,
            detail = Meta(cycle, pos)
        };
    }

    // ── Nội bộ ────────────────────────────────────────────────────────────

    private static Dictionary<string, object> Meta(Cycle cycle, Position pos) => new()
    {
        [KEY_CYCLE_ID] = cycle.id,
        [KEY_PROGRESS] = pos.progress,
        [KEY_STAGE] = pos.stage
    };

    private static Dictionary<string, object> CycleMeta(Cycle cycle) => new()
    {
        [KEY_CYCLE_ID] = cycle.id
    };
}
```

### [ ] A2 — Popup: gọi 3 hàm

```csharp
// Lúc popup HIỆN (mọi nguồn: lần đầu sau lv21, reset đợt, đầu session, sau khi thắng mỗi N giây, bấm icon)
EndlessTreasureTracking.OnPopupShown(cycle, pos,
    nextPaidProductId: skuCuaBacTraPhiKeTiep,        // SKU bundle user đang đứng trước
    triggerEvent: "session_start",                   // bảng giá trị bên dưới
    triggerType: OfferTriggerType.SystemTriggered);  // bấm icon mở → UserInitiated

// User bấm nút MUA trên popup
EndlessTreasureTracking.OnPopupClicked();

// Popup ĐÓNG (tắt / mua xong / rời màn)
EndlessTreasureTracking.OnPopupClosed();
```

| Popup hiện vì | `triggerEvent` | `triggerType` |
|---|---|---|
| Lần đầu sau khi thắng lv mở khoá | `unlock` | `SystemTriggered` |
| Đợt mới bắt đầu (reset) | `cycle_reset` | `SystemTriggered` |
| Đầu session | `session_start` | `SystemTriggered` |
| Sau khi thắng level (mỗi N giây) | `level_win_interval` | `SystemTriggered` |
| User bấm icon Lobby | `icon_click` | `UserInitiated` |

⚠ `nextPaidProductId` **bắt buộc đúng SKU** của bundle đang bày — SDK dựa vào nó để tự nối lần
popup này với giao dịch mua (conversion). Stage đang là quà free thì truyền SKU của bundle trả phí
kế tiếp; không còn bundle nào thì `null`.
**Kiểm:** mở popup rồi tắt → một dòng `f_sdk_iap_offer_data` `offerId = endless_treasure`, có
`endless_treasure_stage`; mua trong popup → dòng `f_sdk_in_app_data` mang `offerImpressionId`.

### [ ] A3 — Icon Lobby: gọi 2 hàm

```csharp
// Icon HIỆN RA với user (OnEnable của icon, khi đang trong đợt) — gọi thoải mái, SDK tự gộp cụm
EndlessTreasureTracking.OnIconShown(cycle);

// User BẤM icon (rồi mở popup → A2 với triggerEvent = "icon_click")
EndlessTreasureTracking.OnIconClicked(cycle);
```

Chưa biết đợt (config chưa về) thì **đừng gọi** — log thiếu `endless_treasure_cycle_id` là không
quy được về đợt nào.

### [ ] A4 — Mua bundle: truyền `where`

Chỗ gọi hàm mua của module IAP, truyền placement:

```csharp
// where / placement của lượt mua:
FIapPlacement.EndlessTreasure        // = "endless_treasure"
```

Module IAP tự log `Iap.OnStarted` / `OnPurchased` như mọi gói khác — dev Endless Treasure **không**
gọi thêm. Đợt / Stage của giao dịch server lấy qua `offerImpressionId` (A2), không cần gắn lên log
mua.
**Kiểm:** dòng `f_sdk_in_app_data` của bundle có `where = endless_treasure`.

### [ ] A5 — Nhận quà: chọn đúng trường hợp

Tạo context **một lần cho một Stage**, dùng chung cho mọi món quà của Stage đó:

```csharp
var ctx = EndlessTreasureTracking.RewardContext(cycle, pos, isPaid: false);          // Stage free
var ctx = EndlessTreasureTracking.RewardContext(cycle, pos, isPaid: true, txId);     // bundle trả phí
```

Rồi cộng quà theo **đúng một** trong hai trường hợp:

**Trường hợp 1 — Endless Treasure tự cộng quà vào kho** (quà free, hoặc bundle mà code Endless
Treasure tự cộng sau khi mua xong):

```csharp
// Cộng thẳng vào kho:
ResourceCollector.Instance.ResourceAdd(resourceId, amount, data, "endless_treasure", param: ctx);

// Hoặc qua bộ phát thưởng của Events Core:
EventRewardGranter.Grant(reward, "endless_treasure", context: ctx);
```

Cần GameData ≥ 1.1.7 (và Events Core legacy bản có `Grant(..., context)` nếu dùng dòng thứ hai).

**Trường hợp 2 — Nội dung bundle do module Packs cộng** (bundle làm bằng `APackElement`): Packs
hiện **không có chỗ nhận context** → phải chờ owner Packs làm thư
`docs/modules/Request-Packs-RewardContext.md`. Sau khi Packs có bản mới, pack Endless Treasure
override:

```csharp
protected override ResourceParam GetRewardContext() =>
    EndlessTreasureTracking.RewardContext(cycle, pos, isPaid: true);
```

⚠ **Một món quà chỉ MỘT dòng resource.** Đã đi qua kho (trường hợp 1 hoặc 2) thì **không** gọi
thêm `FalconBigDataController.Resource.OnEarned` — gọi thêm là đếm đôi.
**Kiểm:** nhận Stage free có 2 món → đúng 2 dòng `f_sdk_resource_data`, mỗi dòng
`resourceWhen = event_endless_treasure`, `resourceWhere = free_step`, có `detail.endless_treasure_stage`.

### [ ] A6 — Tự kiểm lại luật "số đọc từ config"

- `cycle.startTs` / `cycle.endTs` lấy từ data đợt thật, **không** `now + 168h`.
- `pos.stage` lấy từ config Stage đang hiển thị, **không** tự tính `34 + (n − 34) % 6`.
- `pos.progress` về **1** khi đợt reset; `cycle.index` tăng 1.

## B. Owner module Packs — chỉ khi trường hợp 2 ở A5

### [ ] P1 — Làm thư `docs/modules/Request-Packs-RewardContext.md`

Thêm hook `GetRewardContext()` vào `APackElement` + payload mới chở context + `GameDataConnector`
truyền `param` xuống kho. Chi tiết từng dòng trong thư.

## C. BigData — đã xong

- [x] Hằng `FIapPlacement.EndlessTreasure = "endless_treasure"` (BigData 1.3.9).

## D. Báo data team / loader

### [ ] D1 — Đăng ký vocab

- Funnel `event_endless_treasure` (shape `once_per_cycle`, action `open`).
- `offerId = endless_treasure`, `offerCategory = progressive`, `offerLayout = chain`.
- Placement IAP `endless_treasure` (thêm giá trị vào hợp đồng placement — loader ký).
- `resourceWhen = event_endless_treasure`; `resourceWhere = free_step` / `paid_step`.
- Surface Ui `endless_treasure_icon`.
- Key: `endless_treasure_cycle_id`, `endless_treasure_progress`, `endless_treasure_stage`.

---

# 📖 Giải thích & tham chiếu (không cần đọc để sửa)

## Vì sao thiết kế như vậy

| Quyết định | Lý do |
|---|---|
| Không log level, không funnel tiến độ | Endless Treasure không có ván chơi riêng. Tiến độ chuỗi suy từ dòng nhận quà (Stage free) và dòng mua (bundle) — log thêm funnel là trùng |
| Funnel chỉ có bước `open` | Chỗ duy nhất ghi **lịch đợt** bằng field chuẩn (`cycleIndex` / `cycleStartTs` / `cycleEndTs`) — một lần mỗi đợt (SDK tự nén). Các log khác chỉ cần `endless_treasure_cycle_id` để nối về |
| Popup đi `IapOffer`, icon đi `Ui` | Popup là lần hiển thị chủ đích → mỗi lần một dòng, có thời lượng, tự nối giao dịch. Icon nằm yên trên màn hình → log mỗi lần hiện là quá nhiều dòng; `Ui` gộp cụm theo ngày |
| Hai số `progress` + `stage` | `progress` = độ sâu (user đi được bao xa, dừng ở đâu). `stage` = món nào trong config. Luật lặp (34→39 hay kiểu khác) nằm hết trong config game, server không cần biết |
| Mọi số đọc từ config/data | Mỗi game một lịch, một luật lặp. Hardcode ở server hay ở class tracking là lệch câm khi game đổi config |
| `where` trên log mua, không gắn đợt/Stage | Module IAP log giao dịch; đợt/Stage đã nằm trên dòng popup, nối qua `offerImpressionId`. SKU dùng riêng nên `productId` cũng tự nhận ra gói |
| Tiền tố `endless_treasure_` | Luật tiền tố live-op (`LiveOpsTrackingPattern.md` §1) — tránh đụng key event khác |

**`itemType` / `itemId` trên dòng resource là cột legacy** — GameData đang điền bằng tham số
`where`, không dùng để phân tích. "Type" của quà đọc từ `currency` (= id tài nguyên).

## Chỉ số GD → log

| # | Chỉ số | Cách tính |
|---|---|---|
| 1 | Impression + CTR theo ngày | Popup: đếm dòng `f_sdk_iap_offer_data` `offerId = endless_treasure`; CTR = dòng `isClicked = true` / tổng. Icon: `f_sdk_ui_impression` `surfaceId = endless_treasure_icon`, `sum(count | click) / sum(count | impression)`. Tách popup theo `triggerEvent` để biết nguồn nào hiệu quả |
| 2a | Số lượng + % từng gói lẻ theo đợt | `f_sdk_in_app_data` `where = endless_treasure` GROUP BY `productId`, đợt (qua `offerImpressionId` → `endless_treasure_cycle_id`, hoặc tuần của giao dịch) |
| 2b | Rev từng gói lẻ theo đợt | Như trên, `SUM(localizedPrice)` quy USD |
| 2c | Rev Endless Treasure so với tổng IAP tuần | Rev `where = endless_treasure` / Rev toàn bộ `f_sdk_in_app_data` cùng tuần |
| 3 | Source (theo type) so với toàn bộ source | `f_sdk_resource_data` `flowType = Source`: phần `resourceWhen = event_endless_treasure` / toàn bộ, GROUP BY `currency`. Tách free / trả phí bằng `resourceWhere` |
| 4 | First-time purchase so với gói khác | Giao dịch đầu đời của mỗi user rơi vào `where` nào. Conversion = distinct user mua / distinct user có popup (dòng offer) |
| 5 | Tỉ lệ mua lại theo tuần | User mua ở đợt N có mua ở đợt N+1 không (đợt qua `endless_treasure_cycle_id` / `cycleIndex`) |
| 6 | Tỉ lệ mua gói value cao hơn theo tuần | Theo user qua các tuần: giá (`localizedPrice`) hoặc `endless_treasure_stage` cao nhất đã mua |

## Main concern → cách đo

| Concern | Đo | Giới hạn |
|---|---|---|
| 1. Payrate thấp do thiết kế / value | Phễu popup hiện → bấm (`isClicked`) → bắt đầu mua (`f_sdk_iap_start_purchase_data`) → mua xong; kèm `max(endless_treasure_progress)` mỗi đợt = user dừng ở bậc nào | Tách được "không bấm" với "bấm mà không mua"; tách thiết kế với value cần GD thử biến thể |
| 2. High value payer chờ gói rẻ | Theo user qua các tuần: giá trung bình / cao nhất của **toàn bộ** giao dịch, trước và sau khi mở khoá | Cần vài tuần dữ liệu |
| 3. Cannibal gói khác | Doanh thu gói khác của user đã thấy Endless Treasure, trước / sau mở khoá | ⚠ **Không kết luận được nếu thiếu nhóm đối chứng** — user qua lv mở khoá tự nhiên chi khác. Đề xuất **A/B holdout** (tắt Endless Treasure cho 10–20% user đủ điều kiện, dùng nhãn AB sẵn có trên mọi log) |

## Thông tin GD đã chốt (22/09)

- Đợt dài một tuần; mỗi đợt reset về Stage 1, quà free nhận lại được.
- Chuỗi vô tận, lặp lại một đoạn Stage (bản hiện tại: 34 → 39).
- SKU các bundle dùng riêng, không chung với gói khác.
- Lobby có icon riêng.

Các số trên là **cấu hình của bản hiện tại** — tracking không dựa vào chúng, dev luôn đọc từ config
(A6).
