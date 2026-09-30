# Falcon Modules InApp

## Mô tả
- **Giới thiệu:** Hệ thống hỗ trợ Unity InAppPurchasing, với cấu trúc mở rộng cho Google Play, App Store và giả lập trong Editor. Hỗ trợ mở rộng các phương thức xác thực và log giao dịch.
- **Các thành phần chính:**
  - `IAPManager`: Class static trung tâm quản lý hệ thống IAP. Chịu trách nhiệm khởi tạo, mua, khôi phục, lấy thông tin sản phẩm và quản lý callback từ hệ thống.

  - `PurchaseListener`: Lớp xử lý chính giao tiếp với Unity IAP (`IDetailedStoreListener`). Điều phối việc xác thực giao dịch, log, restore, xử lý lỗi và quản lý toàn bộ tiến trình mua.

  - `BuyProcess`: Lớp xử lý giao dịch mua mới, kế thừa từ `APurchaseProcess`. Hỗ trợ timeout, log, callback thành công/thất bại/gian lận.

  - `RestoreProcess`: Lớp xử lý giao dịch được khôi phục lại do lỗi, kế thừa từ `APurchaseProcess`. Gọi callback khi restore thành công hoặc phát hiện gian lận.

  - `APurchaseProcess`: Lớp trừu tượng cơ sở cho toàn bộ quá trình xử lý giao dịch. Quản lý product, trạng thái, xác thực, và callback.

  - `IPurchaseValidation`: Interface cho các lớp xác thực giao dịch (có thể mở rộng qua server, Appsflyer...). Được đăng ký tự động khi khởi tạo.

  - `IPurchaseLog`: Interface cho các lớp ghi log giao dịch (có thể ghi vào analytics, backend...). Được đăng ký tự động khi khởi tạo.

  - `IPurchaseStoreHandler`: Interface cho các store platform handler (Google, Apple, giả lập). Được gọi khi IAP khởi tạo.

  - `PurchaseGoogleHandler`, `PurchaseAppstoreHandler`, `PurchaseFakeStoreHandler`: Cụ thể hóa `IPurchaseStoreHandler` cho từng nền tảng: Google Play, Apple Store và môi trường Unity Editor để test.

  - `IAPInitializerBehaviour`: MonoBehaviour singleton đảm bảo IAPManager.Initialize() và Dispose() được gọi đúng thời điểm khi start và destroy.

  - `IAPConstant`: Chứa các constant quan trọng như tên config, event key, giá mặc định....

  - `SOInAppPurchaseConfig`: ScriptableObject lưu config các sản phẩm IAP như productId, defaultPrice, ProductType....

  - `PersistentSingleton<T>`: Lớp helper tạo singleton không bị huỷ khi chuyển scene, dùng cho các `MonoBehaviour` như `IAPInitializer`.
## Quick Start

### 1. Cài In App Purchasing mới nhất của Unity
### 2. Tạo file cấu hình

Vào Unity Editor:

```
Menu: Falcon > Modules > InApp > Settings
```

Unity sẽ tạo một file `SOInAppPurchaseConfig` tại thư mục mặc định: `Assets/FalconAssets/Modules/Core/InAppPurchase/Resources`

Tên file là:

```
SO_FCM_InAppPurchase_Config
```

Trong file cấu hình sẽ chứa danh sách các validation (module validate giao dịch) và các logger (module log giao dịch).
Có thể tắt bật các module này bằng dấu tick enable

### 3. Cấu hình sản phẩm

Trong `SOInAppPurchaseConfig`, bạn có thể thiết lập:

- `purchaseTimeout`: Thời gian timeout cho mỗi giao dịch (tính bằng giây).
- `products`: Danh sách sản phẩm bao gồm:
    - `productID`: ID trùng với Unity Dashboard.
    - `defaultPrice`: Giá fallback nếu không lấy được từ store, điền giá trị tiền tệ $ và dùng dấu chấm để phân cách phần thập phân.
    - `type`: Loại sản phẩm (Consumable / NonConsumable / Subscription).

- Có thể bấm Generate Script Product Name để tạo các biến Product Name

### 4. Các module validate và logger:
- Appsflyer: cần cài thêm Appsflyer Purchase Connect để tự động xác thực và log giao dịch lên AF. Còn k cài thì đã có log AF thủ công có sẵn trong module rồi
- CSSC: cài để xác thực và log giao dịch lên server bằng CSSC
- MMP: cài để log giao dịch lên Adjust
- BigData: log giao dịch lên Falcon BigData

## Danh sách đầy đủ API

### `IAPManager`
- `void Purchase(string productId, Action success, Action failure, string where, string why)`: Gọi mua sản phẩm.
- `void Purchase(string productId, Action<Product> success, Action failure, string where, string why)`: Gọi mua sản phẩm.
- `void RestorePurchase(Action<RestoreResult> callback)`: Khôi phục giao dịch (riêng cho bên ios), truyền callback vào để thông báo kết quả khôi phục(thành công hoặc thất bại, android luôn trả về thất bại), kết hợp bắt sự kiện onPurchaseRestoreInBackground khi muốn bắt rõ restore product nào. Restore dùng khi muốn lấy lại gói mua Non-consumable sau khi cài lại game, hoặc giải quyết lỗi không nhận được vật phẩm
- `ProductMetadata GetProductMetadata(string productID)`: Lấy metadata sản phẩm.
- `string GetLocalizedPrice(string productID)`: Lấy giá hiển thị đã được địa phương hóa theo ID.
- `double GetUsdPriceDouble(string productID)`: Lấy giá usd (parse từ `defaultPrice` trong config) theo ID, trả về 0 nếu không có product.
- `Task<ProductMetadata> GetProductMetadataAsync(string productID)`: Lấy metadata sản phẩm.
- `Task<string> GetLocalizedPriceAsync(string productID)`: Lấy giá hiển thị đã được địa phương hóa theo ID.
- `Task<double> GetUsdPriceDoubleAsync(string productID)`: Lấy giá usd theo ID, chờ đến khi IAP khởi tạo xong.
- `float GetDefaultPriceValue(string productID)`: Lấy giá trị tiền default trong config, currency $
- `Action<Product> onPurchaseSuccess`: callback khi giao dịch thành công, add event handler để đăng kí sự kiện, có thể dùng để xử lý UI...
- `Action<Product> onPurchaseRestoreInBackground`: callback khi giao dịch được khôi phục ở dưới nền (ví dụ sau khi timeout), add event handler để đăng kí sự kiện, có thể dùng để xử lý UI...
- `Action<Product> onHackDetected`: callback khi hiện hành vi gian lận, add event handler để đăng kí sự kiện, có thể dùng để xử lý UI...
- `Action<Product> onPurchaseTimeOut`: callback khi hết thời gian chờ xác thực mà không có phản hồi từ validator, add event handler để đăng kí sự kiện, có thể dùng để xử lý UI...
- `Action<PurchaseFailureReason> onPurchaseFailed`: callback khi giao dịch thành công, add event handler để đăng kí sự kiện, có thể dùng để xử lý UI...
- `Action OnInitializedSuccess`: callback khi IAP Init thành công, add event handler để đăng kí sự kiện
- `double Ltv`: Lấy ltv iap của người dùng
- `bool IsProductHavingReceipts(string productId)`: kiểm tra xem đã có hóa đơn của gói chưa (Non-consumable), trong trường hợp cài lại game mà muốn lấy lại gói đã mua. Lưu ý gọi sau khi OnInitializedSuccess xong
- `SubscriptionInfo GetSubscriptionInfo(string productId)`: lấy thông tin subscription của sản phẩm (ngày hết hạn, trạng thái đăng ký, free trial...). Trả về null nếu IAP chưa khởi tạo, product không có receipt hoặc không phải loại Subscription. Dùng các method của `SubscriptionInfo` như `getExpireDate()`, `isExpired()`, `isSubscribed()` để xử lý tiếp.
### `Event Bus`
Dùng cho các module khác

```csharp
//Lấy giá tiền
var price = GameRequest<string, string>.Request("falcon.modules.iap.get_localized_price", productId);

//Lấy giá tiền cho đến khi khởi tạo xong iap
var taskPrice = GameRequest<string, Task<string>>.Request(PacksConstant.EVENT_GET_LOCALIZED_PRICE, _config.productId);
await taskPrice;
price = taskPrice.Result;

//Lấy ltv
var ltv = GameRequest<double>.Request("falcon.modules.iap.get_ltv");

//Truyền productId, success, failed, where, why để gọi giao dịch
GameEvent<(string, Action, Action, string, string)>.Emit("falcon.modules.iap.purchase", (productId, OnBuySuccess, null, null, null));

//Truyền productId, success, failed, where, why để gọi giao dịch
GameEvent<(string, Action<Product>, Action, string, string)>.Emit("falcon.modules.iap.purchase", (productId, OnBuySuccessProduct, null, null, null));

//Đăng kí sự kiện khi bắt đầu mua
GameEvent.Register("falcon.modules.iap.start_purchase", OnStartPurchase, null);

//Đăng kí sự kiện khi mua thành công
GameEvent<(string currencyCode, double price)>.Register("falcon.modules.iap.purchase_success", OnPurchaseSuccess, null);

//Đăng kí sự kiện khi mua thành công, chỉ nhận giá usd
GameEvent<double>.Register("falcon.modules.iap.purchase_success.usd", OnPurchaseSuccessUsd, null);
```

### `IPurchaseValidation`
Kế thừa để tạo class xác thực giao dịch tùy theo nhu cầu. Phải được đặt trong namespace có tên là Falcon.{NAME_SPACE}.

```csharp
namespace Falcon.Modules.MyValidator
{
  public class MyValidator : IPurchaseValidation
  {
      public void Initialized() { ... }
  
      public void SendValidate(APurchaseProcess process)
      {
          // Gửi dữ liệu lên server và trả kết quả
      }
  }
}

```

Tất cả các class này sẽ được `IAPManager` tự động tìm và nạp khi khởi động.
Nếu không có class validator nào thì khi giao dịch sẽ hoàn thành luôn mà không cần đợi xác thực.
Nếu có nhiều class valiator thì 1 trong những class đó xác thực thành công thì giao dịch hoàn thành luôn.

### `IPurchaseLog`
Kế thừa để tạo class log giao dịch theo nhu cầu. Phải được đặt trong namespace có tên là Falcon.{NAME_SPACE}.
```csharp
namespace Falcon.Modules.MyLogger
{
  public class MyLogger : IPurchaseLog
  {
      public void Log(APurchaseProcess process)
      {
          // Gửi dữ liệu lên server
      }
  }
}
```

Ngoài `Log` (mua thành công), interface còn 3 hook của phễu checkout, mặc định rỗng nên override cái nào cần:

| Hook | Gọi khi |
|---|---|
| `OnPurchaseStarted(product, where, why)` | Ngay trước khi mở billing flow của store |
| `OnPurchaseFailed(product, reason)` | Billing trả lỗi/huỷ cho giao dịch đang mở |
| `OnPurchaseDeferred(product)` | Google trả giao dịch treo (deferred/PENDING) |

Cả 4 đều đi qua cùng một cổng chặn log trên editor (`isLogPurchaseInEditor`).

### `FInAppData`
Lưu những thông tin của user về inapp
```csharp
   [FGameDataType("iap_data")]
    public class FInAppData : FGameData<FInAppData>
    {
        /// <summary>
        /// Lưu trữ data các giá local
        /// </summary>
        public Dictionary<string, LocalizedData> localizeData = new();
        /// <summary>
        /// Lần giao dịch đầu và cuối
        /// </summary>
        public RecordData firstRecord, lastRecord;
        
        /// <summary>
        /// Dữ liệu về các localize giá trong quá trình giao dịch
        /// </summary>
        public class LocalizedData
        {
            /// <summary>
            /// Số giao dịch đã thành công
            /// </summary>
            public int count;
            /// <summary>
            /// Mã localize giá
            /// </summary>
            public string isoCurrencyCode;
            /// <summary>
            /// Lượng giao dịch cao nhất ở giá local này
            /// </summary>
            public decimal max;
            /// <summary>
            /// Tổng lượng giao dịch của giá local này
            /// </summary>
            public decimal total;
        }

        /// <summary>
        /// Thông tin lưu được khi xảy ra 1 giao dịch
        /// </summary>
        public class RecordData
        {
            /// <summary>
            /// Giao dịch khi đang ở level cao nhất đã vượt qua
            /// </summary>
            public int level;
            /// <summary>
            /// Thời điểm giao dịch theo UTC
            /// </summary>
            public long timestamp; //milliseconds
            /// <summary>
            /// Tên produt id giao dịch
            /// </summary>
            public string productId;
        }
    }
```
Tất cả các class này sẽ được `IAPManager` tự động tìm và nạp khi khởi động.

## Chi tiết

### Khởi tạo hệ thống
- IAPInitializerBehaviour là một PersistentSingleton được tạo tự động khi game khởi động qua `OnGameStart` trong `IAPManager`.

- IAPManager sẽ:

  - Load tất cả validator và logger từ Reflection (LoadAllValidationsAndLoggers)

  - Tạo PurchaseListener và gọi Initialize để đăng ký sản phẩm với Unity IAPIAPManager.
  Không cần gọi thủ công trong code.

### Xử lý giao dịch

#### Khi user mua:
- `IAPManager.Purchase(productId, ...)` được gọi trực tiếp hoặc qua Event Bus
- `PurchaseListener.Purchase()` sẽ:
  - Check product, tránh mua trùng lặp.
  - Tạo một `BuyProcess` tương ứng.
  - Gọi `InitiatePurchase` từ Unity IAP

#### Khi Unity IAP trả về kết quả:
- `PurchaseListener.ProcessPurchase(...)` sẽ:
  - Nếu là giao dịch mua: gọi `.Start()` trên `BuyProcess`.
  - Nếu là khôi phục (restore): tạo `RestoreProcess`.
- Cả hai process đều kế thừa `APurchaseProcess` và xử lý callback khi xác thực.

### Lưu ý: Khi dùng bất kì API nào như GetLocalizedPrice, GetProductMetadata... cần đảm bảo IAP đã được khởi tạo xong (IAPManager.IsInitializedSuccess == true)