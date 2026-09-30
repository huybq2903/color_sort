# Yêu cầu cập nhật — module IAP/InAppPurchase ↔ BigData 1.3.2 (phễu checkout §D4)

Gửi owner module IAP (+ `InAppValidation`). Server hiện chỉ thấy mua-XONG nên abandonment mù hoàn
toàn: không biết bao nhiêu người mở dialog rồi bỏ, vì sao bỏ. BigData 1.3.2 mở phễu checkout
`start → fail | success`, chung một `purchaseAttemptId` do SDK sinh. Hai dòng code phía module IAP
là phễu chạy — dev game zero việc.

## Việc cần làm

| # | Ở đâu trong module IAP | Gọi gì |
|---|---|---|
| 1 | Ngay TRƯỚC `launchBillingFlow` / StoreKit payment | `FalconBigDataController.Iap.OnStarted(productId, where, localizedPrice, isoCurrencyCode)` — giá lấy từ `product.metadata`, `where` dùng hằng số `FIapPlacement` nếu khớp. Cần nhét thêm key-value tuỳ ý: bản param `OnStarted(new IapPurchaseAttemptParam { ..., extraMeta = ... })` — log fail cùng lượt tự mang theo |
| 2 | Callback lỗi/huỷ của billing | `FalconBigDataController.Iap.OnFailed(reason)` — map theo bảng dưới |
| 3 | Mua thành công (sau validate) | Chuyển `BigdataLogger` từ `new FInAppLog(param).Send()` sang `FalconBigDataController.Iap.OnPurchased(param)` — cùng event id, wire không đổi; SDK vẫn tự đóng `purchaseAttemptId` + `offerImpressionId` + bẫy PENDING. **MỘT giao dịch MỘT đường** — chạy cả hai là đếm đôi doanh thu. Nhớ điền `transactionId` (bẫy PENDING cần nó) và `localizedPrice` (field doanh thu, không được vắng) |

## Bảng map `responseCode` → `IapPurchaseFailReason`

| Billing trả về | Enum |
|---|---|
| USER_CANCELED | `UserCanceled` |
| ITEM_UNAVAILABLE | `ItemUnavailable` |
| BILLING_UNAVAILABLE / SERVICE_DISCONNECTED / SERVICE_UNAVAILABLE | `BillingUnavailable` |
| NETWORK_ERROR / timeout | `Network` |
| DEVELOPER_ERROR / ITEM_ALREADY_OWNED... | `DeveloperError` |
| **PENDING** (Google) | `Pending` — ⚠ KHÔNG phải fail thật, xem dưới |
| không map được | `Unknown` — thà nói không biết còn hơn gán bừa |

**PENDING**: gọi `OnFailed(Pending, pendingTransactionId: purchase.transactionId)` — **truyền kèm
transactionId của giao dịch treo**. SDK ghi sổ để khi nó hoàn tất (có thể vài ngày sau, đúng lúc
một lượt mua MỚI cùng sản phẩm đang mở) không gán nhầm giao dịch cũ vào lượt mới. Event mua vẫn
bắn như thường — server hiểu phễu này, hai bản tin không mâu thuẫn.

## Ràng buộc phải biết

- **`localizedPrice` trên log mua KHÔNG được thiếu** — thiếu là SDK log lỗi và điền 0 (LTV cộng 0
  cho giao dịch đó). Đây là field doanh thu, hợp đồng không cho vắng.
- App bị kill giữa dialog → lượt start "cụt đuôi", KHÔNG bù ở phiên sau — đó chính là số đo
  abandonment, server cần nó. Callback lỗi về ở phiên sau khi không còn lượt mở thì SDK tự bỏ qua
  kèm warning, module không phải xử lý.
- Threads: callback billing gọi từ thread nào cũng được, BigData tự khoá.
- Event id đã ký: `f_sdk_iap_start_purchase_data` / `f_sdk_iap_purchase_fail_data`. Tham chiếu:
  `EntityLifecycle-Design.md` §4e, `DWH-EventLog-API-Spec.md` §5.12–5.13.
