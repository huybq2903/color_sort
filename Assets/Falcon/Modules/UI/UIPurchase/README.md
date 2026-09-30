# UI Purchase

## Tổng quan
- **Giới thiệu:** Giao diện thông báo trạng thái mua hàng In App Purchasing
- **Các thành phần chính:**
    - Giao diện đợi Store gửi thông tin purchase
    - Giao diện purchase thất bại
    - Giao diện purchase thành công
## Quick Start

Import Asset Popup: Falcon > Modules > InApp > Import UI Purchase

## Danh sách đầy đủ API

### `UIPopupPurchaseWaiting`: class popup đợi purchase
### `UIPopupPurchaseFail`: class popup purchase thất bại
### `UIPopupPurchaseSuccess`: class popup purchase thành công

### `PurchaseUIHandler`
#### `Action onStartPurchase`: Khi bắt đầu purchase, đã tự động gọi UIPopupPurchaseWaiting trong đây, có thể gán lại để custom
#### `Action onCloseSuccess`: Khi đóng UIPopupPurchaseSuccess, đã tự động tắt UIPopupPurchaseWaiting trong đây, có thể gán lại để custom
#### `Action onPurchaseSuccess`: Khi purchase thành công, đã tự động gọi UIPopupPurchaseSuccess trong đây, có thể gán lại để custom
#### `Action onPurchaseFail`: Khi purchase thất bại, đã tự động gọi UIPopupPurchaseFail trong đây, có thể gán lại để custom
