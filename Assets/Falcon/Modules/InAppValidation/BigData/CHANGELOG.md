## 1.0.5
Nối phễu checkout BigData 1.3.2 (§D4): bắn Iap.OnStarted lúc mở billing flow, Iap.OnFailed lúc billing lỗi/huỷ (map PurchaseFailureReason sang IapPurchaseFailReason), Iap.OnFailed(Pending, transactionId) khi Google trả giao dịch treo. Log mua thành công chuyển từ new FInAppLog(param).Send() sang Iap.OnPurchased(param) - cùng event id, wire không đổi, SDK tự đóng purchaseAttemptId/offerImpressionId
----------------------------------


## 1.0.4
Thêm custom InAppParam
----------------------------------


## 1.0.3
Thêm lựa chọn có thể thêm why vào product id khi gửi lên
----------------------------------


## 1.0.2
Sửa lỗi liên quan đến log trong editor
----------------------------------


## 1.0.1
Update: Up to IAP 5.0
----------------------------------