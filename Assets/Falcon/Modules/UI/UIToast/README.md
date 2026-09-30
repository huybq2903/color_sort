# Module @Module_UI_Toast

## Tổng quan
- **Tên:** `Module_UI_Toast`
- **Giới thiệu:** Cung cấp giải pháp để thị thông báo nhanh. Sau một một thời gian sẽ tự động ẩn đi.
- **Các thành phần chính:**
    - `UIToast`: Component xử lý bật/tắt UIToastItem
    - `UIToastItem`: Component hiển thị nội dung Toast.
    - `UIToastConfig`: File cấu hình

## Quick Start
    - Sử dụng đường dẫn editor: "Falcon/Modules/UI/Toast/Assets" để Import Assets cho phần này.
    - Bên trong thư mục Addressable/Prefabs có file UIToast
    - UIToast Cần phải kéo vào scene hoặc sinh ra tự động (Sử dụng: Instantie, Addressable,...).

## Chi tiết

### UIToast
Sử dụng EventBus Helpers để xử lý
Tên event: `falcon.modules.ui.toast_open`.
Tham số: chuỗi string, nội dung cần hiển thị.

Ví dụ:
```csharp
GameEvent<string>.Emit("falcon.modules.ui.toast_open", "Hello World !");
```

### UIToastItem
Đây là component hiển thị nội dung.

```csharp
public class UIToastItem : MonoBehaviour
{
    public TextMeshProUGUI txtContent;
    //...
}
```
Thành phần:
txtContent: hiển thị nội dung.

### Cấu hình
`UIToastConfig`
Đây là file cấu hình ScriptableObject nằm trong thư mục Resources, để có thể tùy chỉnh một số tham số được cung cấp.
Các thành phần đã được mô tả chi tiết ở bên ngoài Inspector.

## Lưu ý:
- Nên duplicate code, prefab trong module ra thư mực riêng. Không được phép chỉnh sửa trực tiếp.
- Những file mẫu trong Import Assets. Có thể sử dụng nó và kéo về thư mục riêng để tùy chỉnh. 
- Khi cập nhật module mới nhất, sẽ không ảnh hưởng tới file Import Assets. Tuy nhiên khi lấy Import Assets mới nhất
có thể sẽ bị cảnh báo ghi đè file, do trùng GUID.
- Nếu sửa code, prefab trong module có thể sẽ làm module hoạt động sai hoặc mất code.
Cần phải kế thừa hoặc tạo riêng.
