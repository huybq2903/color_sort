# Chốt hợp đồng bộ event quảng cáo v2 — gửi loader

Cảm ơn phần thử thật trên ByteHouse. Bên mình **nhận cả 5 điều kiện**, đã code xong phía SDK — bộ event ad v2 nằm ở **BigData 1.4.0** (chưa publish)
(test 322/324, 2 đỏ còn lại là của SystemInfo, không liên quan). Dưới đây là bản chốt để các bạn
**seed mapping rule trước**, rồi bên mình mới phát hành SDK — đúng thứ tự các bạn yêu cầu ở §3.4.

## 1. Hai event mới — tên và trường CẤP TRÊN (phần cần seed rule)

### `f_sdk_ad_show_attempt`

Lấy tên các bạn đề xuất (`f_sdk_ad_show_attempt`), bỏ `f_sdk_ad_show_request_data` vì lệch đúng
một chữ với `f_sdk_ad_request_data`.

| Key | Kiểu | Bắt buộc | Ghi chú |
|---|---|---|---|
| `type` | enum string | ✓ | Format ad — **seed vào `sub_event`** |
| `adWhere` | string | — | **seed vào `event_where`** |
| `adWhen` | string | — | **seed vào `event_when`** |
| `adMediation` | string | — | **seed vào `sub_event_2`** |
| `adAvailable` | bool | ✓ | Kho có ad sẵn lúc game hỏi không |
| `adViewId` | string | — | Chỉ có khi `adAvailable = true` |

### `f_sdk_ad_load_stats` (thay `f_sdk_ad_load_success` + `f_sdk_ad_load_fail`)

| Key | Kiểu | Bắt buộc | Ghi chú |
|---|---|---|---|
| `adType` | enum string | ✓ | **seed vào `sub_event`** |
| `adMediation` | string | — | **seed vào `sub_event_2`** — đúng nút thắt các bạn nêu, giờ nằm ở cấp trên |
| `units` | array&lt;object&gt; | ✓ | Ở lại trong `event_extra_props` |

Phần tử `units[]`: `u` (adUnitId) · `ok` · `fail` · `okMs` · `failMs` · `tier` · `floor` · `net` · `err`.

⚠ **Tách hai trục giá (chốt owner 23/09, sau góp ý của các bạn về trường USD riêng):**

| Khoá | Nghĩa | Ai gửi | Vắng nghĩa là |
|---|---|---|---|
| `tier` | **BẬC** trong config (hệ số 1.5 / 3.0 / 5.0) | Mọi mediation | Unit **không đặt** giá sàn |
| `floor` | **GIÁ SÀN THẬT**, USD/1000 impression — **trung bình** các lần trong dòng | Chỉ mediation biết thật (AdMob) | **Không biết** giá thật |

**Cập nhật 1.4.1 (hình dạng event KHÔNG đổi, chỉ đổi nghĩa một trường):** `floor` ra khỏi khoá gộp.
Lý do: AdMob tính `floor = revenue × hệ số × 1000`, `revenue` là giá ad vừa load nên là số thực
liên tục — để trong khoá thì nhánh AdMob mỗi lần load một dòng, mất tác dụng gộp và làm đầy trần
cụm khiến phần MAX cũng bị xả sớm. Từ 1.4.1: khoá gộp chỉ gồm `adType × adMediation × u × tier`,
còn `floor` báo **trung bình** của các lần trong dòng. Nhờ các bạn ghi mốc phiên bản khi đọc số.

Tên lấy đúng nghĩa tự nhiên: `floor` là tiền, `tier` là bậc — không dùng cặp `floor`/`floorUsd` vì
nhìn lướt dễ nhầm. Đây là event MỚI nên đổi tên không tạo thêm đời dữ liệu: dữ liệu cũ (nơi `floor`
mang hệ số) nằm ở hai event cũ đã đóng băng.

## 2. Năm điều kiện của các bạn — bên mình làm gì

| # | Điều kiện | Đã làm |
|---|---|---|
| 1 | Mất khả năng cắt theo `event` (ok/fail chung dòng) | **Chấp nhận**, ghi vào spec §5.11e để sau không ai ngạc nhiên |
| 2 | Cấm mặc định `floor: 0` | **Đã ràng buộc bằng code + test**, và áp cho cả `tier`: cả hai là `double?`, null thì key biến mất khỏi payload (`NullValueHandling.Ignore`); test `MissingTier_StaysNull_NotZero` + `TierAndFloor_AreTwoSeparateAxes` chặn hồi quy. Ngoài ra SDK **cảnh báo lúc chạy** khi mediation điền `floor` mà không có `tier` — bắt ca code cũ gán hệ số vào ô tiền (đổi tên nên vẫn compile, sai im lặng) |
| 3 | Seed rule TRƯỚC khi SDK phát hành | **Đang làm đúng thứ tự đó** — file này là bản chốt tên + trường cấp trên để các bạn seed. Bên mình chưa publish |
| 4 | Trần mảng + không mất im lặng | **Trần 64 phần tử**. Khác đề xuất của các bạn một chỗ: vượt thì **CHIA LÔ thành nhiều dòng**, không cắt + gắn cờ — mọi số trong bản tin đều cộng được nên nhiều dòng cho tổng y hệt, không phải xử lý cờ tràn, và **không mất dữ liệu** |
| 5 | View phẳng chỉ tiện, không rẻ | **Ghi nhận**, chưa xin materialized view. Khi lượng query tăng thì bàn tiếp |

Thêm một thay đổi nội bộ không đụng hợp đồng: trần cụm trong RAM nâng 32 → 256 khoá, để cụm khỏi
bị cắt vụn trước khi tới lúc flush.

## 3. Hai sửa nhỏ các bạn đã duyệt — đã code

- `adMediation` trên dòng load: nằm ở cấp trên của `f_sdk_ad_load_stats`.
- `hasClick` gửi `false` tường minh: đã sửa, kèm test. Từ bản này, vắng `hasClick` nghĩa là **mốc
  close đi đường cũ**, còn `false` là **đo thật**.

## 4. Chốt các câu còn lại

- **Ngưỡng "bỏ cuộc cả đợt" = 6 lần load fail liên tiếp:** owner bên mình **chốt giữ**. Chỉ dùng cho
  `request_giveup_rate`, **không** dùng làm fill rate.
- **Ba tên chỉ số:** `load_fill_rate` · `request_giveup_rate` · `view_fill_rate`. Từ khi có
  `f_sdk_ad_show_attempt`, **`view_fill_rate = count(adAvailable = true) / count(*)`** — mẫu số giờ
  là "lần user cần ad", không còn là "đợt nạp hàng".
- **`f_sdk_ad_called`:** log đời cũ, sẽ xoá khi nhánh IronSource và GMA chuyển sang API mới. Không
  dùng để đếm request.
- **iOS:** đang hỏi team game lịch phát hành bản iOS mang SDK mới, sẽ báo lại.
- **Bất biến `f_sdk_ad_show_attempt` ≥ `f_sdk_ad_show_data`:** đồng ý đưa vào bộ giám sát của các bạn.

## 5. Việc kế tiếp, theo thứ tự

1. **Các bạn seed mapping rule** cho hai event ở §1 và báo lại đã deploy.
2. Bên mình publish SDK; Mediation thêm lời gọi `OnShowAttempt` ở đầu hàm show và truyền
   `adMediation` vào mốc load (thư riêng đã gửi họ).
3. Sau 1 ngày có dữ liệu, các bạn chạy lại bộ kiểm A1–A11 + B1–B6 trên event mới, thêm hai phép:
   bất biến `show_attempt ≥ show`, và phân bố `tier`/`floor` vắng-key theo mediation.

---
*Ack/câu hỏi gửi lại như lệ; file xoá khi ship.*
