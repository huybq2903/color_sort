## 1.3.4
Đổi tên GetLocalizedPriceDouble/GetLocalizedPriceDoubleAsync thành GetUsdPriceDouble/GetUsdPriceDoubleAsync cho đúng bản chất (hàm này luôn parse defaultPrice usd trong config, không phải giá localized). Thêm event "falcon.modules.iap.purchase_success.usd" (GameEvent<double>) emit giá usd khi mua thành công
----------------------------------


## 1.3.3
Mở phễu checkout cho logger: IPurchaseLog thêm 3 hook mặc định rỗng OnPurchaseStarted (ngay trước InitiatePurchase) / OnPurchaseFailed (callback lỗi billing, kèm PurchaseFailureReason) / OnPurchaseDeferred (Google trả giao dịch treo). Module không phụ thuộc BigData - logger nào cần thì tự implement, logger cũ không phải sửa
----------------------------------


## 1.3.2
Fix null PurchaseListener ở frame đầu tiên
----------------------------------


## 1.3.1
Thêm hàm GetSubscriptionInfo(string productId) để lấy thông tin subscription (ngày hết hạn, trạng thái đăng ký, free trial...
----------------------------------


## 1.3.0
Thêm tham số action callback vào trong hàm Restore để bắt sự kiện thành công hoặc thất bại khi khôi phục giao dịch
----------------------------------


## 1.2.9
Thêm hàm overloading void Purchase(string productId, Action<Product> success, Action failure, string where, string why)
----------------------------------


## 1.2.8
Sửa lỗi thi thoảng gọi validate fail 2 lần
----------------------------------


## 1.2.7
thêm lại việc khởi tạo Unity Services trước khi khởi tạo IAP, để rỗng biến environment trong setting nếu k cần khởi tạo
----------------------------------


## 1.2.6
Thêm bật tắt log purchase trên editor trong setting
----------------------------------


## 1.2.5
Thêm CultureInfo.InvariantCulture vào GetLocalizedPriceDouble khi parse string sang double để k bị lỗi ở các quốc gia khác nhau
----------------------------------


## 1.2.4
Trên editor giờ sẽ hiện giá đc điền trong SO (trên device vẫn bình thường)
----------------------------------


## 1.2.3
Sửa lỗi khiến thi thoảng không gọi vào được onFail khi thất bại của hàm Purchase
----------------------------------


## 1.2.2
Hiện dialog mua trên editor
----------------------------------


## 1.2.1
Bỏ khởi tạo unity services (không cần)
Chỉnh lại 1 số hàm log exception
----------------------------------


## 1.2.0
Tự động khởi tạo Unity Service nếu chưa có
Tắt log Inapp trên Editor
----------------------------------


## 1.1.9
Thêm event bus cho GetLocalizePriceAsync
----------------------------------


## 1.1.8
Thêm hàm GetProductMetadataAsync, GetLocalizedPriceAsync, GetLocalizedPriceDoubleAsync để đợi lấy các thông tin
về product cho đến khi IAP đc khởi tạo
Thêm IsProductHavingReceipts(string productId) để kiểm tra product NonConsume
----------------------------------


## 1.1.7
Thêm: FInAppData lưu trữ những thông tin giao dịch của user
----------------------------------


## 1.1.6
Thêm biến IsInitializedSuccess để biết là đã khởi tạo thành công
Thêm event OnInitializedSuccess khi khởi tạo thành công
----------------------------------


## 1.1.5
Sửa lỗi không build đc ở bản 1.1.4
----------------------------------


## 1.1.4
Thêm config có cho xác thực local không nếu timeout
Có thể lựa chọn module validation và logger nào được active trong config
----------------------------------


## 1.1.3
Thêm Event bus lấy Ltv
----------------------------------


## 1.1.2
Thêm: API lấy ltv inapp
Sửa: bỏ qua validate bên thứ 3 nếu timeout
----------------------------------


## 1.1.1
Thêm Generate Script Product Name trong SO Config để tạo các biến Product Name
----------------------------------


## 1.1.0
New: Hỗ trợ Unity In-App Purchasing 5.0.0
----------------------------------


## 1.0.9
Fix: Lỗi không lấy được giá địa phương
----------------------------------


## 1.0.8
Update readme require namespace
----------------------------------


## 1.0.7
Update readme
----------------------------------