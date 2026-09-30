# Gửi loader — bộ event quảng cáo: mô tả + kết quả truy vấn mong đợi

Nhờ các bạn chạy đối chiếu bộ event ad theo bảng ở §3. Mục đích: xác nhận bằng **dữ liệu thật**
những điều hiện bên mình mới chỉ **suy ra từ code**, trước khi trả lời bên data và trước khi chốt
lại hợp đồng trường `floor`.

Đề nghị chạy trên **1 ngày trọn vẹn** (không dùng cửa sổ vài giờ — lý do ở §2), chia theo
`package_name` × `platform` × `app_version`.

## 1. Bảy event của một lần xem ad

Vòng đời một **ad view**: `request → show → impression → close`. Khoá nối là **`adViewId`** (SDK
sinh ở mốc request, đóng lên mọi mốc sau).

| # | Event | Bắn khi | Trường chính |
|---|---|---|---|
| 1 | `f_sdk_ad_request_data` | Mediation mở một **đợt xin ad** | `adViewId`, `type`, `adWhere`, `adWhen`, `adMediation`, `networkType` |
| 2 | `f_sdk_ad_show_data` | Gọi hiển thị (chỉ inter/rewarded/AppOpen; banner không có) | `adViewId`, `type`, `adWhere`, `adMediation`, `requestToShowMs` |
| 3 | `f_sdk_ads_data` | **Impression** — ad lên hình, có doanh thu | `adViewId`, `type`, `adWhere`, `adWhen`, `adRev`, `impressionCount`, `adNetwork`, `adMediation`, `adUnitId`, `waterfallPosition`, `fillLatencyMs`, `adPrecision`, `adCountry` |
| 4 | `f_sdk_ad_close_data` | Ad đóng, người chơi về game | `adViewId`, `type`, `adWhere`, `shownDurationSec`, `hasClick`, `adCompleted` |
| 5 | `f_sdk_ad_load_success` | **Bản tin CỤM** — các lần load THÀNH CÔNG của một unit | `adType`, `adUnitId`, `floor`, `networkName`, `count`, `totalLoadingMs` |
| 6 | `f_sdk_ad_load_fail` | **Bản tin CỤM** — các lần load THẤT BẠI | `adType`, `adUnitId`, `floor`, `count`, `totalLoadingMs`, `lastErrorMess` |
| 7 | `f_sdk_ad_view_label` | Nhãn gắn vào một ad view | `adViewId`, `labelKey` = `load_error` (bỏ cuộc cả đợt) / `show_error` (show hỏng), `labelValue` = mã lỗi |

Ghi chú quan trọng:

- **Event 5 và 6 KHÔNG thuộc vòng đời ad view** — chúng không có `adViewId`, là telemetry cho từng
  lần gọi load của từng ad unit, dùng để chỉnh giá sàn. Nối với vòng đời qua `adUnitId`.
- **Banner**: chỉ có `request` + `impression`. Một banner instance = một `adViewId`, mọi lần
  refresh dùng lại đúng id đó → nhiều dòng impression cùng một `adViewId` là ĐÚNG.
- **`impressionCount`**: banner đi đường gộp cụm, một dòng có thể mang nhiều impression.

## 2. Hai đặc điểm phải biết trước khi đọc số

1. **Event 5, 6 và impression banner là bản tin GỘP.** Một dòng = `count` (hoặc
   `impressionCount`) lần. **Luôn `SUM(count)`, không `COUNT(*)`.**
2. **Cụm chỉ được gửi khi app xuống nền**, và không lưu lại nếu app bị kill cứng. Cửa sổ vài giờ
   sẽ hụt các phiên đang chơi → đọc theo ngày trở lên.

## 3. Kết quả truy vấn mong đợi

Nhờ chạy và điền cột "Thực tế". Cột "Mong đợi" là điều bên mình suy từ code — chỗ nào lệch là chỗ
cần đào tiếp.

### 3a. Vòng đời ad view (event 1–4, 7)

| # | Phép kiểm | Mong đợi | Lệch thì nghĩa là |
|---|---|---|---|
| A1 | Mỗi `adViewId` có bao nhiêu dòng `request` | Đúng **1** | >1 là id bị trùng giữa các lần xem (lỗi cũ từng thấy 5%) |
| A2 | Số `adViewId` của `show` **không** có trong `request` | **0** | Show không đi từ request nào — mất mốc request |
| A3 | Số `adViewId` của `impression` **không** có trong `request` | **0** | Impression mồ côi, không quy được về đợt xin ad |
| A4 | Số `adViewId` của `close` **không** có trong `show` | **0** (trừ banner, banner không có show) | Log close của view chưa từng hiển thị |
| A5 | Tỉ lệ `request` : `impression` mỗi user mỗi ngày (tách theo `type`) | **dưới ~5:1** | Cao hơn nhiều = mốc request bị gọi lặp trong vòng retry. Bản Mediation cũ từng ~40:1, bản mới đã có van chặn — **đây là phép kiểm chính để biết bản mới đã lên chưa** |
| A6 | `show` không có impression cùng id / tổng `show` | **dưới vài %** | Tỉ lệ hiển thị hỏng thật |
| A7 | `request` không có impression cùng id / tổng `request` | Là **fill-fail theo lượt xem** — nhờ báo con số | Dùng thay cho fill rate khi A5 đạt |
| A8 | Rewarded: phân bố `adCompleted` trên `close` | Có **cả `true` lẫn `false`** | Chỉ toàn `true` = vẫn đang hardcode, bản cũ |
| A9 | Số dòng `f_sdk_ad_view_label` `labelKey = load_error` so với số `adViewId` | **≤ 1 nhãn / view** | >1 là spam kênh nhãn |
| A10 | Có dòng `request`/`impression` với `type = Banner` và `type = AppOpen` không | **Có** | Thiếu = hai format đó chưa nối vòng đời |
| A11 | Banner: số impression trung bình trên một `adViewId` | **> 1** | =1 nghĩa là mỗi lần refresh lại mở view mới, sai ngữ nghĩa |

### 3b. Telemetry load (event 5, 6)

| # | Phép kiểm | Mong đợi | Lệch thì nghĩa là |
|---|---|---|---|
| B1 | `SUM(count)` so với `COUNT(*)`, theo từng event | fail: `SUM(count)` lớn hơn nhiều lần; success: xấp xỉ bằng | Xác nhận lại kết luận cũ (21/09: fail 2,35 triệu dòng = 8,39 triệu lần) |
| B2 | Fill rate theo lần load: `SUM(count)` success / `SUM(count)` (success + fail) | Nhờ báo số, tách theo `adType` và `adUnitId` | |
| B3 | Phân bố `floor`: `> 0`, `= 0`, **không có key** — theo `app_version` và theo prefix `adUnitId` | Dòng thiếu key dồn ở unit AdMob (`ca-app-pub-…`) | Như kết quả 21/09 |
| B4 | **Dải giá trị `floor`** (min / max / vài giá trị phổ biến) theo `app_version` | Hiện tại: số tiền (`1.5`–`50`…). Bản Mediation mới: hệ số (`1.0`–`3.0`, phần lớn là `1.2`, `1.4`) | **Đây là phép kiểm mốc đổi đời dữ liệu** — xem §4 |
| B5 | `lastErrorMess` trên dòng fail: các mã hay gặp | Là mã lỗi ngắn, không phải câu dài | Câu dài = đang gửi `Message` thay vì `Code` |
| B6 | `totalLoadingMs / count` (thời gian load trung bình) theo `adType` | Số hợp lý (vài trăm ms đến vài giây) | 0 hàng loạt = mediation không truyền thời gian |

## 4. Cảnh báo: trường `floor` sắp đổi nghĩa

| Giai đoạn | `floor` chứa gì | Ví dụ |
|---|---|---|
| Hiện tại (Mediation ≤ 1.4.29) | **Số tiền** — hệ số × giá quảng cáo đo được trong phiên × 1000 | `37.37`, `49.83` |
| Bản mới (Mediation 1.4.31 trở đi) | **Hệ số bậc trong config** | `1.2`, `1.4` |

Lý do đổi: số tiền cũ lấy nền từ quảng cáo đầu tiên xem được trong phiên, nên cùng một bậc mà mỗi
máy, mỗi ngày ra một số khác nhau, và đầu phiên ra `0`. Không group được.

Vì vậy nhờ các bạn:

1. **Chạy B4 định kỳ** để phát hiện đúng ngày dải giá trị đổi, rồi ghi mốc theo `app_version`.
2. Khi phân tích, **group theo `(adMediation/adUnitId, floor)`**, đừng group mỗi `floor` — hệ số
   của MAX và của AdMob tính trên hai nền khác nhau, không so trực tiếp được.
3. `0` và **không có key** cùng nghĩa "**không đặt giá sàn**" → `COALESCE(floor, 0)`.

Hai trường đang cân nhắc thêm, nhờ các bạn cho ý kiến trước:

- **`adMediation` trên hai event load** — hiện không có, đang phải đoán mediation bằng prefix của
  `adUnitId`.
- **Một trường riêng cho giá sàn bằng USD**, nếu MO vẫn cần số tiền bên cạnh hệ số.

## 5. Câu hỏi kèm theo

1. Kết quả A5 hiện tại là bao nhiêu (để biết bản Mediation có van đã lên production chưa)?
2. Ngưỡng "bỏ cuộc cả đợt" đang do Mediation tự đặt: **6 lần load fail liên tiếp**, khoảng 2 phút.
   Các bạn và bên data có đồng ý lấy mốc này cho công thức `fill rate = 1 − load_error / request`
   không?
3. Có thấy `event_extra_props` của hai event load bị thiếu trường nào so với §1 không?

---
*Ack/câu hỏi gửi lại như lệ; file xoá khi xong.*
