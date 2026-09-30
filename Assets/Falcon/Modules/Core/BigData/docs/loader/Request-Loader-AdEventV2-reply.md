# Trả lời `Request-Loader-AdEventV2.md` — ý kiến về bộ event quảng cáo v2

> **Người chạy:** đội stools (làm thay loader) · **Ngày:** 2026-09-23
> **Cách làm:** thử thật trên chính ByteHouse production (hàm JSON, VIEW, MATERIALIZED VIEW) và đọc code
> pipeline, **không suy từ tài liệu ClickHouse** — ByteHouse là bản fork, có chỗ khác.

## Tóm tắt

**Không có gì CHẶN. Hình dạng `units[]` chạy được, và tiền lệ đã có sẵn trong kho.**

Năm chỗ cần chốt trước khi các bạn code:

1. **Mất khả năng cắt theo `event`** — khoản trả giá thật duy nhất, nhưng chấp nhận được.
2. **Cấm mặc định `floor: 0` trong `units[]`** — làm thế là xoá sạch phân biệt "không đặt sàn" / "đặt sàn 0",
   tức mất luôn chẩn đoán đã tìm ra nhóm AdMob-không-đặt-floor.
3. **"View phẳng" §3.2 là tiện tay, KHÔNG phải rẻ** — đừng hiểu là đã giải quyết xong tốc độ.
4. **BẮT BUỘC: seed mapping rule cho hai event mới TRƯỚC khi SDK phát hành** — thiếu rule đã từng gây
   4,36 triệu dòng/ngày `sub_event = UNKNOWN`. Đây là việc rẻ nhất và giá trị nhất của cả đợt.
5. **Đổi tên `f_sdk_ad_show_request_data`** — đụng `f_sdk_ad_request_data`, lệch một chữ mà nghĩa gần như
   ngược nhau.

Đèn xanh không điều kiện: **bỏ hai event load cũ là miễn phí phía loader**, và hai sửa nhỏ ở §4 đều tốt.

---

## 1. Đã thử gì trên ByteHouse thật

| Phép thử | Kết quả |
|---|---|
| `JSONExtractArrayRaw(j,'units')` / `JSONLength` | ✅ |
| `arrayJoin` bung từng phần tử + `JSONExtractString(e,'u')` | ✅ |
| `JSONExtract(j,'units','Array(Tuple(u String, floor Float64, ok Int64, fail Int64))')` rồi `arrayJoin` + `tupleElement` | ✅ ra đúng cột, đúng kiểu |
| Trả về `Array(Tuple(...))` **nguyên khối** làm một cột kết quả | ❌ **driver JDBC vỡ** (`Unknown data type: u`, rớt kết nối). Lỗi phía CLIENT, server không sao — luật: **luôn `arrayJoin` trước khi trả về** |
| Phân biệt **vắng key** vs **gửi 0** *bên trong* một phần tử mảng | ✅ `JSONType(e,'floor')` trả `Int64` khi có, `Null` khi vắng |
| `CREATE VIEW` | ✅ |
| `CREATE MATERIALIZED VIEW … ENGINE = CnchMergeTree` | ✅ (đã tạo và xoá sạch trong `dwh_system_test`) |
| Mảng object đã từng đi lọt cả pipeline vào kho chưa | ✅ **`f_sdk_iap_validated.line_items`** đang nằm trong kho đúng dạng mảng object |

Phía pipeline: `SchemaCheckStep.bucketResidualFields` đẩy **nguyên giá trị** của field lạ vào
`event_extra_props`, không ràng buộc kiểu — `List<object>` đi qua bình thường. `line_items` là bằng chứng
chạy thật, không phải suy luận.

## 2. Điều làm đề xuất của các bạn ĐỨNG VỮNG

Có một hiểu nhầm dễ xảy ra cần nói trước: **`adType` / `adUnitId` / `floor` / `count` hôm nay ĐÃ nằm trong
JSONB rồi**, không phải cột typed (chúng tôi đã xác nhận trong `AdLoadFloorCheck-reply.md` §2). Vậy nên v2
**không phải** là "hạ cấp từ cột typed xuống JSON" — mà là "nhiều dòng JSON nhỏ → ít dòng JSON to".

Tổng số byte JSON phải đọc **giảm**, đúng theo ước tính của các bạn. Đó là lý do chúng tôi không phản đối
hình dạng này.

## 3. Năm chỗ cần chốt

### 3.1 Mất khả năng cắt theo `event` — khoản trả giá thật

Hôm nay `WHERE event = 'f_sdk_ad_load_fail'` chỉ đọc **378.676 dòng** và bỏ qua **185.775 dòng** success
(số đo 22/09, `com.fc.sdk.block.escape`). Sang v2, `ok` và `fail` nằm chung một dòng ⇒ **mọi câu chỉ hỏi
fail đều phải đọc cả phần success**.

Không chặn, và đổi lại tổng byte giảm nên nhìn chung vẫn lãi. Nhưng đây là chỗ duy nhất thật sự xấu đi,
nên ghi vào hợp đồng để sau này không ai ngạc nhiên.

### 3.2 `floor` — KHÔNG được mặc định `0` trong `units[]`

Đây là điểm chúng tôi muốn nhấn nhất trong phần kỹ thuật.

Cả kết luận "**99,8% dòng vắng `floor` là unit AdMob**" (K2, `AdLoadFloorCheck-reply.md`) đứng trên đúng một
thứ: phân biệt được **vắng key** với **gửi giá trị 0**. Trong v2 vẫn phân biệt được — chúng tôi đã thử:

```sql
SELECT JSONExtractString(e,'u') AS unit,
       JSONType(e,'floor')      AS kieu,    -- 'Int64'/'Double' = CÓ gửi · 'Null' = VẮNG key
       JSONExtractFloat(e,'floor') AS gia_tri
FROM (SELECT arrayJoin(JSONExtractArrayRaw(units_json,'units')) AS e)
```

Nhưng chỉ đúng **nếu SDK giữ nguyên hành vi hiện tại: unit không đặt sàn thì BỎ HẲN key `floor`**. Nếu bản
mới điền `"floor": 0` cho gọn mảng thì "không đặt sàn" và "đặt sàn 0" trộn làm một, **vĩnh viễn không tách
lại được**, và chẩn đoán AdMob ở trên chết theo.

**Đề nghị ghi vào hợp đồng:** trong `units[]`, `floor` **vắng mặt** khi unit không đặt giá sàn; không điền 0.

Hệ quả nữa: toàn bộ query chẩn đoán floor phải viết lại theo dạng `arrayJoin` rồi mới `JSONType` trên từng
phần tử, thay vì `JSONType(j,'floor')` trên cả cột. Chúng tôi làm, chỉ báo để các bạn biết là có việc.

### 3.3 "Dựng view phẳng" là TIỆN TAY, không phải RẺ

Chúng tôi dựng được, và `CREATE VIEW` chạy trên ByteHouse (đã thử). Nhưng cần nói thẳng:

- **VIEW thường không vật chất hoá.** Mỗi lần ai đó query cái view ấy, ByteHouse vẫn **parse lại JSON và
  arrayJoin lại từ đầu**. Nó giải quyết *sự bất tiện* (người dùng cuối khỏi tự viết `arrayJoin`), **không**
  giải quyết tốc độ.
- Thứ thật sự làm nó rẻ là **MATERIALIZED VIEW** — chúng tôi đã thử `CREATE MATERIALIZED VIEW … ENGINE =
  CnchMergeTree` và ByteHouse **nhận**. Nhưng đó là thêm một bảng phải nuôi, phải tính TTL, phải backfill,
  phải theo dõi. Đó là một quyết định riêng, không nằm trong phạm vi "ký event id".

Nên: chúng tôi **đồng ý dựng view phẳng**, và nói trước rằng nếu sau này số lượng query tăng thì sẽ phải
bàn tiếp chuyện vật chất hoá.

### 3.4 BẮT BUỘC — seed mapping rule TRƯỚC khi SDK phát hành

Đây là rủi ro lớn nhất của cả đợt, và nó **đã xảy ra rồi một lần**.

Event mới đi qua loader mà **chưa có `ConfigEventMapping`** thì `type` / `adWhere` / `adWhen` /
`adMediation` **kẹt nguyên trong `event_extra_props`** và cột `sub_event` mang `UNKNOWN` — không ai query
được theo loại ad. Tiền lệ ghi thẳng trong code (`ConfigEventMappingSeed`, họ §3.4.2d):

> *"phát hiện 2026-09-03 probe zoodoku — **4,36 triệu dòng/ngày `ad_request_data` 100% `sub_event=UNKNOWN`**
> vì CHƯA CÓ RULE (event mới đi qua nguyên xi)"*

Và dòng đã ghi trước ngày seed thì **ở lại UNKNOWN vĩnh viễn** trong kho, phải đọc bằng đường vòng
`jsonb_extract_string(event_extra_props,'$.type')`.

**Đề nghị:** các bạn chốt tên event + danh sách field ở cấp trên (`adType`, `adMediation`, `adWhere`,
`adWhen`) **trước**, bên mình seed rule và deploy, **rồi** SDK mới phát hành. Đảo thứ tự là mất dữ liệu
sạch của những ngày đầu.

### 3.5 Đổi tên `f_sdk_ad_show_request_data`

Hiện đã có `f_sdk_ad_request_data` nghĩa là "**mediation mở một đợt xin ad**" (nạp hàng vào kho). Event mới
tên `f_sdk_ad_show_request_data` nghĩa là "**game muốn chiếu ad cho user**". Hai cái **lệch đúng một chữ
`show`** mà ý nghĩa gần như ngược nhau — một cái là phía cung, một cái là phía cầu.

Người viết query sẽ nhầm, và nhầm kiểu này không có gì báo: câu vẫn chạy, số vẫn ra, chỉ sai nghĩa.

**Đề nghị tên không nhầm được:** `f_sdk_ad_opportunity` hoặc `f_sdk_ad_show_attempt`. Bản thân ý tưởng mốc
`show_request` thì chúng tôi **rất tán thành** — nó chính là mẫu số mà `A7` đang thiếu, và lập luận của các
bạn về việc bỏ phương án `no_ad_available` (mốc chỉ-ghi-khi-hỏng, quên là hỏng biến mất im lặng) là đúng.

## 4. Đèn xanh không điều kiện

**Bỏ `f_sdk_ad_load_success` + `f_sdk_ad_load_fail`: miễn phí phía loader.** Đã rà toàn bộ code:

- không có `ConfigEventMapping` nào,
- không có cook / aggregate nào,
- không có profile counter nào,
- không có probe DQ nào

đọc hai event này. Chúng thuần là telemetry nằm trong `event_extra_props`. Chỉ hai file tài liệu phải sửa
(`event-catalog.md`, `sdk-event-inventory.md`) — việc của bên mình.

**Hai sửa nhỏ §4: đồng ý cả hai.**
- `adMediation` lên dòng load — đúng nút thắt bên mình đã nêu; trong v2 nó nằm sẵn ở cấp trên của event, tốt hơn.
- `hasClick` gửi `false` tường minh — đo 22/09 xác nhận hiện **0 dòng nào** mang `false` trên mọi loại/bản.

## 5. Trần kích thước mảng / gói tin (câu §3.3)

**Không có trần khai báo nào trên cột JSONB** — `event_extra_props` là `JSONB`, không giới hạn độ dài trong
schema. Ràng buộc thật nằm ở kích thước bản tin Kafka và dòng staging, cả hai đều rộng hơn nhiều so với con
số các bạn dự tính.

Để các bạn có mốc so, đây là cỡ `event_extra_props` **hiện tại** (`com.fc.sdk.block.escape`, 22/09,
1.497.395 dòng — toàn bộ event của gói, không riêng quảng cáo):

| | byte |
|---|---:|
| trung bình | **139** |
| p99 | **366** |
| lớn nhất | **922** |

Dòng `f_sdk_ad_load_stats` 32 unit mà các bạn ước ~2,5 KB sẽ là **~2,7 lần dòng to nhất hệ đang chở**. Đó là
bước nhảy đáng kể về *cỡ một dòng*, nhưng **tổng byte vẫn giảm** vì số dòng giảm ~10 lần — và 2,5 KB tự nó
không ở đâu gần giới hạn nào.

**Đề nghị:** chốt trần **64 phần tử** trong `units[]` (~5 KB), và SDK **cắt bớt + gắn cờ** khi vượt thay vì
bỏ im lặng — có cờ thì bên mình đo được tần suất tràn, không có cờ thì mất dữ liệu mà không ai biết. Nếu
các bạn cần con số chắc hơn (đo thật tới ngưỡng nào thì gãy) thì báo, bên mình dựng phép thử riêng — nhưng
theo đánh giá hiện tại thì **không cần**, 2,5 KB còn cách giới hạn rất xa.

## 6. Những câu còn lại

- **Ngưỡng "bỏ cuộc cả đợt" 6 lần fail:** phía loader **không có ý kiến kỹ thuật phản đối** — chạy được.
  Nhưng đây là quyết định hợp đồng chỉ số, không phải quyết định kỹ thuật, nên bên mình **chuyển lên owner
  và bên data quyết**, không tự chốt.
- **Ba tên chỉ số** (`load_fill_rate` / `request_giveup_rate` / `view_fill_rate`): đồng ý, và cảm ơn đã chốt
  tên riêng — đó là điều bên mình lo nhất khi thấy ba con số 98,6% / 59,1% / 10,0% cùng được gọi là "fill rate".
- **A2/A3 là giả thuyết (a):** ghi nhận. Vậy `show_request` đúng là chỗ lấp. Sau khi có nó, bên mình sẽ đo lại
  bất biến `show_request ≥ show` mà các bạn đề xuất — đó là bất biến tốt, kiểm được, nên đưa vào bộ giám sát.
- **`f_sdk_ad_called` sẽ xoá:** tốt. Hiện nó là **51% lượng dòng ad của iOS** (9.078 / 17.727 dòng, 22/09) nên
  xoá được là giảm thật.
- **iOS:** nhờ báo lại lịch. Toàn bộ phân tích v2 hiện dựa trên **một gói, android, một ngày** — khi iOS lên
  bộ event mới, hình dạng có thể khác và phải đo lại.

## 7. Công thức query cho bộ mới (để các bạn hình dung người đọc số sẽ viết gì)

```sql
-- Bung units[] thành từng dòng rồi tính như bảng thường
SELECT jsonb_extract_string(event_extra_props,'$.ad_type')      AS ad_type,
       jsonb_extract_string(event_extra_props,'$.ad_mediation') AS mediation,
       JSONExtractString(u,'u')     AS unit,
       JSONType(u,'floor')          AS floor_co_gui,   -- 'Null' = VẮNG (không đặt sàn)
       JSONExtractFloat(u,'floor')  AS floor,
       sum(JSONExtractInt(u,'ok'))   AS lan_ok,
       sum(JSONExtractInt(u,'fail')) AS lan_fail,
       round(100 * sum(JSONExtractInt(u,'ok'))
             / nullIf(sum(JSONExtractInt(u,'ok')) + sum(JSONExtractInt(u,'fail')), 0), 2) AS load_fill_rate,
       round(sum(JSONExtractInt(u,'okMs'))   / nullIf(sum(JSONExtractInt(u,'ok')),   0)) AS ms_tb_ok,
       round(sum(JSONExtractInt(u,'failMs')) / nullIf(sum(JSONExtractInt(u,'fail')), 0)) AS ms_tb_fail
FROM (SELECT event_extra_props,
             arrayJoin(JSONExtractArrayRaw(toJSONString(event_extra_props), 'units')) AS u
      FROM dwh_system.f_sdk_event_general
      WHERE created_day = '2026-XX-XX' AND event = 'f_sdk_ad_load_stats'
        AND package_name = '…')
GROUP BY ad_type, mediation, unit, floor_co_gui, floor
SETTINGS max_threads = 4;
```

**Ba cái bẫy trong câu trên, ghi lại để khỏi mất công lần sau:**

1. **Đừng trả `Array(Tuple(...))` về làm một cột** — driver JDBC vỡ (`Unknown data type: u`) và rớt kết nối.
   Luôn `arrayJoin` trước.
2. **`JSONExtractFloat` trả `0` khi vắng key**, y như `jsonb_extract_*`. Muốn biết "có gửi hay không" thì
   **phải** hỏi `JSONType(u,'floor')` — đó là lý do §3.2 quan trọng.
3. `toJSONString(event_extra_props)` tốn RAM — luôn khoanh `created_day` + `event` (+ `package_name`) trước.

---
*Ack/câu hỏi gửi lại như lệ.*
