# Gửi loader — đề xuất bộ event quảng cáo v2: thêm mốc `show_request`, gộp 2 event load thành 1

Dựa trên kết quả đối soát 22/09 của các bạn (`Request-Loader-AdEventAudit-reply.md`). Ba việc,
xin ý kiến trước khi bên mình code. Cuối file có phần trả lời các câu các bạn hỏi.

**Bối cảnh số liệu** (android, `com.fc.sdk.block.escape`, 22/09, ~3.700 user):

| Nhóm | Dòng/ngày | % | Dòng/user/ngày |
|---|---:|---:|---:|
| 2 event load (`ad_load_success` + `ad_load_fail`) | 564.451 | **60,5%** | ~158 |
| Vòng đời view (request/show/impression/close/label) | 365.150 | 39,2% | ~97 |
| Log cũ `ad_called` | 3.797 | 0,4% | ~1 |

iOS chưa lên bộ event mới, nên khi iOS lên, lượng dòng sẽ tăng thêm theo đúng tỉ lệ này. Đề xuất
dưới đây nhắm vào 60,5% đó.

## 1. Chia rõ HAI HỌ event quảng cáo

| Họ | Event | Bản chất | Dùng để |
|---|---|---|---|
| **Load** | `ad_load_*` | Bản tin **gộp**, không có `adViewId` | Tính tỉ lệ theo giá sàn, chỉnh floor |
| **Show** | `show_request` → `show` → `impression` → `close` (+ nhãn `show_error`) | Vòng đời **một lần xem ad**, nối bằng `adViewId` | Phễu người dùng, doanh thu |

Mốc `request` (xin ad từ mediation) thuộc họ Load về ý nghĩa, dù vẫn mang `adViewId`: từ bản
Mediation có van, **một đợt xin ad phục vụ nhiều lần chiếu**, nên nó là "một đợt nạp hàng", không
phải "một lần user cần ad".

## 2. Event MỚI: `f_sdk_ad_show_request_data` — "game muốn chiếu ad"

**Vì sao cần:** hiện `show` chỉ bắn khi **đã có ad trong kho**. Lúc kho rỗng, game muốn chiếu mà
không chiếu được thì **không có dòng nào**. Trước đây `request` bắn ngay lúc game cần ad nên coi
như mẫu số; từ bản có van thì không còn đúng (dữ liệu của các bạn: 15% số lần show không có
`request` cùng id, khoảng cách request→show tăng từ 26 giây lên 219 giây vì nạp sẵn).

| Key | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `type` | enum string | ✓ | Format ad |
| `adWhere` | string | ✓ | Chỗ game định chiếu |
| `adWhen` | string | — | Ngữ cảnh kích hoạt |
| `adMediation` | string | — | Mediation đang dùng |
| `adViewId` | string | — | Có ad trong kho thì mang id của view sắp chiếu; kho rỗng thì **vắng** |

- **Bắn khi:** game gọi hiển thị ad, **trước** nhánh kiểm tra kho.
- **"Không có ad" không cần event riêng:** `show_request` không có `show` cùng id (hoặc không có
  `adViewId`) chính là ca đó — đúng luật "vắng mặt là dữ liệu" đang dùng.
- **Lượng:** ~40 nghìn dòng/ngày, bằng 4% lượng hiện tại.
- **Bất biến kiểm chứng được:** `show_request ≥ show`. Lệch là biết ngay có luồng quên gọi.

Đổi lại, chỉ số cho MO thành đúng nghĩa:

```
view_fill_rate = show / show_request        -- "user cần ad thì bao nhiêu lần có ad"
```

Bên mình đã cân nhắc phương án ngược lại (chỉ bắn `no_ad_available` khi kho rỗng, rẻ hơn ~33
nghìn dòng) nhưng **bỏ**: đó là mốc chỉ-ghi-khi-hỏng, quên gọi một luồng là phần hỏng biến mất im
lặng và fill rate đẹp lên giả tạo, không có bất biến nào để soi.

## 3. Event MỚI: `f_sdk_ad_load_stats` — thay cho `ad_load_success` + `ad_load_fail`

**Vấn đề của bản hiện tại:** gộp theo khoá `(loại × unit × floor × network × kết cục)` nên một
lần xả cụm đẻ tới 32 dòng; `ad_load_success` thực tế **không nén được gì** (1,02 lần load/dòng).

**Hình dạng đề xuất** — một dòng cho mỗi `(loại ad × mediation)` mỗi lần xả, chi tiết từng unit
nằm trong **một mảng bản ghi**:

```json
{
  "event": "f_sdk_ad_load_stats",
  "adType": "Interstitial",
  "adMediation": "Max",
  "units": [
    {"u":"unit_a","floor":5.0,"ok":3,"fail":412,"okMs":51200,"failMs":1780400,"net":"AppLovin","err":"NoFill"},
    {"u":"unit_b","floor":3.0,"ok":12,"fail":233,"okMs":205000,"failMs":998300,"net":"Meta","err":"NetworkError"}
  ]
}
```

| Khoá trong `units[]` | Nghĩa |
|---|---|
| `u` | `adUnitId` |
| `floor` | Giá sàn / hệ số của unit (xem §5) |
| `ok` / `fail` | Số lần load thành công / thất bại đã gộp |
| `okMs` / `failMs` | Tổng thời gian load của mỗi nhóm (chia cho `ok`/`fail` ra trung bình) |
| `net` | Network thắng (chỉ có khi `ok > 0`) |
| `err` | Mã lỗi **cuối** của nhóm fail |

**Vì sao mảng bản ghi, không phải nhiều map song song:** 5 map cùng khoá dễ lệch nhau (unit có
trong map này, thiếu ở map kia, không có gì báo), và bên đọc phải tự ghép. Mảng bản ghi thì mỗi
phần tử tự đủ nghĩa.

**Vì sao khoá là ad unit, không phải floor:** floor là số thực, làm khoá thì dính định dạng
(`1.2` với `1.20`, dấu thập phân theo vùng) và hai unit cùng floor bị gộp mất.

**Hiệu quả ước tính:** từ **158 dòng/user/ngày xuống ~20**, giảm gần 90% nhóm chiếm 60,5% lượng
quảng cáo. Gói tin: ~2,5 KB/dòng (32 unit) so với ~22 KB cho 32 dòng rời.

**Cần các bạn:**

1. **Ký event id mới** — không sửa tại chỗ 2 event cũ, vì hình dạng khác hẳn, dữ liệu không trộn
   được. Hai event cũ ngừng gửi từ cùng bản SDK.
2. **Dựng một view phẳng** bung `units[]` (mỗi phần tử một dòng), để người dùng cuối vẫn query như
   bảng thường, khỏi phải tự `arrayJoin`.
3. Cho biết có ràng buộc nào về kích thước mảng / gói tin không, để bên mình đặt trần số unit.

## 4. Hai sửa nhỏ kèm theo (BigData làm khi các bạn ký)

| # | Sửa | Vì sao |
|---|---|---|
| 1 | `adMediation` lên dòng load | Các bạn đã nêu là nút thắt; trong §3 nó nằm sẵn ở cấp trên của event mới |
| 2 | `hasClick` gửi **`false` tường minh** thay vì vắng key | Hiện code chỉ set `true` khi có click, không click thì để trống → các bạn phải `COALESCE`. Sửa xong sẽ phân biệt được "không click" với "không biết" |

## 5. Trả lời các câu trong reply của các bạn

- **`floor` đổi đời tại app 2.7.1, chỉ ở MAX:** xác nhận. Bản mới gửi **hệ số bậc**, không phải
  tiền. **AdMob chưa đổi** — bên mình đã yêu cầu Mediation đổi nốt cho đồng nhất. Dải thật
  `1,1–13,0` (phổ biến 5,0 / 3,0 / 1,5 / 2,0) là bảng bậc của game, không phải 1,2 / 1,4 như doc
  cũ; đang nhờ Mediation xác nhận bảng bậc. Trong lúc đó, group theo `(mediation, unit, floor)` là
  **bắt buộc**, đúng như các bạn viết.
- **`f_sdk_ad_called`:** log đời cũ (`FAdCalledLog`) của nhánh IronSource và GMA chưa migrate.
  **KHÔNG dùng để đếm request**, sẽ xoá khi hai nhánh đó chuyển sang API mới. Đang là 51% lượng
  dòng iOS vì iOS còn ở bản cũ.
- **iOS chưa lên bộ event mới:** đúng, bên mình sẽ kiểm với team game lịch phát hành bản iOS mang
  SDK mới rồi báo lại.
- **A2/A3 (show/impression không có request, 15%):** là giả thuyết (a) của các bạn — **không phải
  mất log**. Ad nạp sẵn được chiếu mà không cần đợt load mới, lúc đó SDK tự xoay `adViewId` mới cho
  lần chiếu đó nên không có dòng request đi kèm. Mốc `show_request` ở §2 lấp đúng chỗ này.
- **Ba cách tính fill rate:** đồng ý đặt tên riêng. Chốt cách gọi: `load_fill_rate` (10,0% / 12,6%)
  · `request_giveup_rate` (1,4% / 1,3%) · **`view_fill_rate`** — và sau khi có `show_request` thì
  `view_fill_rate = show / show_request`, đây là số đưa cho MO.
- **Ngưỡng "bỏ cuộc cả đợt" 6 lần fail liên tiếp:** bên mình đề nghị giữ, nhưng chỉ dùng cho
  `request_giveup_rate`, không dùng làm fill rate. Nhờ các bạn và bên data xác nhận.
- **`totalLoadingMs` trên dòng success trung bình 17–19 giây:** đang hỏi Mediation, nghi đo từ đầu
  cả waterfall chứ không phải của lần load ấy.
- **`lastErrorMess` trộn 3 dạng:** đã báo Mediation chuẩn hoá (mã chữ ngắn), phần số trần
  (`0`/`1`/`3`) là của AdMob, sẽ kèm từ điển.
- **`adCompleted` trên Interstitial luôn `true`, vắng 46%:** interstitial không có khái niệm "xem
  hết". Đang yêu cầu Mediation bỏ trường này khỏi interstitial.
- **AppOpen không có dòng nào:** code Mediation bản mới đã nối vòng đời AppOpen; đang hỏi lại xem
  game có bật AppOpen không.

---
*Ack/câu hỏi gửi lại như lệ; file xoá khi ship.*
