# DWH Event-Log API — Đặc tả cấu trúc dữ liệu

Tài liệu mô tả **hợp đồng dữ liệu (wire contract)** để gửi log analytics lên DWH. Mục đích: một ứng dụng/SDK khác có thể tự dựng lại bản tin mà không cần đọc source Unity.

> Mọi tên trường nêu trong tài liệu là **key JSON thật** xuất hiện trên đường truyền.

---

## 1. Endpoint

| Mục đích | Method | URL |
|---|---|---|
| Gửi 1 log | `POST` | `https://dwhapi-v2.data4game.com/event-log-v2` |
| Gửi nhiều log (batch) | `POST` | `https://dwhapi-v2.data4game.com/batch/event-log-v2` |

- `Content-Type: application/json`
- Nếu danh sách batch chỉ có **1 phần tử**, phía gửi tự chuyển sang endpoint đơn (`/event-log-v2`).

---

## 2. Bao bì bản tin (Envelope)

Đơn vị log gửi đi luôn là một **envelope** gồm đúng 5 trường:

| Key | Kiểu | Mô tả |
|---|---|---|
| `data` | **string** | **Chuỗi JSON đã được encode** chứa toàn bộ nội dung event (xem mục 4). Đây là JSON lồng dạng string, KHÔNG phải object. |
| `clientSendTime` | long | Thời điểm client gửi, epoch milliseconds (UTC). |
| `event` | string | Định danh loại event, ví dụ `f_sdk_ads_data` (xem mục 5). |
| `packageName` | string | Package/bundle id của ứng dụng. |
| `platform` | string | Nền tảng, ví dụ `Android`, `iOS`. |

### 2.1. Request đơn — `/event-log-v2`

Body = **một** envelope:

```json
{
  "data": "{\"event\":\"f_sdk_ads_data\",\"adWhere\":\"main_menu\", ... }",
  "clientSendTime": 1750838400000,
  "event": "f_sdk_ads_data",
  "packageName": "com.falcon.mygame",
  "platform": "Android"
}
```

### 2.2. Request batch — `/batch/event-log-v2`

Body = **một mảng JSON các chuỗi**, mỗi phần tử là một envelope đã được serialize thành string (lồng escape nhiều lớp):

```json
[
  "{\"data\":\"{\\\"event\\\":\\\"f_sdk_ads_data\\\", ...}\",\"clientSendTime\":1750838400000,\"event\":\"f_sdk_ads_data\",\"packageName\":\"com.falcon.mygame\",\"platform\":\"Android\"}",
  "{\"data\":\"{\\\"event\\\":\\\"f_sdk_level_data\\\", ...}\",\"clientSendTime\":1750838400500,\"event\":\"f_sdk_level_data\",\"packageName\":\"com.falcon.mygame\",\"platform\":\"Android\"}"
]
```

> ⚠️ Lưu ý lồng escape: ở batch có **3 lớp**:
> 1. Mảng JSON ngoài cùng.
> 2. Mỗi phần tử là string của envelope.
> 3. Bên trong envelope, `data` lại là string của object event.

---

## 3. Quy ước serialize (BẮT BUỘC tuân thủ để khớp dữ liệu)

| Quy ước | Chi tiết |
|---|---|
| **Trường null bị loại bỏ** | Các trường optional khi null sẽ **không xuất hiện** trong JSON (không gửi `null`). Trường bắt buộc luôn có. |
| **Enum → string** | Mọi enum serialize bằng **tên** (ví dụ `Interstitial`, `Pass`, `Source`), không phải số. |
| **TimeSpan → giây (số nguyên)** | Các trường thời lượng (`duration`, `sessionTime`, `impressionDuration`) được quy đổi thành **tổng số giây** kiểu integer. |
| **Ngày giờ → chuỗi** | Định dạng `yyyy-MM-dd HH:mm:sszzz` (ví dụ `2026-06-26 14:30:00+07:00`). |
| **Ưu tiên khi trùng key** | Các trường **base + central user params** (mục 4.1, 4.2) được ghi vào trước theo cơ chế "không ghi đè". Nếu một trường riêng của event trùng tên thì **giá trị của lớp base/user-param thắng**. |
| **`event`** | Bên trong `data` cũng lặp lại key `event` = đúng giá trị của envelope. |

---

## 4. Cấu trúc bên trong `data`

Object trong `data` gồm **3 lớp** ghép lại. Lớp 1 và 2 có mặt ở **mọi** event; lớp 3 là phần riêng theo từng `event`.

### 4.1. Lớp BASE — luôn có ở mọi event

| Key | Kiểu | Mô tả |
|---|---|---|
| `event` | string | Định danh event (trùng envelope). |
| `logTypeCreateId` | int | Số thứ tự log loại này được **tạo** trên thiết bị (đếm tăng dần từ 0 theo từng `event`). |
| `logTypeSendId` | int | Số thứ tự log loại này được **gửi** (đếm tăng dần từ 0 theo từng `event`). |
| `uuid` | string | UUID sinh cho mỗi log (chống trùng). Server dedup theo `uuid` trong **time-window 1 tiếng** — client gửi lại (retry/re-enqueue) trong vòng 1 tiếng là an toàn; gửi lại muộn hơn có thể bị tính trùng. |
| `createdDateLocal` | string | Thời điểm tạo log theo giờ local, định dạng `yyyy-MM-dd HH:mm:sszzz`. |

### 4.2. Lớp CENTRAL USER PARAMS — luôn có ở mọi event

Khối thông tin user/thiết bị/phiên được chèn vào **mọi** event. Trường nào nguồn dữ liệu null thì bị bỏ (theo mục 3).

**General**

| Key | Kiểu | Mô tả |
|---|---|---|
| `accountId` | string | Id tài khoản người chơi. |
| `level` | int | Level cao nhất đã qua (alias của `maxPassedLevel`). |
| `maxPassedLevel` | int | Level cao nhất đã qua. |
| `installVersion` | string | Phiên bản app lúc cài đặt lần đầu. |
| `advertisingId` | string | Advertising ID (bỏ nếu null). |

**Session**

| Key | Kiểu | Mô tả |
|---|---|---|
| `installDay` | string | Ngày đăng nhập đầu tiên (UTC), `yyyy-MM-dd HH:mm:sszzz`. |
| `installDayLocal` | string | Ngày đăng nhập đầu tiên (local). |
| `activeDays` | int | Số ngày hoạt động. |
| `sessionId` | int/string | Id phiên hiện tại. |
| `sessionUid` | string | Uid phiên hiện tại. |
| `firstLogin` | long | Mốc thời gian đăng nhập đầu (epoch ms). |
| `totalPlayTime` | long | Tổng thời gian chơi, **giây**. |
| `retentionDay` | int | Retention day. |

**Ad**

| Key | Kiểu | Mô tả |
|---|---|---|
| `adLtv` | double | LTV quảng cáo tích lũy (bỏ nếu null). |
| `interAdCount` | int | Số interstitial đã xem. |
| `rewardAdCount` | int | Số reward đã xem. |

**IAP**

| Key | Kiểu | Mô tả |
|---|---|---|
| `firstInAppLv` | int | Level lúc IAP đầu tiên (bỏ nếu null). |
| `firstInAppDateStr` | string | Ngày IAP đầu tiên (bỏ nếu null). |
| `firstInAppProduct` | string | Product IAP đầu tiên (bỏ nếu null). |
| `lastInAppLv` | int | Level lúc IAP gần nhất (bỏ nếu null). |
| `lastInAppDateStr` | string | Ngày IAP gần nhất (bỏ nếu null). |
| `lastInAppProduct` | string | Product IAP gần nhất (bỏ nếu null). |
| `inAppCount` | int | Tổng số IAP. |
| `inAppMax` | number | Giá trị IAP lớn nhất (chỉ có khi có dữ liệu LTV). |
| `inAppTotal` | number | Tổng giá trị IAP (chỉ có khi có dữ liệu LTV). |
| `inAppCurrency` | string | Mã tiền tệ ISO của LTV IAP (chỉ có khi có dữ liệu LTV). |

**App**

| Key | Kiểu | Mô tả |
|---|---|---|
| `platform` | string | Nền tảng. |
| `appVersion` | string | Phiên bản app hiện tại. |

**Device**

| Key | Kiểu | Mô tả |
|---|---|---|
| `deviceId` | string | Id thiết bị. |
| `deviceOs` | string | Hệ điều hành. |
| `deviceName` | string | Tên thiết bị. |
| `deviceModel` | string | Model thiết bị. |
| `screenWidth` | int | Rộng màn hình (px). |
| `screenHeight` | int | Cao màn hình (px). |
| `screenDpi` | number | DPI màn hình. |
| `deviceGpu` | string | Tên GPU. |
| `deviceCpu` | string | Loại CPU. |
| `deviceRam` | int | RAM (MB). |
| `deviceGpuRam` | int | VRAM GPU (MB). |
| `deviceCpuCount` | int | Số nhân CPU. |
| `deviceCpuFrequency` | int | Xung CPU. |
| `language` | string | Ngôn ngữ thiết bị. |
| `idFv` | string | IDFV (iOS). |

### 4.3. Lớp CUSTOM PARAMS — gắn từ MMP / AB-test / nguồn khác

Đây là các param **động**, được gộp thêm vào lớp central user params ở trên. Chúng **chỉ xuất hiện khi có dữ liệu** (ví dụ: chỉ có sau khi nhận được attribution callback từ MMP; chỉ có khi user nằm trong một AB-test). Các key có thể vắng mặt hoàn toàn ở những bản tin đầu phiên.

> Lưu ý: tại một thời điểm thường **chỉ một MMP active** (Adjust *hoặc* AppsFlyer) tùy cấu hình build, nên thường chỉ một trong hai khối dưới đây xuất hiện.

**MMP — Adjust (attribution)** — các key dưới đây bắt nguồn từ payload attribution của Adjust SDK:

| Key | Kiểu | Mô tả |
|---|---|---|
| `network` | string | Ad network nguồn. |
| `campaign` | string | Campaign. |
| `adgroup` | string | Ad group. |
| `creative` | string | Creative. |
| `clickLabel` | string | Click label. |
| `trackerName` | string | Tên tracker. |
| `trackerToken` | string | Token tracker. |
| `costType` | string | Loại chi phí. |
| `costAmount` | number | Giá trị chi phí. |
| `costCurrency` | string | Tiền tệ chi phí. |
| `fbInstallReferrer` | string | Facebook install referrer. |

**MMP — AppsFlyer (conversion data)** — toàn bộ conversion data thô của AppsFlyer được trải phẳng vào; các key thường gặp:

| Key | Kiểu | Mô tả |
|---|---|---|
| `media_source` | string | Nguồn media. |
| `campaign` | string | Tên campaign. |
| `campaign_id` | string | Id campaign. |
| `adgroup` | string | Ad group. |
| `adgroup_id` | string | Id ad group. |
| `af_c_id` | string | Campaign id (AppsFlyer). |
| `af_ad` | string | Ad. |
| `af_status` | string | Trạng thái (`Organic`/`Non-organic`). |
| `is_first_launch` | bool | Lần mở app đầu tiên hay không. |
| `is_retargeting` | bool | Có phải retargeting hay không. |
| `advertising_id` | string | Advertising id. |
| `orig_cost` | string | Chi phí gốc. |
| `af_cost_value` | number | Giá trị chi phí. |
| `af_cost_currency` | string | Tiền tệ chi phí. |
| `af_cost_model` | string | Mô hình chi phí. |
| `cost_cents_USD` | number | Chi phí (cents USD). |

> AppsFlyer trả về dict conversion thô nên **mọi key khác** mà SDK gửi kèm cũng sẽ đi theo, không giới hạn ở danh sách trên.

**AB Testing**

| Key | Kiểu | Mô tả |
|---|---|---|
| `abTestingValue` | string | Giá trị/biến thể AB-test mà user thuộc về. |
| `abTestingVariable` | string | Campaign/biến AB-test đang chạy. |

**Khác**

| Key | Kiểu | Mô tả |
|---|---|---|
| `serverAccountId` | string | Account id phía server (sau khi đăng nhập). |

> Ngoài ra app có thể tự gắn thêm các custom param khác (key động, không cố định).

### 4.4. Lớp PER-EVENT — riêng theo từng `event`

Xem mục 5.

---

## 5. Danh mục event và trường riêng

Có **12** giá trị `event`. Mỗi mục dưới đây chỉ liệt kê **phần trường riêng** (cộng thêm vào lớp 4.1 + 4.2).

### 5.1. `f_sdk_ads_data` — Quảng cáo

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `type` | enum string | ✓ | Loại ad: `Banner`, `Interstitial`, `Reward`, `AppOpen`, `Native`, `CollapsibleBanner`, `MREC`. |
| `adWhere` | string | ✓ | Vị trí hiển thị ad. |
| `adWhen` | string | ✓ | Thời điểm/ngữ cảnh hiển thị. |
| `adRev` | double | ✓ | Doanh thu ad (≥ 0). |
| `impressionCount` | int | ✓ | Số impression (mặc định 1). |
| `adPrecision` | string | — | Độ chính xác doanh thu (bỏ nếu null). |
| `adCountry` | string | — | Quốc gia (bỏ nếu null). |
| `adNetwork` | string | — | Ad network (bỏ nếu null). |
| `adMediation` | string | — | Mediation (bỏ nếu null). |
| `currentLevel` | int | — | Level hiện tại (bỏ nếu null). |
| `hasClick` | bool | — | Có click hay không (bỏ nếu null; thường có ở log ad mở rộng). |
| `timeShow` | double | — | Thời gian hiển thị (bỏ nếu null; thường có ở log ad mở rộng). |

Ngoài ra ở lớp envelope/log có thể kèm: `typeCount` (int), `adLtv` (double, bỏ nếu null).

### 5.1b. Bổ sung cho `f_sdk_ads_data` *(MỚI)*

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `waterfallPosition` | int | — | Tầng waterfall ad này thắng (§D2). |
| `fillLatencyMs` | int | — | *(MỚI)* Từ lúc xin ad tới lúc lên hình (ms) — SDK tự đo (§D2). Server tự trừ timestamp `ad_request`↔event này cũng ra; đây là bản fallback. |
| `adUnitId` | string | — | Ad unit id của mediation (trước đây chỉ gửi Firebase/MMP/game-server). |
| `adInstanceName` | string | — | Tên instance của network trong waterfall. |
| `adFormatRaw` | string | — | Chuỗi format THÔ từ mediation, không map. Dùng để phát hiện lệch: mediation map format→AdType bằng switch có nhánh default nên format lạ bị dán nhãn sai. |
| `adViewId` | string | — | Lần xem ad chứa impression này — nối với `f_sdk_ad_request_data`. Banner tự refresh: nhiều impression cùng một `adViewId` (một banner instance). Vắng mặt nếu đường load nằm ngoài SDK. |

### 5.2. `f_sdk_level_data` — Level

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `status` | enum string | ✓ | `Fail`, `Pass`, `Skip`, `Start`, `HeartBeat`. |
| `currentLevel` | int | ✓ | Level hiện tại. |
| `difficulty` | string | ✓ | Độ khó. |
| `playTurnId` | string | ✓ | Id của lượt chơi. |
| `duration` | long (giây) | ✓ | Thời lượng lượt chơi. |
| `levelProgress` | int | ✓ | % hoàn thành level (0–100). `Pass`→100, `Start`→0. |
| `comeBackAfterFirstPass` | bool | ✓ | Có quay lại chơi sau khi đã pass lần đầu hay không. |
| `failCount` | int | ✓ | Số lần fail (ở lớp log). |
| `playCount` | int | ✓ | Số lần chơi (ở lớp log). |
| `winStreak` | int | ✓ | Chuỗi THẮNG liên tiếp, xuyên level + xuyên session, SDK tự tính. `Start`/`HeartBeat` mang chuỗi TRƯỚC trận; terminal mang chuỗi đã tính trận đó. Xem ghi chú drop dưới bảng. |
| `loseStreak` | int | ✓ | Như trên cho chuỗi THUA. |
| `currentLevelId` | string | — | Id level (bỏ nếu null). |
| `failReason` | enum string | — | CHỈ trên `Fail`: `OutOfMoves` / `OutOfTime` / `Died` / `Quit` (bỏ nếu null — hợp đồng §D). |
| `levelLabels` | object | — | Nhãn của BẢN THIẾT KẾ màn (§D11), kèm hai key đăng ký `moves_limit`/`time_limit_sec`. Bỏ nếu màn không có nhãn. |
| `movesUsed` | int | — | Số nước đi đã dùng (bỏ nếu null). |
| `score` | int | — | Điểm (bỏ nếu null). |
| `wave` | int | — | Wave (bỏ nếu null). |
| `boostersUsed` | object<string,int> | — | Map booster→số lượng dùng trong màn (bỏ nếu null). |
| `preBoostersUsed` | object<string,int> | — | Map pre-booster→số lượng (chỉ ở log Start; bỏ nếu null). |

> Các giá trị booster âm sẽ bị loại khỏi map trước khi gửi.

> **`winStreak`/`loseStreak` với ván DROP**: chỉ `Pass`/`Fail` đụng chuỗi. `Skip` và ván bỏ ngang
> không có terminal (drop) **trong suốt** — chuỗi đứng yên đi qua chúng. SDK không suy "bỏ = thua"
> hộ: đó là CHÍNH SÁCH per-game chứ không phải bằng chứng (§H3), và suy sai một chiều là chuỗi trên
> log lệch khỏi chuỗi trên UI của game — đúng cái mà counter này sinh ra để tránh. Game coi bỏ ván
> là thua thì báo `Fail` với `failReason = Quit` (đúng vocab §D, chuỗi reset tự nhiên); server muốn
> "chuỗi tính cả drop" thì tự derive được — turn không terminal nhìn thấy ngay trong stream.
> Hệ quả đọc số: winStreak có thể ĐỨNG YÊN qua nhiều lần bỏ ngang liên tiếp.
>
> **Chuỗi PHÂN TÍCH ≠ chuỗi TÍNH NĂNG.** Cột này là metric chuẩn fleet (một định nghĩa, mọi game,
> server replay Pass/Fail kiểm lại được). Game có feature streak riêng (first-attempt-only, drop
> cũng cắt, milestone mở lợi thế...) thì đó là STATE của game — báo bằng
> `Label.User("streak_status", n)` (nhãn số, gửi khi đổi) + funnel milestone, KHÔNG nhét vào cột
> này và không có setter để nhét: mỗi game một định nghĩa là cột mất so-chéo-game.

> **`elo` đã RỜI event này** (1.3.0): nó tả NGƯỜI CHƠI nên đi bằng `f_sdk_user_label` key `elo`,
> gửi khi đổi — xem §D11. Cột elo cũ ngừng có dữ liệu từ bản này (chủ đích, không phải hỏng).

### 5.3. `f_sdk_in_app_data` — IAP (mua hàng)

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `productId` | string | ✓ | Id sản phẩm. |
| `where` | string | ✓ | Vị trí kích hoạt mua. |
| `iapWhen` | string | ✓ | Thời điểm/ngữ cảnh mua. |
| `isoCurrencyCode` | string | ✓ | Mã tiền tệ ISO. |
| `transactionId` | string | ✓ | Mã giao dịch. |
| `localizedPrice` | decimal | ✓ | Giá nội địa hóa (≥ 0). |
| `purchaseToken` | string | — | Token mua (bỏ nếu null). |
| `purchaseMethod` | string | — | Phương thức mua (bỏ nếu null). |
| `networkType` | string enum | — | *(MỚI)* Loại kết nối lúc mua — SDK tự đo (kế thừa từ họ param lượt mua). |
| `currentLevel` | int | — | Level hiện tại (bỏ nếu null). |
| `purchaseAttemptId` | string | — | *(MỚI)* Lượt mua (phễu checkout) dẫn tới giao dịch này — nối với `f_sdk_iap_start_purchase_data`. Vắng mặt khi giao dịch hoàn tất ở phiên khác (vd Google PENDING). |
| `offerImpressionId` | string | — | *(MỚI)* Lần hiển thị offer dẫn tới giao dịch này — SDK đóng dấu khi `productId` khớp `offerProductId` của offer đang hiển thị. Không quy được thì vắng mặt. |

### 5.4. `f_sdk_iap_offer_data` — Hiển thị/Tương tác offer IAP

> **Khi nào bắn:** MỘT log cho cả lượt hiển thị, bắn lúc offer **đóng** (không log per-frame).
> Kệ shop nhiều offer = một log mang mảng `offerProductId`.
> Các key `offerImpressionId` / `impressionDuration` / `isClicked` / `isPurchased` do SDK tự điền.
>
> `offerImpressionId` *(MỚI)*: id của lần hiển thị này, được đóng dấu lên `f_sdk_in_app_data`
> khi người chơi mua đúng sản phẩm trong `offerProductId` → đo conversion offer→purchase thật.
> `isPurchased` chỉ có giá trị khi game khai `offerProductId`; không khai thì key vắng mặt.
>
> `offerSurfaceType` *(MỚI)*: `popup` / `embedded_card` / `shop` — **bắt buộc tách kiểu thì chỉ số
> mới có nghĩa**: cùng event này đang trộn card nằm lì (49 lần/user/ngày, CTR 0.03%), popup chủ
> đích (CTR 0.3–5%) và kiểu chỉ-log-khi-bấm (CTR 100%). Không khai thì key vắng mặt.
>
> ⚠ **Đo conversion phải join theo `offerImpressionId`, KHÔNG dựa vào `isPurchased`**: giao dịch
> có thể hoàn tất SAU khi log impression đã gửi (app pause lúc mở store dialog, hoặc cửa sổ ân hạn
> sau khi offer đóng) — khi đó log mua vẫn mang đúng id nhưng `isPurchased` của log impression đã
> đi rồi nên là `false`/`null`. Khoá `offerImpressionId` nằm trên cả hai log nên join được từ cả
> hai phía.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `offerId` | string | ✓ | Id offer. |
| `offerCategory` | string | ✓ | Phân loại offer. |
| `triggerType` | enum string | ✓ | `UserInitiated`, `SystemTriggered`, `Contextual`, `ExternalLink`. |
| `offerLayout` | string | ✓ | Kiểu layout hiển thị. |
| `discountRate` | float | ✓ | Tỷ lệ giảm giá dạng thập phân (0.0–1.0). |
| `placementScene` | string | ✓ | Màn hình xuất hiện. |
| `triggerEvent` | string | ✓ | Điều kiện/thời điểm trigger. |
| `isClicked` | bool | ✓ | Có click vào offer hay không. |
| `impressionDuration` | long (giây) | ✓ | Thời gian offer hiển thị. |
| `offerProductId` | array<string> | — | Danh sách product id được offer (bỏ nếu null). |
| `currentLevel` | int | — | Level hiện tại (bỏ nếu null). |

### 5.5. `f_sdk_resource_data` — Dòng tài nguyên (currency)

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `exchangeId` | string | — | *(MỚI)* Định danh MỘT giao dịch (§D10): mọi vế của cùng một lần đổi mang chung giá trị này (`Sink` 500 gold + `Source` 3 booster). Vắng mặt ở dòng tài nguyên không thuộc giao dịch nào. SDK sinh khi game gọi `FalconBigDataController.Resource.OnExchange(sinks, sources)`; game tự set (id giao dịch của server) cũng được. |
| `flowType` | enum string | ✓ | `Source` (nhận vào) hoặc `Sink` (tiêu ra). |
| `itemType` | string | ✓ | Loại item. |
| `currency` | string | ✓ | Loại tiền/tài nguyên. |
| `itemId` | string | ✓ | Id item. |
| `resourceWhere` | string | ✓ | VỊ TRÍ phát sinh — màn/panel nào. Vocab riêng của từng game. |
| `resourceWhen` | string | ✓ | NGUYÊN NHÂN phát sinh — vì sao tài nguyên chảy. Vocab fleet, xem dưới. |
| `amount` | long | ✓ | Số lượng (≥ 0). |
| `currentLevel` | int | — | Level hiện tại (bỏ nếu null). |
| `valueBefore` | long | — | Số dư trước (bỏ nếu null; có ở log resource mở rộng). |
| `valueAfter` | long | — | Số dư sau (bỏ nếu null; có ở log resource mở rộng). |
| `detail` | object | — | Chi tiết bổ sung dạng object (bỏ nếu null). |

> **Vocab `resourceWhen` (§D9, sửa cột đích lần 2 — 2026-08-12)** — hằng số `FResourceWhen`:
> `shop` · `battle_pass` · `daily_quest` · `piggy_bank` · `lucky_wheel` · `daily_bonus` ·
> `level_reward` · `reward` · `gift` · tiền tố `event_`. Tên feature trùng với `funnelName` tương
> ứng để join được hai mặt (tiến độ ↔ ví thưởng).
> Trong SDK này `*When` nghĩa là **ngữ cảnh KÍCH HOẠT** chứ không phải mốc đồng hồ — cùng khuôn
> `adWhen = "level_fail"` ("ad nổ VÌ thua màn"). Nhóm này từng bị xếp vào `itemType` (tả loại vật
> phẩm, đang có `release_cache`/`unlimited_time` của game) rồi `resourceWhere` (tả màn/panel);
> đúng chỗ là `resourceWhen` vì nó trả lời VÌ SAO.
> ⚠ Đừng đọc `playTurnId`/`adViewId` mà SDK đóng tự động như nguyên nhân — chúng chỉ nói ĐỒNG THỜI.
> `itemType` giữ nghĩa "loại vật phẩm/giao dịch" và mang vocab riêng của từng game, SDK chỉ khai
> `FResourceWhen.Iap` (dời từ itemType 18/08 — loader đọc 2 era cho `buy_count_by_class`).
> Vocab `placement` của log mua (`where` trên `f_sdk_in_app_data` / `f_sdk_iap_start_purchase_data`)
> dùng hằng số `FIapPlacement`: `shop` · `battle_pass` · `piggy_bank` · `event_shop` ·
> `starter_pack` · `remove_ads`.

> **Nghĩa của `elo`**: điểm trình độ NGƯỜI CHƠI. Độ khó của MÀN không có key riêng và client
> không gửi — nó là hàm của kết quả **cả fleet**, server giải ngược từ tỉ lệ pass quan sát được:
> `E = 1 / (1 + 10^((R_level − R_user)/400))`. Đầu vào đã có sẵn trong log: `currentLevelId`
> (danh tính bản thiết kế — đổi tune là id mới nên không trộn hai phiên bản màn), `status`
> Pass/Fail, `playCount`/`failCount`, và `elo` của người chơi từ `f_sdk_user_label` khi game có.
> Bản thân `elo` của người chơi cũng là thứ server tổng hợp được từ chính những tín hiệu trên;
> client chỉ gửi khi game đã có sẵn một hệ elo (hoặc nhận lại giá trị server đã tính).

### 5.6. `f_sdk_funnel_data` — Funnel

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `funnelName` | string | ✓ | Tên funnel. |
| `action` | string | ✓ | Hành động/bước. |
| `priority` | int | ✓ | Thứ tự ưu tiên/bước (≥ 0). |
| `cycleId` | string | — | *(MỚI)* Ranh giới chu kỳ của funnel lặp ("season_5", "2026-08-12"…) — server tách mùa trực tiếp thay vì suy từ `funnelDay`. Vắng mặt nếu game không truyền (§H4). Với shape client `OncePerCycle`, SDK lọc trùng theo (cycleId, action, priority) ngay tại client. |
| `cycleIndex` | int | — | *(MỚI 1.3.8)* Số thứ tự chu kỳ (mùa/giải thứ mấy) — trục sort bằng SỐ, khỏi parse tên cycleId ("season_9" > "season_10" khi sort chuỗi). Vắng mặt nếu game không truyền. |
| `cycleStartTs` / `cycleEndTs` | long | — | *(MỚI 1.3.8)* Mốc mở/đóng chu kỳ, epoch millis UTC, từ CONFIG event (đúng số UI countdown vẽ). Countdown-tại-log = `cycleEndTs − ts` — cắt nhóm theo countdown không cần bảng lịch vận hành. ⚠ Chu kỳ ĐỘNG: đọc theo giá-trị-tại-thời-điểm-log (extend giữa mùa thì các dòng sau mang giá trị mới — đó là lịch sử thật, không phải lỗi). Client giữ RAW kể cả khi end ≤ start (bug config phải thấy được để truy). |
| `funnelShape` | string | — | *(MỚI 1.3.8, SDK tự đóng)* Hình dạng van ĐANG ÁP tại client lúc log: `once_ordered` / `once_per_cycle` / `repeatable`. Vai DQ: catalog giữ shape CHUẨN theo thiết kế, field này chở shape THỰC TẾ — lệch nhau = client quên Register (funnel rơi về luật ftue, bước bị veto trắng phía client). Không phải hằng số nên mới đáng chở: nó thay đổi theo build/trạng thái đăng ký của client. |
| `funnelDay` | string | — | Ngày funnel (nếu có). |
| `currentLevel` | int | — | Level hiện tại (bỏ nếu null). |

> Funnel kiểu cũ có thể kèm thêm các key động (extra meta) được trải phẳng vào cùng cấp.

**Vocab đăng ký (§D6 + §D9)** — hằng số `FFunnelName` / `FFunnelAction` trong SDK:

| `funnelName` | Hình dạng | `action` chuẩn |
|---|---|---|
| `ftue` | một lần, đúng thứ tự | `scene_enter` → `tutorial_start` → `tutorial_complete` |
| `battle_pass`, `daily_quest`, `piggy_bank`, `lucky_wheel`, `event_<type>` | lặp lại, được nhảy cóc mốc | `join` / `milestone` / `claim` / `complete` (`priority` = số tier/mốc) |

- Tên ngoài danh sách vẫn gửi được nhưng **phải đăng ký với loader trước**.
- `funnelDay` của funnel lặp lại = ngày `join` **gần nhất** (không phải lần đầu đời máy) —
  nếu không thì `event_duration` của battle pass mùa 5 tính từ mùa 1, thành số vô nghĩa.
  Chưa từng gửi `join` thì lấy ngày hiện tại.


### 5.6b. `f_sdk_ui_impression` — Exposure UI (bản tin CỤM, 1.3.9)

Máy đếm icon/popup/banner event hiện ra & được bấm. Grain per-lần-hiện CẤM lên wire (volume
không chặn trên) — client gộp theo (surface × action), flush lúc app pause.

| Field | Kiểu | Ghi chú |
|---|---|---|
| `surfaceId` | string | Bề mặt UI — vocab per game/event, tên ổn định |
| `uiAction` | string | `impression` / `click` (vocab FUiAction) |
| `count` | int | Số lần gộp — server đếm `sum(count)`, KHÔNG `count(*)` |
| extras | flatten | Ngữ cảnh bất biến của surface — bản của LẦN ĐẦU trong cụm |

CTR ngày = `sum(count|click) / sum(count|impression)` per surface. Không persist qua kill.
KHÁC bước funnel `open`: open là MỐC phễu (đã xem/chưa), đây là MÁY ĐẾM (bao nhiêu lần).

### 5.7. `f_sdk_event_data` — Event tùy biến

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `eventName` | string | ✓ | Tên event tùy biến. |
| `eventWhere` | string | ✓ | Vị trí. |
| `eventWhen` | string | ✓ | Thời điểm/ngữ cảnh. |
| `currentLevel` | int | — | Level hiện tại (bỏ nếu null). |
| `paramsStr` | string | — | **Tham số tùy biến đã serialize thành chuỗi JSON** (bỏ nếu null). Lưu ý: là string, không phải object. |

### 5.8. `f_sdk_session_data` — Phiên chơi

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `gameMode` | string | ✓ | Chế độ chơi. |
| `sessionTime` | long (giây) | ✓ | Thời lượng phiên. |
| `modeTotalTime` | long | ✓ | Tổng thời gian theo mode (ở lớp log). |
| `currentLevel` | int | — | Level hiện tại (bỏ nếu null). |
| `avgFps` | float | — | *(MỚI §D7)* FPS trung bình của **quãng mà bản tin này khai** (không phải cả phiên) — SDK tự đo. Gộp cả phiên thì cân theo `sessionTime` từng bản tin. |
| `frameDropCount` | int | — | *(MỚI §D7)* Số khung > 100ms trong quãng (ngưỡng cố định phía client). Khoảng > 2s coi là app không render (ads/background/load) nên KHÔNG tính. |
| `memoryWarningCount` | int | — | *(MỚI §D7)* Số lần OS cảnh báo thiếu bộ nhớ (`onTrimMemory` / `didReceiveMemoryWarning`). |

> 3 field perf chỉ có trên bản tin `gameMode = USER_TOTAL_TIME` (bản tin lúc app pause và bản tin
> bảo hiểm định kỳ). Mỗi bản tin mang số liệu của đúng quãng nó khai rồi reset ⇒ cộng lại
> **không đếm đôi**; nhờ có mặt cả trên bản tin định kỳ nên app bị kill cứng vẫn còn số liệu.
> Không đo được (chưa có frame nào) thì cả 3 vắng mặt, không gửi 0 giả.

### 5.9. `f_sdk_property_data` — Thuộc tính user

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `pName` | string | ✓ | Tên thuộc tính. |
| `pValue` | string | ✓ | Giá trị thuộc tính. |
| `priority` | int | ✓ | Ưu tiên (≥ 0). |
| `currentLevel` | int | — | Level hiện tại (bỏ nếu null). |

### 5.10. `f_sdk_retention_data` — Retention

> **Khi nào bắn:** log này được gửi lên **khi retention của người chơi thay đổi** — tức khi người chơi bước sang một ngày active mới (mốc retention day mới). Không bắn lặp lại trong cùng một ngày.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `localDate` | string | ✓ | Ngày local của mốc retention, định dạng `yyyy-MM-dd HH:mm:sszzz`. |

### 5.11. `f_sdk_ad_request_data` — Xin ad từ mediation *(MỚI — chờ loader xác nhận event id)*

> **Khi nào bắn:** ngay trước mỗi lần gọi load ad của mediation (mọi format, kể cả retry).
> Điểm vào phễu fill — request KHÔNG có impression mang cùng `adViewId` chính là view fill-fail,
> nên không cần event fail riêng.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `adViewId` | string | ✓ | Id của cả lần xem ad (request → show → impression → close), SDK sinh. Xuất hiện lại trên `f_sdk_ads_data` nếu ad lên hình. |
| `type` | string enum | ✓ | Format ad (`Banner`/`Interstitial`/`Reward`/`AppOpen`/...). |
| `adWhere` | string | — | Vị trí/ngữ cảnh nếu biết lúc load (bỏ nếu preload). |
| `adWhen` | string | — | *(MỚI)* Ngữ cảnh kích hoạt nếu biết lúc load — cùng tên với `adWhen` của `f_sdk_ads_data` để join. |
| `adMediation` | string | — | Mediation đang dùng (bỏ nếu null). |
| `networkType` | string enum | — | *(MỚI)* `wifi`/`cellular`/`none` lúc xin ad — SDK tự đo, giải thích fill-fail. |

### 5.11b. `f_sdk_ad_show_data` — Gọi hiển thị ad *(MỚI — ngoài hợp đồng §B, cần loader xác nhận)*

> **Khi nào bắn:** mediation gọi hiển thị ad (inter/rewarded; banner không có mốc này).
> Show mà KHÔNG có impression mang cùng `adViewId` = hiển thị hỏng (display failure).

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `adViewId` | string | ✓ | Cùng id với `f_sdk_ad_request_data`. |
| `type` | string enum | ✓ | Format ad. |
| `adWhere` | string | — | Placement (lúc show thì đã biết, khác lúc request). SDK điền từ cache của lần xem nếu mốc này bỏ trống. |
| `adWhen` | string | — | *(MỚI)* Ngữ cảnh kích hoạt — như trên. |
| `adMediation` | string | — | Mediation đang dùng. SDK điền từ mốc request nếu bỏ trống (bất biến trong một lần xem). |
| `requestToShowMs` | int | — | Chờ bao lâu từ lúc xin ad tới lúc hiển thị — SDK tự đo. Bỏ nếu show không đi từ request nào. |

### 5.11c. `f_sdk_ad_close_data` — Ad đóng lại *(MỚI — ngoài hợp đồng §B, cần loader xác nhận)*

> **Khi nào bắn:** ad đóng, người chơi quay về game — mốc cuối của vòng đời một lần xem ad.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `adViewId` | string | ✓ | Cùng id với các mốc trước. |
| `type` | string enum | ✓ | Format ad. |
| `adWhere` | string | — | Placement. SDK điền từ cache của lần xem nếu bỏ trống. |
| `adWhen` | string | — | *(MỚI)* Ngữ cảnh kích hoạt — SDK điền từ cache nếu bỏ trống. |
| `adMediation` | string | — | Mediation đang dùng — SDK điền từ mốc request nếu bỏ trống. |
| `shownDurationSec` | int | — | Ad nằm trên màn hình bao lâu — SDK tự đo từ mốc show. Bỏ nếu chưa từng show. |
| `hasClick` | bool | — | Có bấm vào ad không (mediation điền). |
| `adCompleted` | bool | — | Xem hết hay bỏ giữa chừng — chính là `ad_completed` của §D2, đặt ở đây vì chỉ lúc đóng mới biết. |

### 5.11d. `f_sdk_ad_show_attempt` — Game MUỐN chiếu ad *(MỚI 23/09 — loader đã duyệt hình dạng, chờ seed rule)*

> **Khi nào bắn:** mediation vào hàm show, **TRƯỚC** nhánh kiểm tra kho (`IsReady`). Bắn ở CẢ hai
> nhánh (có ad / không có ad), chỉ khác `adAvailable`.
>
> **Vì sao cần:** từ bản mediation có van nạp sẵn, một `f_sdk_ad_request_data` phục vụ NHIỀU lần
> chiếu → `request` là "một đợt nạp hàng", không còn là "một lần user cần ad". Trước mốc này, ca
> "muốn chiếu mà kho rỗng" không có dòng nào trên wire.
>
> **Bất biến kiểm được:** số dòng event này ≥ số `f_sdk_ad_show_data`.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `type` | enum string | ✓ | Format ad |
| `adAvailable` | bool | ✓ | Lúc game hỏi thì kho có ad sẵn không — mediation truyền, SDK không đoán hộ |
| `adWhere` | string | — | Chỗ game định chiếu — có cả ở ca không có ad, nên đọc được "chỗ nào hay hụt ad" |
| `adWhen` | string | — | Ngữ cảnh kích hoạt |
| `adMediation` | string | — | Mediation đang dùng |
| `adViewId` | string | — | View sắp chiếu — **chỉ có khi `adAvailable = true`**; kho rỗng thì vắng. SDK tự đọc từ cache |

```
view_fill_rate = count(adAvailable = true) / count(*)     per adWhere / type
```

### 5.11e. `f_sdk_ad_load_stats` — Telemetry load, bản tin GỘP dạng mảng *(MỚI 23/09 — THAY `f_sdk_ad_load_success` + `f_sdk_ad_load_fail`)*

> **Khi nào bắn:** lúc app pause (hoặc khi kho cụm chạm 256 khoá). MỘT dòng cho mỗi
> (`adType` × `adMediation`), chi tiết từng ad unit nằm trong mảng `units[]`.
>
> **Không mang `adViewId`** — đây là telemetry per-attempt của từng unit, không thuộc vòng đời một
> lần xem ad. Nối với vòng đời qua `adUnitId`.
>
> Hai event cũ (`f_sdk_ad_load_success`, `f_sdk_ad_load_fail`) **ngừng gửi** từ cùng bản SDK; loader
> xác nhận không mapping/cook/counter/probe nào đọc chúng.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `adType` | enum string | ✓ | Format ad |
| `adMediation` | string | — | Mediation của nhóm unit này |
| `units` | array&lt;object&gt; | ✓ | Mỗi phần tử một ad unit — schema dưới. Trần **64 phần tử**; vượt thì CHIA LÔ thành nhiều dòng (không cắt bỏ — mọi số đều cộng được nên tổng giữ nguyên) |

Phần tử của `units[]` (tên khoá viết tắt để gói tin không phình — 32 unit ≈ 2,5 KB):

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `u` | string | ✓ | `adUnitId` |
| `ok` / `fail` | int | ✓ | Số lần load thành công / thất bại đã gộp |
| `okMs` / `failMs` | long | ✓ | Tổng thời gian load của mỗi nhóm — chia `ok`/`fail` ra trung bình |
| `tier` | double | — | **BẬC** giá sàn trong config (hệ số 1.5 / 3.0 / 5.0) — mediation nào cũng biết thật, là trục chính để so bậc nào fill tốt hơn. ⚠ **Unit không đặt giá sàn thì key VẮNG MẶT, không gửi `0`** — vắng-key là thứ duy nhất phân biệt "không đặt sàn" với "đặt sàn 0" |
| `floor` | double | — | **GIÁ SÀN THẬT**, USD trên 1000 lượt hiển thị — **TRUNG BÌNH các lần load trong bản ghi** (từ 1.4.1; cùng khuôn với `okMs`/`failMs`, làm tròn 4 chữ số thập phân). CHỈ mediation biết con số thật mới gửi (AdMob tự đặt sàn từng slot); MAX không biết (giá sàn nằm trên dashboard) nên vắng. ⚠ **Cấm** suy ra bằng cách nhân `tier` với doanh thu đo được — nền nhân là giá ad vừa xem nên cùng bậc ra số khác nhau theo máy/theo ngày (bài học công thức cũ, loader đo 22/09). Vắng ở đây = **không biết**, khác `tier` vắng = **không đặt sàn** |

> **Khoá gộp** (từ 1.4.1) = `adType` × `adMediation` × `u` × `tier` — toàn thứ RỜI RẠC. `floor` là
> số đo nên KHÔNG nằm trong khoá: AdMob tính `floor = revenue × hệ số × 1000` với `revenue` là giá
> của ad vừa load, tức số thực liên tục — để trong khoá thì nhánh AdMob mỗi attempt một dòng, mất
> hẳn tác dụng gộp. Bản 1.4.0 có lỗi này, 1.4.1 sửa.
| `net` | string | — | Network thắng — chỉ có khi `ok > 0` |
| `err` | string | — | Mã lỗi CUỐI của nhóm fail — không làm khoá gộp (message lạ sẽ tách bản ghi vô hạn) |

> **Đọc:** `arrayJoin` mảng ra từng dòng rồi cộng như bảng thường. `load_fill_rate = Σok / (Σok + Σfail)`.
> Phân biệt "vắng tier" với "tier = 0" phải hỏi `JSONType` trên từng phần tử — hàm trích JSON trả
> `0` cho cả hai. Đừng trả `Array(Tuple(...))` nguyên khối về client: driver JDBC vỡ.

### 5.12. `f_sdk_iap_start_purchase_data` — Mở lượt mua *(MỚI — chờ loader xác nhận event id)*

> **Khi nào bắn:** ngay trước khi gọi billing flow (`launchBillingFlow` / StoreKit payment).
> Đây là điểm vào phễu checkout — không có event này thì abandonment mù hoàn toàn.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `purchaseAttemptId` | string | ✓ | Id lượt mua, SDK sinh. Xuất hiện lại trên log fail hoặc log mua thành công tương ứng. |
| `productId` | string | ✓ | Sản phẩm đang mua. |
| `where` | string | — | Vị trí/ngữ cảnh kích hoạt mua (bỏ nếu null). |
| `localizedPrice` | decimal | — | *(MỚI)* Giá nội địa hoá — đo giá trị bị bỏ giỏ mà không phải join catalog. |
| `isoCurrencyCode` | string | — | *(MỚI)* Mã tiền tệ của giá trên. |
| `networkType` | string enum | — | *(MỚI)* `wifi`/`cellular`/`none` lúc mở billing flow — SDK tự đo. |

### 5.13. `f_sdk_iap_purchase_fail_data` — Lượt mua không hoàn tất *(MỚI — chờ loader xác nhận event id)*

> **Khi nào bắn:** billing callback trả lỗi/huỷ/treo. Mang cùng `purchaseAttemptId` với log mở lượt.
>
> ⚠ `failReason = pending` **KHÔNG phải fail thật** (Google PENDING): giao dịch còn treo, log mua
> vẫn sẽ bắn khi hoàn tất sau — có thể ở phiên khác, khi đó log mua KHÔNG mang `purchaseAttemptId`.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `purchaseAttemptId` | string | ✓ | Cùng id với log mở lượt. |
| `productId` | string | ✓ | Sản phẩm. |
| `where` | string | — | Vị trí/ngữ cảnh (bỏ nếu null). |
| `localizedPrice` | decimal | — | *(MỚI)* Giá nội địa hoá. |
| `isoCurrencyCode` | string | — | *(MỚI)* Mã tiền tệ. |
| `networkType` | string enum | — | *(MỚI)* Loại kết nối **tại mốc thất bại** (không phải lúc mở) — giải thích `failReason = network`. |
| `failReason` | string enum | ✓ | `user_canceled` / `item_unavailable` / `billing_unavailable` / `network` / `developer_error` / `pending` / `unknown` *(giá trị `unknown` là bổ sung của client khi không map được responseCode — cần loader xác nhận)*. |

### 5.14. `f_sdk_app_open_data` — Mốc mở app *(MỚI — chờ loader xác nhận event id)*

> **Khi nào bắn:** mỗi lần app vào foreground. Bản `cold` (process vừa khởi tạo) đồng thời là
> **mốc mở phiên** — hợp đồng v-next §B liệt kê `session_start` và `app_open` riêng, nhưng trong
> SDK này session = 1 process nên hai mốc trùng khít 1:1; client chỉ gửi `app_open` với
> `launch_type=cold`. **Cần loader xác nhận** id `f_sdk_app_open_data` và việc không có event
> `session_start` riêng.
>
> Lần quay lại từ background có thời lượng nền dưới `fAppOpenMinBackgroundSec` (remote config,
> default 30s) thì KHÔNG gửi — lọc nhiễu kiểu thoát ra vài giây. `openIndex` vẫn tăng cho những
> lần bị lọc, nên khoảng nhảy của nó cho biết có bao nhiêu lần quay lại ngắn.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `launchType` | string enum | ✓ | `cold` = process mới (≡ mốc mở phiên, tuyệt đối không false-positive) / `hot` = quay lại foreground cùng process. Unity KHÔNG sinh `warm` (activity/scene dựng lại) — giá trị đó dành cho SDK native. |
| `backgroundDurationSec` | int | — | Thời lượng nằm dưới background. Bỏ khi `cold` hoặc khi không đo được (không bịa 0). |
| `openIndex` | int | ✓ | Thứ tự lần vào foreground trong phiên, bắt đầu từ 1, đếm cả lần bị lọc. |
| `networkType` | string enum | — | *(MỚI)* Loại kết nối lúc mở app — SDK tự đo. |
| `openSource` | string enum | — | *(MỚI §B4)* `icon` / `push` / `deeplink` / `widget` / `other`. Unity không đọc được intent (Android) / launchOptions (iOS) nên **module push/deeplink phải gọi `App.ReportOpenSource` trước khi bản tin bắn**; không ai gọi thì field vắng mặt — SDK KHÔNG mặc định `icon`. |
| `pushCampaignId` | string | — | *(MỚI §B4)* Campaign của push đã kéo người chơi vào. Chỉ có khi `openSource = push`. |
| `startupDurationMs` | int | — | *(MỚI §B4)* **Chỉ ở bản `cold`.** Từ lúc code C# sống dậy (`SubsystemRegistration`) tới lúc dựng bản tin này. ⚠ **THIẾU native bootstrap** (OS tạo process → Unity runtime lên) vì Unity không nhìn thấy khoảng đó ⇒ đây là **số tương đối** để so giữa các bản/các máy, không phải cold-start tuyệt đối. Đo bằng đồng hồ đơn điệu (`Stopwatch`), không phải wall-clock. |

> `startupDurationMs` là **ngoại lệ duy nhất** client được tự đo khoảng thời gian: đoạn này nằm
> trước khi hệ thống event sống nên server mù. Từ mốc `app_open` trở đi, mọi khoảng cách thời gian
> server tự trừ giữa các event — client không cộng hộ. Mốc đo cố định trong SDK; nếu đổi sẽ báo loader.

### 5.18. `levelLabels` — nhãn của MÀN, đi ké level event *(§D11, viết lại lần 3 ngày 2026-08-11)*

> **Không có event riêng.** Game khai qua `Label.Level(id, key, value)` →
> giá trị vào kho nhãn của SDK → SDK-core đóng dấu bundle lên **CHỈ level events**, y khuôn
> `playTurnId` đi ké mọi log.

| Key | Kiểu | Mô tả |
|---|---|---|
| `levelLabels` | object PHẲNG | `{"cluster":"hard_1","hand_tuned":true,"tier":3}` — giữ nguyên kiểu JSON. Vắng mặt khi màn chưa có nhãn. |

**Vì sao không bắn event riêng** (đường cũ đã HỦY): bridge `user_level_daily` có grain theo NGÀY,
mà bắn-một-lần-đời-cài thì ngày người chơi quay lại màn cũ không có bản tin nào → hộp ngày đó rỗng,
trừ khi server nuôi bảng state cỡ tỷ dòng để forward-fill. Stamp thì ngày nào chơi là ngày đó có
nhãn tươi, và **ván vắt qua nửa đêm UTC tự lành** vì heartbeat của ngày sau cũng mang bundle.

Bốn luật đi kèm:
1. **Chỉ level events.** Stamp sang stream ad/iap/resource là vi phạm hợp đồng — "rev ads theo cụm
   khó" đã với được qua `playTurnId`, không đáng trả byte trên stream khác.
2. **Bundle tả màn CỦA CHÍNH EVENT ĐÓ** (`currentLevel` trên event), không phải màn đang mở —
   heartbeat trễ sau khi sang màn mới phải mang nhãn của màn nó tả.
3. **Gỡ nhãn = thôi kèm key.** Cook đọc "hôm nay không thấy key" là *không còn nhãn ngày đó*, không
   giữ giá trị cũ. `labelValue: null` chỉ còn nghĩa cho họ label event-based (user/instance).
4. **Nhãn khai trước cho màn chưa ai chơi không bao giờ phát ra** — đúng thiết kế, không phải bug.

> **Van 256B/bundle** (đếm BYTE, không đếm key — đếm key thì phạt oan nhãn số). Vượt: client cảnh
> báo kèm số cụ thể và **giữ nguyên bundle cũ**; loader vượt thì DROP BUNDLE, không drop record.
> Van này bảo vệ WIRE, khác van ≤20 key/≤64 ký tự vốn bảo vệ namespace của nhãn user — hai van hai việc.

> Đừng gửi qua đây thứ đã có kênh riêng: `difficulty` (đã ở level event), `movesLimit`/`timeLimitSec`
> (§D), `currentLevelId`. Nhãn để dành cho thứ CHƯA có chỗ.

### 5.17. `f_sdk_permission_*` — Trạng thái quyền/consent *(MỚI §D8)*

> **Một event cho MỖI loại quyền**, không phải một event chung mang key:
> `f_sdk_permission_att` · `f_sdk_permission_ads_consent` · `f_sdk_permission_push`.
> Luật phân xử của hợp đồng: *vocab ĐÓNG toàn-fleet mà analyst lọc trực tiếp thì nằm trong TÊN
> EVENT; vocab MỞ per-game thì nằm trong param.* Phí migrate bất đối xứng chốt án — gộp rồi tách
> là cả chiến dịch convert (án `f_sdk_level`), tách rồi gộp thì `event LIKE 'f_sdk_permission_%'`
> là xong.
>
> **Khi nào bắn:** khi ĐỔI giá trị + lần đăng nhập đầu. SDK nhớ giá trị đã gửi nên game gọi lại
> cùng giá trị bao nhiêu lần cũng không đẻ log. "Trạng thái hiện tại" phía server =
> `argMax(sub_event, created_date)` per event.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `permissionValue` | string | ✓ | Giá trị trạng thái → server đưa vào `sub_event`. |
| `currentLevel` | int | — | Level lúc đổi trạng thái (bỏ nếu null). |

| Event | `permissionValue` hợp lệ |
|---|---|
| `f_sdk_permission_att` | `authorized` / `denied` / `restricted` / `not_determined` |
| `f_sdk_permission_ads_consent` | `granted` / `denied` |
| `f_sdk_permission_push` | `granted` / `denied` |

> API phía client kiểu hoá sẵn nên không gõ sai vocab được: `App.ReportAttStatus(AttStatus)`,
> `App.ReportAdsConsent(bool)`, `App.ReportPushPermission(bool)`; `App.ReportPermission(type, string)`
> chỉ là cửa thoát cho quyền mới chưa kịp có hàm riêng.
>
> `sub_event_2` client **để trống** — hợp đồng dành trục đó cho nguồn trigger (`prompt` /
> `settings` / `login_check`) khi nào có nhu cầu.
>
> ⚠ **KHÔNG dùng `property_data`** cho mấy trạng thái này: đó là log di sản thời hệ sơ khai dùng
> sinh biểu đồ động (dấu vết còn nguyên ở field `priority` = "thứ tự step"), phía server nó thành
> event `f_sdk_property`. Mượn kênh legacy là trộn ngữ nghĩa với dữ liệu biểu đồ đời cũ.

### 5.16. `f_sdk_<entity>_label` — Họ bản tin gán nhãn *(MỚI §D11)*

> **Một format, mỗi entity một event**: `f_sdk_user_label` · `f_sdk_session_label` ·
> `f_sdk_turn_label` · `f_sdk_iap_offer_label` · `f_sdk_iap_purchase_label` · `f_sdk_ad_view_label`
> (ba cái sau hợp thức hoá 2026-08-11; id đi trong param vì — khác `sessionUid`/`playTurnId` — ba
> id đó không được đóng tự động lên mọi log).
>
> ⚠ **Nhãn của MÀN KHÔNG nằm trong họ này** — level đi đường STAMP, xem mục 5.18.
> Entity vào TÊN EVENT (vocab đóng), `labelKey` ở param (vocab MỞ per-game) — đúng luật phân xử §D8.
>
> **Một cửa cho cả "nhãn" lẫn "tham số động"** — dev không phải phân biệt; ranh giới đó là việc của
> data team.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `labelKey` | string | ✓ | Loại nhãn — **TỰ DO, không đăng ký trước** (registry đã bỏ 2026-08-10). Server đưa vào `sub_event`. |
| `labelValue` | **JSON nguyên vẹn** | ✓ | bool / số / chữ — KHÔNG ép về string, không nhét vào slot typed. **`null` = GỠ nhãn**, nên field này luôn có mặt kể cả khi null. |
| `adViewId` / `offerImpressionId` / `purchaseAttemptId` | string | — | Mỗi cái CHỈ có ở event instance tương ứng. |

**Ngữ nghĩa thời gian khác nhau theo entity** — đây là chỗ dễ nhầm nhất:

| Event | Server làm gì | Hiệu lực |
|---|---|---|
| `f_sdk_user_label` | merge vào bucket `user_properties` (chung chỗ + cùng ngữ nghĩa last-wins với `falcon_user_filter`) → snapshot lên mọi event SAU | **từ-mốc-trở-đi**, quá khứ MIỄN NHIỄM |
| `f_sdk_session_label` / `_turn_label` / `_ad_view_label` / `_offer_label` / `_purchase_label` | cook nhặt SQL (argMax theo instance) vào hộp tương ứng, KHÔNG qua profile | trọn instance; chỉ gán được khi instance còn MỞ |

> **Van loader enforce (vượt = DROP + DQ đếm, không im lặng)**: ≤20 label key/user · value chữ ≤64
> ký tự. SDK chặn sớm ở client kèm cảnh báo để dev không phải đi tìm dữ liệu mất tích.
>
> **AB testing KHÔNG đi đường này** — đã có kênh riêng `ab_campaigns` (first-wins).

### 5.18. `levelLabels` — nhãn của MÀN, đi ké level event *(§D11, viết lại lần 3 ngày 2026-08-11)*

> **Không có event riêng.** Game khai qua `Label.Level(id, key, value)` →
> giá trị vào kho nhãn của SDK → SDK-core đóng dấu bundle lên **CHỈ level events**, y khuôn
> `playTurnId` đi ké mọi log.

| Key | Kiểu | Mô tả |
|---|---|---|
| `levelLabels` | object PHẲNG | `{"cluster":"hard_1","hand_tuned":true,"tier":3}` — giữ nguyên kiểu JSON. Vắng mặt khi màn chưa có nhãn. |

**Vì sao không bắn event riêng** (đường cũ đã HỦY): bridge `user_level_daily` có grain theo NGÀY,
mà bắn-một-lần-đời-cài thì ngày người chơi quay lại màn cũ không có bản tin nào → hộp ngày đó rỗng,
trừ khi server nuôi bảng state cỡ tỷ dòng để forward-fill. Stamp thì ngày nào chơi là ngày đó có
nhãn tươi, và **ván vắt qua nửa đêm UTC tự lành** vì heartbeat của ngày sau cũng mang bundle.

Bốn luật đi kèm:
1. **Chỉ level events.** Stamp sang stream ad/iap/resource là vi phạm hợp đồng — "rev ads theo cụm
   khó" đã với được qua `playTurnId`, không đáng trả byte trên stream khác.
2. **Bundle tả màn CỦA CHÍNH EVENT ĐÓ** (`currentLevel` trên event), không phải màn đang mở —
   heartbeat trễ sau khi sang màn mới phải mang nhãn của màn nó tả.
3. **Gỡ nhãn = thôi kèm key.** Cook đọc "hôm nay không thấy key" là *không còn nhãn ngày đó*, không
   giữ giá trị cũ. `labelValue: null` chỉ còn nghĩa cho họ label event-based (user/instance).
4. **Nhãn khai trước cho màn chưa ai chơi không bao giờ phát ra** — đúng thiết kế, không phải bug.

> **Van 256B/bundle** (đếm BYTE, không đếm key — đếm key thì phạt oan nhãn số). Vượt: client cảnh
> báo kèm số cụ thể và **giữ nguyên bundle cũ**; loader vượt thì DROP BUNDLE, không drop record.
> Van này bảo vệ WIRE, khác van ≤20 key/≤64 ký tự vốn bảo vệ namespace của nhãn user — hai van hai việc.

> Đừng gửi qua đây thứ đã có kênh riêng: `difficulty` (đã ở level event), `movesLimit`/`timeLimitSec`
> (§D), `currentLevelId`. Nhãn để dành cho thứ CHƯA có chỗ.

### 5.17. `f_sdk_permission_*` — Trạng thái quyền/consent *(MỚI §D8)*

> **Một event cho MỖI loại quyền**, không phải một event chung mang key:
> `f_sdk_permission_att` · `f_sdk_permission_ads_consent` · `f_sdk_permission_push`.
> Luật phân xử của hợp đồng: *vocab ĐÓNG toàn-fleet mà analyst lọc trực tiếp thì nằm trong TÊN
> EVENT; vocab MỞ per-game thì nằm trong param.* Phí migrate bất đối xứng chốt án — gộp rồi tách
> là cả chiến dịch convert (án `f_sdk_level`), tách rồi gộp thì `event LIKE 'f_sdk_permission_%'`
> là xong.
>
> **Khi nào bắn:** khi ĐỔI giá trị + lần đăng nhập đầu. SDK nhớ giá trị đã gửi nên game gọi lại
> cùng giá trị bao nhiêu lần cũng không đẻ log. "Trạng thái hiện tại" phía server =
> `argMax(sub_event, created_date)` per event.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `permissionValue` | string | ✓ | Giá trị trạng thái → server đưa vào `sub_event`. |
| `currentLevel` | int | — | Level lúc đổi trạng thái (bỏ nếu null). |

| Event | `permissionValue` hợp lệ |
|---|---|
| `f_sdk_permission_att` | `authorized` / `denied` / `restricted` / `not_determined` |
| `f_sdk_permission_ads_consent` | `granted` / `denied` |
| `f_sdk_permission_push` | `granted` / `denied` |

> API phía client kiểu hoá sẵn nên không gõ sai vocab được: `App.ReportAttStatus(AttStatus)`,
> `App.ReportAdsConsent(bool)`, `App.ReportPushPermission(bool)`; `App.ReportPermission(type, string)`
> chỉ là cửa thoát cho quyền mới chưa kịp có hàm riêng.
>
> `sub_event_2` client **để trống** — hợp đồng dành trục đó cho nguồn trigger (`prompt` /
> `settings` / `login_check`) khi nào có nhu cầu.
>
> ⚠ **KHÔNG dùng `property_data`** cho mấy trạng thái này: đó là log di sản thời hệ sơ khai dùng
> sinh biểu đồ động (dấu vết còn nguyên ở field `priority` = "thứ tự step"), phía server nó thành
> event `f_sdk_property`. Mượn kênh legacy là trộn ngữ nghĩa với dữ liệu biểu đồ đời cũ.

### 5.16. `f_sdk_label_assign_data` — Gán nhãn instance đang mở *(MỚI — chờ loader xác nhận event id)*

> **Khi nào bắn:** game gọi `Label.Session(key, value)` / `Label.Turn(key, value)` (hợp đồng §B3).
> Định danh instance KHÔNG nằm trong param — `sessionUid` đi theo central user params và
> `playTurnId` được đóng tự động khi lượt còn mở, như mọi event khác.

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `labelKey` | string | ✓ | Loại nhãn — **PHẢI đăng ký trước với loader**; key vô danh bị đếm DQ và không được nhặt. |
| `labelValue` | string | ✓ | Giá trị nhãn, giữ ngắn. |
| `labelScope` | string enum | ✓ | `user` / `session` / `turn` / `ad_view` / `offer` / `purchase` — nhãn dán cho grain nào. Bắt buộc vì bản tin mang nhiều id cùng lúc; thiếu scope là nhãn dính nhầm hết. |
| `adViewId` | string | — | Chỉ có khi `labelScope = ad_view`. |
| `offerImpressionId` | string | — | Chỉ có khi `labelScope = offer`. |
| `purchaseAttemptId` | string | — | Chỉ có khi `labelScope = purchase`. |

> **`labelScope = user`** là hiện thực của "KÊNH NHÃN TỰ PHỤC VỤ — KIỂU B" (gold-rollups §14):
> `(accountId, labelKey, labelValue, effective_from = clientSendTime)`. Khác 5 scope kia ở ngữ
> nghĩa thời gian — nhãn có hiệu lực **từ mốc gán trở đi**, quá khứ miễn nhiễm; server nạp vào
> profile nên mọi event sau đó tự mang nhãn (agg = một câu GROUP BY, zero join).
> ⚠ **Cần loader chốt**: dùng chung event `label_assign` này (client đề xuất — cùng hình dạng, một
> registry, một bộ hàng rào) hay tách event `f_sdk_user_label` riêng như §14 phác. Client đang gửi
> theo phương án dùng chung; đổi sang tách chỉ là đổi event id.
> Client tự áp trần **12 key** nhãn user (§14 chốt 10-15) — nhãn user đi kèm mọi dòng event nên
> mỗi key là một cột phình trên toàn bộ dữ liệu.
>
> `sessionUid` và `playTurnId` KHÔNG nằm trong bảng vì chúng đã được đóng tự động lên mọi log —
> ba id trên thì phải đóng tường minh, và đặt đúng tên key như các event khác để server join bằng
> cột sẵn có.
>
> **Giới hạn theo thiết kế:** chỉ dán được cho instance **CÒN MỞ** — SDK không giữ id của phiên/lượt
> đã đóng. `Label.Turn` lúc không có lượt nào mở thì SDK cảnh báo và **không gửi** (nhãn không có
> `playTurnId` thì server không biết dán vào đâu). Nhãn cho instance quá khứ đi kênh khác.

### 5.12. (Mục dự phòng)

Các biến thể log "mở rộng" (ad mở rộng, resource mở rộng, puzzle level) **không tạo `event` mới** — chúng tái sử dụng các `event` ở trên (`f_sdk_ads_data`, `f_sdk_resource_data`, `f_sdk_level_data`) nhưng điền thêm các trường optional đã liệt kê (ví dụ `hasClick`, `timeShow`, `valueBefore`, `valueAfter`, `detail`, `boostersUsed`...).

---

## 6. Response trả về

### 6.1. Endpoint đơn — `/event-log-v2`

Trả về **plain text** (không phải JSON). Thành công khi body đúng bằng chuỗi:

```
Request processed successfully.
```

(chấp nhận cả biến thể có ký tự xuống dòng `\n` ở cuối). Mọi giá trị khác coi là **thất bại**.

### 6.2. Endpoint batch — `/batch/event-log-v2`

Trả về **JSON**:

```json
{
  "successCount": 10,
  "failCount": 2,
  "errors": [
    { "exception": "mô tả lỗi", "data": "payload của message bị lỗi" }
  ]
}
```

| Key | Kiểu | Mô tả |
|---|---|---|
| `successCount` | int | Số message xử lý thành công. |
| `failCount` | int | Số message lỗi. |
| `errors` | array | Danh sách lỗi; rỗng nếu không có lỗi. |
| `errors[].exception` | string | Mô tả/loại lỗi. |
| `errors[].data` | string | Nội dung message gây lỗi. |

Quy ước phía client hiện tại: nếu `errors` rỗng coi như cả batch thành công; nếu `errors` có phần tử thì log lỗi tương ứng.

---

## 7. Ví dụ `data` đã giải mã (đọc cho dễ)

Ví dụ nội dung object **bên trong** `data` của một event `f_sdk_ads_data` (đã un-escape để dễ đọc — trên wire nó là một string):

```json
{
  "event": "f_sdk_ads_data",
  "logTypeCreateId": 12,
  "logTypeSendId": 12,
  "uuid": "0f4c2b9a-1d3e-4f56-8a7b-9c0d1e2f3a4b",
  "createdDateLocal": "2026-06-26 14:30:00+07:00",

  "accountId": "acc_123456",
  "level": 47,
  "maxPassedLevel": 47,
  "installVersion": "1.0.0",
  "installDay": "2026-06-01 00:00:00+00:00",
  "activeDays": 25,
  "sessionId": 8,
  "totalPlayTime": 36000,
  "platform": "Android",
  "appVersion": "1.4.2",
  "deviceModel": "SM-G991B",

  "type": "Interstitial",
  "adWhere": "level_complete",
  "adWhen": "after_win",
  "adRev": 0.0123,
  "impressionCount": 1,
  "adNetwork": "applovin",
  "currentLevel": 47
}
```

(Các trường central user params được rút gọn trong ví dụ; thực tế đầy đủ như mục 4.2.)
