# Falcon Shop Module

## 1. Mô tả

- **Giới thiệu:**  
  Falcon Shop là module quản lý hệ thống cửa hàng (shop) cho game mobile, hỗ trợ:
  - Hiển thị shop chính (UIShop) theo nhóm pack (Special, Ads, Bundle, Currency)
  - Mini shop đơn giản (UIMiniShop) hiển thị gói ưu tiên
  - Cấu hình động từ server (ShopConfig & MiniShopConfig)
  - Hiệu ứng slide animation đẹp mắt

- **Thành phần chính:**
- `ShopConfig`: config những gói nào được hiển thị ở shop, và được đặt ở đâu
- `MiniShopConfig`: config những gói nào được hiển thị ở minishop
- `UIShop`: UI xử lý việc khởi tạo những pack nào ra shop từ config
- `UIMiniShop`: UI xử lý việc khởi tạo những pack nào ra miniShop từ config
- `ShopManager`: xử lý logic cache config, cache AssetReference nhận được.
---

## 2. Quick Start
### Bước 0 (Tùy chọn): 
Nếu dùng module `Account` thì tải thêm module `Account4EventBus` để khi Login thành công sẽ tự động gửi CSShopConfig lên
### Bước 1: Lấy file asset và config
```
Falcon > Modules > Shop > Import Asset
```
- Trong asset bao gồm:
  - UIPopupShop: prefab popup shop
  - UIShop: prefab top shop ngoài home
  - UIShopLineTitle: prefab title nhóm các pack
  - UIWrapperItem: prefab bọc ngoài các pack mỗi khi được đưa vào shop, dùng để làm animation
  - SO_FCM_Shop_Config: SO config mặc định dưới local cho shop
### Bước 2: Lên CMS cấu hình các pack hoặc cấu hình local

#### 1. Config trong `SO_FCM_Shop_Config`
- Mở file `SO_FCM_Shop_Config` (trong `Resources`).
- Sửa 2 mảng:
  - `ShopConfig.elementConfigs[]`: dùng cho Shop chính.
  - `MiniShopConfig.elementConfigs[]`: dùng cho Mini Shop.

##### Shop chính (`ShopConfig`):

| Trường        | Ý nghĩa                      |
|---------------|-------------------------------|
| idPack        | Tên duy nhất của pack         |
| type          | Nhóm (1: Bundle, 2: Currency, 3: No Ads, 4: Special) |
| priority      | Sắp xếp trong nhóm            |

##### Mini shop (`MiniShopConfig`):

| Trường        | Ý nghĩa                      |
|---------------|-------------------------------|
| idPack        | Tên duy nhất của pack         |
| position      | Vị trí (1–4)                  |
| priority      | Ưu tiên hiển thị ở vị trí     |

#### 2. Cập nhật từ Server qua CMS
- Server có thể trả về `SCShopConfig`, chứa `ShopConfig` và `MiniShopConfig` mới.
- Hệ thống sẽ gọi `ShopManager.SaveShopConfig(...)` để lưu lại và cập nhật nội dung UI tự động.

### Lưu ý: Gói phải được đăng kí qua EventBus dưới để shop cache được AssetReference.

### Bước 3:
- Kéo UIShop vào Tab menu shop tương ứng, gọi UIPopupShop nếu muốn shop popup.
---

## 3. Danh sách API

### ShopManager
- `Action onInitialize`: callback khi khởi tạo, đăng kí nếu muốn custom thêm 
- `Dictionary<int, Func<string>> dictKeyGroupTitle`: gán lại dictionary này nếu muốn thay đổi tên các group theo index hoặc localize
```csharp
ShopManager.dictKeyGroupTitle = new Dictionary<int, Func<string>>
{
   { 1, () => LocalizationManager.GetTranslation("shop.special") },
   { 2, () => LocalizationManager.GetTranslation("shop.ads") },
   { 3, () => LocalizationManager.GetTranslation("shop.bundle") },
   { 4, () => LocalizationManager.GetTranslation("shop.currency") },
};
```

### Các sự kiện EventBus:

1. Đăng kí pack:
```csharp
GameEvent<(AssetReference, string)>.Emit("falcon.modules.shop.create_pack_shop", (new AssetReference(address), idPack));
```
AssetReference là một wrapper metadata cho asset trong hệ thống Addressables, gọi `new AssetReference(string address)` sẽ trỏ tới asset có địa chỉ đó trong addressable

2. Muốn ẩn pack:
```csharp
GameEvent<string>.Emit("falcon.modules.shop.hide_pack_shop", idPack);
```
---

## 4. Chi tiết

- MiniShop hỗ trợ tối đa 4 vị trí cố định, có thể tuỳ biến `MiniShopConfig`.
- Cấu trúc hỗ trợ sử dụng `AssetReference` để load prefab động từ Addressables.

- Khi pack được tạo:
  - Load prefab từ `AssetReference`
  - Gửi `SendMessage("Setup", idPack)`: gọi hàm Setup trong pack qua Reflection, để setup UI của pack qua idPack
  - Gắn prefab vào `UIWrapperItem`
  - Hiển thị theo thứ tự group (UIShop) hoặc position (UIMiniShop)

- Giao diện có animation nếu `isAnimationSlide = true`, gọi `SetDelayByPosition()`.

---

## 5. Các lỗi thường gặp

| Lỗi | Nguyên nhân                                                                                                     | Cách khắc phục                                      |
|------|-----------------------------------------------------------------------------------------------------------------|-----------------------------------------------------|
| Không hiện pack | - ID không khớp config<br/>- Chưa thêm idPack vào config của shop<br/>- Chưa đăng kí AssetReference qua EvenBus |                            |
| UI lỗi vị trí | Position không nằm trong [1–4]                                                                                  | Kiểm tra `MiniShopConfig.position`                  |
| Prefab không có hàm Setup | Không xử lý `SendMessage("Setup")`                                                                              | Đảm bảo pack prefab có xử lý `Setup(string idPack)` |