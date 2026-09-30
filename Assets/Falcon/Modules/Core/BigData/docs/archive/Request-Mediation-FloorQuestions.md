# Hỏi Mediation — 3 câu về `floor`

Mỗi lần ad load xong (thành công hay thất bại), Mediation gọi
`FalconBigDataController.Ad.OnLoadResult(...)` và truyền vào `floor`. Bên data soi dữ liệu thật
thì thấy 3 chỗ lạ. BigData đã kiểm: **BigData không sửa, không làm mất `floor`** — Mediation
truyền gì thì lên server nấy. Nên nhờ bên Mediation trả lời giúp.

## Câu 1 — Có dòng bị mất `floor`

Game `com.fc.sdk.block.escape` (android, bản 2.7.0 – 2.7.2), 2 ad unit MAX:
`c99a0850961f734d` và `e0a73c09b585b48f` (không dùng multicall).

- Code của các bạn ở chỗ này gửi `floor = 0`. Đa số dòng đúng là `0`.
- Nhưng khoảng **1 – 1,5% dòng không có `floor`** — nghĩa là có chỗ nào đó gọi `OnLoadResult`
  mà **không điền `floor`**.

👉 **Hỏi:** Ngoài 4 chỗ load trong `FalconMaxService` (interstitial / rewarded × thành công /
thất bại), còn chỗ nào khác gọi `OnLoadResult` cho 2 unit này không?

## Câu 2 — `floor` là số gì?

Tài liệu cũ ghi `floor` là **hệ số nhân** (kiểu `1.2`, `1.4`). Nhưng dữ liệu thật lại ra
`37.37`, `49.83`… — giống **giá sàn** hơn.

| Chỗ | Các bạn đang gửi |
|---|---|
| MAX có multicall | `a.multiplier × valueDefault... × MultiCall.MULTIPLIER` |
| AdMob | `floorEcpmUsd` (không đặt floor thì để trống) |
| MAX không multicall | `0` |

👉 **Hỏi (trả lời có/không là được):**

1. `floor` là **giá sàn eCPM, tính bằng USD** — đúng không?
2. `0` và **để trống** đều nghĩa là "**unit này không đặt giá sàn**" — đúng không?
3. `MultiCall.MULTIPLIER` là cái gì?

## Câu 3 — Lỗi cũ đã sửa chưa?

File `MultiCallMaxProviderParent`, khi **interstitial load thất bại**, `floor` đang tính bằng
`valueDefaultRewarded` — lẽ ra phải là `valueDefaultInterstitial` (nhánh thành công đang dùng đúng
cái này).

👉 **Hỏi:** Đã sửa chưa? Sửa ở bản Mediation nào?

---
*Trả lời vào file `Request-Mediation-FloorQuestions-reply.md` cùng thư mục là được.*
