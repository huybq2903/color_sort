# 📦 Falcon IAP Validation - Server

## 1. Mô tả

- **Giới thiệu:**  
  Module xác thực giao dịch IAP bằng cách gửi dữ liệu mua hàng về server CSSC nội bộ, hỗ trợ cả Google Play và Apple Store. Kết quả xác thực từ server sẽ quyết định việc đánh dấu giao dịch thành công hay thất bại.

- **Thành phần chính:**

| Thành phần            | Mô tả | Lớp liên quan |
|-----------------------|-------|----------------|
| **Gửi lệnh xác thực** | Tạo và gửi message xác thực theo nền tảng | `GoogleCSSCValidationHandler`, `AppleCSSCValidationHadler` |
| **CS**                | Chứa dữ liệu cần gửi cho server | `CSInappVerifyAndroid`, `CSInappVerifyIos` |
| **SC**                | Kết quả từ server gửi về client | `SCInappVerifyAndroidRsp`, `SCInappVerifyIosRsp` |
| **Logging giao dịch** | Gửi log đơn hàng cho server | `CSInappInfo` |
| **Interface mở rộng** | Cho phép mở rộng sang nền tảng khác | `IStoreCSSCValidationHandler` |

## 2. Quick Start
Cài module là xong
## 3. Chi tiết

- Mỗi lệnh xác thực gửi về sẽ nhận về 1 `SCInappVerify...Rsp`.
- Nếu `status == 0` → gọi `process.OnReceiveValidation(Purchased)`
- Nếu `status == 1` → gọi `process.OnReceiveValidation(Failed)`

## 6. Các lỗi thường gặp

| Lỗi | Nguyên nhân | Cách khắc phục |
|------|-------------|----------------|
| Không gửi được message | Namespace sai hoặc thiếu gọi `.Send()` | Kiểm tra class message có kế thừa `CSMessage` và có `[FAMessage]` đúng |
| `receipt` parse lỗi | Dữ liệu JSON trong receipt không đúng định dạng | Kiểm tra field `"Payload"` trong receipt Android |
