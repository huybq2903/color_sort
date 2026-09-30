# Module Falcon Firebase Analytics

## Tổng quan
- **Tên:** `Falcon Firebase Analytics`
- **Giới thiệu:** 
Firebase Analytics là một công cụ phân tích giúp theo dõi hành vi người dùng.

## Quick Start

### 1. Cài đặt
- Tải file config từ console của firebase vào thư mục Assets (google-services.json , GoogleService-Info.plist)

### 2. Cài thêm Firebase Analytics SDK
- Vào menu "Falcon/Manager/ThirdParty Import", chọn cài 
 + Firebase Analytics (v.12.2.0)

### 3. Khởi tạo Falcon Firebase
- Falcon Firebase Analytics sẽ tự động tạo 1 object trên scene và khởi tạo

### 4. Gửi bản tin lên Server
Các sự kiện trong game cần log
- earn_virtual_currency : kiếm đc tài nguyên trong game (gem, diamond, coin, gold, ...)
- spend_virtual_currency : tiêu tài nguyên trong game (mua, nâng cấp item, ...)
```csharp
public class FalconFirebaseLog
{
    public const string EARN_VIRTUAL_CURRENCY = "earn_virtual_currency";
    public const string SPEND_VIRTUAL_CURRENCY = "spend_virtual_currency";

    public static void Log(string eventName);
    public static void Log(string eventName, string parameters, string value);
    public static void Log(string eventName, string parameters, long value = 0);
    public static void Log(string eventName, string parameters, long value, string parameters2);
}

----------------------cách dùng----------------------

    /// <summary>
    /// Gửi sự kiện "earn_virtual_currency" lên server
    /// khi kiếm đc tài nguyên trong game (gem, diamond, coin, gold, ...)
    /// </summary>
    /// <param name="virtual_currency_name">tên tài nguyên (ví dụ : gem, diamond, coin, gold, ...)</param>
    /// <param name="value">số lượng tài nguyên kiếm được</param>
    /// <param name="where">nơi kiếm được tài nguyên (ví dụ : levelpass, x5bywatchads, ...)</param>
    
    FalconFirebaseLog.Log(FalconFirebaseLog.EARN_VIRTUAL_CURRENCY, string virtual_currency_name, long value, string where);
    
    
    /// <summary>
    /// Gửi sự kiện "spend_virtual_currency" lên server
    /// khi tiêu tài nguyên trong game (mua, nâng cấp item, ...)
    /// </summary>
    /// <param name="virtual_currency_name">tên tài nguyên (ví dụ : gem, diamond, coin, gold, ...)</param>
    /// <param name="value">số lượng tài nguyên sử dụng</param>
    /// <param name="item_name">ID hoặc tên item sử dụng tài nguyên (mua, nâng cấp)</param>
    
    FalconFirebaseLog.Log(FalconFirebaseLog.SPEND_VIRTUAL_CURRENCY, string virtual_currency_name, long value, string item_name);
    /// ví dụ : bỏ 500 gem để mua item booster_time ...
    /// FalconFirebaseLog.Log(FalconFirebaseLog.SPEND_VIRTUAL_CURRENCY, "gem", 500, "booster_time");