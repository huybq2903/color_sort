# Module Falcon Mmp

## Tổng quan
- **Tên:** `Falcon Mmp`
- **Giới thiệu:** Mmp (Mobile Measurement Partner) là nền tảng giúp đo lường, theo dõi và phân tích hiệu quả chiến dịch tiếp thị di động.
- **Các thành phần chính:**
    - Appsflyer SDK
    - Adjust SDK

## Quick Start

### 1. Cài đặt thêm SDK
- Vào menu "Falcon/Manager/ThirdParty Import", chọn cài "Appsflyer (ver 6.16.21)" hoặc "Adjust (ver 5.4.0)"
- Vào File cấu hình trên menu "Falcon/Modules/ThirdParty/Mmp settings"
- Chọn Mmp sẽ sử dụng, điền thông tin tương ứng do UA cung cấp.

### 2. Khởi tạo Falcon Mmp
- MMP sẽ tự động tạo 1 object trên scene và khởi tạo

### 3. Gửi bản tin lên Server
Các sự kiện trong game cần log
- af_complete_registration : khi đăng ký tài khoản trong game thành công (phụ thuộc tùy game) (ví dụ: Facebook, Google...)
- af_tutorial_completion : khi hoàn thành tutorial (bắt buộc)
- af_achievement_unlocked : khi đạt được mốc thành tích nào đó

```csharp
public class FalconMmpLog
{
    public const string MMP_COMPLETE_REGISTRATION = "af_complete_registration";
    public const string MMP_TUTORIAL_COMPLETION = "af_tutorial_completion";
    public const string MMP_ACHIEVEMENT_UNLOCKED = "af_achievement_unlocked";

    public const string MMP_REGISTRATION_METHOD = "af_registration_method";
    public const string MMP_SUCCESS = "af_success";
    public const string MMP_TUTORIAL_ID = "af_tutorial_id";
    public const string MMP_CONTENT_ID = "content_id";
    public static void LogEvent(string eventName, Dictionary<string, string> dictionary)
}
----------------------cách dùng----------------------
    
    /// <summary>
    /// Gửi sự kiện "af_complete_registration"
    /// khi đăng ký tài khoản trong game thành công
    /// (phụ thuộc tùy game) (ví dụ: Facebook, Google...)
    /// </summary>
    /// <param name="af_registration_method">cách đăng ký (VD : Facebook, Google ...)</param>

    var dictionary = new Dictionary<string, string>();
    dictionary.Add(FalconMmpLog.MMP_REGISTRATION_METHOD, "Facebook");
    FalconMmpLog.LogEvent(FalconMmpLog.MMP_COMPLETE_REGISTRATION, dictionary);

    
    /// <summary>
    /// Gửi sự kiện "af_tutorial_completion" khi hoàn thành tutorial (bắt buộc)
    /// </summary>
    /// <param name="af_success">trạng thái user hoàn thành tutorial (true, false)</param>
    /// <param name="af_tutorial_id">id tutorial (VD: 1, 2 ...)</param>
    /// <param name="af_registration_method">(optional param) cách đăng ký (VD : Facebook, Google ...)</param>
    
    var dictionary = new Dictionary<string, string>();
    dictionary.Add(FalconMmpLog.MMP_SUCCESS, "true");
    dictionary.Add(FalconMmpLog.MMP_TUTORIAL_ID, "2");
    FalconMmpLog.LogEvent(FalconMmpLog.MMP_TUTORIAL_COMPLETION, dictionary);

    /// <summary>
    /// Gửi sự kiện "af_achievement_unlocked" khi đạt đến mốc thành tích
    /// </summary>
    /// <param name="content_id">id thành tích đạt được</param>
    /// <param name="af_level">level của người chơi</param>
    /// <param name="af_registration_method">(optional param) cách đăng ký (VD : Facebook, Google ...)</param>
    
    var dictionary = new Dictionary<string, string>();
    dictionary.Add(FalconMmpLog.MMP_CONTENT_ID, "3");
    dictionary.Add(FalconMmpLog.MMP_LEVEL, "16");
    FalconMmpLog.LogEvent(FalconMmpLog.MMP_ACHIEVEMENT_UNLOCKED, dictionary);
```