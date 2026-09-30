# Module Helper @ConfigImporter

## Tổng quan
- **Tên:** `ConfigImporter`
- **Giới thiệu:** Cung cấp một API nhất quán để các module có thể import file cấu hình `.unitypackage` của mình. Ngoài ra, nó cũng cung cấp một cửa sổ Editor để quản lý và cài đặt các gói third-party từ một CDN từ xa.
- **Tính năng chính:**
    - Import config `.unitypackage` cho các module từ local hoặc CDN.
    - Tự động gán các prefab vào Addressables sau khi import.
    - Cung cấp cửa sổ Editor để Import các gói third-party (Firebase, MAX, etc).

## Quick Start

### 1. Import cấu hình cho một Module
Để import file cấu hình cho module của bạn, hãy tạo một `MenuItem` và gọi đến hàm `ConfigImporter.ImportConfig`.
```csharp
using Falcon.Helpers.ConfigImporter.Editor;
using UnityEditor;

public class MyModuleEditor
{
    [MenuItem("Falcon/Modules/MyModule/Import Config")]
    private static void ImportMyModuleConfig()
    {
        // Đường dẫn đến module của bạn
        var modulePath = "Falcon/Modules/MyModule";
        ConfigImporter.ImportConfig(modulePath);
    }
}
```
Khi chạy, `ConfigImporter` sẽ tìm và import file `Assets/Falcon/Modules/MyModule/SetupFile/ModuleAssets.unitypackage`.

### 2. Import các gói Third-Party
Để mở cửa sổ quản lý, hãy sử dụng Editor Menu tại `Falcon > Manager > Third-Party Importer`.

Cửa sổ sẽ hiển thị danh sách các gói 3rd-Party có sẵn từ CDN để bạn có thể nhấn "Install".

## Danh sách đầy đủ API
- `ConfigImporter.ImportConfig(string modulePath)`: Bắt đầu quá trình import cho một module cụ thể.

## Chi tiết
### Phụ thuộc
- `falcon.helpers.addressable`

## Lỗi thường gặp
### Không tìm thấy file .unitypackage
Khi bạn chạy `ImportConfig`, nếu file `ModuleAssets.unitypackage` không được tìm thấy trong thư mục `SetupFile` của module, hệ thống sẽ thử tải nó từ một CDN đã được định cấu hình. Nếu cả hai đều thất bại, quá trình import sẽ dừng lại. Hãy đảm bảo file package tồn tại ở một trong hai vị trí đó. 