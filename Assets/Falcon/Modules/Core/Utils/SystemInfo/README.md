# Module @SystemInformation

## Tổng quan
- **Tên:** `SystemInformation`
- **Giới thiệu:** Cung cấp cách thuận tiện để truy cập các thông tin chi tiết về thiết bị và ứng dụng trong thời gian chạy. Module được thiết kế để hoạt động trên nhiều nền tảng khác nhau được Unity hỗ trợ, bao gồm Editor, Android, iOS và WebGL. Module được tổ chức thành hai lớp tĩnh chính: `SystemInformation.Device` (thông tin về thiết bị) và `SystemInformation.App` (thông tin về ứng dụng).

## Quick Start
Để truy cập thông tin, bạn có thể sử dụng các thuộc tính tĩnh từ lớp `SystemInformation`:

```csharp
// Thông tin thiết bị
Debug.Log($"Model thiết bị: {SystemInformation.Device.DeviceModel}");
Debug.Log($"Hệ điều hành: {SystemInformation.Device.OperatingSystem}");
// ... và các thông tin khác

// Thông tin ứng dụng
Debug.Log($"Phiên bản ứng dụng: {SystemInformation.App.Version}");
Debug.Log($"Tên gói: {SystemInformation.App.PackageName}");
// ... và các thông tin khác
```

## Danh sách đầy đủ API

#### `SystemInformation.Device`

- `DeviceModel`: Model thiết bị (ví dụ: "iPhone13,2", "Pixel 5")
- `OperatingSystem`: Hệ điều hành (ví dụ: "iOS 15.1", "Android OS 12")
- `SystemLanguage`: Ngôn ngữ hệ thống (ví dụ: "English", "Vietnamese")
- `SystemLanguageISO`: Mã ngôn ngữ ISO hai chữ cái (ví dụ: "en", "vi")
- `Resolution`: Độ phân giải màn hình dạng "widthxheight"
- `DeviceMemory`: Kích thước bộ nhớ hệ thống (MB)
- `GraphicsMemorySize`: Kích thước bộ nhớ đồ họa (MB)
- `Platform`: Nền tảng hiện tại (ví dụ: "android", "ios", "editor", "webgl")
- `DeviceUUID`: Định danh thiết bị duy nhất
- `AdvertisingID`: ID quảng cáo (IDFA cho iOS, GAID cho Android)

#### `SystemInformation.App`

- `Version`: Phiên bản ứng dụng (ví dụ: "1.0.0")
- `VersionInt`: Biểu diễn số nguyên của phiên bản dùng để so sánh phiên bản.
- `PackageName`: Tên gói hoặc định danh bundle của ứng dụng
- `InstallVendor`: Tên cửa hàng hoặc nhà cung cấp nơi ứng dụng được cài đặt

#### Thông tin thư viện Android (chỉ có trên thiết bị Android)

- `NumberLibFiles`: Số lượng file thư viện native
- `TotalLibFileSize`: Tổng kích thước file thư viện native (bytes)
- `LibFileNameList`: Danh sách tên file thư viện native
- `LibFolder`: Tên thư mục thư viện native
- `LibMD5`: Hash MD5 được tạo từ tên thư mục, danh sách file và tổng kích thước


## Lỗi thường gặp

### App version không đúng định dạng
Khi app version có các ký tự đặc biệt như có chữ cái, các dấu khác thường, sẽ dẫn đến function `VersionInt` trả về giá trị không chính xác. 

Quy ước sử dụng đặt tên version theo cấu trúc [SEMVER VERSIONING](https://semver.org/). Ví dụ: `1.0.0`.