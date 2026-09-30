# Trả lời `Request-Loader-AdLoadFloorCheck.md` — kết quả K1–K4 + `count`

> **Người chạy:** đội stools (làm thay loader, loader đang bận) · **Ngày chạy:** 2026-09-22
> **Nguồn:** silver `dwh_system.f_sdk_event_general`, event `f_sdk_ad_load_success` / `f_sdk_ad_load_fail`
> **Khoảng dữ liệu:** 1 ngày — `created_day = 2026-09-21` (3.471.211 dòng, 8 package). Chỉ lấy 1 ngày vì
> VW đang tải cao; muốn mở rộng thì chạy lại đúng các query ở §4.

## Tóm tắt

1. **`floor` thiếu thật chỉ ~1% dòng, và 99,5% trong số đó là unit AdMob** → nhóm **AdMob bridge không đặt floor**.
   Không phải bản cũ, không phải pipeline.
2. **Pipeline giữ nguyên `floor = 0`** — 953 nghìn dòng `floor = 0`, gấp ~28 lần số dòng vắng.
3. **`count` là Int64 trên 100% dòng** nhưng **KHÔNG có cột typed** — nằm trong JSONB `event_extra_props`.
4. Chênh "có floor < tổng" bên data thấy **gần như chắc do query dùng `floor > 0`** — điều kiện đó tự loại
   27,5% dòng `floor = 0` (dòng CÓ gửi floor). Chưa xác nhận được vì không có query của data (xem K4).

---

## 1. Kết quả K1–K4

### Tổng thể (21/09)

| | Dòng | Tỉ lệ |
|---|---:|---:|
| Tổng | 3.471.211 | 100% |
| `floor > 0` | 2.482.982 | 71,5% |
| `floor = 0` (có gửi, giá trị 0) | 953.170 | 27,5% |
| **Vắng `floor`** | **34.094** | **0,98%** |
| ↳ trong đó unit AdMob (`ca-app-pub-…`) | 33.926 | 99,5% số vắng |

| Event | Dòng | Vắng | `= 0` | `> 0` | Vắng ở unit AdMob |
|---|---:|---:|---:|---:|---:|
| `f_sdk_ad_load_success` | 1.117.928 | 26.639 | 873.722 | 216.602 | 26.533 |
| `f_sdk_ad_load_fail` | 2.353.283 | 7.455 | 79.448 | 2.266.380 | 7.393 |

### K1 — theo `app_version`: KHÔNG dồn ở bản cũ

- Chỉ **2/8 package** có dòng vắng:
  - `com.fc.sdk.block.escape` (android): ~6% dòng (33.669 / 562.821), **rải đều cả 2.7.0 / 2.7.1 / 2.7.2** (4.220 / 492 / 28.957 dòng vắng).
  - `com.fc.zoodoku.sodoku.puzzle`: android 2.2–2.4 vài chục đến vài trăm dòng; ios 2.2 có 2 dòng.
- 6 package còn lại: **0 dòng vắng** ở mọi version.
- **Không dòng nào gửi `floor` dạng chuỗi** (era 1.3.4–1.3.5 đã hết trong ngày này).

### K2 — theo unit / network: dồn ở **AdMob bridge**

| Unit | Loại | Vắng / tổng dòng | Ghi chú |
|---|---|---:|---|
| `ca-app-pub-…/9396301370` (block.escape, Inter) | AdMob | **100%** (success 12.141 + 1.502 + 19; fail 3.366) | unit không đặt floor |
| `ca-app-pub-…/4581968712` (block.escape, Reward) | AdMob | **100%** (success 10.192 + 2.624 + 19; fail 3.685) | như trên |
| `ca-app-pub-…/5257660098`, `…/4424864078` (zoodoku) | AdMob | **100%** (vài trăm dòng) | như trên |
| `ca-app-pub-…/4007253648`, `…/3516404259`, `…/6742217870`, `…/8776469797` | AdMob multi-floor | **0%** — đủ `floor > 0` | AdMob CÓ đặt floor thì log đủ |
| `c99a0850961f734d`, `e0a73c09b585b48f` (block.escape) | MAX không multicall | **~1–1,5%** vắng, phần còn lại `floor = 0` | ⚠ **chưa giải thích** — xem §3 |
| Các unit MAX multicall | MAX multicall | 0% vắng, `floor > 0` | khớp bảng luồng của client |

### K3 — `floor = 0` so với vắng: pipeline **KHÔNG** làm rơi `0`

`floor = 0` = 953.170 dòng vs vắng = 34.094 dòng. Nếu pipeline coi `0` là rỗng thì cột `= 0` phải gần 0 —
thực tế ngược lại. Soi dòng thô cũng thấy `"floor":0` nằm nguyên trong `event_extra_props`.

### K4 — điều kiện "có floor" bên data: **chưa xác nhận** (không có query của data)

Hai khả năng, đều khớp với việc "đếm có floor < tổng":

| Query data dùng | Kết quả | Hệ quả |
|---|---|---|
| `jsonb_extract_float64(event_extra_props,'$.floor') > 0` | thấy ~28,5% dòng "thiếu" | 27,5 điểm là dòng `floor = 0` — **CÓ gửi**; chỉ ~1 điểm thiếu thật |
| `… IS NOT NULL` | NO-OP — đếm đủ 100% | `jsonb_extract_*` **không bao giờ trả NULL** khi vắng key (trả `0`) → không lọc gì |

Cách phân biệt đúng vắng / bằng 0:

```sql
JSONType(toJSONString(event_extra_props), 'floor') = 0   -- vắng key (enum 'Null')
jsonb_extract_float64(event_extra_props, '$.floor')       -- giá trị; = 0 khi vắng HOẶC gửi 0
```

⚠ `toJSONString` tốn RAM — luôn khoanh `created_day` + `event` (+ `package_name`) trước.

**Nhờ bên data xác nhận:** điều kiện "có floor" đang dùng là gì.

## 2. `count` của hai event load

- `count` là **Int64 trên 100% dòng** (`JSONType` = `Int64`, 0 dòng vắng).
- **Không có cột typed** — nằm trong JSONB `event_extra_props`. Đọc:

```sql
sum(jsonb_extract_int64(event_extra_props, '$.count'))
```

- `COUNT(*)` sai nặng: event fail 2.353.283 dòng = **8.390.893** lần load (~3,6×); success 1.117.928 dòng =
  1.119.956 lần load (gần như count = 1).
- Fill rate theo lần load, 21/09, toàn bộ 8 package:
  `1.119.956 / (1.119.956 + 8.390.893) ≈ 11,8%`.

## 3. Việc còn mở

1. **MAX không multicall vắng ~1–1,5%** (`c99a0850961f734d`, `e0a73c09b585b48f`): cùng `app_version` mà dòng
   có dòng không — không khớp giải thích "bản Mediation < 1.4.29". Nhờ Mediation xem, nghi lần load trước khi
   có config.
2. **Doc `event-catalog.md` có thể lệch thực tế:** ghi `floor` = PREFIX/multiplier (vd `1.2`), nhưng data thật
   có `37.37`, `49.83`… — trông như **giá sàn tuyệt đối**. Nhờ loader rà lại mô tả.
3. **Đồng ý quy ước `COALESCE(floor, 0)`** khi group theo tầng floor — số liệu K2 xác nhận dòng vắng là luồng
   AdMob không đặt floor.

## 4. Query đã chạy (để chạy lại / mở rộng khoảng ngày)

```sql
-- K1: theo package × platform × app_version
SELECT package_name, platform, app_version,
       count() AS rows_,
       sum(jsonb_extract_int64(event_extra_props,'$.count')) AS loads,
       countIf(JSONType(j,'floor') = 0) AS floor_absent,
       countIf(JSONType(j,'floor') IN ('Double','Int64','UInt64')
               AND jsonb_extract_float64(event_extra_props,'$.floor') = 0) AS floor_zero,
       countIf(JSONType(j,'floor') = 'String') AS floor_string
FROM (SELECT package_name, platform, app_version, event_extra_props,
             toJSONString(event_extra_props) AS j
      FROM dwh_system.f_sdk_event_general
      WHERE created_day = '2026-09-21'
        AND event IN ('f_sdk_ad_load_success','f_sdk_ad_load_fail'))
GROUP BY package_name, platform, app_version
SETTINGS max_threads = 4;

-- K2: theo event × ad_type × network × unit (2 package có dòng vắng)
SELECT package_name, platform, event,
       jsonb_extract_string(event_extra_props,'$.ad_type')      AS ad_type,
       jsonb_extract_string(event_extra_props,'$.network_name') AS network,
       jsonb_extract_string(event_extra_props,'$.ad_unit_id')   AS unit,
       count() AS rows_,
       countIf(JSONType(j,'floor') = 0) AS absent,
       countIf(JSONType(j,'floor') != 0 AND jsonb_extract_float64(event_extra_props,'$.floor') = 0) AS zero,
       countIf(jsonb_extract_float64(event_extra_props,'$.floor') > 0) AS positive
FROM (SELECT package_name, platform, event, event_extra_props, toJSONString(event_extra_props) AS j
      FROM dwh_system.f_sdk_event_general
      WHERE created_day = '2026-09-21'
        AND event IN ('f_sdk_ad_load_success','f_sdk_ad_load_fail')
        AND package_name IN ('com.fc.sdk.block.escape','com.fc.zoodoku.sodoku.puzzle'))
GROUP BY package_name, platform, event, ad_type, network, unit
SETTINGS max_threads = 4;

-- K3 + count: tổng theo event
SELECT event, count() AS rows_,
       countIf(JSONType(j,'floor') = 0) AS floor_absent,
       countIf(JSONType(j,'floor') != 0 AND jsonb_extract_float64(event_extra_props,'$.floor') = 0) AS floor_zero,
       countIf(jsonb_extract_float64(event_extra_props,'$.floor') > 0) AS floor_pos,
       countIf(JSONType(j,'floor') = 0
               AND startsWith(jsonb_extract_string(event_extra_props,'$.ad_unit_id'),'ca-app-pub-')) AS absent_admob_unit,
       groupUniqArray(toString(JSONType(j,'count'))) AS count_types,
       sum(jsonb_extract_int64(event_extra_props,'$.count')) AS sum_count
FROM (SELECT event, event_extra_props, toJSONString(event_extra_props) AS j
      FROM dwh_system.f_sdk_event_general
      WHERE created_day = '2026-09-21'
        AND event IN ('f_sdk_ad_load_success','f_sdk_ad_load_fail'))
GROUP BY event
SETTINGS max_threads = 4;
```

---
*Ack/câu hỏi gửi lại như lệ; file xoá khi xong.*
