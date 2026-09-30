# Trả lời `Request-Loader-AdEventAudit.md` — kết quả A1–A11, B1–B6

> **Người chạy:** đội stools (làm thay loader) · **Ngày chạy:** 2026-09-23
> **Nguồn:** silver `dwh_system.f_sdk_event_general`
> **Khoảng dữ liệu:** 1 ngày trọn vẹn — `created_day = 2026-09-22`, **chỉ package `com.fc.sdk.block.escape`**
> (951.125 dòng event ad; **931.601 android + 17.727 ios**). Mọi phép kiểm vòng đời A1–A11 lọc
> `ad_view_id != ''` nên **trên thực tế là android-only** — lý do ở §0. Muốn mở rộng thì chạy lại query ở §6.

## 0. Số dòng từng event (22/09) và một việc phải nói ngay: **iOS chưa lên bộ event mới**

| Event | android | ios | user | Ghi chú |
|---|---:|---:|---:|---|
| `f_sdk_ad_load_fail` | 378.676 | 0 | 3.579 | = **1.483.363 lần load** (bản tin cụm) |
| `f_sdk_ad_load_success` | 185.775 | 0 | 3.425 | = **188.694 lần load** |
| `f_sdk_ad_request_data` | 154.709 | 0 | 3.768 | |
| `f_sdk_ad_impression` | 145.794 | **8.649** | 3.752 / 961 | = **518.461** lần hiển thị (android, gồm banner gộp) + 8.468 (ios) |
| `f_sdk_ad_show_data` | 33.597 | 0 | 2.721 | |
| `f_sdk_ad_close_data` | 28.890 | 0 | 2.490 | |
| `f_sdk_ad_called` | 3.797 | **9.078** | 418 / 1.012 | event CŨ, không có `adViewId` |
| `f_sdk_ad_view_label` | 2.160 | 0 | 601 | |
| **Tổng** | **931.601** | **17.727** | | |

**Toàn bộ 17.727 dòng ios chỉ nằm ở hai event CŨ** (`f_sdk_ad_called` ← wire `f_sdk_ads_called_data`, và
`f_sdk_ad_impression` ← wire `f_sdk_ads_data`), **app_version ≤ 2.6.5**, và **0 dòng có `adViewId`**.
Không có một dòng `request` / `show` / `close` / `label` / `load_*` nào từ ios.

→ **Bộ event vòng đời ad hiện là android-only.** Mọi kết luận A1–A11 dưới đây vì thế chỉ nói về android;
ios chưa đo được gì. Nhờ các bạn xác nhận bản iOS mang SDK mới đã phát hành chưa.

## Tóm tắt — 6 điều cần biết trước

1. **Van Mediation ĐÃ lên production.** A5 trên 2.7.2: Interstitial **1,4 : 1**, Reward **1,8 : 1**
   (trung vị mỗi user 1,2 và 1,3). Bản 2.7.0 vẫn còn traffic và vẫn là **36,9 : 1** và **69,1 : 1**
   (trung vị 27,0 và 39,4; có user bắn **2.456** request/ngày). Ngưỡng "dưới 5:1" — **đạt từ 2.7.1**.
2. **Tên event và tên trường trong kho KHÁC §1.** `f_sdk_ads_data` nằm trong kho dưới tên
   **`f_sdk_ad_impression`**, và `type` / `adRev` / `adWhere` / `adWhen` / `adMediation` / `adNetwork` /
   `shownDurationSec` / `labelKey` **không nằm trong `event_extra_props`** — chúng được nâng lên **cột riêng**.
   Bảng ánh xạ ở §1. Phần lớn "trường thiếu" mà các bạn nghi là **bị đổi tên**, không mất.
3. **Chưa có `AppOpen`.** Không một dòng request/impression/show/close nào mang `type = AppOpen` (A10 trượt
   một nửa). Banner thì có đủ và A11 đạt rõ.
4. **`hasClick` KHÔNG BAO GIỜ gửi `false`** — 0 dòng false trên cả ba bản, chỉ có `true` hoặc vắng key.
   Bên đọc số phải hiểu vắng = không click (`COALESCE(has_click, false)`), nếu không sẽ tính nhầm CTR.
5. **`floor` ĐÃ đổi đời — nhưng chỉ ở MAX, và mốc là `2.7.1`, không phải 1.4.31.** Xem §4: trên 2.7.0 unit
   MAX cho giá trị liên tục `0,02 – 1.926`; từ 2.7.1 thành **rời rạc `1,1 – 13,0`, phổ biến `5,0 / 3,0 / 1,5 / 2,0`**.
   Unit AdMob **chưa đổi** — vẫn liên tục `0 – 5.359`. Hai nền đang sống song song trong cùng một ngày.
6. **Ba cách tính fill rate cho ba con số rất lệch nhau** (98,6% / 21,5% / 10,0%) — xem §5 câu 2. Phải chốt
   nghĩa trước khi đưa số cho MO.

---

## 1. Ánh xạ tên: wire SDK → cột trong kho

Đây là việc bắt buộc đọc trước, nếu không mọi query sẽ ra rỗng.

| §1 gọi là | Trong kho | Ghi chú |
|---|---|---|
| event `f_sdk_ads_data` | **event `f_sdk_ad_impression`** | loader ĐỔI TÊN (rule `ads_data`) |
| `type` | cột **`sub_event`** | 100% dòng có, không còn trong extra |
| `adNetwork` (impression) | cột **`sub_event_2`** | |
| `adMediation` (request/show/close) | cột **`sub_event_2`** | 100% dòng có |
| `adRev` | cột **`event_rev`** | |
| `adWhere` | cột **`event_where`** | |
| `adWhen` | cột **`event_when`** | |
| `timeShow` (impression) | cột **`event_duration`** (giây) | |
| `shownDurationSec` (close) | cột **`event_duration`** (giây) | |
| `fillLatencyMs` | cột **`event_amount`** (đổi sang **GIÂY**) | `fill_latency_ms` vẫn giữ trong extra |
| `requestToShowMs` | cột **`event_amount`** (đổi sang **GIÂY**) | `request_to_show_ms` vẫn giữ trong extra |
| `labelKey` | cột **`sub_event`** | `label_value` giữ nguyên trong extra |
| `adViewId`, `impressionCount`, `adUnitId`, `waterfallPosition`, `adPrecision`, `adCountry`, `hasClick`, `adCompleted`, `networkType`, `floor`, `count`, `networkName`, `totalLoadingMs`, `lastErrorMess` | trong `event_extra_props` | tên snake_case |

### Độ phủ thật của từng trường (22/09)

| Event | Dòng | Có đủ | Thiếu / phủ thấp |
|---|---:|---|---|
| `f_sdk_ad_request_data` | 154.677 | `sub_event` 100%, `sub_event_2` 100%, `ad_view_id` 100%, `network_type` 100% | **`event_where`/`event_when` 0%** (`adWhere`/`adWhen` không được gửi) |
| `f_sdk_ad_show_data` | 33.595 | `sub_event` 100%, `sub_event_2` 100%, `event_where` 100%, `ad_view_id` 100% | `request_to_show_ms` **85%** (15% vắng); `event_when` 0% |
| `f_sdk_ad_impression` | 154.428 | `sub_event`/`sub_event_2`/`event_rev`/`event_where` 100%, `ad_when` 99%, `impression_count` 99%, `ad_precision` 100%, `ad_country` 100%, `ad_mediation` 100% | **`ad_view_id` 82%**, **`ad_unit_id` 20%**, **`fill_latency_ms` 17%**, **`waterfall_position` 9%**, `time_show`→`event_duration` **8%**, `has_click` 8% |
| `f_sdk_ad_close_data` | 28.890 | `sub_event`/`sub_event_2`/`event_where` 100%, `shown_duration_sec` **98%**, `ad_view_id` 100% | `ad_completed` **72%**, `has_click` **19%** |
| `f_sdk_ad_view_label` | 2.156 | `sub_event`(=labelKey) 100%, `label_value` 100%, `ad_view_id` 100% | — |
| `f_sdk_ad_load_success` | 185.738 | `ad_unit_id`/`ad_type`/`count`/`network_name`/`total_loading_ms` 100% | `floor` **85%**; **không có `ad_mediation`** |
| `f_sdk_ad_load_fail` | 378.563 | `ad_unit_id`/`ad_type`/`count`/`total_loading_ms`/`last_error_mess` 100% | `floor` **98%**; **không có `ad_mediation`**, không có `network_name` |

⚠ **`ad_view_id` vắng trên 27.794 dòng impression (18%) — nhưng KHÔNG phải lỗi bản hiện tại**:

| Bản | Loại | Dòng | Vắng |
|---|---|---:|---:|
| ≤ 2.6.9 | cả ba loại | 21.480 | **100%** |
| 2.7.0 | Banner | 6.316 | **100%** |
| 2.7.0 | Inter / Reward | 2.018 | 0% |
| **2.7.1 trở đi** | cả ba loại | 124.619 | **0%** (đúng 1 dòng) |

→ Banner chỉ được nối vòng đời **từ 2.7.1**; bản ≤2.6.9 chưa có `adViewId` bao giờ. Trong 21.480 dòng
"≤2.6.9" có **8.649 dòng ios** (§0) — phía ios chưa có `adViewId` ở bất kỳ bản nào.

📌 Còn một event nữa các bạn không kể trong §1: **`f_sdk_ad_called`** (12.875 dòng = 9.078 ios + 3.797
android, từ wire `f_sdk_ads_called_data`) — cũng mang nghĩa "request", **không có `ad_view_id`**. Ai đếm
request mà gom cả hai event sẽ **đếm đôi**. Nhờ xác nhận event này còn dùng hay đã bỏ — xem thêm §0, đây là
event duy nhất phía ios còn bắn cho vòng đời ad.

---

## 2. Kết quả 3a — vòng đời ad view

Đơn vị: **một `adViewId`** (đã loại dòng không có `adViewId`).

| # | Phép kiểm | Mong đợi | **Thực tế (22/09, 2.7.2)** | Đạt? |
|---|---|---|---|---|
| A1 | `adViewId` có >1 dòng request | đúng 1 | Inter **47 / 26.919 = 0,17%**; Reward 40 / 22.354 = 0,18%; Banner 23 / 12.038 = 0,19% | ✅ (lỗi 5% cũ đã hết) |
| A2 | show không có request | 0 | Inter **2.833**; Reward **1.989** (**15,0%** / **16,4%** số view có show) | ❌ |
| A3 | impression không có request | 0 | Inter **2.678**; Reward **1.865**; Banner 7 | ❌ |
| A4 | close không có show | 0 (trừ Banner) | Inter **22**; Reward **31**; Banner 0 | ✅ gần như sạch |
| A5 | request : impression | < 5:1 | **Inter 1,4:1 · Reward 1,8:1 · Banner 0,1:1** | ✅ |
| A6 | show không có impression | dưới vài % | Inter **1.929/18.851 = 10,2%**; Reward **752/12.142 = 6,2%** | ⚠ hơi cao |
| A7 | request không có impression | báo số | **Inter 40,9% · Reward 53,2% · Banner 22,9%** | — |
| A8 | Reward: phân bố `adCompleted` | có cả true/false | Reward 2.7.2: **10.721 true / 478 false / 0 vắng** | ✅ hết hardcode |
| A9 | nhãn `load_error` / view | ≤ 1 | **1.897 nhãn / 1.883 view = 1,007** (`show_error`: 259/248) | ✅ |
| A10 | có `Banner` và `AppOpen` | có | Banner ✅ (12.025 request / 111.597 impression) · **AppOpen ❌ KHÔNG CÓ DÒNG NÀO** | ❌ một nửa |
| A11 | Banner: impression TB / view | > 1 | **10,22 dòng impression/view**, và theo `impressionCount` là **44,99 lần/view** | ✅ |

### A5 chi tiết — đây là câu trả lời cho §5 câu 1

| Loại | Bản | request | impression | user | **tỉ lệ** | req/user | **trung vị req:imp mỗi user** | p90 | req nhiều nhất của 1 user |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Interstitial | **2.7.0** | 47.581 | 1.288 | 272 | **36,9 : 1** | 174,9 | **27,0** | 128,0 | **1.727** |
| Interstitial | 2.7.1 | 191 | 121 | 28 | 1,6 : 1 | 6,8 | 1,3 | 2,7 | 28 |
| Interstitial | **2.7.2** | 24.049 | 16.958 | 3.493 | **1,4 : 1** | 6,9 | **1,2** | 3,0 | 116 |
| Reward | **2.7.0** | 50.341 | 729 | 272 | **69,1 : 1** | 185,1 | **39,4** | 292,4 | **2.456** |
| Reward | 2.7.1 | 152 | 64 | 28 | 2,4 : 1 | 5,4 | 1,9 | 5,6 | 21 |
| Reward | **2.7.2** | 20.338 | 11.432 | 3.491 | **1,8 : 1** | 5,8 | **1,3** | 4,0 | 103 |
| Banner | 2.7.2 | 11.924 | 95.282 | 3.499 | 0,1 : 1 | 3,4 | 0,2 | 1,0 | 103 |

**Đọc:** van có thật và hiệu quả. 2.7.0 chỉ còn **272 user** (so với **3.493** user của 2.7.2) nhưng gánh
**63% tổng số dòng request** của cả ngày (97.922 / 154.677). Ai gộp mọi `app_version` sẽ ra
**Interstitial 2,8:1 · Reward 4,1:1** — vẫn **lọt** ngưỡng 5:1 và **che mất** việc 2.7.0 đang ở 36,9:1 và
69,1:1. Bắt buộc tách theo `app_version`.

### Hai chỗ A2/A3 trượt — nằm ở bản MỚI, không phải bản cũ, và KHÔNG phải lỗi đo

`show`/`impression` không có `request` cùng id: **2.7.0 chỉ 183 + 172 ca (12,3% số view có show); 2.7.2 là
2.833 + 2.678 ca (15,0%)**. Ngược chiều trực giác — bản mới nhiều hơn bản cũ.

**Đã loại giả thuyết "do cắt theo ngày"**: chạy lại trên cửa sổ **hai ngày** (21–22/09) để dòng `request`
của view vắt qua nửa đêm cũng được tính, con số chỉ giảm từ 2.897 → **2.833** (−2,2%) và 2.039 → **1.989**
(−2,5%). Ranh giới ngày hấp thụ được ~2%, phần còn lại là thật.

Manh mối kèm theo — **khoảng cách request → show (trung vị)**:

| Bản | Interstitial | Reward |
|---|---:|---:|
| 2.7.0 | **26 giây** | **19 giây** |
| 2.7.2 | **219 giây** | **168 giây** |

Bản mới xin ad **sớm hơn ~8 lần** trước khi hiển thị — đúng dáng của việc nạp sẵn (cache). Từ đó hai khả
năng, bên mình **không phân biệt được từ dữ liệu kho**, nhờ Mediation soi:

(a) van gom nhiều lần hiển thị vào **một đợt xin ad** đã nạp sẵn, nên view thứ hai trở đi **không bắn event
    request** — nếu vậy A2/A3 **không phải lỗi**, mà là **ngữ nghĩa mới** và cần ghi vào hợp đồng (kéo theo:
    "1 request = 1 lần xem" không còn đúng, mọi công thức chia cho `request` phải xem lại);
(b) event request bị rơi thật.

Phân biệt được bằng: với view có show mà không có request, `adViewId` của nó có **trùng** với `adViewId` của
một view khác **có** request trong cùng phiên không. Trùng ⇒ (a); không trùng ⇒ (b). Bên mình không có mốc
phiên của Mediation nên không kiểm được.

### A8 — thêm một điểm ngoài bảng

| Loại | Bản | close | `adCompleted` true | false | vắng | `hasClick` true | false | vắng |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Reward | 2.7.2 | 11.199 | 10.721 | **478** | 0 | 1.551 | **0** | 9.648 |
| Interstitial | 2.7.2 | 15.622 | 8.484 | **0** | 7.138 | 3.556 | **0** | 12.066 |

- Reward đã hết hardcode (4,3% `false`) ✅
- **`hasClick` không bao giờ có giá trị `false`** trên bất kỳ loại/bản nào — chỉ `true` hoặc vắng key.
- Interstitial **không bao giờ có `adCompleted = false`** và vắng 46% — nhờ xác nhận Interstitial có định
  nghĩa "completed" không, hay trường này chỉ dành cho Reward.

---

## 3. Kết quả 3b — telemetry load

### B1 + B6

| Event | Loại | Dòng | **Lần load** (`SUM(count)`) | lần/dòng | **ms TB mỗi lần** |
|---|---|---:|---:|---:|---:|
| `f_sdk_ad_load_fail` | Interstitial | 196.503 | **754.059** | 3,84 | 4.353 |
| `f_sdk_ad_load_fail` | Reward | 182.082 | **728.245** | 4,00 | 5.446 |
| `f_sdk_ad_load_success` | Interstitial | 82.020 | 83.675 | 1,02 | **17.775** |
| `f_sdk_ad_load_success` | Reward | 103.719 | 104.983 | 1,01 | **19.882** |

✅ Khớp mong đợi: fail gộp ~4 lần/dòng, success ~1. **`COUNT(*)` sai gần 4 lần** trên event fail.

⚠ **B6 có số lạ**: load **thành công** trung bình **17,8–19,9 giây**, trong khi load **thất bại** chỉ
**4,3–5,4 giây**. Thành công lâu gấp 4 lần thất bại là ngược đời — nghi `totalLoadingMs` trên dòng success
đo từ lúc request đầu tiên của cả waterfall chứ không phải của lần load ấy. **Nhờ Mediation xác nhận ngữ
nghĩa**, nếu không mọi báo cáo "thời gian load" sẽ sai.

### B2 — fill rate theo **lần load**

| Loại | lần OK | lần fail | **fill** |
|---|---:|---:|---:|
| Interstitial | 83.675 | 754.064 | **9,99%** |
| Reward | 104.983 | 728.249 | **12,60%** |

### B3 — `floor` theo `app_version`

| Bản | dòng | vắng key | `= 0` | `> 0` | vắng ở unit AdMob |
|---|---:|---:|---:|---:|---:|
| 2.7.0 | 33.830 | 2.768 (8,2%) | 7.876 | 23.186 | **2.768 / 2.768 = 100%** |
| 2.7.1 | 4.700 | 229 (4,9%) | 714 | 3.757 | **229 / 229 = 100%** |
| 2.7.2 | 525.800 | 32.445 (6,2%) | 79.517 | 413.838 | **32.390 / 32.445 = 99,8%** |

✅ Xác nhận lại kết luận 21/09: dòng vắng `floor` **gần như toàn bộ là unit AdMob**, rải đều mọi bản,
không dồn ở bản cũ.

### B5 — `lastErrorMess`

| Giá trị | dòng | lần | dài (ký tự) |
|---|---:|---:|---:|
| `NoFill` | 242.489 | 1.062.762 | 6 |
| `NetworkError` | 15.509 | 258.473 | 12 |
| `3` | 92.813 | 98.879 | 1 |
| `0` | 5.608 | 25.228 | 1 |
| `Unspecified` | 1.205 | 10.736 | 11 |
| `1` | 9.552 | 9.949 | 1 |
| `No ads meet eCPM…` | 6.644 | 6.816 | **75** |
| `Error while conne…` | 242 | 1.771 | **126** |
| `AdLoadFailed` / `InvalidAdUnitId` / `Unable to obtain…` | 1.853 | 3.676 | 12 / 15 / 36 |

❌ **Không đạt**: có ba dạng trộn lẫn — mã chữ ngắn, **số trần** (`0`/`1`/`2`/`3` — 108k dòng, không có từ
điển), và **câu dài tới 126 ký tự**. Nhờ client chuẩn hoá về một dạng và gửi kèm từ điển cho dạng số.

---

## 4. `floor` — mốc đổi đời ĐÃ XẢY RA, tại `2.7.1`, và chỉ ở MAX

Chỉ tính dòng `floor > 0`:

| Bản | Mediation (suy từ prefix unit) | dòng | nhỏ nhất | lớn nhất | **giá trị phổ biến** |
|---|---|---:|---:|---:|---|
| 2.7.0 | MAX/khác | 11.305 | 0,02 | **1.926,33** | 6,09 · 20,06 · 0,89 … (liên tục) |
| 2.7.0 | AdMob | 11.881 | 0,01 | 1.155,80 | 0,09 · 0,07 · 0,66 … (liên tục) |
| **2.7.1** | **MAX/khác** | 2.394 | **1,10** | **13,00** | **5,0 · 3,0 · 1,5 · 2,0** (rời rạc) |
| 2.7.1 | AdMob | 1.363 | 0,09 | 253,11 | 1,24 · 2,38 · 2,59 … (liên tục) |
| **2.7.2** | **MAX/khác** | 250.119 | **1,10** | **13,00** | **5,0 · 3,0 · 1,5 · 2,0** (rời rạc) |
| 2.7.2 | AdMob | 163.719 | 0,00 | 5.359,52 | 0,35 · 1,73 · 4,57 … (liên tục) |

**Ba điều cần chốt lại với §4 của các bạn:**

1. Bên mình chỉ đo được `app_version`, không đo được bản Mediation — và **trong dữ liệu mốc đổi rơi đúng vào
   `app_version 2.7.1`**, **chỉ áp cho unit MAX**. Nhờ các bạn xác nhận bản Mediation nào đi kèm 2.7.1 để ghi
   mốc cho khớp hai hệ đánh số.
2. Dải hệ số thực tế là **1,1 – 13,0**, **không phải 1,0 – 3,0** như §4 dự đoán, và phổ biến là
   **5,0 / 3,0 / 1,5 / 2,0**, không phải 1,2 / 1,4. Nhờ Mediation xác nhận bảng bậc thật.
3. **AdMob chưa đổi** — vẫn là số tiền liên tục tới 5.359. Nên khuyến nghị §4 điểm 2 (group theo
   `(mediation, floor)`) là **bắt buộc**, không phải "nên".

✅ Đồng ý quy ước `COALESCE(floor, 0)` — B3 xác nhận dòng vắng là luồng AdMob không đặt giá sàn.

**Hai trường các bạn đang cân nhắc — ý kiến bên mình:**

- **`adMediation` trên hai event load: RẤT NÊN THÊM.** Đo xong mới thấy đây là trường chặn: hiện bên mình
  phải suy mediation bằng `startsWith(ad_unit_id, 'ca-app-pub-')`, mà suy như thế **chỉ tách được AdMob với
  "phần còn lại"** — không tách được MAX với mediation thứ ba nào xuất hiện sau này, và vỡ ngay nếu AdMob đổi
  định dạng unit id. Mà §4 lại bắt buộc group theo mediation mới đọc được `floor`. Thêm trường này là gỡ
  đúng nút thắt.
- **Trường USD riêng bên cạnh hệ số: NÊN, nếu MO thật sự cần tiền.** Đừng nhồi hai nghĩa vào `floor` thêm
  lần nữa — chính vì thế mà giờ có hai nền trong cùng một cột. Đặt tên rõ (`floor_usd`) và để `floor` thuần
  hệ số.

---

## 5. Trả lời §5

**Câu 1 — A5 hiện tại bao nhiêu, van đã lên chưa?**
Đã lên, từ **2.7.1**. Trên **2.7.2**: Interstitial **1,4:1**, Reward **1,8:1**, trung vị mỗi user **1,2** và
**1,3**, p90 **3,0** và **4,0** — dưới ngưỡng 5:1 khá xa. Bản **2.7.0 vẫn đang chạy** với **36,9:1** và
**69,1:1**; nó chỉ còn 272 user nhưng **chiếm 63% tổng dòng request của cả ngày**, nên còn bản đó thì mọi số
gộp-mọi-bản vẫn méo (gộp lại ra 2,8:1 và 4,1:1 — lọt ngưỡng một cách giả tạo). Đề nghị ép nâng cấp hoặc loại
2.7.0 khỏi báo cáo.

**Câu 2 — có đồng ý lấy mốc "6 lần load fail liên tiếp" cho `fill rate = 1 − load_error / request` không?**
**Đây là quyết định hợp đồng giữa owner bên mình và bên data, bên mình không tự chốt.** Nhưng đưa số trước để
các bên chốt có căn cứ — ba cách tính đang cho ba kết quả rất khác nhau, **trên cùng một ngày, cùng một gói**:

| Công thức | Interstitial | Reward | Nó thật sự đo gì |
|---|---:|---:|---|
| `1 − load_error / request` (công thức đề xuất) | **98,6%** | **98,7%** | tỉ lệ đợt xin ad **không bỏ cuộc hẳn** |
| `1 − (request không có impression) / request` (A7) | **59,1%** | **46,8%** | tỉ lệ đợt xin ad **ra được ad lên hình** |
| `SUM(count)` success / tổng `SUM(count)` (B2) | **10,0%** | **12,6%** | tỉ lệ **mỗi lần gọi load** thành công |

Chênh 98,6% với 59,1% không phải sai số — chúng đo hai thứ khác nhau. Công thức đề xuất đo **"đợt xin ad có
bỏ cuộc không"**; nó **không** đo "người chơi có thấy ad không". Nếu MO hiểu "fill rate" theo nghĩa thứ hai
mà nhận con số thứ nhất thì lệch **gần 40 điểm**. Đề nghị: đặt tên riêng cho từng chỉ số
(`request_giveup_rate` / `view_fill_rate` / `load_fill_rate`) thay vì dùng chung chữ "fill rate".

**Câu 3 — `event_extra_props` của hai event load có thiếu trường nào so với §1 không?**
So với §1, hai event load **đủ** `adType`, `adUnitId`, `count`, `totalLoadingMs`, `networkName` (success),
`lastErrorMess` (fail) — **100% dòng**. Thiếu đúng hai thứ:
- **`adMediation`: 0% dòng** — đúng như §4 ghi (xem ý kiến ở §4).
- **`floor`: vắng 15% trên success, 2% trên fail** — 99,8% số vắng là unit AdMob (B3).
- Thêm: `networkName` **chỉ có trên success**, fail không có — nếu muốn phân tích fail theo network thì hiện
  không làm được.

---

## 6. Query đã chạy

```sql
-- Độ phủ trường: đổi danh sách key trong countIf(JSONType(j,'<key>') != 0)
SELECT event, count() AS n,
       countIf(ifNull(sub_event,'')   NOT IN ('','UNKNOWN')) AS c_sub_event,
       countIf(ifNull(sub_event_2,'') NOT IN ('','UNKNOWN')) AS c_sub_event_2,
       countIf(ifNull(event_where,'') NOT IN ('','UNKNOWN')) AS c_event_where,
       countIf(event_rev IS NOT NULL)      AS c_event_rev,
       countIf(event_duration IS NOT NULL) AS c_event_duration,
       countIf(event_amount IS NOT NULL)   AS c_event_amount,
       countIf(JSONType(j,'ad_view_id') != 0) AS x_ad_view_id
       /* … thêm key khác tương tự … */
FROM (SELECT event, sub_event, sub_event_2, event_where, event_rev, event_duration, event_amount,
             toJSONString(event_extra_props) AS j
      FROM dwh_system.f_sdk_event_general
      WHERE package_name = 'com.fc.sdk.block.escape' AND created_day = '2026-09-22'
        AND event IN ('f_sdk_ad_request_data','f_sdk_ad_show_data','f_sdk_ad_impression',
                      'f_sdk_ad_close_data','f_sdk_ad_load_success','f_sdk_ad_load_fail',
                      'f_sdk_ad_view_label','f_sdk_ad_called'))
GROUP BY event SETTINGS max_threads = 4;

-- A1–A7, A11: MỘT lần gom theo adViewId rồi mới đếm (đừng làm 7 phép anti-join riêng)
SELECT loai, ban, count() AS views,
       countIf(req > 1)                 AS a1,
       countIf(shw > 0 AND req = 0)     AS a2,
       countIf(imp > 0 AND req = 0)     AS a3,
       countIf(cls > 0 AND shw = 0)     AS a4,
       countIf(shw > 0)                 AS co_shw,
       countIf(shw > 0 AND imp = 0)     AS a6,
       countIf(req > 0)                 AS co_req,
       countIf(req > 0 AND imp = 0)     AS a7,
       round(avgIf(imp, imp > 0), 2)    AS a11_dong_impression,
       round(avgIf(imp_n, imp > 0), 2)  AS a11_so_impression
FROM (
  SELECT jsonb_extract_string(event_extra_props,'$.ad_view_id') AS vid,
         anyIf(sub_event, ifNull(sub_event,'') NOT IN ('','UNKNOWN')) AS loai,
         any(app_version) AS ban,
         countIf(event = 'f_sdk_ad_request_data') AS req,
         countIf(event = 'f_sdk_ad_show_data')    AS shw,
         countIf(event = 'f_sdk_ad_impression')   AS imp,
         countIf(event = 'f_sdk_ad_close_data')   AS cls,
         -- greatest(...,1): jsonb_extract_int64 trả 0 khi VẮNG key; 1% dòng impression vắng
         -- impression_count nhưng vẫn là 1 lần hiển thị thật, nên sàn về 1 thay vì bỏ qua
         sumIf(greatest(jsonb_extract_int64(event_extra_props,'$.impression_count'), 1),
               event = 'f_sdk_ad_impression')     AS imp_n
  FROM dwh_system.f_sdk_event_general
  WHERE package_name = 'com.fc.sdk.block.escape' AND created_day = '2026-09-22'
    AND event IN ('f_sdk_ad_request_data','f_sdk_ad_show_data','f_sdk_ad_impression','f_sdk_ad_close_data')
    AND jsonb_extract_string(event_extra_props,'$.ad_view_id') != ''   -- BẮT BUỘC: key vắng trả '' và gom hết vào 1 nhóm giả
  GROUP BY vid)
GROUP BY loai, ban SETTINGS max_threads = 4;

-- A5: tỉ lệ tổng + trung vị trên từng user (phải tách app_version, nếu không 2.7.0 làm méo hết)
SELECT loai, ban, count() AS users,
       round(median(req / greatest(imp, 1)), 1)          AS giua,
       round(quantile(0.9)(req / greatest(imp, 1)), 1)   AS p90,
       max(req) AS req_lon_nhat
FROM (SELECT account_id, sub_event AS loai, app_version AS ban,
             countIf(event = 'f_sdk_ad_request_data') AS req,
             countIf(event = 'f_sdk_ad_impression')   AS imp
      FROM dwh_system.f_sdk_event_general
      WHERE package_name = 'com.fc.sdk.block.escape' AND created_day = '2026-09-22'
        AND event IN ('f_sdk_ad_request_data','f_sdk_ad_impression')
      GROUP BY account_id, loai, ban)
WHERE req > 0 GROUP BY loai, ban SETTINGS max_threads = 4;

-- A8: adCompleted/hasClick là BOOL trong JSON — jsonb_extract_string trả '' và làm mọi số về 0
SELECT sub_event AS loai, app_version AS ban, count() AS dong,
       countIf(JSONExtractBool(j,'ad_completed'))                                   AS ht_true,
       countIf(JSONType(j,'ad_completed') = 'Bool' AND NOT JSONExtractBool(j,'ad_completed')) AS ht_false,
       countIf(JSONType(j,'ad_completed') = 0)                                      AS ht_vang,
       countIf(JSONExtractBool(j,'has_click'))                                      AS click_true,
       countIf(JSONType(j,'has_click') = 'Bool' AND NOT JSONExtractBool(j,'has_click'))       AS click_false
FROM (SELECT sub_event, app_version, toJSONString(event_extra_props) AS j
      FROM dwh_system.f_sdk_event_general
      WHERE package_name = 'com.fc.sdk.block.escape' AND created_day = '2026-09-22'
        AND event = 'f_sdk_ad_close_data')
GROUP BY loai, ban SETTINGS max_threads = 4;

-- B1/B2/B6 (luôn SUM(count), không COUNT(*)) và B3/B4 (floor theo bản × mediation)
SELECT app_version,
       if(startsWith(jsonb_extract_string(event_extra_props,'$.ad_unit_id'),'ca-app-pub-'),'AdMob','MAX/khac') AS med,
       count() AS dong,
       sum(jsonb_extract_int64(event_extra_props,'$.count')) AS lan,
       round(sum(jsonb_extract_int64(event_extra_props,'$.total_loading_ms'))
             / nullIf(sum(jsonb_extract_int64(event_extra_props,'$.count')), 0)) AS ms_tb,
       round(min(jsonb_extract_float64(event_extra_props,'$.floor')), 2) AS nho,
       round(max(jsonb_extract_float64(event_extra_props,'$.floor')), 2) AS lon,
       topK(4)(round(jsonb_extract_float64(event_extra_props,'$.floor'), 2)) AS pho_bien
FROM dwh_system.f_sdk_event_general
WHERE package_name = 'com.fc.sdk.block.escape' AND created_day = '2026-09-22'
  AND event IN ('f_sdk_ad_load_success','f_sdk_ad_load_fail')
  AND jsonb_extract_float64(event_extra_props,'$.floor') > 0
GROUP BY app_version, med SETTINGS max_threads = 4;
```

**Ba cái bẫy đã vấp khi chạy, ghi lại để lần sau khỏi mất công:**
1. `jsonb_extract_string` **không bao giờ trả NULL** — key vắng trả `''`. Mọi phép gom theo `ad_view_id` phải
   lọc `!= ''`, nếu không 27.794 dòng vắng gom thành **một** view giả và A1 báo trùng khống.
2. Trường **Bool** (`adCompleted`, `hasClick`) đọc bằng `jsonb_extract_string` ra `''` → đếm được 0 true và 0
   false, trông y hệt "đang hardcode". Phải dùng `JSONExtractBool`.
3. `toJSONString` tốn RAM — luôn khoanh `created_day` + `event` + `package_name` trước.
4. Mọi phép "X không có Y cùng id" phải chạy **thêm một lần trên cửa sổ 2 ngày** rồi so, vì cắt đúng 1 ngày
   sẽ chặt mất dòng `request` của view vắt qua nửa đêm. Ở đây cửa sổ 2 ngày chỉ hấp thụ ~2% nên A2/A3 là
   thật — nhưng nếu không kiểm thì không biết 2% hay 100%.

---
*Ack/câu hỏi gửi lại như lệ.*
