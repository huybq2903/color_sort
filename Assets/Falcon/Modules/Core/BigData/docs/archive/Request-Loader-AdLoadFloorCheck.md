# Gửi loader — nhờ kiểm `floor` thiếu trên ad_load + chốt cách tính fill rate

Bên data báo hai việc trên `f_sdk_ad_load_success` / `f_sdk_ad_load_fail`:

1. Đếm dòng **có** `floor` thấp hơn tổng số dòng — nghi `floor` log không đủ.
2. Cần biết field nào làm mẫu số "số request" để tính fill rate.

Nhờ các bạn chạy mấy phép kiểm dưới đây rồi trả lời theo mục **Cần trả lời**.

## 1. `floor` thiếu — nhờ kiểm

**Client gửi thế nào:** `floor` là `double`, **null thì key vắng mặt** khỏi payload (không gửi
`null`, không gửi `0` thay). Theo code Mediation hiện tại:

| Luồng | `floor` |
|---|---|
| MAX multicall | Luôn có giá trị (multiplier × floor mặc định) |
| MAX không multicall | `0` — chỉ từ bản Mediation mới (1.4.29); bản cũ hơn có thể **vắng mặt** |
| AdMob bridge | Có thể **vắng mặt** (unit không đặt floor) |

**Nhờ kiểm (cùng khoảng thời gian data đang xem):**

| # | Phép kiểm | Để biết |
|---|---|---|
| K1 | Tỉ lệ dòng **vắng** `floor`, chia theo `app_version` | Thiếu dồn ở bản cũ → do bản Mediation chưa nâng |
| K2 | Như K1, chia theo `adUnitId` / network (dòng success có `networkName`) | Dồn ở unit AdMob → luồng AdMob không đặt floor |
| K3 | Số dòng `floor = 0` so với số dòng vắng `floor` | Kiểm pipeline có đang coi `0` là rỗng / làm rơi `0` không |
| K4 | Điều kiện "có floor" trong query của data (`IS NOT NULL` hay `> 0`?) | `> 0` là tự loại mất dòng `floor = 0` — dòng đó CÓ gửi floor |

**Quy ước đề xuất khi phân tích:** dòng vắng `floor` = **không chạy bid floor**, cùng nghĩa với
`floor = 0` → được phép `COALESCE(floor, 0)` khi group theo tầng floor.

## 2. Fill rate — mẫu số nào

**Dùng ngay được — fill theo lần load (từ chính hai event load):**

```sql
fill_rate = SUM(count) FILTER (WHERE event = 'f_sdk_ad_load_success')
          / SUM(count) FILTER (WHERE event IN ('f_sdk_ad_load_success', 'f_sdk_ad_load_fail'))
-- group được theo floor, adUnitId, adType, networkName, ngày
```

- Hai event là **bản tin CỤM**: một dòng = `count` lần load cùng (unit × floor × network ×
  kết cục). Đếm bằng `SUM(count)`, **không** `COUNT(*)`.
- Cụm flush lúc app pause, **không persist** qua kill → đọc theo ngày trở lên; cửa sổ vài giờ sẽ
  hụt các phiên chưa pause.

**Chưa dùng được — `f_sdk_ad_request_data` làm mẫu số:** Mediation hiện gọi request ở **mọi lần
retry tự động** (multicall nhân theo unit) → số request phồng nhiều lần, không phản ánh nhu cầu ad
thật (đã báo trong `Request-Loader-PendingMarks`, chờ bản Mediation sửa nhịp gọi). Sau khi sửa,
nó cho **fill theo lượt xem**:

```sql
fill_rate_view = COUNT(DISTINCT r.ad_view_id có impression cùng ad_view_id trong f_sdk_ads_data)
               / COUNT(DISTINCT r.ad_view_id)            -- r = f_sdk_ad_request_data
```

## Cần trả lời

1. Kết quả K1–K4 — nguyên nhân `floor` thiếu thuộc nhóm nào (bản cũ / AdMob / pipeline / query).
2. Pipeline có giữ nguyên `floor = 0` không (không ép về null).
3. Xác nhận `count` của hai event load đang được load thành cột số và bên data dùng `SUM(count)`.

---
*Ack/câu hỏi gửi lại như lệ; file xoá khi xong.*
