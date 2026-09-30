# Module Translate

## Tổng quan
- **Tên:** `Module Translate`
- **Giới thiệu:** Đây là module dịch ngôn ngữ, giúp dịch một string hoặc một nhóm các Text/TextMeshProUGUI.
- **Các thành phần chính:**
    -   TranslateManager.cs, các API sẽ gọi qua đây.
    -   GoogleClientTranslateGroup.cs để quản lý một nhóm các Text/TextMeshProUGUI bạn muốn dịch.
    -   prefab TranslateButton để user tương tác, bấm vào sẽ yêu cầu dịch / hiện nội dung gốc.

## Quick Start
Yêu cầu phải có kết nối tới server và đã login thành công.
Sau khi cài module, bạn có thể:

### 1. Gọi API yêu cầu dịch một string
Dịch một string, ngôn ngữ nguồn Server sẽ tự detect, ngôn ngữ đích là ngôn ngữ máy của bạn

```csharp
TranslateManager.Translate(yourString, onTranslateCompleteAction);
```

Dịch một string. Thường sẽ dùng hàm trên thay vì hàm này.

```csharp
TranslateManager.Translate(yourString, sourceLanguage, targetLanguage, onTranslateCompleteAction);
```
### 2. Tạo một nhóm các Text/TextMeshProUGUI, sẽ tự động được dịch hoặc dịch khi người chơi bấm nút
- Thêm Component GoogleClientTranslateGroup vào 1 GameObject để quản lý.
- Kéo các Text/TextMeshProUGUI cần dịch vào component vừa thêm.
- Chọn _autoTranslate là true(sẽ tự dịch) hoặc false(sẽ tự tạo ra 1 button để người dùng bấm).
- Run và trải nghiệm.

## Chi tiết
### Cách hoạt động của module
- Khi bạn yêu cầu dịch, bạn đã gửi 1 CS lên Server để yêu cầu dịch. Sau đó server gửi SC về cho vài người bất kỳ để nhờ họ dịch hộ.
- Sau khi học dịch xong, họ lại gửi CS lên Server, và Server sẽ gửi kết quả dịch thành công đó về cho bạn.

### Thay đổi prefab TranslateButton
- Bạn không thể trực tiếp thay đổi prefab này vì nó thuộc module. Nếu muốn thay đổi, VD như thay ảnh hay gì đó, bạn làm như sau:
- Tạo prefab variant từ prefab gốc.
- Đặt variant mới tạo vào địa chỉ khác nằm ngoài module (nhưng phải trong thư mục Resources để còn load ra chứ. VD: Assets/Resources/Temp/NewTranslateButton.prefab)
- Tạo 1 class implement interface ITranslateCustom. VD:
```csharp
public class TranslateCustom : ITranslateCustom
    {
        public string ResourcesPathOfYourTranslateButton()
        {
            return "Temp/NewTranslateButton";
        }
    }
```

- Register class vừa tạo
```csharp
TranslateManager.Register(new TranslateCustom());
```

- Xong rồi, từ giờ module sẽ sinh prefab mà bạn tạo thay vì prefab gốc. Sửa gì thì sửa ở prefab của bạn nhé.

## Lỗi thường gặp
- Dịch sang ngôn ngữ mà font không hỗ trợ.
- RectTransform ban đầu để quá bé, nên dịch xong không thấy chữ đâu.
- Dịch từ ngôn ngữ A sang ngôn ngữ A :v