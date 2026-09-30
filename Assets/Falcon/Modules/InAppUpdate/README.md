# Module `InAppUpdate`

## Tổng quan
- **Tên:** `InAppUpdate`

- **Giới thiệu:**  
  Module dùng để kiểm tra phiên bản ứng dụng và hiển thị popup thông báo cập nhật cho người dùng. Module hỗ trợ 2 loại cập nhật: **Force Update** (bắt buộc) và **Optional Update** (tùy chọn).

### 1. Cài đặt và cấu hình
Phần này chỉ dành cho người dùng mới. Người dùng cũ update version thì xem hướng dẫn ở file ChangeLog.md nếu có thay đổi về cấu hình và cài đặt.

- Bước 1: Trên Unity Menu chọn Falcon > Modules > InAppUpdate > Assets. Bước này tạo asset cần thiết của module.
- Bước 2: Cấu hình Remote Config với key `force_and_optional_versions` có giá trị dạng `"1.0.0;1.1.1"` (version Force Update; version Optional Update).

### 2. Gọi code
Gọi hàm để kiểm tra và hiển thị popup cập nhật nếu cần:

```csharp
// Gọi đồng bộ (không chờ kết quả). Nếu tại thời điểm gọi popup chưa load xong thì nó sẽ load nốt đến khi xong mới show.
PopupInAppUpdate.ShowUpdatePopupIfNeeded();

// Hoặc gọi bất đồng bộ (async/await). Dùng hàm này nếu muốn biết khi nào popup được load xong và show thành công.
await PopupInAppUpdate.ShowUpdatePopupIfNeededAsync();
```

## Cách hoạt động

### Kiểm tra phiên bản
1. Module lấy cấu hình từ Remote Config (`InAppUpdateConfig.force_and_optional_versions`).
2. So sánh `Application.version` với:
   - Phiên bản Force Update: Nếu nhỏ hơn → hiển thị popup Force Update (không có nút đóng).
   - Phiên bản Optional Update: Nếu nhỏ hơn → hiển thị popup Optional Update (có nút đóng).

### Hiển thị popup
- **Force Update:** 
  - Popup không có nút đóng, người dùng bắt buộc phải cập nhật.
  - Text hiển thị từ localization key: `InAppUpdate/in_app_update_force_description`
  
- **Optional Update:**
  - Popup có nút đóng, người dùng có thể bỏ qua.
  - Text hiển thị từ localization key: `InAppUpdate/in_app_update_description`

### Mở cửa hàng ứng dụng
- **iOS:** Mở App Store với App ID đã cấu hình trong prefab.
- **Android:** Mở Google Play Store với package name của ứng dụng.

## Lưu ý
- Module chỉ kiểm tra một lần mỗi lần khởi động ứng dụng.
- Cần cấu hình Remote Config trước khi gọi hàm kiểm tra.
- Cần setup localization cho 2 key: `InAppUpdate/in_app_update_force_description` và `InAppUpdate/in_app_update_description`.
- Prefab `PopupInAppUpdate` được load từ Addressable với path `"PopupInAppUpdate"`.

